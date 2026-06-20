using System;
using System.Collections.Generic;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>Serialized per-session Ink state (reuses InkStoryManager.SaveState/LoadState).</summary>
    [Serializable]
    public class DialogueSessionSnapshot
    {
        public string DialogueId;
        public string InkState;
    }

    /// <summary>Serialized quest lifecycle + per-objective progress.</summary>
    [Serializable]
    public class QuestInstanceSnapshot
    {
        public string QuestId;
        public int State; // QuestState
        public List<string> CompletedObjectives = new List<string>();
        public List<string> ObjectiveIds = new List<string>();
        public List<int> ObjectiveCounts = new List<int>();
    }

    /// <summary>Serialized per-run actor identity.</summary>
    [Serializable]
    public class NpcInstanceSnapshot
    {
        public string InstanceId;
        public string ArchetypeId;
        public string ChosenDisplayName;
        public string FactionId;
    }

    /// <summary>Serialized casting binding (fragments referenced by id; context bag flattened).</summary>
    [Serializable]
    public class CastingSnapshot
    {
        public string ActorInstanceId;
        public string DialogueId;
        public string QuestId;
        public string EnemyId;
        public List<string> ContextSubjectTokens = new List<string>();
        public List<string> ContextSubjectValues = new List<string>();
    }

    /// <summary>
    /// Aggregate save image for all narrative run-state (R14 save/load): the unified fact store, the
    /// serializable PRNG state (B2 — so post-load procedural draws replay identically), active quests,
    /// actors, castings, and per-session Ink state. Plain primitives so a future JsonUtility/Newtonsoft
    /// writer is trivial; the file-IO implementation is a ROADMAP item.
    /// </summary>
    [Serializable]
    public class RunNarrativeSnapshot
    {
        public int Seed;
        public ulong RngState;
        public FactStoreSnapshot Facts = new FactStoreSnapshot();
        public List<NpcInstanceSnapshot> Actors = new List<NpcInstanceSnapshot>();
        public List<QuestInstanceSnapshot> Quests = new List<QuestInstanceSnapshot>();
        public List<CastingSnapshot> Castings = new List<CastingSnapshot>();
        public List<DialogueSessionSnapshot> Sessions = new List<DialogueSessionSnapshot>();
    }
}
