namespace ConcertBuddy.ConsoleApp.Search;

/// <summary>
/// A text typed with the wrong keyboard layout: "ьуефддшсф" is "metallica" typed on the Russian layout, "rbyj" is
/// "кино" typed on the English one (#94). Only the letter keys of the two standard layouts are swapped.
/// </summary>
public static class KeyboardLayout
{
    private const string Russian = "йцукенгшщзхъфывапролджэячсмитьбюё";
    private const string English = "qwertyuiop[]asdfghjkl;'zxcvbnm,.`";

    /// <summary>
    /// The text as if typed on the other layout, lower case: Russian letters become English keys, otherwise English
    /// letters become Russian ones. The same text when it has no letters of either layout.
    /// </summary>
    public static string Swap(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Any(c => Russian.Contains(c)))
        {
            return Map(lower, Russian, English);
        }
        if (lower.Any(c => c is >= 'a' and <= 'z'))
        {
            return Map(lower, English, Russian);
        }
        return text;
    }

    private static string Map(string text, string from, string to)
    {
        return string.Concat(text.Select(c => from.IndexOf(c) is var i and >= 0 ? to[i] : c));
    }
}
