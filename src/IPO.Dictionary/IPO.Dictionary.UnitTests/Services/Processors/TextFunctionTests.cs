using AwesomeAssertions;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.Dictionary.Services.DictionarySearch.Processing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Services.Processors
{
    [TestClass]
    public class TextFunctionTests
    {
        [TestMethod]
        public void TextContainsValueWhenTextContainsWordReturnsTrue()
        {
            // Arrange
            var word = new DictionaryValue("marker_here", DictionaryValueType.Word);
            var testTextLines = new string[] { "This is the first line", $"This is the {word.Value} second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), word);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void TextContainsValueWhenTextNotContainsWordAsConnectedReturnsFalse()
        {
            // Arrange
            var word = new DictionaryValue("marker_here", DictionaryValueType.Word);
            var testTextLines = new string[] { "This is the first line", $"This is the{word.Value} second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), word);

            // Assert
            result.Should().BeFalse();
        }


        [TestMethod]
        public void TextContainsValueWhenTextNotContainsWordReturnsFalse()
        {
            // Arrange
            var word = new DictionaryValue("marker_here", DictionaryValueType.Word);
            var testTextLines = new string[] { "This is the first line", $"This is the second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), word);

            // Assert
            result.Should().BeFalse();
        }



        [TestMethod]
        public void TextContainsValueWhenTextContainsPhraseReturnsTrue()
        {
            // Arrange
            var phrase = new DictionaryValue("this is a marker.", DictionaryValueType.Phrase);
            var testTextLines = new string[] { "This is the first line", $"This is the {phrase.Value} second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), phrase);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void TextContainsValueWhenTextNotContainsPhraseAsConnectedReturnsFalse()
        {
            // Arrange
            var phrase = new DictionaryValue("This is a marker.", DictionaryValueType.Phrase);
            var testTextLines = new string[] { "This is the first line", $"This is the{phrase.Value} second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), phrase);

            // Assert
            result.Should().BeFalse();
        }


        [TestMethod]
        public void TextContainsValueWhenTextNotContainsPhraseReturnsFalse()
        {
            // Arrange
            var phrase = new DictionaryValue("This is a marker.", DictionaryValueType.Phrase);
            var testTextLines = new string[] { "This is the first line", $"This is the second line", "This is the third line" };

            // Act
            var result = TextFunctions.TextContainsValue(string.Join(Environment.NewLine, testTextLines), phrase);

            // Assert
            result.Should().BeFalse();
        }

        [DataRow("This is a test sentence for unit tests.", "a test sentence", true)]
        [DataRow("This is a test sentence for unit tests.", " a test sentence", true)]
        [DataRow("This is a test sentence for unit tests.", "a test sentence ", true)]
        [DataRow("This is a test sentence\nfor unit tests.", "a test sentence", true)]
        [DataRow("a test sentence for unit tests.", "a test sentence", true)]
        [DataRow(" a test sentence for unit tests.", "a test sentence", true)]
        [DataRow("ta test sentence for unit tests.", "a test sentence", false)]
        [DataRow(".a test sentence for unit tests.", "a test sentence", true)]
        [DataRow("This isa test sentence for unit tests.a test sentence", "a test sentence ", true)]
        [DataRow("This isa test sentence\nfor unit tests.a test sentence", "a test sentence", true)]
        [DataRow("This isa test sentence\nfor unit tests.a test sentencel", "a test sentence", false)]
        [DataRow("a test sentence", "a test sentence", true)]
        [DataRow("a test sentence.", "a test sentence", true)]
        [DataRow(".a test sentence", "a test sentence", true)]
        [DataRow(".a-test sentence", "a test sentence", false)]
        [TestMethod]
        public void TextContainsPhraseReturnsExpectedResult(string text, string phrase, bool expectedResult)
        {
            // Arrange 

            // Act
            var result = TextFunctions.TextContainsPhrase(text, phrase);

            // Assert
            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void RemovePunctuationsWhenWordLengthIsZeroReturnsWord()
        {
            // Arrange
            var word = "";

            // Act
            var result = TextFunctions.RemovePunctuations(word);

            // Assert
            result.Should().Be(word);
        }

        [DataRow("(test","test")]
        [DataRow("test)", "test")]
        [DataRow("(test)", "test")]
        [DataRow("(te)st)", "te)st")]
        [DataRow("te)st)", "te)st")]
        [DataRow("(te)st", "te)st")]
        [DataRow("te?s)t", "te?s)t")]
        [DataRow("?te?s)t?", "te?s)t")]
        [TestMethod]
        public void RemovePunctuationsWhenContainsPunctuationsReturnsWordWithoutPunctuations(string word, string expectedResult)
        {
            // Arrange 

            // Act
            var result = TextFunctions.RemovePunctuations(word);

            // Assert
            result.Should().Be(expectedResult);
        }
    }
}
