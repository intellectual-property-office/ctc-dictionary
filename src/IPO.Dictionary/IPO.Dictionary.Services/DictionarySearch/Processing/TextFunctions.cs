using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace IPO.Dictionary.Services.DictionarySearch.Processing
{
    public static class TextFunctions
    {
        public static bool @TextContainsValue(string text, DictionaryValue value) => value.Type switch
        {
            DictionaryValueType.Word => TextContainsWord(text, value.Value),
            DictionaryValueType.Phrase => TextContainsPhrase(text, value.Value),
            _ => throw new NotImplementedException()
        };

        public static bool TextContainsWord(string text, string word)
        {
            word = word.ToLowerInvariant();
            var regex = new Regex(@"\s", RegexOptions.Multiline);
            var wordsInText = regex.Split(text.ToLowerInvariant()).Where(o => !string.IsNullOrWhiteSpace(o))
                                    .Select(o => RemovePunctuations(o.Trim()));

            return wordsInText.Contains(word);
        }


        public static bool TextContainsPhrase(string text, string phrase)
        {
            var phraseRegexString = phrase.Trim().ToLowerInvariant().Replace(" ", "\\s");

            var phraseRegex = new Regex($"([^a-zA-Z0-9]{phraseRegexString}[^a-zA-Z0-9])|(^{phraseRegexString})|(([^a-zA-Z0-9]{phraseRegexString}(\\s|$)))", RegexOptions.Multiline);

            return phraseRegex.Match(text.ToLowerInvariant()).Success;
        }

        public static string RemovePunctuations(string word)
        {
            if (word.Length == 0)
                return word;

            if (char.IsPunctuation(word[0]))
            {
                word = word.Remove(0, 1);
            }

            if (word.Length > 0 && char.IsPunctuation(word[word.Length - 1]))
            {
                word = word.Remove(word.Length - 1, 1);
            }

            return word;
        }
    }
}