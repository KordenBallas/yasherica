using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RecipeBookTests
    {
        private static RecipeBook BuildBook(params RecipeData[] recipes)
        {
            return new RecipeBook(new List<RecipeData>(recipes));
        }

        private static RecipeData Recipe(string output, params string[] inputs)
        {
            return new RecipeData(inputs, output);
        }

        [Test]
        public void TryMatch_ExactInputs_ReturnsOutput()
        {
            var book = BuildBook(Recipe("snake", "fire", "water"));

            Assert.IsTrue(book.TryMatch(new[] { "fire", "water" }, out string output));
            Assert.AreEqual("snake", output);
        }

        [Test]
        public void TryMatch_IsOrderIndependent()
        {
            var book = BuildBook(Recipe("snake", "fire", "water"));

            Assert.IsTrue(book.TryMatch(new[] { "water", "fire" }, out string output));
            Assert.AreEqual("snake", output);
        }

        [Test]
        public void TryMatch_RespectsDuplicateCounts()
        {
            var book = BuildBook(Recipe("rock", "fire", "fire"));

            Assert.IsTrue(book.TryMatch(new[] { "fire", "fire" }, out _));
            Assert.IsFalse(book.TryMatch(new[] { "fire" }, out _));
            Assert.IsFalse(book.TryMatch(new[] { "fire", "fire", "fire" }, out _));
        }

        [Test]
        public void TryMatch_NoMatchingRecipe_ReturnsFalse()
        {
            var book = BuildBook(Recipe("snake", "fire", "water"));

            Assert.IsFalse(book.TryMatch(new[] { "rock", "water" }, out string output));
            Assert.IsNull(output);
        }

        [Test]
        public void TryMatch_MultipleRecipes_Disambiguates()
        {
            var book = BuildBook(
                Recipe("snake", "fire", "water"),
                Recipe("lizard", "fire", "rock"),
                Recipe("virus", "water", "bacteria"));

            Assert.IsTrue(book.TryMatch(new[] { "rock", "fire" }, out string output));
            Assert.AreEqual("lizard", output);
        }

        [Test]
        public void TryMatch_EmptyInputs_ReturnsFalse()
        {
            var book = BuildBook(Recipe("snake", "fire", "water"));

            Assert.IsFalse(book.TryMatch(new string[0], out _));
            Assert.IsFalse(book.TryMatch(null, out _));
        }

        [Test]
        public void Constructor_DuplicateRecipes_FirstOneWins()
        {
            var book = BuildBook(
                Recipe("snake", "fire", "water"),
                Recipe("virus", "water", "fire"));

            Assert.IsTrue(book.TryMatch(new[] { "fire", "water" }, out string output));
            Assert.AreEqual("snake", output);
        }
    }
}
