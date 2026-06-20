using System.Collections.Generic;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// ScriptableObject collecting the authored <see cref="FactKeyDefinition"/> assets into one
    /// vocabulary catalogue. Installer-wired and mapped to the Core registry at install time
    /// (<c>FactKeyRegistryMapper</c>). Configuration data only.
    /// </summary>
    [CreateAssetMenu(fileName = "FactKeyRegistry", menuName = "Narrative/Facts/Fact Key Registry")]
    public class FactKeyRegistry : ScriptableObject
    {
        [SerializeField] private List<FactKeyDefinition> _keys = new List<FactKeyDefinition>();

        public IReadOnlyList<FactKeyDefinition> Keys => _keys;
    }
}
