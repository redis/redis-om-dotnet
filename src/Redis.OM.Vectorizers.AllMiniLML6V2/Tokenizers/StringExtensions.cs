namespace Redis.OM.Vectorizers.AllMiniLML6V2.Tokenizers;

internal static class StringExtension
{
    public static IEnumerable<string> SplitAndKeep(
        this string inputString, params char[] delimiters)
    {
        int start = 0, index;

        while ((index = inputString.IndexOfAny(delimiters, start)) != -1)
        {
            if (index - start > 0)
                yield return inputString.Substring(start, index - start);

            yield return inputString.Substring(index, 1);

            start = index + 1;
        }

        if (start < inputString.Length)
        {
            yield return inputString.Substring(start);
        }
    }
}