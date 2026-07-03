using System.Runtime.CompilerServices;
using Combat.Config;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Builds a flat-top HexDirectionConfig for tests. Uses ScriptableObject.CreateInstance
    /// inside Unity; falls back to an uninitialized managed shell when the suite runs on the
    /// plain .NET runtime (no native Unity engine), where only the offsets field is read.
    /// </summary>
    internal static class TestHexDirectionConfig
    {
        public static readonly HexDirectionOffset[] FlatTopOffsets =
        {
            new HexDirectionOffset(HexDirection.E,  new Vector2Int(+1,  0)),
            new HexDirectionOffset(HexDirection.SE, new Vector2Int( 0, +1)),
            new HexDirectionOffset(HexDirection.SW, new Vector2Int(-1, +1)),
            new HexDirectionOffset(HexDirection.W,  new Vector2Int(-1,  0)),
            new HexDirectionOffset(HexDirection.NW, new Vector2Int( 0, -1)),
            new HexDirectionOffset(HexDirection.NE, new Vector2Int(+1, -1)),
        };

        public static HexDirectionConfig CreateFlatTop()
        {
            HexDirectionConfig config;
            try
            {
                config = ScriptableObject.CreateInstance<HexDirectionConfig>();
            }
            catch (System.Security.SecurityException)
            {
                config = (HexDirectionConfig)RuntimeHelpers.GetUninitializedObject(typeof(HexDirectionConfig));
            }

            config.directionOffsets = FlatTopOffsets;
            return config;
        }

        public static void Destroy(HexDirectionConfig config)
        {
            if (config == null) return;
            try
            {
                Object.DestroyImmediate(config);
            }
            catch (System.Security.SecurityException)
            {
                // Plain .NET runtime: nothing native to destroy.
            }
        }
    }
}
