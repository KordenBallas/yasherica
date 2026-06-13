using System;
using System.Collections.Generic;
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
    /// for testing: rig prefab with idle animation, skinned part meshes (A/B variants),
    /// slot/socket/part/attachment/assembly definitions, and attachment prefabs.
    /// Idempotent: regenerates the whole Resources/CharacterSystem folder.
    /// </summary>
    public static class PlaceholderCharacterGenerator
    {
        private const string RootFolder = "Assets/__Project/Resources/CharacterSystem";
        private const string SkeletonId = "skeleton.placeholder";

        private static readonly Vector3 PartBoundsCenter = new Vector3(0f, 0.8f, 0f);
        private static readonly Vector3 PartBoundsSize = new Vector3(2.5f, 2.5f, 2.5f);

        [MenuItem("Tools/Character System/Generate Placeholder Assets")]
        public static void GenerateAll()
        {
            RecreateFolders();

            var materials = CreateMaterials();
            var rigContext = CreateRig();

            try
            {
                var slots = CreateSlotDefinitions();
                var sockets = CreateSocketDefinitions();
                var skeleton = CreateSkeletonDefinition(rigContext.RigPrefab, sockets);
                var parts = CreatePartDefinitions(rigContext.SceneBones, materials, slots, sockets, skeleton);
                CreateAttachmentDefinitions(materials);
                CreateAssemblyDefinition(skeleton, parts);
            }
            finally
            {
                Object.DestroyImmediate(rigContext.SceneInstance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderCharacterGenerator] Placeholder character assets generated under {RootFolder}.");
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

            var serializedContext = new SerializedObject(sceneContext);
            var installersProperty = serializedContext.FindProperty("_installers");
            var insertIndex = installersProperty.arraySize;
            installersProperty.InsertArrayElementAtIndex(insertIndex);
            installersProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = installer;
            serializedContext.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[PlaceholderCharacterGenerator] Registered CharacterSystemInstaller on the SceneContext.");
        }

        // ----- Rig ---------------------------------------------------------

        private sealed class RigContext
        {
            public GameObject RigPrefab;
            public GameObject SceneInstance;
            public Dictionary<string, Transform> SceneBones;
        }

        private static RigContext CreateRig()
        {
            var rigRoot = PlaceholderRigBuilder.BuildRigInScene("PlaceholderRig", out var bones);

            var idleClip = PlaceholderAnimationBuilder.BuildIdleClip($"{RootFolder}/Animation/PlaceholderIdle.anim");
            var controller = PlaceholderAnimationBuilder.BuildController(idleClip, $"{RootFolder}/Animation/PlaceholderIdle.controller");
            rigRoot.GetComponent<Animator>().runtimeAnimatorController = controller;

            var rigPrefab = PrefabUtility.SaveAsPrefabAsset(rigRoot, $"{RootFolder}/Skeletons/PlaceholderRig.prefab");

            // The scene instance stays alive (in bind pose) until part meshes are built,
            // because their bindposes are read from these exact bone transforms.
            return new RigContext { RigPrefab = rigPrefab, SceneInstance = rigRoot, SceneBones = bones };
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
                VariantA = CreateMaterial("Mat_VariantA", new Color(0.4f, 0.7f, 0.9f)),
                VariantB = CreateMaterial("Mat_VariantB", new Color(0.9f, 0.55f, 0.3f)),
                Prop = CreateMaterial("Mat_Prop", new Color(0.5f, 0.5f, 0.5f))
            };
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, $"{RootFolder}/Parts/Materials/{name}.mat");
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
            ("slot.tail", "Tail")
        };

        private static Dictionary<string, SlotDefinition> CreateSlotDefinitions()
        {
            var slots = new Dictionary<string, SlotDefinition>(StringComparer.Ordinal);
            foreach (var (id, displayName) in SlotSpecs)
            {
                var assetName = $"Slot_{displayName.Replace(" ", string.Empty)}";
                slots.Add(id, CreateDefinition<SlotDefinition>($"{RootFolder}/Slots/{assetName}.asset", serialized =>
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
            // Tier-1 (always on the skeleton).
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

        private static readonly string[] Tier1SocketIds =
        {
            "socket.shoulder.l", "socket.shoulder.r",
            "socket.palm.l", "socket.palm.r",
            "socket.foot.l", "socket.foot.r",
            "socket.back", "socket.tail.base"
        };

        private static Dictionary<string, SocketDefinition> CreateSocketDefinitions()
        {
            var sockets = new Dictionary<string, SocketDefinition>(StringComparer.Ordinal);
            foreach (var (id, parentBone, localPosition) in SocketSpecs)
            {
                var assetName = $"Socket_{id.Replace("socket.", string.Empty).Replace('.', '_')}";
                sockets.Add(id, CreateDefinition<SocketDefinition>($"{RootFolder}/Sockets/{assetName}.asset", serialized =>
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

        private static SkeletonDefinition CreateSkeletonDefinition(GameObject rigPrefab, Dictionary<string, SocketDefinition> sockets)
        {
            var boneNames = new List<string>();
            foreach (var bone in PlaceholderRigBuilder.Bones)
            {
                boneNames.Add(bone.Name);
            }

            var tier1 = new List<SocketDefinition>();
            foreach (var socketId in Tier1SocketIds)
            {
                tier1.Add(sockets[socketId]);
            }

            return CreateDefinition<SkeletonDefinition>($"{RootFolder}/Skeletons/PlaceholderSkeleton.asset", serialized =>
            {
                serialized.FindProperty("_id").stringValue = SkeletonId;
                serialized.FindProperty("_rigPrefab").objectReferenceValue = rigPrefab;
                SetStringList(serialized.FindProperty("_boneNames"), boneNames);
                SetObjectList(serialized.FindProperty("_tier1Sockets"), tier1);
            });
        }

        // ----- Parts -------------------------------------------------------

        private sealed class PartSpec
        {
            public string Id;
            public string AssetName;
            public string SlotId;
            public bool IsVariantB;
            public PlaceholderMeshBuilder.BoxSpec[] Boxes;
            public string[] ContributedSocketIds = Array.Empty<string>();
        }

        private static PlaceholderMeshBuilder.BoxSpec[] ArmBoxes(float sign, float thickness)
        {
            var side = sign < 0 ? "L" : "R";
            return new[]
            {
                new PlaceholderMeshBuilder.BoxSpec($"UpperArm.{side}", new Vector3(sign * 0.09f, 0f, 0f), new Vector3(0.2f, 0.09f * thickness, 0.09f * thickness)),
                new PlaceholderMeshBuilder.BoxSpec($"LowerArm.{side}", new Vector3(sign * 0.08f, 0f, 0f), new Vector3(0.18f, 0.08f * thickness, 0.08f * thickness)),
                new PlaceholderMeshBuilder.BoxSpec($"Hand.{side}", new Vector3(sign * 0.04f, 0f, 0f), new Vector3(0.1f, 0.1f * thickness, 0.1f * thickness))
            };
        }

        private static PlaceholderMeshBuilder.BoxSpec[] LegBoxes(float sign, float thickness)
        {
            var side = sign < 0 ? "L" : "R";
            return new[]
            {
                new PlaceholderMeshBuilder.BoxSpec($"UpperLeg.{side}", new Vector3(0f, -0.17f, 0f), new Vector3(0.12f * thickness, 0.36f, 0.12f * thickness)),
                new PlaceholderMeshBuilder.BoxSpec($"LowerLeg.{side}", new Vector3(0f, -0.17f, 0f), new Vector3(0.1f * thickness, 0.36f, 0.1f * thickness)),
                new PlaceholderMeshBuilder.BoxSpec($"Foot.{side}", new Vector3(0f, -0.02f, -0.05f), new Vector3(0.12f * thickness, 0.07f, 0.22f))
            };
        }

        private static PartSpec[] BuildPartSpecs()
        {
            return new[]
            {
                new PartSpec
                {
                    Id = "part.head.a", AssetName = "Head_A", SlotId = "slot.head",
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Head", new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.2f, 0.22f)),
                        new PlaceholderMeshBuilder.BoxSpec("Ear.L", new Vector3(0f, 0.05f, 0f), new Vector3(0.05f, 0.12f, 0.03f)),
                        new PlaceholderMeshBuilder.BoxSpec("Ear.R", new Vector3(0f, 0.05f, 0f), new Vector3(0.05f, 0.12f, 0.03f))
                    },
                    ContributedSocketIds = new[] { "socket.hat", "socket.ear.l", "socket.ear.r" }
                },
                new PartSpec
                {
                    Id = "part.head.b", AssetName = "Head_B", SlotId = "slot.head", IsVariantB = true,
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Head", new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.16f, 0.26f))
                    },
                    ContributedSocketIds = new[] { "socket.hat" }
                },
                new PartSpec
                {
                    Id = "part.torso.a", AssetName = "Torso_A", SlotId = "slot.torso",
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Spine", new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.2f, 0.2f)),
                        new PlaceholderMeshBuilder.BoxSpec("Chest", new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.22f, 0.22f)),
                        new PlaceholderMeshBuilder.BoxSpec("Wing.L", new Vector3(-0.1f, 0.02f, 0.04f), new Vector3(0.22f, 0.06f, 0.1f)),
                        new PlaceholderMeshBuilder.BoxSpec("Wing.R", new Vector3(0.1f, 0.02f, 0.04f), new Vector3(0.22f, 0.06f, 0.1f))
                    },
                    ContributedSocketIds = new[] { "socket.wing.l", "socket.wing.r" }
                },
                new PartSpec
                {
                    Id = "part.torso.b", AssetName = "Torso_B", SlotId = "slot.torso", IsVariantB = true,
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Spine", new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.2f, 0.26f)),
                        new PlaceholderMeshBuilder.BoxSpec("Chest", new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.24f, 0.28f))
                    }
                },
                new PartSpec { Id = "part.arm.l.a", AssetName = "ArmL_A", SlotId = "slot.arm.l", Boxes = ArmBoxes(-1f, 1f) },
                new PartSpec { Id = "part.arm.l.b", AssetName = "ArmL_B", SlotId = "slot.arm.l", IsVariantB = true, Boxes = ArmBoxes(-1f, 1.6f) },
                new PartSpec { Id = "part.arm.r.a", AssetName = "ArmR_A", SlotId = "slot.arm.r", Boxes = ArmBoxes(1f, 1f) },
                new PartSpec { Id = "part.arm.r.b", AssetName = "ArmR_B", SlotId = "slot.arm.r", IsVariantB = true, Boxes = ArmBoxes(1f, 1.6f) },
                new PartSpec { Id = "part.leg.l.a", AssetName = "LegL_A", SlotId = "slot.leg.l", Boxes = LegBoxes(-1f, 1f) },
                new PartSpec { Id = "part.leg.l.b", AssetName = "LegL_B", SlotId = "slot.leg.l", IsVariantB = true, Boxes = LegBoxes(-1f, 1.6f) },
                new PartSpec { Id = "part.leg.r.a", AssetName = "LegR_A", SlotId = "slot.leg.r", Boxes = LegBoxes(1f, 1f) },
                new PartSpec { Id = "part.leg.r.b", AssetName = "LegR_B", SlotId = "slot.leg.r", IsVariantB = true, Boxes = LegBoxes(1f, 1.6f) },
                new PartSpec
                {
                    Id = "part.tail.a", AssetName = "Tail_A", SlotId = "slot.tail",
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Tail.0", new Vector3(0f, 0f, 0.07f), new Vector3(0.1f, 0.1f, 0.18f)),
                        new PlaceholderMeshBuilder.BoxSpec("Tail.1", new Vector3(0f, 0f, 0.07f), new Vector3(0.08f, 0.08f, 0.17f)),
                        new PlaceholderMeshBuilder.BoxSpec("Tail.2", new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0.06f, 0.16f))
                    },
                    ContributedSocketIds = new[] { "socket.tail.tip" }
                }
            };
        }

        private static Dictionary<string, PartDefinition> CreatePartDefinitions(
            Dictionary<string, Transform> sceneBones,
            Materials materials,
            Dictionary<string, SlotDefinition> slots,
            Dictionary<string, SocketDefinition> sockets,
            SkeletonDefinition skeleton)
        {
            var parts = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);

            foreach (var spec in BuildPartSpecs())
            {
                var mesh = PlaceholderMeshBuilder.BuildPartMesh($"Mesh_{spec.AssetName}", spec.Boxes, sceneBones, out var orderedBoneNames);
                AssetDatabase.CreateAsset(mesh, $"{RootFolder}/Parts/Meshes/Mesh_{spec.AssetName}.asset");

                var prefab = CreatePartPrefab(spec.AssetName, mesh, spec.IsVariantB ? materials.VariantB : materials.VariantA);

                var contributed = new List<SocketDefinition>();
                foreach (var socketId in spec.ContributedSocketIds)
                {
                    contributed.Add(sockets[socketId]);
                }

                parts.Add(spec.Id, CreateDefinition<PartDefinition>($"{RootFolder}/Parts/Part_{spec.AssetName}.asset", serialized =>
                {
                    serialized.FindProperty("_id").stringValue = spec.Id;
                    serialized.FindProperty("_slot").objectReferenceValue = slots[spec.SlotId];
                    serialized.FindProperty("_targetSkeleton").objectReferenceValue = skeleton;
                    serialized.FindProperty("_partPrefab").objectReferenceValue = prefab;
                    SetStringList(serialized.FindProperty("_boneNames"), orderedBoneNames);
                    SetObjectList(serialized.FindProperty("_contributedSockets"), contributed);
                }));
            }

            return parts;
        }

        private static GameObject CreatePartPrefab(string assetName, Mesh mesh, Material material)
        {
            var partObject = new GameObject($"PartPrefab_{assetName}");
            try
            {
                var renderer = partObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh;
                renderer.sharedMaterial = material;
                // Whole-character bounds so per-part culling can never pop after bone remap.
                renderer.localBounds = new Bounds(PartBoundsCenter, PartBoundsSize);
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

                attachments.Add(CreateDefinition<AttachmentDefinition>($"{RootFolder}/Attachments/Attachment_{assetName}.asset", serialized =>
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

        private static void CreateAssemblyDefinition(SkeletonDefinition skeleton, Dictionary<string, PartDefinition> parts)
        {
            var defaultParts = new List<PartDefinition>
            {
                parts["part.head.a"],
                parts["part.torso.a"],
                parts["part.arm.l.a"],
                parts["part.arm.r.a"],
                parts["part.leg.l.a"],
                parts["part.leg.r.a"],
                parts["part.tail.a"]
            };

            CreateDefinition<CharacterAssemblyDefinition>($"{RootFolder}/Assemblies/PlaceholderAssembly_A.asset", serialized =>
            {
                serialized.FindProperty("_id").stringValue = "assembly.placeholder.a";
                serialized.FindProperty("_skeleton").objectReferenceValue = skeleton;
                SetObjectList(serialized.FindProperty("_parts"), defaultParts);
                SetObjectList(serialized.FindProperty("_defaultAttachments"), new List<AttachmentDefinition>());
            });
        }

        // ----- Helpers -----------------------------------------------------

        private static void RecreateFolders()
        {
            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
            }

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

        private static TDefinition CreateDefinition<TDefinition>(string assetPath, Action<SerializedObject> configure)
            where TDefinition : ScriptableObject
        {
            var definition = ScriptableObject.CreateInstance<TDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);

            var serialized = new SerializedObject(definition);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
