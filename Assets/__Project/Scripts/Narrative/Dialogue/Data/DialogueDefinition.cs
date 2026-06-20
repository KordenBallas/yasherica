using System.Collections.Generic;
using Narrative.Facts.Data;
using UnityEngine;

namespace Narrative.Dialogue.Data
{
    /// <summary>
    /// ScriptableObject for a dialogue fragment (R4): the compiled Ink JSON, entry knot, the variables
    /// the binding layer injects, the authoritative fact-write shapes its Ink <c>fact:</c> tags may
    /// produce (the write-gate, W2-1/W3-3), and matching tags. The reserved availability vars
    /// <c>quest_available</c>/<c>combat_available</c> are always injected by the runner (W2-2).
    /// Configuration data only.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "Narrative/Dialogue/Dialogue")]
    public class DialogueDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _dialogueId;

        [Header("Ink")]
        [Tooltip("Compiled Ink JSON (TextAsset). Must not hardcode specific NPCs/items.")]
        [SerializeField] private TextAsset _inkJsonAsset;
        [SerializeField] private string _startKnot = "start";
        [Tooltip("Variables the binding layer injects on fresh start (e.g. npc_name)")]
        [SerializeField] private List<string> _declaredVariables = new List<string>();

        [Header("Footprint & Matching")]
        [Tooltip("Authoritative, complete set of key shapes this dialogue's fact: tags may write")]
        [SerializeField] private List<FactKeyShape> _declaredFactWrites = new List<FactKeyShape>();
        [SerializeField] private List<string> _dialogueTags = new List<string>();

        public string DialogueId => _dialogueId;
        public TextAsset InkJsonAsset => _inkJsonAsset;
        public string StartKnot => _startKnot;
        public IReadOnlyList<string> DeclaredVariables => _declaredVariables;
        public IReadOnlyList<FactKeyShape> DeclaredFactWrites => _declaredFactWrites;
        public IReadOnlyList<string> DialogueTags => _dialogueTags;
    }
}
