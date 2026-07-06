using System.IO;
using Core.Logging;

namespace Core.Persistence
{
    /// <summary>
    /// <c>meta.json</c> under the saves directory. Corrupt policy is Quarantine (renamed to
    /// <c>meta.json.corrupt</c>): the world's accumulated memory is evidence worth keeping even
    /// when unreadable, and the game continues on an empty memory (FR13).
    /// </summary>
    public sealed class MetaMemoryStore : IMetaMemoryStore
    {
        public const string FileName = "meta.json";

        private readonly JsonSaveFile _file;

        public MetaMemoryStore(string directory, ISaveSerializer serializer, IGameLogger logger = null)
        {
            _file = new JsonSaveFile(Path.Combine(directory, FileName), serializer,
                CorruptFilePolicy.Quarantine, logger);
        }

        public MetaMemorySnapshot LoadOrEmpty() =>
            _file.TryLoad(MetaMemorySnapshot.CurrentVersion, out MetaMemorySnapshot snapshot)
                ? snapshot
                : new MetaMemorySnapshot();

        public void Save(MetaMemorySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.Version = MetaMemorySnapshot.CurrentVersion;
            _file.TrySave(snapshot);
        }
    }
}
