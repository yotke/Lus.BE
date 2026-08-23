using Lus.Contracts.Common;

namespace Lus.Contracts.Common;

/// <summary>
/// Bilingual user-facing messages for builder agent failures.
/// Slim port of ArmyLuz AiUserMessages — TimeoutError / UnexpectedError only.
/// </summary>
public static class AiUserMessages
{
    public static string TimeoutError(LanguageType lang) =>
        lang == LanguageType.He
            ? "פג הזמן המוקצב לעיבוד. נסה שוב."
            : "The request timed out. Please try again.";

    public static string UnexpectedError(LanguageType lang) =>
        lang == LanguageType.He
            ? "אירעה שגיאה בעיבוד. אנא נסה שוב."
            : "An unexpected error occurred. Please try again.";
}
