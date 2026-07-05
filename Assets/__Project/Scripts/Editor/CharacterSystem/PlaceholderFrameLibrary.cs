using System;
using System.Collections.Generic;
using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Data tables for every placeholder body plan the generator emits: the base biped plus
    /// the two P2-1 demonstrator frames (legless serpent, spider-legged). Each frame is a
    /// complete recipe — bone hierarchy, Tier-1 socket set, idle/run gait sways, parts, and a
    /// preview assembly — so adding a further placeholder plan is one more entry here.
    ///
    /// Invariant: bones shared across frames keep identical names AND rest TRS (all rigs are
    /// built at origin, facing -Z), so a base part's bindposes stay valid on any frame whose
    /// bone set resolves it — that is what makes cross-frame survival purely structural.
    /// </summary>
    public static class PlaceholderFrameLibrary
    {
        public sealed class SwaySpec
        {
            public string Bone;
            public Vector3 Axis;
            public float AmplitudeDegrees;
            public float FrequencyHz;
            public float PhaseFraction;
            public float CenterOffsetDegrees;

            public SwaySpec(string bone, Vector3 axis, float amplitude, float frequency, float phase, float centerOffset = 0f)
            {
                Bone = bone;
                Axis = axis;
                AmplitudeDegrees = amplitude;
                FrequencyHz = frequency;
                PhaseFraction = phase;
                CenterOffsetDegrees = centerOffset;
            }
        }

        public sealed class FramePartSpec
        {
            public string Id;
            public string AssetName;
            public string SlotId;
            public bool IsVariantB;
            public PlaceholderMeshBuilder.BoxSpec[] Boxes;
            public string[] ContributedSocketIds = Array.Empty<string>();

            // Content seeded only when the part asset is first created (hand-tuning survives regen).
            public string DisplayName;
            public bool GovernsBodyPlan;
            public int BodyPlanPriority;
            public (string TraitId, float Weight)[] TraitAffinities = Array.Empty<(string, float)>();
            public MutationRarity Rarity = MutationRarity.Common;
        }

        public sealed class FrameSpec
        {
            /// <summary>Asset-name stem: "Placeholder" keeps the base frame's historical paths.</summary>
            public string AssetBaseName;
            public string SkeletonId;
            public string DisplayName;
            public (string Name, string Parent, Vector3 LocalPosition)[] Bones;
            public string[] Tier1SocketIds;
            public SwaySpec[] IdleSways;
            public SwaySpec[] RunSways;
            public FramePartSpec[] Parts;
            public string AssemblyId;
            public string AssemblyAssetName;
            public string[] DefaultPartIds;

            /// <summary>Full transform path of a bone under the rig root (for animation curves).</summary>
            public string BonePath(string boneName)
            {
                var byName = new Dictionary<string, (string Parent, Vector3 P)>(StringComparer.Ordinal);
                foreach (var (name, parent, position) in Bones)
                {
                    byName[name] = (parent, position);
                }

                var segments = new List<string>();
                var current = boneName;
                while (current != null)
                {
                    segments.Insert(0, current);
                    current = byName[current].Parent;
                }

                return string.Join("/", segments);
            }
        }

        private static readonly string[] BaseTier1SocketIds =
        {
            "socket.shoulder.l", "socket.shoulder.r",
            "socket.palm.l", "socket.palm.r",
            "socket.foot.l", "socket.foot.r",
            "socket.back", "socket.tail.base"
        };

        // Legless frames: everything but the foot sockets.
        private static readonly string[] LeglessTier1SocketIds =
        {
            "socket.shoulder.l", "socket.shoulder.r",
            "socket.palm.l", "socket.palm.r",
            "socket.back", "socket.tail.base"
        };

        public static readonly FrameSpec Base = BuildBaseFrame();
        public static readonly FrameSpec Serpent = BuildSerpentFrame();
        public static readonly FrameSpec Spider = BuildSpiderFrame();

        public static readonly FrameSpec[] Frames = { Base, Serpent, Spider };

        // ----- shared bone-table pieces -------------------------------------

        private static List<(string, string, Vector3)> CommonUpperBody()
        {
            // The shared torso/head/arm/wing/tail core every placeholder frame carries.
            // Identical names + rest positions across frames (see class invariant).
            return new List<(string, string, Vector3)>
            {
                (PlaceholderRigBuilder.RootBoneName, null, Vector3.zero),
                ("Pelvis", "Root", new Vector3(0f, 0.8f, 0f)),
                ("Spine", "Pelvis", new Vector3(0f, 0.15f, 0f)),
                ("Chest", "Spine", new Vector3(0f, 0.2f, 0f)),
                ("Neck", "Chest", new Vector3(0f, 0.15f, 0f)),
                ("Head", "Neck", new Vector3(0f, 0.1f, 0f)),
                ("Ear.L", "Head", new Vector3(-0.08f, 0.12f, 0f)),
                ("Ear.R", "Head", new Vector3(0.08f, 0.12f, 0f)),
                ("Shoulder.L", "Chest", new Vector3(-0.12f, 0.12f, 0f)),
                ("UpperArm.L", "Shoulder.L", new Vector3(-0.1f, 0f, 0f)),
                ("LowerArm.L", "UpperArm.L", new Vector3(-0.18f, 0f, 0f)),
                ("Hand.L", "LowerArm.L", new Vector3(-0.16f, 0f, 0f)),
                ("Shoulder.R", "Chest", new Vector3(0.12f, 0.12f, 0f)),
                ("UpperArm.R", "Shoulder.R", new Vector3(0.1f, 0f, 0f)),
                ("LowerArm.R", "UpperArm.R", new Vector3(0.18f, 0f, 0f)),
                ("Hand.R", "LowerArm.R", new Vector3(0.16f, 0f, 0f)),
                ("Wing.L", "Chest", new Vector3(-0.1f, 0.05f, 0.08f)),
                ("Wing.R", "Chest", new Vector3(0.1f, 0.05f, 0.08f)),
                ("Tail.0", "Pelvis", new Vector3(0f, -0.05f, 0.1f)),
                ("Tail.1", "Tail.0", new Vector3(0f, -0.02f, 0.15f)),
                ("Tail.2", "Tail.1", new Vector3(0f, 0f, 0.15f))
            };
        }

        private static List<SwaySpec> CommonIdleSways()
        {
            return new List<SwaySpec>
            {
                new SwaySpec("Spine", Vector3.forward, 5f, 0.5f, 0f),
                new SwaySpec("Head", Vector3.right, 6f, 0.5f, 0.2f),
                new SwaySpec("Ear.L", Vector3.right, 12f, 1.5f, 0f),
                new SwaySpec("Ear.R", Vector3.right, 12f, 1.5f, 0.5f),
                new SwaySpec("UpperArm.L", Vector3.right, 20f, 0.5f, 0f),
                new SwaySpec("UpperArm.R", Vector3.right, 20f, 0.5f, 0.5f),
                new SwaySpec("Wing.L", Vector3.forward, 25f, 1f, 0f),
                new SwaySpec("Wing.R", Vector3.forward, -25f, 1f, 0f)
            };
        }

        // ----- base biped ----------------------------------------------------

        private static FrameSpec BuildBaseFrame()
        {
            var bones = CommonUpperBody();
            bones.AddRange(new (string, string, Vector3)[]
            {
                ("UpperLeg.L", "Pelvis", new Vector3(-0.1f, -0.05f, 0f)),
                ("LowerLeg.L", "UpperLeg.L", new Vector3(0f, -0.35f, 0f)),
                ("Foot.L", "LowerLeg.L", new Vector3(0f, -0.35f, 0f)),
                ("UpperLeg.R", "Pelvis", new Vector3(0.1f, -0.05f, 0f)),
                ("LowerLeg.R", "UpperLeg.R", new Vector3(0f, -0.35f, 0f)),
                ("Foot.R", "LowerLeg.R", new Vector3(0f, -0.35f, 0f))
            });

            var idle = CommonIdleSways();
            idle.AddRange(new[]
            {
                new SwaySpec("Tail.0", Vector3.up, 15f, 0.5f, 0f),
                new SwaySpec("Tail.1", Vector3.up, 15f, 0.5f, 0.15f),
                new SwaySpec("Tail.2", Vector3.up, 15f, 0.5f, 0.3f)
            });

            // Gait sign -1 mirrors the swings so the cycle reads forward on the -Z-facing
            // placeholder model (see the original BuildRunClip note).
            const float gait = -1f;
            var run = new[]
            {
                new SwaySpec("UpperLeg.L", Vector3.right, gait * 35f, 1f, 0f),
                new SwaySpec("UpperLeg.R", Vector3.right, gait * 35f, 1f, 0.5f),
                new SwaySpec("LowerLeg.L", Vector3.right, gait * 30f, 1f, 0.25f, gait * -30f),
                new SwaySpec("LowerLeg.R", Vector3.right, gait * 30f, 1f, 0.75f, gait * -30f),
                new SwaySpec("UpperArm.L", Vector3.right, gait * 30f, 1f, 0.5f),
                new SwaySpec("UpperArm.R", Vector3.right, gait * 30f, 1f, 0f),
                new SwaySpec("Spine", Vector3.right, gait * 5f, 2f, 0f)
            };

            return new FrameSpec
            {
                AssetBaseName = "Placeholder",
                SkeletonId = "skeleton.placeholder",
                DisplayName = "Base biped",
                Bones = bones.ToArray(),
                Tier1SocketIds = BaseTier1SocketIds,
                IdleSways = idle.ToArray(),
                RunSways = run,
                Parts = BuildBasePartSpecs(),
                AssemblyId = "assembly.placeholder.a",
                AssemblyAssetName = "PlaceholderAssembly_A",
                DefaultPartIds = new[]
                {
                    "part.head.a", "part.torso.a", "part.arm.l.a", "part.arm.r.a",
                    "part.leg.l.a", "part.leg.r.a", "part.tail.a"
                }
            };
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

        private static FramePartSpec[] BuildBasePartSpecs()
        {
            return new[]
            {
                new FramePartSpec
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
                new FramePartSpec
                {
                    Id = "part.head.b", AssetName = "Head_B", SlotId = "slot.head", IsVariantB = true,
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Head", new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.16f, 0.26f))
                    },
                    ContributedSocketIds = new[] { "socket.hat" }
                },
                new FramePartSpec
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
                new FramePartSpec
                {
                    Id = "part.torso.b", AssetName = "Torso_B", SlotId = "slot.torso", IsVariantB = true,
                    Boxes = new[]
                    {
                        new PlaceholderMeshBuilder.BoxSpec("Spine", new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.2f, 0.26f)),
                        new PlaceholderMeshBuilder.BoxSpec("Chest", new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.24f, 0.28f))
                    }
                },
                new FramePartSpec { Id = "part.arm.l.a", AssetName = "ArmL_A", SlotId = "slot.arm.l", Boxes = ArmBoxes(-1f, 1f) },
                new FramePartSpec { Id = "part.arm.l.b", AssetName = "ArmL_B", SlotId = "slot.arm.l", IsVariantB = true, Boxes = ArmBoxes(-1f, 1.6f) },
                new FramePartSpec { Id = "part.arm.r.a", AssetName = "ArmR_A", SlotId = "slot.arm.r", Boxes = ArmBoxes(1f, 1f) },
                new FramePartSpec { Id = "part.arm.r.b", AssetName = "ArmR_B", SlotId = "slot.arm.r", IsVariantB = true, Boxes = ArmBoxes(1f, 1.6f) },
                new FramePartSpec { Id = "part.leg.l.a", AssetName = "LegL_A", SlotId = "slot.leg.l", Boxes = LegBoxes(-1f, 1f) },
                new FramePartSpec { Id = "part.leg.l.b", AssetName = "LegL_B", SlotId = "slot.leg.l", IsVariantB = true, Boxes = LegBoxes(-1f, 1.6f) },
                new FramePartSpec { Id = "part.leg.r.a", AssetName = "LegR_A", SlotId = "slot.leg.r", Boxes = LegBoxes(1f, 1f) },
                new FramePartSpec { Id = "part.leg.r.b", AssetName = "LegR_B", SlotId = "slot.leg.r", IsVariantB = true, Boxes = LegBoxes(1f, 1.6f) },
                new FramePartSpec
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

        // ----- legless serpent ------------------------------------------------

        private static FrameSpec BuildSerpentFrame()
        {
            var bones = CommonUpperBody();
            bones.AddRange(new (string, string, Vector3)[]
            {
                ("Tail.3", "Tail.2", new Vector3(0f, 0f, 0.15f)),
                ("Tail.4", "Tail.3", new Vector3(0f, 0f, 0.15f)),
                ("Tail.5", "Tail.4", new Vector3(0f, 0f, 0.15f))
            });

            var idle = CommonIdleSways();
            for (var i = 0; i <= 5; i++)
            {
                idle.Add(new SwaySpec($"Tail.{i}", Vector3.up, 10f + i * 2.5f, 0.5f, i * 0.1f));
            }

            var run = new List<SwaySpec>
            {
                // The slither: a lateral wave travelling down the spine-tail chain.
                new SwaySpec("Spine", Vector3.up, 10f, 1f, 0.9f),
                new SwaySpec("Head", Vector3.up, 6f, 1f, 0.8f),
                new SwaySpec("UpperArm.L", Vector3.right, -20f, 1f, 0f),
                new SwaySpec("UpperArm.R", Vector3.right, -20f, 1f, 0.5f)
            };
            for (var i = 0; i <= 5; i++)
            {
                run.Add(new SwaySpec($"Tail.{i}", Vector3.up, 25f + i * 3f, 1f, i * 0.15f));
            }

            return new FrameSpec
            {
                AssetBaseName = "Serpent",
                SkeletonId = "skeleton.serpent",
                DisplayName = "Serpent frame",
                Bones = bones.ToArray(),
                Tier1SocketIds = LeglessTier1SocketIds,
                IdleSways = idle.ToArray(),
                RunSways = run.ToArray(),
                Parts = new[]
                {
                    new FramePartSpec
                    {
                        Id = "part.spine.serpent", AssetName = "SpineSerpent", SlotId = "slot.tail", IsVariantB = true,
                        DisplayName = "Serpent Spine",
                        GovernsBodyPlan = true, BodyPlanPriority = 20,
                        TraitAffinities = new[] { ("toxic", 0.7f), ("water", 0.5f), ("sharp", 0.3f) },
                        Rarity = MutationRarity.Rare,
                        Boxes = new[]
                        {
                            new PlaceholderMeshBuilder.BoxSpec("Pelvis", new Vector3(0f, -0.06f, 0.04f), new Vector3(0.24f, 0.16f, 0.26f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.0", new Vector3(0f, 0f, 0.07f), new Vector3(0.16f, 0.16f, 0.2f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.1", new Vector3(0f, 0f, 0.07f), new Vector3(0.15f, 0.15f, 0.19f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.2", new Vector3(0f, 0f, 0.07f), new Vector3(0.13f, 0.13f, 0.18f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.3", new Vector3(0f, 0f, 0.07f), new Vector3(0.11f, 0.11f, 0.18f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.4", new Vector3(0f, 0f, 0.07f), new Vector3(0.09f, 0.09f, 0.17f)),
                            new PlaceholderMeshBuilder.BoxSpec("Tail.5", new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0.06f, 0.16f))
                        }
                    }
                },
                AssemblyId = "assembly.placeholder.serpent",
                AssemblyAssetName = "PlaceholderAssembly_Serpent",
                DefaultPartIds = new[]
                {
                    "part.head.a", "part.torso.a", "part.arm.l.a", "part.arm.r.a", "part.spine.serpent"
                }
            };
        }

        // ----- spider-legged --------------------------------------------------

        private const int SpiderLegCount = 8;
        private const float SpiderHipRadius = 0.15f;
        private const float SpiderTipRadius = 0.28f;
        private const float SpiderTipDrop = 0.62f;

        private static FrameSpec BuildSpiderFrame()
        {
            var bones = CommonUpperBody();
            for (var k = 0; k < SpiderLegCount; k++)
            {
                var angle = k * Mathf.PI * 2f / SpiderLegCount;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                bones.Add(($"SpiderHip.{k}", "Pelvis", new Vector3(SpiderHipRadius * sin, -0.05f, SpiderHipRadius * cos)));
                bones.Add(($"SpiderTip.{k}", $"SpiderHip.{k}", new Vector3(SpiderTipRadius * sin, -SpiderTipDrop, SpiderTipRadius * cos)));
            }

            var idle = CommonIdleSways();
            idle.Add(new SwaySpec("Tail.0", Vector3.up, 12f, 0.5f, 0f));
            idle.Add(new SwaySpec("Tail.1", Vector3.up, 12f, 0.5f, 0.15f));
            idle.Add(new SwaySpec("Tail.2", Vector3.up, 12f, 0.5f, 0.3f));
            for (var k = 0; k < SpiderLegCount; k++)
            {
                idle.Add(new SwaySpec($"SpiderHip.{k}", LegSwingAxis(k), 5f, 0.5f, k / (float)SpiderLegCount));
            }

            var run = new List<SwaySpec>
            {
                new SwaySpec("Spine", Vector3.right, -4f, 2f, 0f)
            };
            for (var k = 0; k < SpiderLegCount; k++)
            {
                // Tripod-ish scuttle: alternate legs half a cycle out of phase, with a small
                // per-leg stagger so the wave reads organic.
                var phase = (k % 2) * 0.5f + k * 0.05f;
                run.Add(new SwaySpec($"SpiderHip.{k}", LegSwingAxis(k), 18f, 2f, phase));
            }

            var spiderBoxes = new List<PlaceholderMeshBuilder.BoxSpec>
            {
                new PlaceholderMeshBuilder.BoxSpec("Pelvis", new Vector3(0f, -0.06f, 0f), new Vector3(0.3f, 0.12f, 0.3f))
            };
            for (var k = 0; k < SpiderLegCount; k++)
            {
                var angle = k * Mathf.PI * 2f / SpiderLegCount;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                spiderBoxes.Add(new PlaceholderMeshBuilder.BoxSpec(
                    $"SpiderHip.{k}",
                    new Vector3(0.14f * sin, -0.3f, 0.14f * cos),
                    new Vector3(0.07f, 0.66f, 0.07f)));
                spiderBoxes.Add(new PlaceholderMeshBuilder.BoxSpec(
                    $"SpiderTip.{k}",
                    new Vector3(0f, -0.04f, 0f),
                    new Vector3(0.05f, 0.1f, 0.05f)));
            }

            return new FrameSpec
            {
                AssetBaseName = "Spider",
                SkeletonId = "skeleton.spider",
                DisplayName = "Spider frame",
                Bones = bones.ToArray(),
                Tier1SocketIds = LeglessTier1SocketIds,
                IdleSways = idle.ToArray(),
                RunSways = run.ToArray(),
                Parts = new[]
                {
                    new FramePartSpec
                    {
                        Id = "part.legs.spider", AssetName = "LegsSpider", SlotId = "slot.legs.cluster", IsVariantB = true,
                        DisplayName = "Spider Leg Cluster",
                        GovernsBodyPlan = true, BodyPlanPriority = 10,
                        TraitAffinities = new[] { ("chitin", 0.8f), ("toxic", 0.5f), ("sharp", 0.4f) },
                        Rarity = MutationRarity.Rare,
                        Boxes = spiderBoxes.ToArray()
                    }
                },
                AssemblyId = "assembly.placeholder.spider",
                AssemblyAssetName = "PlaceholderAssembly_Spider",
                DefaultPartIds = new[]
                {
                    "part.head.a", "part.torso.a", "part.arm.l.a", "part.arm.r.a", "part.tail.a", "part.legs.spider"
                }
            };
        }

        /// <summary>Horizontal axis tangent to leg k's radial direction, so swinging around it
        /// steps the leg fore/aft along its own facing.</summary>
        private static Vector3 LegSwingAxis(int k)
        {
            var angle = k * Mathf.PI * 2f / SpiderLegCount;
            return new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
        }
    }
}
