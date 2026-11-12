using ChatUp.Application.Common.Security;
using ChatUp.Infrastructure.Services.Cryptography;

namespace ChatUp.Services.Security
{
    public enum UiKeySource
    {
        /// <summary>A key connected from Settings → API keys, stored encrypted on this server.</summary>
        Connected,
        /// <summary>"ApiKeys:Default" in C:\app\chatup\appconfig.json.</summary>
        Configured,
        /// <summary>The publicly known fallback compiled into the app.</summary>
        BuiltIn
    }

    /// <summary>
    /// Holds the API key this UI sends with every API call (singleton — shared by all users).
    /// Order of preference: the connected key file, then the configured default, then the built-in fallback.
    /// The connected key is kept encrypted in C:\app\chatup\chatup-ui.apikey, next to appconfig.json.
    /// </summary>
    public sealed class UiApiKeyStore
    {
        public const string ConfigFolder = @"C:\app\chatup";
        public static readonly string FilePath = Path.Combine(ConfigFolder, "chatup-ui.apikey");

        private readonly object _gate = new();
        private readonly Cryptography _crypto;
        private readonly IConfigurationRoot _config;
        private readonly ILogger<UiApiKeyStore> _logger;

        private string _key = ApiKeyDefaults.BuiltInDefaultKey;

        public UiApiKeyStore(ILogger<UiApiKeyStore> logger)
        {
            _logger = logger;

            // Same shared config the API reads, so both sides agree on EncryptionKey and ApiKeys:Default
            _config = new ConfigurationBuilder()
                .SetBasePath(ConfigFolder)
                .AddJsonFile("appconfig.json", optional: true, reloadOnChange: true)
                .Build();
            _crypto = new Cryptography(_config);

            Load();
        }

        public string CurrentKey
        {
            get { lock (_gate) return _key; }
        }

        public UiKeySource Source { get; private set; }

        /// <summary>Set when a connected key file exists but couldn't be read.</summary>
        public string? LoadProblem { get; private set; }

        public string CurrentKeyPreview => ApiKeyDefaults.Mask(CurrentKey);

        /// <summary>Raised after the key changes (e.g. so hub connections can reconnect).</summary>
        public event Action? Changed;

        /// <summary>Uses <paramref name="key"/> for this UI from now on and saves it encrypted.</summary>
        public void Connect(string key)
        {
            key = key.Trim();
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key is empty.", nameof(key));

            Directory.CreateDirectory(ConfigFolder);
            File.WriteAllText(FilePath, _crypto.Encrypt(key));

            lock (_gate)
            {
                _key = key;
                Source = UiKeySource.Connected;
                LoadProblem = null;
            }

            _logger.LogInformation("UI connected to API key {Preview}", ApiKeyDefaults.Mask(key));
            Changed?.Invoke();
        }

        /// <summary>Forgets the connected key and falls back to the configured or built-in default.</summary>
        public void Disconnect()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);

            lock (_gate)
            {
                _key = DefaultKey(out var source);
                Source = source;
                LoadProblem = null;
            }

            Changed?.Invoke();
        }

        private void Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    var stored = _crypto.Decrypt(File.ReadAllText(FilePath).Trim());
                    if (!string.IsNullOrWhiteSpace(stored))
                    {
                        _key = stored;
                        Source = UiKeySource.Connected;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LoadProblem = "The saved key file couldn't be decrypted (was EncryptionKey changed?). Using the default key instead.";
                    _logger.LogError(ex, "Could not read {File}", FilePath);
                }
            }

            _key = DefaultKey(out var source);
            Source = source;
        }

        private string DefaultKey(out UiKeySource source)
        {
            var configured = _config[ApiKeyDefaults.DefaultKeyConfigPath];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                source = UiKeySource.Configured;
                return configured.Trim();
            }

            source = UiKeySource.BuiltIn;
            return ApiKeyDefaults.BuiltInDefaultKey;
        }
    }
}
