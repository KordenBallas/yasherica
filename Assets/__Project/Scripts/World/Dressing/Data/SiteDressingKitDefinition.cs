using System.Collections.Generic;
using UnityEngine;

namespace World.Dressing.Data
{
    /// <summary>
    /// A site-dressing kit (site-camp-dressing brief FR1–FR3): the role lists the site-dressing
    /// design expects — structures (the skyline), small props, the focal cluster (the camp fire),
    /// gate/threshold pieces — plus a shared ground overlay. Matched to platforms by
    /// <c>SiteStamp.DressingThemeId</c>; a kit with structures dresses as a settlement, one with
    /// only a focal + props as a camp. Configuration data only, no logic.
    /// </summary>
    [CreateAssetMenu(fileName = "SiteDressingKit", menuName = "World/Dressing/Site Dressing Kit")]
    public class SiteDressingKitDefinition : DressingKitDefinition
    {
        [Tooltip("Matched against SiteDefinition._dressingThemeId (e.g. settlement-kit, camp-kit)")]
        [SerializeField] private string _dressingThemeId = string.Empty;

        [Header("Role lists (order is the planner's stable index — append, don't reorder)")]
        [Tooltip("Skyline structures (houses); blocking, whole-cell")]
        [SerializeField] private List<GameObject> _structures = new List<GameObject>();
        [Tooltip("Small props (sacks / barrels / crates); decorative, may share cells")]
        [SerializeField] private List<GameObject> _props = new List<GameObject>();
        [Tooltip("The focal cluster (the camp fire), stacked on one blocking cell")]
        [SerializeField] private List<GameObject> _focalProps = new List<GameObject>();
        [Tooltip("Threshold pieces placed on the block's anchor platform approach edge")]
        [SerializeField] private List<GameObject> _gateProps = new List<GameObject>();

        [Header("Ground")]
        [Tooltip("Shared site ground overlay (street / packed dirt); overrides the biome ground on block platforms")]
        [SerializeField] private Material _groundOverlayMaterial;

        public string DressingThemeId => _dressingThemeId;
        public IReadOnlyList<GameObject> Structures => _structures;
        public IReadOnlyList<GameObject> Props => _props;
        public IReadOnlyList<GameObject> FocalProps => _focalProps;
        public IReadOnlyList<GameObject> GateProps => _gateProps;
        public Material GroundOverlayMaterial => _groundOverlayMaterial;
    }
}
