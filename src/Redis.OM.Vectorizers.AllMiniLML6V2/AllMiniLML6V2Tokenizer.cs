using System.Reflection;
using Redis.OM.Vectorizers.AllMiniLML6V2.Tokenizers;

namespace Redis.OM.Vectorizers.AllMiniLML6V2;

internal class AllMiniLML6V2Tokenizer : UncasedTokenizer
{
    private AllMiniLML6V2Tokenizer(string[] vocabulary) : base(vocabulary)
    {
    }

    internal static AllMiniLML6V2Tokenizer Create()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string fileName = "Redis.OM.Vectorizers.AllMiniLML6V2.Resources.vocab.txt";
        using var stream = assembly.GetManifestResourceStream(fileName);
        if (stream is null)
        {
            throw new FileNotFoundException("Could not find embedded resource file Resources.vocab.txt");
        }
        using var reader = new StreamReader(stream);

        if (stream is null)
        {
            throw new Exception("Could not open stream reader.");
        }

        var vocab = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            vocab.Add(line);
        }

        return new AllMiniLML6V2Tokenizer(vocab.ToArray());
    }
}