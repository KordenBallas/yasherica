using System;
using System.IO;
using Core.Logging;

namespace Core.Persistence
{
    /// <summary>
    /// <c>run.json</c> under the saves directory. Corrupt policy is Delete: losing the one
    /// in-progress run is the accepted worst case (FR13); the cross-run memory lives in its own
    /// independent store.
    /// </summary>
    public sealed class RunSaveStore : IRunSaveStore
    {
        public const string FileName = "run.json";

        private readonly JsonSaveFile _file;

        public RunSaveStore(string directory, ISaveSerializer serializer, IGameLogger logger = null)
        {
            _file = new JsonSaveFile(Path.Combine(directory, FileName), serializer,
                CorruptFilePolicy.Delete, logger);
        }

        public bool Exists() => _file.Exists();

        public bool TryLoad(out RunSaveSnapshot snapshot) =>
            _file.TryLoad(RunSaveSnapshot.CurrentVersion, out snapshot);

        public void Save(RunSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.Version = RunSaveSnapshot.CurrentVersion;
            snapshot.SavedAtUtcTicks = DateTime.UtcNow.Ticks;
            _file.TrySave(snapshot);
        }

        public void Delete() => _file.Delete();
    }
}
