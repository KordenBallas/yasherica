using System.Collections.Generic;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;

namespace Narrative.Dialogue.Data
{
    /// <summary>
    /// The only bridge from <see cref="DialogueDefinition"/> to the UnityEngine-free
    /// <see cref="DialogueData"/> Core record. Extracts the Ink JSON text here so no Unity type
    /// (TextAsset) crosses into Core, and converts authored write-shapes to their Core form.
    /// </summary>
    public static class DialogueMapper
    {
        public static DialogueData ToData(DialogueDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var writes = new List<FactKeyShapeCore>();
            foreach (var shape in definition.DeclaredFactWrites)
            {
                if (shape != null)
                {
                    writes.Add(shape.ToCore());
                }
            }

            var inkJson = definition.InkJsonAsset != null ? definition.InkJsonAsset.text : string.Empty;

            return new DialogueData(
                definition.DialogueId,
                inkJson,
                definition.StartKnot,
                new List<string>(definition.DeclaredVariables),
                writes,
                new List<string>(definition.DialogueTags));
        }
    }
}
