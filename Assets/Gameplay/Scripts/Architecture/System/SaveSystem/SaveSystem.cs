using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Storage;
using UnityEngine;

namespace Gameplay
{
    public sealed class SaveSystem : SystemBase
    {
        private readonly object _operationLock = new object();

        private IStorageService _storageService;
        private CancellationTokenSource _lifetimeCancellation;
        private CancellationTokenSource _activeCancellation;
        private bool _isShutdown;

        protected override void OnInitialize()
        {
            _storageService = this.GetService<IStorageService>();
            _lifetimeCancellation = new CancellationTokenSource();
            _isShutdown = false;
        }

        protected override void OnShutdown()
        {
            CancellationTokenSource activeCancellation;
            lock (_operationLock)
            {
                _isShutdown = true;
                activeCancellation = _activeCancellation;
                _activeCancellation = null;
            }
            CancelOperation(activeCancellation);
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
            _storageService = null;
        }

        public async UniTask<SaveResult> SaveAsync(int slot, CancellationToken cancellationToken = default)
        {
            var relativePath = GetSlotPath(slot);
            var operationCancellation = BeginOperation(cancellationToken);
            try
            {
                var saveData = this.ExecuteQuery<CaptureGameSaveDataQuery, GameSaveData>(new CaptureGameSaveDataQuery());
                var bytes = await UniTask.RunOnThreadPool(() => SaveFileCodec.Encode(saveData), cancellationToken: operationCancellation.Token);
                await _storageService.WriteBytesAsync(relativePath, bytes, StorageWriteMode.Atomic, operationCancellation.Token);
                operationCancellation.Token.ThrowIfCancellationRequested();
                if (!IsCurrent(operationCancellation))
                {
                    return SaveResult.FromCancellation();
                }
                return SaveResult.Succeeded();
            }
            catch (Exception) when (operationCancellation.IsCancellationRequested)
            {
                return SaveResult.FromCancellation();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return SaveResult.Failed(exception.Message);
            }
            finally
            {
                EndOperation(operationCancellation);
            }
        }

        public async UniTask<LoadResult> LoadAsync(int slot, CancellationToken cancellationToken = default)
        {
            var relativePath = GetSlotPath(slot);
            var operationCancellation = BeginOperation(cancellationToken);
            try
            {
                if (!_storageService.Exists(relativePath))
                {
                    return LoadResult.Failed("Save file does not exist.");
                }
                var bytes = await _storageService.ReadBytesAsync(relativePath, operationCancellation.Token);
                var saveData = await UniTask.RunOnThreadPool(() => SaveFileCodec.Decode(bytes), cancellationToken: operationCancellation.Token);
                await UniTask.SwitchToMainThread(operationCancellation.Token);
                if (!IsCurrent(operationCancellation))
                {
                    return LoadResult.FromCancellation();
                }
                var checkResult = this.ExecuteQuery<CheckGameSaveDataQuery, GameSaveDataCheckResult>(new CheckGameSaveDataQuery(saveData));
                if (checkResult != GameSaveDataCheckResult.Success)
                {
                    return LoadResult.Failed(GetCheckError(checkResult, saveData));
                }
                return LoadResult.Succeeded(saveData);
            }
            catch (Exception) when (operationCancellation.IsCancellationRequested)
            {
                return LoadResult.FromCancellation();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return LoadResult.Failed(exception.Message);
            }
            finally
            {
                EndOperation(operationCancellation);
            }
        }

        public SaveSlotSummary GetSlotSummary(int slot)
        {
            var relativePath = GetSlotPath(slot);
            if (!_storageService.Exists(relativePath))
            {
                return SaveSlotSummary.Empty();
            }

            try
            {
                var bytes = _storageService.ReadBytesRange(relativePath, 0, SaveFileCodec.HeaderSize);
                if (!SaveFileCodec.TryReadSummary(bytes, out var sceneId, out var saveTimeUtcTicks))
                {
                    return SaveSlotSummary.Invalid();
                }
                return SaveSlotSummary.Available(sceneId, saveTimeUtcTicks);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return SaveSlotSummary.Invalid();
            }
        }

        private static string GetSlotPath(int slot)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Save slot cannot be negative.");
            }
            return $"save{slot}.sav";
        }

        private static string GetCheckError(GameSaveDataCheckResult result, GameSaveData saveData)
        {
            switch (result)
            {
                case GameSaveDataCheckResult.EmptyData:
                    return "Save data is empty.";
                case GameSaveDataCheckResult.MissingPlayer:
                    return "Player character save data is empty.";
                case GameSaveDataCheckResult.InvalidPlayerId:
                    return $"Player character id is invalid. CharacterId: {saveData?.PlayerCharacter?.Id}";
                case GameSaveDataCheckResult.MissingPlayerSpatial:
                    return "Player spatial save data is empty.";
                case GameSaveDataCheckResult.MissingPlayerRoute:
                    return "Player route id is empty.";
                case GameSaveDataCheckResult.InvalidScene:
                    return $"Save scene is invalid. SceneId: {saveData?.SceneId}";
                default:
                    return $"Save data check failed. Result: {result}";
            }
        }

        private CancellationTokenSource BeginOperation(CancellationToken externalToken)
        {
            CancellationTokenSource previousCancellation;
            CancellationTokenSource operationCancellation;
            lock (_operationLock)
            {
                if (_isShutdown || _lifetimeCancellation == null)
                {
                    throw new InvalidOperationException("SaveSystem has been shut down.");
                }
                previousCancellation = _activeCancellation;
                operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken, _lifetimeCancellation.Token);
                _activeCancellation = operationCancellation;
            }
            CancelOperation(previousCancellation);
            return operationCancellation;
        }

        private bool IsCurrent(CancellationTokenSource operationCancellation)
        {
            lock (_operationLock)
            {
                return !_isShutdown && ReferenceEquals(_activeCancellation, operationCancellation);
            }
        }

        private void EndOperation(CancellationTokenSource operationCancellation)
        {
            lock (_operationLock)
            {
                if (ReferenceEquals(_activeCancellation, operationCancellation))
                {
                    _activeCancellation = null;
                }
            }
            operationCancellation.Dispose();
        }

        private static void CancelOperation(CancellationTokenSource operationCancellation)
        {
            if (operationCancellation == null)
            {
                return;
            }
            try
            {
                operationCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
