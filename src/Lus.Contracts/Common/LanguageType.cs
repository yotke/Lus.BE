namespace Lus.Contracts.Common;

public enum LanguageType
{
    He = 0,
    En = 1
}

public static class LanguageTypeExtensions
{
    public static string ToLangCode(this LanguageType language) =>
        language == LanguageType.He ? "he" : "en";
}
