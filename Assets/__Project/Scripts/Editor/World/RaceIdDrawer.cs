using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using World.Races.Data;

namespace Editor.World
{
    /// <summary>
    /// Draws a [RaceId] string field as a drop-down of "(kindless)" + the ids of every authored
    /// RaceDefinition under Resources/World/Races. The value stays a plain string so adding a race
    /// remains data-only (no enum, race-roster-and-passport.md FR3); an id that no longer matches
    /// any race asset is kept selectable as "<id> (missing)" instead of being silently rewritten.
    /// </summary>
    [CustomPropertyDrawer(typeof(RaceIdAttribute))]
    public class RaceIdDrawer : PropertyDrawer
    {
        private const string RacesResourcePath = "World/Races";
        private const string KindlessLabel = "(kindless)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ids = new List<string> { string.Empty };
            var labels = new List<string> { KindlessLabel };
            foreach (var race in Resources.LoadAll<RaceDefinition>(RacesResourcePath))
            {
                if (race == null || string.IsNullOrEmpty(race.RaceId) || ids.Contains(race.RaceId))
                {
                    continue;
                }

                ids.Add(race.RaceId);
                labels.Add(race.RaceId);
            }

            var current = property.stringValue ?? string.Empty;
            var index = ids.IndexOf(current);
            if (index < 0)
            {
                ids.Add(current);
                labels.Add($"{current} (missing)");
                index = ids.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            var picked = EditorGUI.Popup(position, label.text, index, labels.ToArray());
            if (picked != index)
            {
                property.stringValue = ids[picked];
            }

            EditorGUI.EndProperty();
        }
    }
}
