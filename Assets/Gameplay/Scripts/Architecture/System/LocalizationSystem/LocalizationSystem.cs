using System;
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
        private const int MaxDepth = 8;
        private const string GlobalCatalogLocation = "localization";

        public bool IsInitialized { get; private set; }
        public ISignal<LocalizationCatalogChanged> CatalogChanged => _catalogChanged;

        private LocalizationCatalog _globalCatalog = new LocalizationCatalog(TextSourceFlags.Global);
        private LocalizationCatalog _sequenceCatalog = new LocalizationCatalog(TextSourceFlags.Sequence);
        private readonly LocalizationResolverRegistry _resolverRegistry = new LocalizationResolverRegistry();
        private readonly LocalizationResolveContext _resolveContext = new LocalizationResolveContext();
        private readonly Signal<LocalizationCatalogChanged> _catalogChanged = new Signal<LocalizationCatalogChanged>();
        private readonly ObjectPool<StringBuilder> _stringBuilderPool = new(() => new StringBuilder(), actionOnRelease: static builder => builder.Clear(), collectionCheck: true, defaultCapacity: 1, maxSize: MaxDepth);

        private IAssetService _assetService;
        private GlobalSettingModel _globalSettingModel;
        private LocalizationCompiler _compiler;
        private string _sequenceLocation = string.Empty;
        private long _sequenceRequestVersion;

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
            _globalCatalog.Clear();
            _sequenceCatalog.Clear();
            _catalogChanged.Clear();
            _stringBuilderPool.Dispose();

            _assetService = null;
            _globalSettingModel = null;
            _compiler = null;
            _sequenceLocation = string.Empty;
            ++_sequenceRequestVersion;
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
            var sequenceLocation = _sequenceLocation;
            var sequenceVersion = ++_sequenceRequestVersion;
            var globalCatalog = LoadCatalog(TextSourceFlags.Global, language, GlobalCatalogLocation);
            EnsureSequenceRequest(sequenceVersion, sequenceLocation);
            var sequenceCatalog = string.IsNullOrEmpty(sequenceLocation) ? null : LoadCatalog(TextSourceFlags.Sequence, language, sequenceLocation);
            EnsureSequenceRequest(sequenceVersion, sequenceLocation);

            _globalCatalog = globalCatalog;
            if (sequenceCatalog == null)
            {
                _sequenceCatalog.Clear();
            }
            else
            {
                _sequenceCatalog = sequenceCatalog;
            }

            EmitCatalogChanged(language, sequenceCatalog == null ? TextSourceFlags.Global : TextSourceFlags.Global | TextSourceFlags.Sequence);
        }

        public async UniTask SetSequenceTextSourceAsync(string location, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Sequence localization location cannot be null or empty.", nameof(location));
            }

            var requestVersion = ++_sequenceRequestVersion;
            _sequenceLocation = location;
            var language = _globalSettingModel.Language;

            var catalog = await LoadCatalogAsync(TextSourceFlags.Sequence, language, location, cancellationToken, null);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSequenceRequestCurrent(requestVersion, location))
            {
                return;
            }

            _sequenceCatalog = catalog;
            EmitCatalogChanged(language, TextSourceFlags.Sequence);
        }

        public void ClearSequenceTextSource()
        {
            ++_sequenceRequestVersion;
            _sequenceLocation = string.Empty;
            if (string.IsNullOrEmpty(_sequenceCatalog.Location))
            {
                return;
            }

            _sequenceCatalog.Clear();
            if (IsInitialized)
            {
                EmitCatalogChanged(_globalSettingModel.Language, TextSourceFlags.Sequence);
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
            return (catalog.Flags & allowed) == catalog.Flags && catalog.TryGet(key, out entry);
        }

        private void EnsureSequenceRequest(long version, string location)
        {
            if (!IsSequenceRequestCurrent(version, location))
            {
                throw new OperationCanceledException("Sequence localization request was replaced.");
            }
        }

        private bool IsSequenceRequestCurrent(long version, string location) => version == _sequenceRequestVersion && string.Equals(location, _sequenceLocation, StringComparison.Ordinal);

        private void EmitCatalogChanged(GameLanguage language, TextSourceFlags sources) => _catalogChanged.Emit(new LocalizationCatalogChanged(language, sources));
    }
}
