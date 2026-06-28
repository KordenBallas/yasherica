using Narrative.Encounter;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class KeywordHighlightFormatterTests
    {
        private const string Hex = "FFCC55";

        [Test]
        public void WrapsSingleMark_InColorSpan_AndStripsMarkers()
        {
            Assert.AreEqual(
                "Hi <color=#FFCC55>Mira</color>!",
                KeywordHighlightFormatter.ToRichText("Hi [[Mira]]!", Hex));
        }

        [Test]
        public void WrapsEveryMark_WhenMultiple()
        {
            Assert.AreEqual(
                "<color=#FFCC55>Mira</color> knows <color=#FFCC55>Garrick</color>",
                KeywordHighlightFormatter.ToRichText("[[Mira]] knows [[Garrick]]", Hex));
        }

        [Test]
        public void LeavesUnmarkedTextUntouched()
        {
            Assert.AreEqual("Raiders cleaned out my barn.",
                KeywordHighlightFormatter.ToRichText("Raiders cleaned out my barn.", Hex));
        }

        [Test]
        public void EmptyMark_YieldsNothing()
        {
            Assert.AreEqual("ab", KeywordHighlightFormatter.ToRichText("a[[]]b", Hex));
        }

        [Test]
        public void UnterminatedMark_IsEmittedVerbatim()
        {
            Assert.AreEqual("a [[b c", KeywordHighlightFormatter.ToRichText("a [[b c", Hex));
        }

        [Test]
        public void NullOrEmptyColor_StripsMarkersWithoutColoring()
        {
            Assert.AreEqual("Hi Mira!", KeywordHighlightFormatter.ToRichText("Hi [[Mira]]!", null));
            Assert.AreEqual("Hi Mira!", KeywordHighlightFormatter.ToRichText("Hi [[Mira]]!", ""));
        }

        [Test]
        public void LeadingHashOnColor_IsAccepted()
        {
            Assert.AreEqual(
                "<color=#FFCC55>Mira</color>",
                KeywordHighlightFormatter.ToRichText("[[Mira]]", "#FFCC55"));
        }

        [Test]
        public void NullOrEmptyInput_ReturnedAsIs()
        {
            Assert.IsNull(KeywordHighlightFormatter.ToRichText(null, Hex));
            Assert.AreEqual("", KeywordHighlightFormatter.ToRichText("", Hex));
        }
    }
}
