using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using Core.DI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// One-click generation of every placeholder asset the modular character system needs
    /// for testing: per body plan (base biped + serpent + spider, see
    /// <see cref="PlaceholderFrameLibrary"/>) a rig prefab with idle/run gait, skinned part
    /// meshes, and skeleton/part/assembly definitions, plus the shared slot/socket/attachment
    /// assets.
    ///
    /// Idempotent AND in-place: existing assets are updated at their paths (GUIDs and
    /// hand-authored content fields — display names, abilities, traits, race tags — survive a
    /// regeneration). Scene/prefab/blank references into this folder therefore never break.
    /// Only animation clips/controllers are rebuilt from scratch: they are re-assigned to
    /// their consumers (rig prefab, skeleton definition) in the same run and are otherwise
    /// only loaded by Resources path.
    /// </summary>
    public static class PlaceholderCharacterGenerator
    {
        private const string RootFolder = "Assets/__Project/Resources/CharacterSystem";

        private static readonly Vector3 PartBoundsCenter = new Vector3(0f, 0.8f, 0f);
        private static readonly Vector3 PartBoundsSize = new Vector3(2.5f, 2.5f, 2.5f);

        [MenuItem("Tools/Character System/Generate Placeholder Assets")]
        public static void GenerateAll()
        {
            EnsureFolders();

            try
            {
                var materials = CreateMaterials();
                var slots = CreateSlotDefinitions();
                var sockets = CreateSocketDefinitions();

                var allParts = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);
                var skeletons = new Dictionary<string, SkeletonDefinition>(StringComparer.Ordinal);

                foreach (var frame in PlaceholderFrameLibrary.Frames)
                {
                    GenerateFrame(frame, materials, slots, sockets, allParts, skeletons);
                }

                CreateAttachmentDefinitions(materials);

                foreach (var frame in PlaceholderFrameLibrary.Frames)
                {
                    CreateAssemblyDefinition(frame, skeletons[frame.SkeletonId], allParts);
                }

                Debug.Log($"[PlaceholderCharacterGenerator] Placeholder assets for {PlaceholderFrameLibrary.Frames.Length} body plans generated under {RootFolder}.");
            }
            finally
            {
                // SaveAssets MUST run even if generation throws: definitions are configured via
                // SerializedObject in memory and only persisted by SaveAssets. Skipping it (the old
                // bug — SaveAssets sat after the try) left every definition as a blank shell on disk.
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Builds one frame's rig prefab, gait clips + controller, skeleton definition,
        /// and part definitions. The scene rig instance stays alive until the frame's part
        /// meshes are built (their bindposes are read from these exact bone transforms).</summary>
        private static void GenerateFrame(
            PlaceholderFrameLibrary.FrameSpec frame,
            Materials materials,
            Dictionary<string, SlotDefinition> slots,
            Dictionary<string, SocketDefinition> sockets,
            Dictionary<string, PartDefinition> allParts,
            Dictionary<string, SkeletonDefinition> skeletons)
        {
            var rigRoot = PlaceholderRigBuilder.BuildRigInScene($"{frame.AssetBaseName}Rig", frame.Bones, out var sceneBones);
            try
            {
                var idlePath = $"{RootFolder}/Animation/{frame.AssetBaseName}Idle.anim";
                var runPath = $"{RootFolder}/Animation/{frame.AssetBaseName}Run.anim";
                var controllerPath = $"{RootFolder}/Animation/{frame.AssetBaseName}Locomotion.controller";

                // Clips/controllers are the one delete-and-rebuild exception (see class note).
                AssetDatabase.DeleteAsset(idlePath);
                AssetDatabase.DeleteAsset(runPath);
                AssetDatabase.DeleteAsset(controllerPath);

                var idleClip = PlaceholderAnimationBuilder.BuildSwayClip($"{frame.AssetBaseName}Idle", idlePath, frame.IdleSways, frame.BonePath);
                var runClip = PlaceholderAnimationBuilder.BuildSwayClip($"{frame.AssetBaseName}Run", runPath, frame.RunSways, frame.BonePath);
                var controller = PlaceholderAnimationBuilder.BuildController(idleClip, runClip, controllerPath);
                rigRoot.GetComponent<Animator>().runtimeAnimatorController = controller;

                // SaveAsPrefabAsset over the existing path keeps the prefab GUID.
                var rigPrefab = PrefabUtility.SaveAsPrefabAsset(rigRoot, $"{RootFolder}/Skeletons/{frame.AssetBaseName}Rig.prefab");

                var skeleton = CreateSkeletonDefinition(frame, rigPrefab, sockets, controller);
                skeletons[frame.SkeletonId] = skeleton;

                foreach (var spec in frame.Parts)
                {
                    allParts[spec.Id] = CreatePartDefinition(spec, sceneBones, materials, slots, sockets, skeleton);
                }
            }
            finally
            {
                Object.DestroyImmediate(rigRoot);
            }
        }

        [MenuItem("Tools/Character System/Place Demo Character In Scene")]
        public static void PlaceDemoCharacterInScene()
        {
            var assembly = AssetDatabase.LoadAssetAtPath<CharacterAssemblyDefinition>($"{RootFolder}/Assemblies/PlaceholderAssembly_A.asset");
            if (assembly == null)
            {
                Debug.LogError("[PlaceholderCharacterGenerator] Generate the placeholder assets first (Tools/Character System/Generate Placeholder Assets).");
                return;
            }

            var demoObject = new GameObject("CharacterSystemDemo");
            var bootstrap = demoObject.AddComponent<CharacterSystemDemoBootstrap>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_assembly").objectReferenceValue = assembly;
            SetObjectList(serialized.FindProperty("_alternateParts"), LoadAll<PartDefinition>($"{RootFolder}/Parts", "_B"));
            SetObjectList(serialized.FindProperty("_attachments"), LoadAll<AttachmentDefinition>($"{RootFolder}/Attachments", null));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureInstallerOnSceneContext();

            Selection.activeGameObject = demoObject;
            EditorSceneManager.MarkSceneDirty(demoObject.scene);
            Debug.Log("[PlaceholderCharacterGenerator] Demo placed. Press Play, then use the bootstrap's context menu to swap parts and attach props.");
        }

        /// <summary>
        /// Registers the CharacterSystemInstaller on the scene's SceneContext so the
        /// factory binding resolves. Without this the demo bootstrap's [Inject] fields
        /// stay null and Zenject aborts the whole scene resolve.
        /// </summary>
        private static void EnsureInstallerOnSceneContext()
        {
            var sceneContext = Object.FindObjectOfType<SceneContext>();
            if (sceneContext == null)
            {
                Debug.LogWarning(
                    "[PlaceholderCharacterGenerator] No SceneContext found in the scene. Add a Zenject SceneContext and a CharacterSystemInstaller manually, or the demo will not run.");
                return;
            }

            foreach (var existing in sceneContext.Installers)
            {
                if (existing is CharacterSystemInstaller)
                {
                    return;
                }
            }

            var installer = sceneContext.gameObject.GetComponent<CharacterSystemInstaller>();
            if (installer == null)
            {
                installer = sceneContext.gameObject.AddComponent<CharacterSystemInstaller>();
            }

            // Register through the public Installers property (its setter writes _monoInstallers).
            // FindProperty("_installers") returns null: the field is _monoInstallers with
            // [FormerlySerializedAs("_installers")], which SerializedObject.FindProperty ignores.
            var installers = sceneContext.Installers.ToList();
            installers.Add(installer);
            sceneContext.Installers = installers;
            EditorUtility.SetDirty(sceneContext);

            Debug.Log("[PlaceholderCharacterGenerator] Registered CharacterSystemInstaller on the SceneContext.");
        }

        // ----- Materials ---------------------------------------------------

        private sealed class Materials
        {
            public Material VariantA;
            public Material VariantB;
            public Material Prop;
        }

        private static Materials CreateMaterials()
        {
            return new Materials
            {
                VariantA = LoadOrCreateMaterial("Mat_VariantA", new Color(0.4f, 0.7f, 0.9f)),
                VariantB = LoadOrCreateMaterial("Mat_VariantB", new Color(0.9f, 0.55f, 0.3f)),
                Prop = LoadOrCreateMaterial("Mat_Prop", new Color(0.5f, 0.5f, 0.5f))
            };
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            var path = $"{RootFolder}/Parts/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ----- Slots -------------------------------------------------------

        private static readonly (string Id, string DisplayName)[] SlotSpecs =
        {
            ("slot.head", "Head"),
            ("slot.torso", "Torso"),
            ("slot.arm.l", "Arm Left"),
            ("slot.arm.r", "Arm Right"),
            ("slot.leg.l", "Leg Left"),
            ("slot.leg.r", "Leg Right"),
            ("slot.tail", "Tail"),
            // Body plans: the spider frame-changer's home slot (empty on the base body).
            ("slot.legs.cluster", "Legs Cluster")
        };

        private static Dictionary<string, SlotDefinition> CreateSlotDefinitions()
        {
            var slots = new Dictionary<string, SlotDefinition>(StringComparer.Ordinal);
            foreach (var (id, displayName) in SlotSpecs)
            {
                var assetName = $"Slot_{displayName.Replace(" ", string.Empty)}";
                slots.Add(id, CreateOrUpdateDefinition<SlotDefinition>($"{RootFolder}/Slots/{assetName}.asset", (serialized, _) =>
                {
                    serialized.FindProperty("_id").stringValue = id;
                    serialized.FindProperty("_displayName").stringValue = displayName;
                }));
            }

            return slots;
        }

        // ----- Sockets -----------------------------------------------------

        private static readonly (string Id, string ParentBone, Vector3 LocalPosition)[] SocketSpecs =
        {
            // Tier-1 (always on the skeleton; per-frame subsets are picked by the frame library).
            ("socket.shoulder.l", "Shoulder.L", new Vector3(0f, 0.06f, 0f)),
            ("socket.shoulder.r", "Shoulder.R", new Vector3(0f, 0.06f, 0f)),
            ("socket.palm.l", "Hand.L", new Vector3(-0.05f, -0.04f, 0f)),
            ("socket.palm.r", "Hand.R", new Vector3(0.05f, -0.04f, 0f)),
            ("socket.foot.l", "Foot.L", new Vector3(0f, -0.03f, 0f)),
            ("socket.foot.r", "Foot.R", new Vector3(0f, -0.03f, 0f)),
            ("socket.back", "Chest", new Vector3(0f, 0.04f, 0.14f)),
            ("socket.tail.base", "Tail.0", Vector3.zero),
            // Tier-2 (contributed by parts).
            ("socket.hat", "Head", new Vector3(0f, 0.17f, 0f)),
            ("socket.ear.l", "Ear.L", new Vector3(0f, 0.07f, 0f)),
            ("socket.ear.r", "Ear.R", new Vector3(0f, 0.07f, 0f)),
            ("socket.wing.l", "Wing.L", new Vector3(-0.12f, 0.02f, 0.04f)),
            ("socket.wing.r", "Wing.R", new Vector3(0.12f, 0.02f, 0.04f)),
            ("socket.tail.tip", "Tail.2", new Vector3(0f, 0f, 0.12f))
        };

        private static Dictionary<string, SocketDefinition> CreateSocketDefinitions()
        {
            var sockets = new Dictionary<string, SocketDefinition>(StringComparer.Ordinal);
            foreach (var (id, parentBone, localPosition) in SocketSpecs)
            {
                var assetName = $"Socket_{id.Replace("socket.", string.Empty).Replace('.', '_')}";
                sockets.Add(id, CreateOrUpdateDefinition<SocketDefinition>($"{RootFolder}/Sockets/{assetName}.asset", (serialized, _) =>
                {
                    serialized.FindProperty("_id").stringValue = id;
                    serialized.FindProperty("_parentBoneName").stringValue = parentBone;
                    serialized.FindProperty("_localPosition").vector3Value = localPosition;
                    serialized.FindProperty("_localRotationEuler").vector3Value = Vector3.zero;
                    serialized.FindProperty("_localScale").vector3Value = Vector3.one;
                }));
            }

            return sockets;
        }

        // ----- Skeleton ----------------------------------------------------

        private static SkeletonDefinition CreateSkeletonDefinition(
            PlaceholderFrameLibrary.FrameSpec frame,
            GameObject rigPrefab,
            Dictionary<string, SocketDefinition> sockets,
            RuntimeAnimatorController controller)
        {
            var boneNames = new List<string>();
            foreach (var bone in frame.Bones)
            {
                boneNames.Add(bone.Name);
            }

            var tier1 = new List<SocketDefinition>();
            foreach (var socketId in frame.Tier1SocketIds)
            {
                tier1.Add(sockets[socketId]);
            }

            return CreateOrUpdateDefinition<SkeletonDefinition>($"{RootFolder}/Skeletons/{frame.AssetBaseName}Skeleton.asset", (serialized, _) =>
            {
                serialized.FindProperty("_id").stringValue = frame.SkeletonId;
                serialized.FindProperty("_displayName").stringValue = frame.DisplayName;
                serialized.FindProperty("_rigPrefab").objectReferenceValue = rigPrefab;
                serialized.FindProperty("_animatorController").objectReferenceValue = controller;
                SetStringList(serialized.FindProperty("_boneNames"), boneNames);
                SetObjectList(serialized.FindProperty("_tier1Sockets"), tier1);
            });
        }

        // ----- Parts -------------------------------------------------------

        private static PartDefinition CreatePartDefinition(
            PlaceholderFrameLibrary.FramePartSpec spec,
            Dictionary<string, Transform> sceneBones,
            Materials materials,
            Dictionary<string, SlotDefinition> slots,
            Dictionary<string, SocketDefinition> sockets,
            SkeletonDefinition skeleton)
        {
            var mesh = PlaceholderMeshBuilder.BuildPartMesh($"Mesh_{spec.AssetName}", spec.Boxes, sceneBones, out var orderedBoneNames);
            mesh = SaveMeshInPlace(mesh, $"{RootFolder}/Parts/Meshes/Mesh_{spec.AssetName}.asset");

            var prefab = CreatePartPrefab(spec.AssetName, mesh, spec.IsVariantB ? materials.VariantB : materials.VariantA);

            var contributed = new List<SocketDefinition>();
            foreach (var socketId in spec.ContributedSocketIds)
            {
                contributed.Add(sockets[socketId]);
            }

            return CreateOrUpdateDefinition<PartDefinition>($"{RootFolder}/Parts/Part_{spec.AssetName}.asset", (serialized, created) =>
            {
                // Generator-owned structure: always synced.
                serialized.FindProperty("_id").stringValue = spec.Id;
                serialized.FindProperty("_slot").objectReferenceValue = slots[spec.SlotId];
                serialized.FindProperty("_targetSkeleton").objectReferenceValue = skeleton;
                serialized.FindProperty("_partPrefab").objectReferenceValue = prefab;
                SetStringList(serialized.FindProperty("_boneNames"), orderedBoneNames);
                SetObjectList(serialized.FindProperty("_contributedSockets"), contributed);

                // Content fields (display name, body-plan governance, mutation data) are seeded
                // once and then belong to the designer — regeneration never stomps hand-tuning.
                if (!created)
                {
                    return;
                }

                serialized.FindProperty("_displayName").stringValue = spec.DisplayName ?? string.Empty;
                serialized.FindProperty("_governsBodyPlan").boolValue = spec.GovernsBodyPlan;
                serialized.FindProperty("_bodyPlanPriority").intValue = spec.BodyPlanPriority;
                serialized.FindProperty("_rarity").enumValueIndex = (int)spec.Rarity;

                var affinities = serialized.FindProperty("_traitAffinities");
                affinities.arraySize = spec.TraitAffinities.Length;
                for (var i = 0; i < spec.TraitAffinities.Length; i++)
                {
                    var element = affinities.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_traitId").stringValue = spec.TraitAffinities[i].TraitId;
                    element.FindPropertyRelative("_weight").floatValue = spec.TraitAffinities[i].Weight;
                }
            });
        }

        /// <summary>Persists a freshly-built mesh at the path WITHOUT churning the asset GUID:
        /// an existing mesh asset is overwritten via CopySerialized (references from part
        /// prefabs stay valid), a missing one is created.</summary>
        private static Mesh SaveMeshInPlace(Mesh built, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, path);
                return built;
            }

            EditorUtility.CopySerialized(built, existing);
            Object.DestroyImmediate(built);
            return existing;
        }

        private static GameObject CreatePartPrefab(string assetName, Mesh mesh, Material material)
        {
            var partObject = new GameObject($"PartPrefab_{assetName}");
            try
            {
                var renderer = partObject.AddComponent<SkinnedMeshRenderer>();
                // Whole-character bounds so per-part culling can never pop after bone remap.
                // Set BEFORE the mesh: with the mesh (and its bindposes) already assigned but
                // renderer.bones still empty (bones are remapped at runtime by PartSwapExecutor),
                // set_localBounds logs "Bones do not match bindpose" on every part.
                renderer.localBounds = new Bounds(PartBoundsCenter, PartBoundsSize);
                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(partObject, $"{RootFolder}/Parts/PartPrefabs/PartPrefab_{assetName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(partObject);
            }
        }

        // ----- Attachments -------------------------------------------------

        private static readonly (string Id, string AssetName, string SocketId, Vector3 VisualScale, Vector3 LocalPosition)[] AttachmentSpecs =
        {
            ("attachment.hat", "Hat", "socket.hat", new Vector3(0.26f, 0.1f, 0.26f), Vector3.zero),
            ("attachment.club", "Club", "socket.tail.tip", new Vector3(0.08f, 0.08f, 0.3f), new Vector3(0f, 0f, 0.1f)),
            ("attachment.sword", "Sword", "socket.palm.r", new Vector3(0.05f, 0.05f, 0.55f), new Vector3(0f, 0f, 0.2f))
        };

        private static List<AttachmentDefinition> CreateAttachmentDefinitions(Materials materials)
        {
            var attachments = new List<AttachmentDefinition>();

            foreach (var (id, assetName, socketId, visualScale, localPosition) in AttachmentSpecs)
            {
                var prefab = CreateAttachmentPrefab(assetName, visualScale, materials.Prop);

                attachments.Add(CreateOrUpdateDefinition<AttachmentDefinition>($"{RootFolder}/Attachments/Attachment_{assetName}.asset", (serialized, _) =>
                {
                    serialized.FindProperty("_id").stringValue = id;
                    serialized.FindProperty("_prefab").objectReferenceValue = prefab;
                    serialized.FindProperty("_socketId").stringValue = socketId;
                    serialized.FindProperty("_localPosition").vector3Value = localPosition;
                    serialized.FindProperty("_localRotationEuler").vector3Value = Vector3.zero;
                    serialized.FindProperty("_localScale").vector3Value = Vector3.one;
                }));
            }

            return attachments;
        }

        private static GameObject CreateAttachmentPrefab(string assetName, Vector3 visualScale, Material material)
        {
            // Root stays at scale one (the mounter writes the definition's TRS onto it);
            // the visual cube child carries the authored size.
            var root = new GameObject(assetName);
            try
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = visualScale;
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, $"{RootFolder}/Attachments/Prefabs/{assetName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ----- Assembly ----------------------------------------------------

        private static void CreateAssemblyDefinition(
            PlaceholderFrameLibrary.FrameSpec frame,
            SkeletonDefinition skeleton,
            Dictionary<string, PartDefinition> allParts)
        {
            var defaultParts = new List<PartDefinition>();
            foreach (var partId in frame.DefaultPartIds)
            {
                defaultParts.Add(allParts[partId]);
            }

            CreateOrUpdateDefinition<CharacterAssemblyDefinition>($"{RootFolder}/Assemblies/{frame.AssemblyAssetName}.asset", (serialized, created) =>
            {
                serialized.FindProperty("_id").stringValue = frame.AssemblyId;
                serialized.FindProperty("_skeleton").objectReferenceValue = skeleton;
                SetObjectList(serialized.FindProperty("_parts"), defaultParts);
                if (created)
                {
                    SetObjectList(serialized.FindProperty("_defaultAttachments"), new List<AttachmentDefinition>());
                }
            });
        }

        // ----- Helpers -----------------------------------------------------

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/__Project/Resources", "CharacterSystem");
            foreach (var sub in new[] { "Skeletons", "Slots", "Sockets", "Parts", "Attachments", "Animation", "Assemblies" })
            {
                EnsureFolder(RootFolder, sub);
            }

            EnsureFolder($"{RootFolder}/Parts", "Meshes");
            EnsureFolder($"{RootFolder}/Parts", "PartPrefabs");
            EnsureFolder($"{RootFolder}/Parts", "Materials");
            EnsureFolder($"{RootFolder}/Attachments", "Prefabs");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        /// <summary>Loads the definition at the path (keeping its GUID and any hand-authored
        /// fields) or creates it, then applies <paramref name="configure"/>. The bool argument
        /// is true when the asset was just created (seed-once content fields key off it).</summary>
        private static TDefinition CreateOrUpdateDefinition<TDefinition>(string assetPath, Action<SerializedObject, bool> configure)
            where TDefinition : ScriptableObject
        {
            var definition = AssetDatabase.LoadAssetAtPath<TDefinition>(assetPath);
            var created = definition == null;
            if (created)
            {
                definition = ScriptableObject.CreateInstance<TDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            var serialized = new SerializedObject(definition);
            configure(serialized, created);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void SetStringList(SerializedProperty listProperty, IReadOnlyList<string> values)
        {
            listProperty.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                listProperty.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void SetObjectList<TObject>(SerializedProperty listProperty, IReadOnlyList<TObject> values)
            where TObject : Object
        {
            listProperty.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                listProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static List<TAsset> LoadAll<TAsset>(string folder, string nameFilter) where TAsset : Object
        {
            var results = new List<TAsset>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (nameFilter != null && !System.IO.Path.GetFileNameWithoutExtension(path).Contains(nameFilter))
                {
                    continue;
                }

                results.Add(AssetDatabase.LoadAssetAtPath<TAsset>(path));
            }

            return results;
        }
    }
}
