using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Recipe lookup keyed by a canonical (sorted) form of the input multiset,
    /// so [fire, water] and [water, fire] match the same recipe while
    /// [fire, fire] stays distinct from [fire].
    /// </summary>
    public class RecipeBook : IRecipeBook
    {
        private const char KeySeparator = '\n';

        private readonly Dictionary<string, string> _outputByCanonicalInputs =
            new Dictionary<string, string>();

        public RecipeBook(IReadOnlyList<RecipeData> recipes)
        {
            if (recipes == null)
            {
                throw new System.ArgumentNullException(nameof(recipes));
            }

            foreach (var recipe in recipes)
            {
                // Later duplicates silently lose: first authored recipe for a combination wins.
                string key = BuildCanonicalKey(recipe.InputDefinitionIds);
                if (!_outputByCanonicalInputs.ContainsKey(key))
                {
                    _outputByCanonicalInputs.Add(key, recipe.OutputDefinitionId);
                }
            }
        }

        public bool TryMatch(IReadOnlyList<string> inputDefinitionIds, out string outputDefinitionId)
        {
            if (inputDefinitionIds == null || inputDefinitionIds.Count == 0)
            {
                outputDefinitionId = null;
                return false;
            }

            return _outputByCanonicalInputs.TryGetValue(
                BuildCanonicalKey(inputDefinitionIds), out outputDefinitionId);
        }

        private static string BuildCanonicalKey(IReadOnlyList<string> definitionIds)
        {
            var sorted = new List<string>(definitionIds);
            sorted.Sort(System.StringComparer.Ordinal);
            return string.Join(KeySeparator.ToString(), sorted);
        }
    }
}
