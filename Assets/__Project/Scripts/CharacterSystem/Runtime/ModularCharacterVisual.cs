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
        [SerializeField] private CharacterAssemblyDefinition _assembly;

        [Tooltip("Capsule/placeholder renderer to hide once the model is assembled.")]
        [SerializeField] private Renderer _placeholderRenderer;

        [Header("Model placement under the host (tweak to fit the collider)")]
        [SerializeField] private Vector3 _localPosition = new Vector3(0f, -1f, 0f);
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
            rigTransform.localRotation = Quaternion.identity;
            rigTransform.localScale = _localScale;

            Character = character;
            Animator = character.GetComponent<ICharacterRig>()?.Animator;
        }
    }
}
