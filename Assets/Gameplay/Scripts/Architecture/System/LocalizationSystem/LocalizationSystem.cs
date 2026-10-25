using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Localizations;
using Luban;
using UnityEngine;
using UnityEngine.Pool;

namespace Gameplay
{
    public class LocalizationSystem : SystemBase
    {
        private sealed class SequenceTextCacheEntry
        {
            public LocalizationCatalog Catalog;
            public readonly UniTaskCompletionSource CompletionSource = new UniTaskCompletionSource();
        }

        private const int MaxDepth = 8;
        private const string GlobalCatalogLocation = "localization";

        public bool IsInitialized { get; private set; }
        public ISignal<LocalizationCatalogChanged> CatalogChanged => _catalogChanged;

        private LocalizationCatalog _globalCatalog = new LocalizationCatalog(TextSourceFlags.Global);
        private LocalizationCatalog _sequenceCatalog;
        private readonly Dictionary<string, SequenceTextCacheEntry> _sequenceTextCache = new Dictionary<string, SequenceTextCacheEntry>();
        private readonly LocalizationResolverRegistry _resolverRegistry = new LocalizationResolverRegistry();
        private readonly LocalizationResolveContext _resolveContext = new LocalizationResolveContext();
        private readonly Signal<LocalizationCatalogChanged> _catalogChanged = new Signal<LocalizationCatalogChanged>();
        private readonly ObjectPool<StringBuilder> _stringBuilderPool = new(() => new StringBuilder(), actionOnRelease: static builder => builder.Clear(), collectionCheck: true, defaultCapacity: 1, maxSize: MaxDepth);

        private IAssetService _assetService;
        private GlobalSettingModel _globalSettingModel;
        private LocalizationCompiler _compiler;
        private CancellationTokenSource _sequenceCacheLifetime = new CancellationTokenSource();
        private long _sequenceTextSourceGeneration;
        private long _currentSequenceTextSourceId;

        protected override void OnInitialize()
        {
            BindContext(_resolveContext);
            _assetService = this.GetService<IAssetService>();
            _globalSettingModel = this.GetModel<GlobalSettingModel>();

            _resolverRegistry.Register("item", new ItemNameLocalizationResolver());
            _compiler = new LocalizationCompiler(_resolverRegistry);
        }

        protected override void OnShutdown()
        {
            _catalogChanged.Clear();
            ClearSequenceTextCache(false);
            _globalCatalog.Clear();
            _stringBuilderPool.Dispose();

            _assetService = null;
            _globalSettingModel = null;
            _compiler = null;
            IsInitialized = false;
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
                var language = _globalSettingModel.Language;
                _globalCatalog = await LoadCatalogAsync(TextSourceFlags.Global, language, GlobalCatalogLocation, cancellationToken, progress);
                IsInitialized = true;
                progress?.Report(1f);
                EmitCatalogChanged(language, TextSourceFlags.Global);
            }
            catch
            {
                _globalCatalog.Clear();
                IsInitialized = false;
                throw;
            }
        }

