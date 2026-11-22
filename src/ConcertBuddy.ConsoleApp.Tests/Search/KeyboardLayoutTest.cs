using ConcertBuddy.ConsoleApp.Search;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.Search;

public class KeyboardLayoutTest
{
    [Theory]
    [InlineData("ьуефддшсф", "metallica")]
    [InlineData("ЬУЕФДДШСФ", "metallica")]
    [InlineData("швдуы", "idles")]
    [InlineData("ащщ ашпреукы", "foo fighters")]
    [InlineData("rbyj", "кино")]
    [InlineData("Ltleirf", "дедушка")]
    [InlineData("ljxm [jk", "дочь хол")]
    public void Swap_TextOfOneLayout_TheOtherLayout(string text, string expected)
    {
        KeyboardLayout.Swap(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("ьуефддшсф 2", "metallica 2")]
    [InlineData("ьуефд metal", "metal metal")]
    public void Swap_RussianLettersWithOthers_OnlyRussianLettersChange(string text, string expected)
    {
        KeyboardLayout.Swap(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("1975")]
    [InlineData("!!!")]
    [InlineData("")]
    public void Swap_NoLetters_SameText(string text)
    {
        KeyboardLayout.Swap(text).ShouldBe(text);
    }
}
