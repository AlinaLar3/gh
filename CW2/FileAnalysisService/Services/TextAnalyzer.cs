using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileAnalysisService.Services
{
    public class AnalysisResultData
    {
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int SymbolCount { get; set; }
        public Dictionary<string, int> WordFrequencies { get; set; }
    }

    public class TextAnalyzer
    {
        public AnalysisResultData Analyze(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new AnalysisResultData
                {
                    ParagraphCount = 0,
                    WordCount = 0,
                    SymbolCount = 0,
                    WordFrequencies = new Dictionary<string, int>()
                };
            }

            int symbolCount = text.Length;

            int paragraphCount = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (paragraphCount == 0 && !string.IsNullOrWhiteSpace(text)) paragraphCount = 1;

            var wordFrequencies = new Dictionary<string, int>();
            var words = Regex.Split(text.ToLowerInvariant(), @"\W+")
                               .Where(word => !string.IsNullOrWhiteSpace(word) && word.Length > 1);

            foreach (var word in words)
            {
                if (wordFrequencies.ContainsKey(word))
                {
                    wordFrequencies[word]++;
                }
                else
                {
                    wordFrequencies[word] = 1;
                }
            }
            int wordCount = wordFrequencies.Sum(wf => wf.Value);

            return new AnalysisResultData
            {
                SymbolCount = symbolCount,
                ParagraphCount = paragraphCount,
                WordCount = wordCount,
                WordFrequencies = wordFrequencies.OrderByDescending(wf => wf.Value).ToDictionary(wf => wf.Key, wf => wf.Value)
            };
        }
    }
}