        public void Reload()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{nameof(LocalizationSystem)} is not initialized.");
            }

            var language = _globalSettingModel.Language;
            var globalCatalog = LoadCatalog(TextSourceFlags.Global, language, GlobalCatalogLocation);
            var sequenceCatalogs = new Dictionary<string, LocalizationCatalog>(_sequenceTextCache.Count);
            foreach (var location in _sequenceTextCache.Keys)
            {
                sequenceCatalogs.Add(location, LoadCatalog(TextSourceFlags.Sequence, language, location));
            }

            LocalizationCatalog sequenceCatalog = null;
            if (_sequenceCatalog != null && !sequenceCatalogs.TryGetValue(_sequenceCatalog.Location, out sequenceCatalog))
            {
                sequenceCatalog = LoadCatalog(TextSourceFlags.Sequence, language, _sequenceCatalog.Location);
            }

            _globalCatalog = globalCatalog;
            foreach (var (location, catalog) in sequenceCatalogs)
            {
                _sequenceTextCache[location].Catalog = catalog;
            }
            _sequenceCatalog = sequenceCatalog;
            EmitCatalogChanged(language, sequenceCatalog == null ? TextSourceFlags.Global : TextSourceFlags.Global | TextSourceFlags.Sequence);
        }

        public async UniTask PreloadSequenceTextAsync(string location, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Sequence localization location cannot be null or empty.", nameof(location));
            }

            if (!_sequenceTextCache.TryGetValue(location, out var entry))
            {
                entry = new SequenceTextCacheEntry();
                _sequenceTextCache.Add(location, entry);
                PreloadSequenceTextCatalogAsync(entry, location, _sequenceCacheLifetime.Token).Forget();
            }

            // 调用方只取消自身等待，共享加载随场景缓存一起清理。
            await entry.CompletionSource.Task.AttachExternalCancellation(cancellationToken);
        }

        public SequenceTextSourceToken ActivateSequenceTextSource(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Sequence localization location cannot be null or empty.", nameof(location));
            }

            // 播放入口保证预加载已经完成；未预加载的剧情同步读取且不写入缓存。
            var catalog = _sequenceTextCache.TryGetValue(location, out var entry) ? entry.Catalog : LoadCatalog(TextSourceFlags.Sequence, _globalSettingModel.Language, location);
            _sequenceCatalog = catalog;
            _currentSequenceTextSourceId = ++_sequenceTextSourceGeneration;
            var token = new SequenceTextSourceToken(this, _currentSequenceTextSourceId);
            EmitCatalogChanged(_globalSettingModel.Language, TextSourceFlags.Sequence);
            return token;
        }

        public void ClearSequenceTextCache()
        {
            ClearSequenceTextCache(true);
        }

        internal bool IsSequenceTextSourceActive(long activationId)
        {
            return activationId != 0 && activationId == _currentSequenceTextSourceId;
        }

        internal void CancelSequenceTextSource(long activationId)
        {
            if (!IsSequenceTextSourceActive(activationId))
            {
                return;
            }

            _currentSequenceTextSourceId = 0;
            _sequenceCatalog = null;
            EmitCatalogChanged(_globalSettingModel.Language, TextSourceFlags.Sequence);
        }

        private void ClearSequenceTextCache(bool recreateLifetime)
        {
            _sequenceCacheLifetime.Cancel();
            _sequenceCacheLifetime.Dispose();
            _sequenceCacheLifetime = recreateLifetime ? new CancellationTokenSource() : null;
            _sequenceTextCache.Clear();
            CancelSequenceTextSource(_currentSequenceTextSourceId);
        }

        private async UniTask PreloadSequenceTextCatalogAsync(SequenceTextCacheEntry entry, string location, CancellationToken cancellationToken)
        {
            try
            {
                var catalog = await LoadCatalogAsync(TextSourceFlags.Sequence, _globalSettingModel.Language, location, cancellationToken, null);
                cancellationToken.ThrowIfCancellationRequested();
                entry.Catalog = catalog;
                entry.CompletionSource.TrySetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RemoveSequenceTextCacheEntry(location, entry);
                entry.CompletionSource.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                RemoveSequenceTextCacheEntry(location, entry);
                entry.CompletionSource.TrySetException(exception);
            }
        }

        private void RemoveSequenceTextCacheEntry(string location, SequenceTextCacheEntry entry)
        {
            if (_sequenceTextCache.TryGetValue(location, out var current) && ReferenceEquals(current, entry))
            {
                _sequenceTextCache.Remove(location);
            }
        }

        public string Get(string key, TextSourceFlags allowed = TextSourceFlags.Global)
        {
            if (!TryGetEntry(key, allowed, out var entry))
            {
                return key;
            }

            var tokens = entry.GetTokens(_compiler);
            if (tokens == null)
            {
                return entry.Text;
            }

            var builder = _stringBuilderPool.Get();
            try
            {
                ResolveTokens(builder, tokens, allowed, 0);
                return builder.ToString();
            }
            finally
            {
                _stringBuilderPool.Release(builder);
            }
        }

        private LocalizationCatalog LoadCatalog(TextSourceFlags flags, GameLanguage language, string location)
        {
            var path = $"Localizations/{language.GetCode()}/{location}";
            var operation = _assetService.LoadAssetSync<TextAsset>(path);
            try
            {
                if (operation.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to load localization catalog [{path}].", operation.OperationException);
                }

                var textAsset = operation.Result;
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Localization catalog [{path}] returned null.");
                }

                var catalog = new LocalizationCatalog(flags);
                catalog.Load(location, new ByteBuf(textAsset.bytes));
                return catalog;
            }
            finally
            {
                operation.Release();
            }
        }

        private async UniTask<LocalizationCatalog> LoadCatalogAsync(TextSourceFlags flags, GameLanguage language, string location, CancellationToken cancellationToken, IProgress<float> progress)
        {
            var path = $"Localizations/{language.GetCode()}/{location}";
            var operation = _assetService.LoadAssetAsync<TextAsset>(path);
            try
            {
                var textAsset = await operation.ToUniTask(progress, cancellationToken: cancellationToken, cancelImmediately: true);
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Localization catalog [{path}] returned null.");
                }

                var catalog = new LocalizationCatalog(flags);
                catalog.Load(location, new ByteBuf(textAsset.bytes));
                return catalog;
            }
            finally
            {
                operation.Release();
            }
        }

        private void ResolveTokens(StringBuilder builder, LocalizationToken[] tokens, TextSourceFlags allowed, int depth)
        {
            for (var index = 0; index < tokens.Length; index++)
            {
                var token = tokens[index];
                switch (token.Kind)
                {
                    case LocalizationTokenKind.Literal:
                    case LocalizationTokenKind.FormatPlaceholder:
                        token.Text.AppendTo(builder);
                        break;

                    case LocalizationTokenKind.TextReference:
                        if (TryGetEntry(token.Key, allowed, out var entry))
                        {
                            var entryTokens = entry.GetTokens(_compiler);
                            if (entryTokens == null)
                            {
                                builder.Append(entry.Text);
                            }
                            else if (depth + 1 >= MaxDepth)
                            {
                                builder.Append('{').Append(token.Key).Append('}');
                            }
                            else
                            {
                                ResolveTokens(builder, entryTokens, allowed, depth + 1);
                            }
                        }
                        else
                        {
                            builder.Append('{').Append(token.Key).Append('}');
                        }
                        break;

                    case LocalizationTokenKind.Function:
                        if (_resolverRegistry.TryResolve(token.ResolverId, token.Argument, _resolveContext, out var result))
                        {
                            builder.Append(result);
                        }
                        else
                        {
                            token.Text.AppendTo(builder);
                        }
                        break;
                }
            }
        }

        private bool TryGetEntry(string key, TextSourceFlags allowed, out LocalizationEntry entry)
        {
            if (TryGetEntry(_sequenceCatalog, key, allowed, out entry))
            {
                return true;
            }

            return TryGetEntry(_globalCatalog, key, allowed, out entry);
        }

        private static bool TryGetEntry(LocalizationCatalog catalog, string key, TextSourceFlags allowed, out LocalizationEntry entry)
        {
            entry = null;
            return catalog != null && (catalog.Flags & allowed) == catalog.Flags && catalog.TryGet(key, out entry);
        }

        private void EmitCatalogChanged(GameLanguage language, TextSourceFlags sources) => _catalogChanged.Emit(new LocalizationCatalogChanged(language, sources));
    }
}
