using System.IO;
using Core.Logging;

namespace Core.Persistence
{
    /// <summary>
    /// <c>hub-arrival.json</c> under the saves directory. Corrupt policy is Delete: the marker is
    /// pure arrival presentation, so the worst case of losing it is one missed voice line.
    /// </summary>
    public sealed class HubArrivalStore : IHubArrivalStore
    {
        public const string FileName = "hub-arrival.json";

        private readonly JsonSaveFile _file;

        public HubArrivalStore(string directory, ISaveSerializer serializer, IGameLogger logger = null)
        {
            _file = new JsonSaveFile(Path.Combine(directory, FileName), serializer,
                CorruptFilePolicy.Delete, logger);
        }

        public void MarkDeathReturn()
        {
            _file.TrySave(new HubArrivalSnapshot
            {
                Version = HubArrivalSnapshot.CurrentVersion,
                DeathReturn = true
            });
        }

        public bool TryConsumeDeathReturn()
        {
            bool marked = _file.TryLoad(HubArrivalSnapshot.CurrentVersion, out HubArrivalSnapshot snapshot)
                          && snapshot.DeathReturn;
            _file.Delete();
            return marked;
        }
    }
}
