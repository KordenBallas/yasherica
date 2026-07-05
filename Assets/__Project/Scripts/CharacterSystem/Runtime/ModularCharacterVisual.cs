using System;
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

        /// <summary>Raised right after a rig is assembled: once from <see cref="Start"/> and again
        /// after every body-plan change (<see cref="ReplaceCharacter"/>), so cached-reference
        /// holders re-bind. Late subscribers must also check <see cref="Character"/> for the
        /// already-assembled case.</summary>
        public event Action<IModularCharacter> CharacterAssembled;

        /// <summary>The authored assembly this visual builds from (e.g. for preview clones).</summary>
        public CharacterAssemblyDefinition Assembly => _assembly;

        /// <summary>The assembled rig's Animator, available after <see cref="Start"/> (null until then).</summary>
        public Animator Animator { get; private set; }

        private void Start()
        {
            BuildInitial();
        }

        private void BuildInitial()
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

            AdoptCharacter(character, _assembly != null ? _assembly.Skeleton : null);
        }

        /// <summary>
        /// Re-points this visual at a freshly-built body after a body-plan change: applies the
        /// host-local placement, re-caches <see cref="Character"/>/<see cref="Animator"/>, binds
        /// the governing frame's locomotion controller, and re-fires
        /// <see cref="CharacterAssembled"/> so cached-reference holders re-bind. The caller (the
        /// body-plan coordinator) owns tearing down the previous rig.
        /// </summary>
        public void ReplaceCharacter(ModularCharacter next, SkeletonDefinition governingSkeleton)
        {
            if (next == null)
            {
                return;
            }

            AdoptCharacter(next, governingSkeleton);
        }

        private void AdoptCharacter(ModularCharacter character, SkeletonDefinition skeleton)
        {
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
                // ("Animator is not playing an AnimatorController"). Per-skeleton controller wins
                // so each body plan animates with its own gait (serpent slither vs biped run).
                var controller = skeleton != null && skeleton.AnimatorController != null
                    ? skeleton.AnimatorController
                    : _animatorController != null
                        ? _animatorController
                        : Resources.Load<RuntimeAnimatorController>(DefaultControllerResourcePath);
                if (controller != null)
                {
                    Animator.runtimeAnimatorController = controller;
                }
            }

            CharacterAssembled?.Invoke(Character);
        }
    }
}
