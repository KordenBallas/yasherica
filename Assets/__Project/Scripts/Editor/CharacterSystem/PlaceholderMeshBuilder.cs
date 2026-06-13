using System.Collections.Generic;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Generates skinned low-poly part meshes for the placeholder rig: one axis-aligned
    /// box per covered bone, every vertex fully weighted to its bone. Vertices are
    /// authored in rig space and bindposes are taken straight from the bind-pose rig
    /// (rig at origin, identity rotations), which is the contract that makes
    /// remap-by-name swapping work.
    /// </summary>
    public static class PlaceholderMeshBuilder
    {
        public readonly struct BoxSpec
        {
            public string BoneName { get; }
            /// <summary>World-space offset of the box center from the bone position (bind pose).</summary>
            public Vector3 CenterOffset { get; }
            public Vector3 Size { get; }

            public BoxSpec(string boneName, Vector3 centerOffset, Vector3 size)
            {
                BoneName = boneName;
                CenterOffset = centerOffset;
                Size = size;
            }
        }

        /// <summary>
        /// Builds the skinned mesh. <paramref name="orderedBoneNames"/> returns the bone
        /// names in exactly the mesh's bone-index order; PartDefinition must store this
        /// list unchanged.
        /// </summary>
        public static Mesh BuildPartMesh(
            string meshName,
            IReadOnlyList<BoxSpec> boxes,
            IReadOnlyDictionary<string, Transform> bonesByName,
            out List<string> orderedBoneNames)
        {
            orderedBoneNames = new List<string>(boxes.Count);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var boneWeights = new List<BoneWeight>();
            var bindposes = new List<Matrix4x4>();

            for (var boneIndex = 0; boneIndex < boxes.Count; boneIndex++)
            {
                var box = boxes[boneIndex];
                var bone = bonesByName[box.BoneName];
                orderedBoneNames.Add(box.BoneName);

                // Rig root sits at the origin with identity rotation, so worldToLocalMatrix
                // alone is the correct bindpose (the * root.localToWorldMatrix term is identity).
                bindposes.Add(bone.worldToLocalMatrix);

                AppendBox(vertices, triangles, bone.position + box.CenterOffset, box.Size);

                var weight = new BoneWeight { boneIndex0 = boneIndex, weight0 = 1f };
                for (var i = 0; i < 24; i++)
                {
                    boneWeights.Add(weight);
                }
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                boneWeights = boneWeights.ToArray(),
                bindposes = bindposes.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Appends a 24-vertex box (4 verts per face for hard edges) centered at <paramref name="center"/>.</summary>
        private static void AppendBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            var extents = size * 0.5f;

            // 8 corner positions.
            var corners = new Vector3[8];
            for (var i = 0; i < 8; i++)
            {
                corners[i] = center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);
            }

            // Each face as 4 unique vertices (corner indices in quad order).
            int[][] faces =
            {
                new[] { 0, 2, 3, 1 }, // -z front
                new[] { 5, 7, 6, 4 }, // +z back
                new[] { 4, 6, 2, 0 }, // -x left
                new[] { 1, 3, 7, 5 }, // +x right
                new[] { 2, 6, 7, 3 }, // +y top
                new[] { 4, 0, 1, 5 }  // -y bottom
            };

            foreach (var face in faces)
            {
                var faceBase = vertices.Count;
                foreach (var cornerIndex in face)
                {
                    vertices.Add(corners[cornerIndex]);
                }

                triangles.Add(faceBase);
                triangles.Add(faceBase + 1);
                triangles.Add(faceBase + 2);
                triangles.Add(faceBase);
                triangles.Add(faceBase + 2);
                triangles.Add(faceBase + 3);
            }
        }
    }
}
