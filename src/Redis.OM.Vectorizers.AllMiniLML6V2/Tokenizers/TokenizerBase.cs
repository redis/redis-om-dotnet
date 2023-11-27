using System.Text.RegularExpressions;

namespace Redis.OM.Vectorizers.AllMiniLML6V2.Tokenizers;

internal abstract class TokenizerBase
{
    protected readonly string[] _vocabulary;
    protected readonly Dictionary<string, int> _vocabularyDict;

    public TokenizerBase(string[] vocabulary)
    {
        _vocabulary = vocabulary;
        _vocabularyDict = new Dictionary<string, int>();

        for (int i = 0; i < _vocabulary.Length; i++)
        {
            _vocabularyDict[_vocabulary[i]] = i;
        }
    }

    public List<(string Token, int VocabularyIndex, long SegmentIndex)> Tokenize(params string[] texts)
    {
        IEnumerable<string> tokens = new[] { Tokens.Classification };

        foreach (var text in texts)
        {
            tokens = tokens.Concat(TokenizeSentence(text));
            tokens = tokens.Concat(new[] { Tokens.Separation });
        }

        var tokenAndIndex = tokens.SelectMany(TokenizeSubWords).ToArray();

        var segmentIndexes = SegmentIndex(tokenAndIndex);
        return tokenAndIndex.Zip(segmentIndexes, (tokenIndex, segmentIndex) => (tokenIndex.Token, tokenIndex.VocabularyIndex, segmentIndex)).ToList();
    }

    public List<(long InputIds, long TokenTypeIds, long AttentionMask)> Encode(int sequenceLength, params string[] texts)
    {
        var tokens = Tokenize(texts);

        var padding = Enumerable.Repeat(0L, sequenceLength - tokens.Count).ToArray();
        var tokenIndexes = tokens.Select(token => (long)token.VocabularyIndex).Concat(padding).ToArray();
        var segmentIndexes = tokens.Select(token => token.SegmentIndex).Concat(padding).ToArray();
        var inputMask = tokens.Select(o => 1L).Concat(padding).ToArray();

        var output = tokenIndexes.Zip(segmentIndexes, Tuple.Create)
            .Zip(inputMask, (t, z) => Tuple.Create(t.Item1, t.Item2, z));

        return output.Select(x => (InputIds: x.Item1, TokenTypeIds: x.Item2, AttentionMask: x.Item3)).ToList();
    }

    private IEnumerable<long> SegmentIndex(IEnumerable<(string token, int index)> tokens)
    {
        var segmentIndex = 0;
        var segmentIndexes = new List<long>();
        
        foreach (var (token, index) in tokens)
        {
            segmentIndexes.Add(segmentIndex);

            if (token == Tokens.Separation)
            {
                segmentIndex++;
            }
        }

        return segmentIndexes;
    }

    private IEnumerable<(string Token, int VocabularyIndex)> TokenizeSubWords(string word)
    {
        if (_vocabularyDict.ContainsKey(word))
        {
            return new (string, int)[] { (word, _vocabularyDict[word]) };
        }

        var tokens = new List<(string, int)>();
        var remaining = word;

        while (!string.IsNullOrEmpty(remaining) && remaining.Length > 2)
        {
            string? prefix = null;
            int subWordLength = remaining.Length;
            while (subWordLength >= 1)
            {
                string subWord = remaining.Substring(0, subWordLength);
                if (!_vocabularyDict.ContainsKey(subWord))
                {
                    subWordLength--;
                    continue;
                }

                prefix = subWord;
                break;
            }

            if (prefix == null)
            {
                tokens.Add((Tokens.Unknown, _vocabularyDict[Tokens.Unknown]));
                return tokens;
            }

            var regex = new Regex(prefix);
            remaining = regex.Replace(remaining, "##", 1);
            
            tokens.Add((prefix, _vocabularyDict[prefix]));
        }

        if (!string.IsNullOrEmpty(word) && !tokens.Any())
        {
            tokens.Add((Tokens.Unknown, _vocabularyDict[Tokens.Unknown]));
        }

        return tokens;
    }
    protected abstract IEnumerable<string> TokenizeSentence(string text);
}