using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The E1 clean-swap invariant as a living guard (dressing-kit brief FR4): no store-pack asset
    /// may be referenced from any scene, prefab, or asset under <c>Assets/__Project</c> EXCEPT the
    /// demo-kit quarantine (<c>Resources/World/Dressing/Demo/</c>). Deleting the demo folder must
    /// leave the project with no dangling pack references — that is what makes the demo →
    /// production swap a one-field change and keeps store content out of a distributed build.
    /// </summary>
    [TestFixture]
    public class DemoPackQuarantineTests
    {
        private static readonly string[] PackRoots =
        {
            "RPGPP_LT",
            "Tiny Teacup Studio"
        };

        private const string QuarantineRelativePath = "Resources/World/Dressing/Demo";

        private static readonly Regex GuidLine = new Regex(
            @"^guid:\s*([0-9a-f]{32})", RegexOptions.Compiled | RegexOptions.Multiline);

        [Test]
        public void PackAssets_AreReferencedOnlyFromTheDemoQuarantine()
        {
            string assetsRoot = Application.dataPath;
            var packGuids = HarvestPackGuids(assetsRoot);
            if (packGuids.Count == 0)
            {
                Assert.Ignore("Store packs are not imported — nothing to guard.");
            }

            string projectRoot = Path.Combine(assetsRoot, "__Project");
            string quarantine = Path.Combine(projectRoot, QuarantineRelativePath)
                .Replace('\\', '/');

            var offenders = new List<string>();
            foreach (string file in EnumerateSerializedFiles(projectRoot))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.StartsWith(quarantine))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                foreach (string guid in packGuids)
                {
                    if (text.Contains(guid))
                    {
                        offenders.Add($"{normalized} → {guid}");
                        break;
                    }
                }
            }

            Assert.IsEmpty(offenders,
                "Store-pack assets must be reachable only through demo-kit assets:\n"
                + string.Join("\n", offenders));
        }

        private static HashSet<string> HarvestPackGuids(string assetsRoot)
        {
            var guids = new HashSet<string>();
            foreach (string packRoot in PackRoots)
            {
                string root = Path.Combine(assetsRoot, packRoot);
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string meta in Directory.EnumerateFiles(root, "*.meta", SearchOption.AllDirectories))
                {
                    var match = GuidLine.Match(File.ReadAllText(meta));
                    if (match.Success)
                    {
                        guids.Add(match.Groups[1].Value);
                    }
                }
            }

            return guids;
        }

        private static IEnumerable<string> EnumerateSerializedFiles(string projectRoot)
        {
            foreach (string pattern in new[] { "*.unity", "*.prefab", "*.asset", "*.mat" })
            {
                foreach (string file in Directory.EnumerateFiles(projectRoot, pattern, SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }
    }
}
