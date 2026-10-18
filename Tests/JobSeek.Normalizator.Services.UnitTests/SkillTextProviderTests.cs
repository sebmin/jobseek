using JobSeek.Normalizator.Services.Providers;

namespace JobSeek.Normalizator.Services.UnitTests;

[TestClass]
public sealed class SkillTextProviderTests
{
    private readonly SkillTextProvider _text = new();

    [TestMethod]
    public void SplitToWords_Null_ReturnsEmpty()
    {
        Assert.IsEmpty(_text.SplitToWords(null));
    }

    [TestMethod]
    public void SplitToWords_Empty_ReturnsEmpty()
    {
        Assert.IsEmpty(_text.SplitToWords(""));
    }

    [TestMethod]
    public void SplitToWords_Whitespace_ReturnsEmpty()
    {
        Assert.IsEmpty(_text.SplitToWords("   \t\n"));
    }

    [TestMethod]
    public void SplitToWords_KeepsSkillPunctuationAndStripsSentencePunctuation()
    {
        var words = _text.SplitToWords("C# .NET, node.js or javascripts?");

        CollectionAssert.AreEqual(new[] { "c#", ".net", "node.js", "or", "javascripts" }, words);
    }

    [TestMethod]
    public void SplitToWords_StripsWrappingPunctuation()
    {
        var words = _text.SplitToWords("(JavaScript).");

        CollectionAssert.AreEqual(new[] { "javascript" }, words);
    }

    [TestMethod]
    public void SplitToWords_KeepsALeadingDot()
    {
        var words = _text.SplitToWords(".NET.");

        CollectionAssert.AreEqual(new[] { ".net" }, words);
    }

    [TestMethod]
    public void SplitToWords_StripsColon()
    {
        var words = _text.SplitToWords("Requirements: sql");

        CollectionAssert.AreEqual(new[] { "requirements", "sql" }, words);
    }

    [TestMethod]
    public void SplitToWords_DivideBySlash()
    {
        var words = _text.SplitToWords("javascript/typescript");

        CollectionAssert.AreEqual(new[] { "javascript", "typescript" }, words);
    }
}
