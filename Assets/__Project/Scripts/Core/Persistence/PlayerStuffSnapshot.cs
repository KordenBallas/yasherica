using System;
using System.Collections.Generic;

namespace Core.Persistence
{
    /// <summary>One artifact instance (id + definition), shared by the inventory/socket sections.</summary>
    [Serializable]
    public class ArtifactInstanceDto
    {
        public int InstanceId;
        public string DefinitionId;
    }

    /// <summary>One racked part-blank instance.</summary>
    [Serializable]
    public class BlankInstanceDto
    {
        public int InstanceId;
        public string DefinitionId;
    }

    /// <summary>The artifacts currently socketed into one blank (in socket order). These artifacts
    /// live OUTSIDE the inventory while socketed, so they are carried here, not in Artifacts.</summary>
    [Serializable]
    public class SocketedBlankDto
    {
        public int BlankInstanceId;
        public List<ArtifactInstanceDto> Artifacts = new List<ArtifactInstanceDto>();
    }

    /// <summary>One recorded key choice.</summary>
    [Serializable]
    public class ChoiceDto
    {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// "My stuff" (FR4): the artifact inventory, the shed-part stash, the blank rack with its
    /// in-progress socketing, and the run-progression extras (encountered NPCs, key choices).
    /// A mid-staging crafting session is normalized at capture — staged items and an uncollected
    /// result are carried as plain inventory artifacts and the session resumes Idle (FR7: the
    /// mid-action state re-begins; the items themselves are never lost). Currency has no backing
    /// model yet and is deliberately absent (ROADMAP).
    /// </summary>
    [Serializable]
    public class PlayerStuffSnapshot
    {
        public List<ArtifactInstanceDto> Artifacts = new List<ArtifactInstanceDto>();
        public int NextArtifactInstanceId;
        public List<string> PartIds = new List<string>();
        public List<BlankInstanceDto> Blanks = new List<BlankInstanceDto>();
        public int NextBlankInstanceId;
        public List<SocketedBlankDto> SocketedByBlank = new List<SocketedBlankDto>();
        public List<string> EncounteredNpcs = new List<string>();
        public List<ChoiceDto> Choices = new List<ChoiceDto>();
    }
}
