using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the modular character system. Binds the part/attachment
    /// catalogs and the character factory. Per-character object graphs (controller,
    /// swap executor, socket mounter) are deliberately factory-constructed, not
    /// container-bound: they are transient per-entity state.
    /// </summary>
    public class CharacterSystemInstaller : MonoInstaller
    {
        private const string PartDefinitionsResourcePath = "CharacterSystem/Parts";
        private const string AttachmentDefinitionsResourcePath = "CharacterSystem/Attachments";

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<PartDefinition> _partDefinitions;
        [SerializeField] private List<AttachmentDefinition> _attachmentDefinitions;

        public override void InstallBindings()
        {
            InstallData();
            InstallFactory();
        }

        // IGameLogger is intentionally NOT bound here: LoggingInstaller (installed by AreaInstaller)
        // is the single UnityGameLogger AsSingle binding for the scene. Zenject 6 forbids AsSingle on
        // the same concrete type across multiple bindings, so binding it again here (even with
        // IfNotBound) throws during finalization. Feature installers only resolve IGameLogger.

        private void InstallData()
        {
            var parts = LoadDefinitions(_partDefinitions, PartDefinitionsResourcePath);
            Container.Bind<IPartCatalog>()
                .To<PartCatalog>()
                .AsSingle()
                .WithArguments(parts as IReadOnlyList<PartDefinition>);

            var attachments = LoadDefinitions(_attachmentDefinitions, AttachmentDefinitionsResourcePath);
            Container.Bind<IAttachmentCatalog>()
                .To<AttachmentCatalog>()
                .AsSingle()
                .WithArguments(attachments as IReadOnlyList<AttachmentDefinition>);
        }

        private void InstallFactory()
        {
            Container.Bind<IModularCharacterFactory>().To<ModularCharacterFactory>().AsSingle();
        }

        private List<TDefinition> LoadDefinitions<TDefinition>(List<TDefinition> serialized, string resourcePath)
            where TDefinition : ScriptableObject
        {
            if (serialized != null && serialized.Count > 0)
            {
                return serialized;
            }

            var loaded = new List<TDefinition>(Resources.LoadAll<TDefinition>(resourcePath));
            if (loaded.Count == 0)
            {
                Debug.LogWarning(
                    $"[CharacterSystemInstaller] No {typeof(TDefinition).Name} assets assigned or found under Resources/{resourcePath}.");
            }

            return loaded;
        }
    }
}
