using CharacterSystem.Data.Definitions;
using UnityEngine;
using Zenject;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Thin adapter that turns a host GameObject (e.g. the player Hero) into a modular
    /// character: it assembles the model under the host at runtime and hides the host's
    /// placeholder visual. Movement, facing, and camera stay on the host root, so the
    /// parented rig inherits all motion automatically.
    /// </summary>
    public class ModularCharacterVisual : MonoBehaviour
    {
        // The placeholder locomotion controller lives under Resources; used when no controller is
        // assigned in the inspector. Binding it in code is reliable, unlike the rig prefab's
        // serialized controller reference, which can come up unbound at runtime.
        private const string DefaultControllerResourcePath = "CharacterSystem/Animation/PlaceholderLocomotion";

        [SerializeField] private CharacterAssemblyDefinition _assembly;

        [Tooltip("Capsule/placeholder renderer to hide once the model is assembled.")]
        [SerializeField] private Renderer _placeholderRenderer;

        [Tooltip("Animator controller bound to the assembled rig at runtime. Leave empty to load the placeholder locomotion controller from Resources.")]
        [SerializeField] private RuntimeAnimatorController _animatorController;

        [Header("Model placement under the host (tweak to fit the collider)")]
        [SerializeField] private Vector3 _localPosition = new Vector3(0f, -1f, 0f);
        [Tooltip("Yaw correction so the model's forward faces the host's +Z. The placeholder art faces -Z (tail/back on +Z), so 180. Real art that already faces +Z should use 0.")]
        [SerializeField] private Vector3 _localRotationEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 _localScale = new Vector3(1.25f, 1.25f, 1.25f);

        [Inject] private IModularCharacterFactory _factory;

        public IModularCharacter Character { get; private set; }

        /// <summary>The assembled rig's Animator, available after <see cref="Start"/> (null until then).</summary>
        public Animator Animator { get; private set; }

        private void Start()
        {
            if (_placeholderRenderer != null)
            {
                _placeholderRenderer.enabled = false;
            }

            var character = _factory.Create(_assembly, transform);
            if (character == null)
            {
                return;
            }

            var rigTransform = character.transform;
            rigTransform.localPosition = _localPosition;
            rigTransform.localRotation = Quaternion.Euler(_localRotationEuler);
            rigTransform.localScale = _localScale;

            Character = character;
            Animator = character.GetComponent<ICharacterRig>()?.Animator;

            if (Animator != null)
            {
                // Bind the controller directly: a freshly-resolved asset reference is reliable,
                // whereas the rig prefab's serialized controller can come up unbound at runtime
                // ("Animator is not playing an AnimatorController").
                var controller = _animatorController != null
                    ? _animatorController
                    : Resources.Load<RuntimeAnimatorController>(DefaultControllerResourcePath);
                if (controller != null)
                {
                    Animator.runtimeAnimatorController = controller;
                }
            }
        }
    }
}
