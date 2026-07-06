using System;
using System.IO;
using Core.Logging;

namespace Core.Persistence
{
    /// <summary>What to do with a save file that failed to load (missing files are never "corrupt").</summary>
    public enum CorruptFilePolicy
    {
        /// <summary>Delete the bad file — right for the consumable run save (a lost run is the accepted worst case, FR13).</summary>
        Delete = 0,

        /// <summary>Rename the bad file to <c>*.corrupt</c> — right for the cross-run memory: long-term
        /// progress evidence is quarantined for post-mortem, never silently destroyed.</summary>
        Quarantine = 1
    }

    /// <summary>
    /// Versioned, fail-safe JSON file gateway (FR13/FR14): every read is parse-or-discard (a corrupt
    /// or version-mismatched file is handled per <see cref="CorruptFilePolicy"/> and reported as
    /// absent), and every write goes through a temp file + atomic replace so a crash mid-write can
    /// never destroy the previous good file. Pure C# over an injected absolute path — the Unity side
    /// (persistentDataPath, JsonUtility) enters only through the constructor arguments.
    /// </summary>
    public sealed class JsonSaveFile
    {
        private const string TempSuffix = ".tmp";
        private const string QuarantineSuffix = ".corrupt";

        private readonly string _path;
        private readonly ISaveSerializer _serializer;
        private readonly CorruptFilePolicy _corruptPolicy;
        private readonly IGameLogger _logger;

        public JsonSaveFile(string path, ISaveSerializer serializer, CorruptFilePolicy corruptPolicy,
            IGameLogger logger = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _corruptPolicy = corruptPolicy;
            _logger = logger;
        }

        public bool Exists() => File.Exists(_path);

        /// <summary>
        /// Loads and validates the file. False for a missing file (silently) and for a corrupt or
        /// version-mismatched one (after applying the corrupt policy) — the caller only ever sees
        /// "there is a valid save" or "there is not".
        /// </summary>
        public bool TryLoad<T>(int expectedVersion, out T data) where T : class, IVersionedSnapshot
        {
            data = null;
            if (!File.Exists(_path))
            {
                return false;
            }

            try
            {
                var parsed = _serializer.FromJson<T>(File.ReadAllText(_path));
                if (parsed == null)
                {
                    HandleCorrupt("parsed to null");
                    return false;
                }

                if (parsed.Version != expectedVersion)
                {
                    HandleCorrupt($"version {parsed.Version}, expected {expectedVersion}");
                    return false;
                }

                data = parsed;
                return true;
            }
            catch (Exception e)
            {
                HandleCorrupt(e.Message);
                return false;
            }
        }

        /// <summary>Writes via temp file + atomic swap. Returns false (logged) on IO failure — a
        /// failed autosave must never crash the game.</summary>
        public bool TrySave<T>(T data) where T : class, IVersionedSnapshot
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var temp = _path + TempSuffix;
                File.WriteAllText(temp, _serializer.ToJson(data));
                if (File.Exists(_path))
                {
                    File.Replace(temp, _path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temp, _path);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger?.Error(LogCategory.Persistence, $"[JsonSaveFile] Save failed for '{_path}': {e.Message}");
                return false;
            }
        }

        public void Delete()
        {
            TryDeleteFile(_path);
            TryDeleteFile(_path + TempSuffix);
        }

        private void HandleCorrupt(string reason)
        {
            try
            {
                if (_corruptPolicy == CorruptFilePolicy.Quarantine)
                {
                    var quarantine = _path + QuarantineSuffix;
                    TryDeleteFile(quarantine);
                    File.Move(_path, quarantine);
                    _logger?.Warning(LogCategory.Persistence,
                        $"[JsonSaveFile] Unreadable save '{_path}' ({reason}); quarantined to '{quarantine}'.");
                }
                else
                {
                    File.Delete(_path);
                    _logger?.Warning(LogCategory.Persistence,
                        $"[JsonSaveFile] Unreadable save '{_path}' ({reason}); discarded.");
                }
            }
            catch (Exception e)
            {
                _logger?.Error(LogCategory.Persistence,
                    $"[JsonSaveFile] Failed to dispose of corrupt save '{_path}': {e.Message}");
            }
        }

        private void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                _logger?.Error(LogCategory.Persistence, $"[JsonSaveFile] Failed to delete '{path}': {e.Message}");
            }
        }
    }
}
