using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Dialogue.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free dialogue fragment (R1/R4): the compiled Ink JSON, its entry knot,
    /// the variables the binding layer injects, and the **authoritative** set of fact-write shapes its
    /// Ink <c>fact:</c> tags may produce (the runtime write-gate, W2-1/W3-3). Carries no NPC/quest/enemy
    /// references; matched into a dialogue slot by <see cref="Tags"/>.
    /// </summary>
    public sealed class DialogueData
    {
        public string DialogueId { get; }
        public string InkJson { get; }
        public string StartKnot { get; }
        public IReadOnlyList<string> DeclaredVariables { get; }
        public IReadOnlyList<FactKeyShapeCore> DeclaredFactWrites { get; }
        public IReadOnlyList<string> Tags { get; }

        public DialogueData(string dialogueId, string inkJson, string startKnot,
            IReadOnlyList<string> declaredVariables, IReadOnlyList<FactKeyShapeCore> declaredFactWrites,
            IReadOnlyList<string> tags)
        {
            DialogueId = dialogueId ?? string.Empty;
            InkJson = inkJson ?? string.Empty;
            StartKnot = string.IsNullOrEmpty(startKnot) ? "start" : startKnot;
            DeclaredVariables = declaredVariables ?? System.Array.Empty<string>();
            DeclaredFactWrites = declaredFactWrites ?? System.Array.Empty<FactKeyShapeCore>();
            Tags = tags ?? System.Array.Empty<string>();
        }
    }
}
