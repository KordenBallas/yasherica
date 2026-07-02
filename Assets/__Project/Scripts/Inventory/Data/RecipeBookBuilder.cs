using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Converts authored RecipeDefinition assets into the pure-C# RecipeBook.
    /// Malformed recipes (missing output, empty/invalid inputs) are skipped with a
    /// warning instead of breaking startup.
    /// </summary>
    public class RecipeBookBuilder
    {
        private readonly IGameLogger _logger;

        public RecipeBookBuilder(IGameLogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public RecipeBook Build(IReadOnlyList<RecipeDefinition> definitions)
        {
            var recipes = new List<RecipeData>();

            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (TryConvert(definition, out var recipe))
                    {
                        recipes.Add(recipe);
                    }
                }
            }

            return new RecipeBook(recipes);
        }

        private bool TryConvert(RecipeDefinition definition, out RecipeData recipe)
        {
            recipe = null;

            if (definition == null)
            {
                return false;
            }

            if (definition.Output == null || string.IsNullOrEmpty(definition.Output.Id))
            {
                _logger.Warning(LogCategory.Inventory,$"[RecipeBookBuilder] Recipe '{definition.name}' skipped: missing output artifact.");
                return false;
            }

            if (definition.Inputs == null || definition.Inputs.Count == 0)
            {
                _logger.Warning(LogCategory.Inventory,$"[RecipeBookBuilder] Recipe '{definition.name}' skipped: no input artifacts.");
                return false;
            }

            var inputIds = new List<string>(definition.Inputs.Count);
            foreach (var input in definition.Inputs)
            {
                if (input == null || string.IsNullOrEmpty(input.Id))
                {
                    _logger.Warning(LogCategory.Inventory,$"[RecipeBookBuilder] Recipe '{definition.name}' skipped: invalid input artifact reference.");
                    return false;
                }

                inputIds.Add(input.Id);
            }

            recipe = new RecipeData(inputIds, definition.Output.Id);
            return true;
        }
    }
}
