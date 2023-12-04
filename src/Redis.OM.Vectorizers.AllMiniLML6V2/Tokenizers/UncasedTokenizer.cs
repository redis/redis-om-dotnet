namespace Redis.OM.Vectorizers.AllMiniLML6V2.Tokenizers;

internal abstract class UncasedTokenizer : TokenizerBase
{
    public UncasedTokenizer(string[] vocabulary) : base(vocabulary)
    {
    }

    protected override IEnumerable<string> TokenizeSentence(string text)
    {
        return text.Split(new [] { " ", "   ", "\r\n" }, StringSplitOptions.None)
            .SelectMany(o => o.SplitAndKeep(".,;:\\/?!#$%()=+-*\"'–_`<>&^@{}[]|~'".ToArray()))
            .Select(o => o.ToLower());
    }
}