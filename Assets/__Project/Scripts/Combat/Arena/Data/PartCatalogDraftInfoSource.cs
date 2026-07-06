using CharacterSystem.Data;
using Combat.Arena.Core;

namespace Combat.Arena.Data
{
    /// <summary>
    /// Production <see cref="IArenaDraftPartInfoSource"/>: resolves a catalog part id to its
    /// draft info off <see cref="IPartCatalog"/>. Frame-changing parts are not draftable (the
    /// first draft stays on the base body-plan — P4-5 out-of-scope).
    /// </summary>
    public class PartCatalogDraftInfoSource : IArenaDraftPartInfoSource
    {
        private readonly IPartCatalog _partCatalog;

        public PartCatalogDraftInfoSource(IPartCatalog partCatalog)
        {
            _partCatalog = partCatalog;
        }

        public bool TryGet(string partId, out ArenaDraftPartInfo info)
        {
            info = null;
            if (!_partCatalog.TryGet(partId, out var part) || part.GovernsBodyPlan || part.Slot == null)
            {
                return false;
            }

            info = new ArenaDraftPartInfo(part.Id, part.Slot.Id);
            return true;
        }
    }
}
