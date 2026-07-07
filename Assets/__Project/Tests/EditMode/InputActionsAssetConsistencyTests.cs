using System.IO;
using GameInput.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Drift guard between the pure-C# binding catalog and the runtime actions asset: every control
    /// path the catalog declares must appear in <c>GameActions.inputactions</c>. Text containment is
    /// deliberately a heuristic (no JSON parser in the pure-C# test closure) — it catches the real
    /// failure mode, a binding edited or dropped on one side only.
    /// </summary>
    [TestFixture]
    public class InputActionsAssetConsistencyTests
    {
        private const string AssetRelativePath = "Assets/__Project/Resources/Input/GameActions.inputactions";

        [Test]
        public void EveryCatalogControlPath_AppearsInTheActionsAsset()
        {
            string assetText = File.ReadAllText(LocateAssetPath());
            var catalog = new InputBindingCatalog();

            foreach (var entry in catalog.Entries)
            {
                if (!entry.HasControlPath)
                {
                    continue;
                }

                Assert.IsTrue(assetText.Contains(entry.ControlPath),
                    $"{entry.Action} × {entry.Source} declares '{entry.ControlPath}' but GameActions.inputactions does not contain it");
            }
        }

        /// <summary>Walks up from the working directory to the project root, so the test finds the
        /// asset both under the Unity test runner (CWD = project root) and the CLI dotnet runner.</summary>
        private static string LocateAssetPath()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, AssetRelativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate {AssetRelativePath} above {Directory.GetCurrentDirectory()}");
        }
    }
}
