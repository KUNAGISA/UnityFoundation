using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using QFSW.QC;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class ConsoleSystem : SystemBase
    {
        public bool IsInitialized => _console != null;

        private readonly HashSet<string> _autoCloseCommands = new(StringComparer.Ordinal);
        private bool _autoCloseCommandsLoaded;

        private InputModeToken _inputModeToken;

        private IAssetAsyncOperation<GameObject> _consoleOperation = null;
        private QuantumConsole _console = null;
        private IAssetService _assetService;
        private IPersistentSceneService _persistentSceneService;

        protected override void OnInitialize()
        {
            _assetService = this.GetService<IAssetService>();
            _persistentSceneService = this.GetService<IPersistentSceneService>();
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (IsInitialized)
            {
                progress?.Report(1f);
                return;
            }

            try
            {
                progress?.Report(0f);
                _consoleOperation = _assetService.LoadAssetAsync<GameObject>("Global/Console");
                var prefab = await _consoleOperation.ToUniTask(progress, cancellationToken: cancellationToken, cancelImmediately: true);
                InitializeConsole(prefab);
                progress?.Report(1f);
            }
            catch
            {
                ReleaseFailedInitialization();
                throw;
            }
        }

        private void InitializeConsole(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException("Console asset [Global/Console] returned null.");
            }

            var instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            try
            {
                instance.name = "[Console]";
                instance.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
                _persistentSceneService.MoveToPersistent(instance);

#if !GAMEPLAY_GM
                var inputs = instance.GetComponentsInChildren<TMP_InputField>(true);
                for (var index = 0; index < inputs.Length; ++index)
                {
                    inputs[index].readOnly = true;
                }
#endif

                _console = instance.GetComponent<QuantumConsole>();
                if (_console == null)
                {
                    throw new InvalidOperationException("Console asset [Global/Console] has no QuantumConsole component.");
                }
                _console.OnActivate += OnConsoleActivate;
                _console.OnDeactivate += OnConsoleDeactivate;
                _console.OnInvoke += OnConsoleInvoke;
            }
            catch
            {
                Object.Destroy(instance);
                _console = null;
                throw;
            }
        }

        protected override void OnShutdown()
        {
            if (_console)
            {
                _console.OnActivate -= OnConsoleActivate;
                _console.OnDeactivate -= OnConsoleDeactivate;
                _console.OnInvoke -= OnConsoleInvoke;
                Object.Destroy(_console.gameObject);
            }
            _console = null;

            _consoleOperation?.Release();
            _consoleOperation = null;

            _inputModeToken.Cancel();
            _inputModeToken = default;

            _autoCloseCommands.Clear();
            _autoCloseCommandsLoaded = false;
            _assetService = null;
            _persistentSceneService = null;
        }

        private void OnConsoleActivate()
        {
            _inputModeToken.Cancel();
            _inputModeToken = this.GetService<IInputService>().PushMode(InputModeCatalog.Console);
        }

        private void OnConsoleDeactivate()
        {
            _inputModeToken.Cancel();
            _inputModeToken = default;
        }

        private void OnConsoleInvoke(string input)
        {
            LoadAutoCloseCommands();

            var span = input.AsSpan();
            var space = span.IndexOf(' ');
            var head = space >= 0 ? span[..space] : span;

            foreach (var cmd in _autoCloseCommands)
            {
                if (head.SequenceEqual(cmd))
                {
                    _console.Deactivate();
                    break;
                }
            }
        }

        private void LoadAutoCloseCommands()
        {
            if (_autoCloseCommandsLoaded || !QuantumConsoleProcessor.TableGenerated)
            {
                return;
            }

            foreach (var command in QuantumConsoleProcessor.GetAllCommands())
            {
                if (command.MethodData.IsDefined(typeof(CloseConsoleAfterInvokeAttribute), false))
                {
                    _autoCloseCommands.Add(command.CommandName);
                }
            }

            _autoCloseCommandsLoaded = true;
        }

        private void ReleaseFailedInitialization()
        {
            _consoleOperation?.Release();
            _consoleOperation = null;
        }
    }
}
