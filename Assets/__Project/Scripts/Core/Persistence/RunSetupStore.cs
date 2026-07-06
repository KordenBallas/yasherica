using System.IO;
using Core.Logging;

namespace Core.Persistence
{
    /// <summary>
    /// <c>run-setup.json</c> under the saves directory. Corrupt policy is Delete: a lost launch
    /// setup only ever belonged to a run that does not exist yet — the Area boot falls back to
    /// defaults and the Hub re-picks next time.
    /// </summary>
    public sealed class RunSetupStore : IRunSetupStore
    {
        public const string FileName = "run-setup.json";

        private readonly JsonSaveFile _file;

        public RunSetupStore(string directory, ISaveSerializer serializer, IGameLogger logger = null)
        {
            _file = new JsonSaveFile(Path.Combine(directory, FileName), serializer,
                CorruptFilePolicy.Delete, logger);
        }

        public bool TryLoad(out RunSetupSnapshot snapshot) =>
            _file.TryLoad(RunSetupSnapshot.CurrentVersion, out snapshot);

        public void Save(RunSetupSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.Version = RunSetupSnapshot.CurrentVersion;
            _file.TrySave(snapshot);
        }

        public void Delete() => _file.Delete();
    }
}
