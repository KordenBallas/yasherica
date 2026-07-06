using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Wraps a factory-built modular character in a host root whose +Z axis is the model's FACE.
    /// The placeholder rig art faces -Z (back/tail on +Z) — the hero gets the same 180° yaw
    /// correction from <see cref="ModularCharacterVisual"/> — so any consumer that yaws a
    /// character root (locomotion, combat facing, NPC placement) must host the rig through this
    /// wrap instead of driving the rig root directly, or the model walks back-first.
    /// </summary>
    public static class CharacterRigHost
    {
        // Mirrors ModularCharacterVisual's default yaw correction; real art that already faces
        // +Z would drop this to identity in both places.
        private static readonly Quaternion PlaceholderArtYawCorrection = Quaternion.Euler(0f, 180f, 0f);

        public static GameObject Wrap(ModularCharacter character, string name, Vector3 worldPosition)
        {
            var host = new GameObject(name);
            host.transform.position = worldPosition;

            var rig = character.transform;
            rig.SetParent(host.transform, false);
            rig.localPosition = Vector3.zero;
            rig.localRotation = PlaceholderArtYawCorrection;

            return host;
        }
    }
}
