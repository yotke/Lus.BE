using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Services
{
    /// <summary>
    /// Binds an answer back to the question that asked for it.
    ///
    /// Without this the planner asks "מה התעריף לשעת עבודה?", the user types "225", and the
    /// reply is handed to the content wave as if it were a new work item — so the rate stays
    /// empty, the same question is asked again, and the user answers in a loop. The question's
    /// Id is what closes that loop: it says which field the next message is FOR.
    ///
    /// Deliberately deterministic and LLM-free. A number the user typed is not something to
    /// interpret; money in particular is never guessed (smart concept C6).
    /// </summary>
    public static class QuestionAnswerBinder
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        /// <summary>Question id → the draft path its answer sets.</summary>
        private static readonly Dictionary<string, string> NumericFields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hourly_rate"] = "totals.hourlyRate",
            ["carry_in"] = "totals.carryIn",
            ["vat_percent"] = "totals.vatPercent",
            ["plots_percent"] = "totals.plotsPercent",
        };

        private static readonly Dictionary<string, string> TextFields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["account_number"] = "accountNumber",
        };

        /// <summary>
        /// True when this question's answer is a field value rather than free text. Questions
        /// like "first_row" are NOT bindable — their answer is dictation and belongs to the
        /// content wave.
        /// </summary>
        public static bool IsBindable(string? questionId) =>
            !string.IsNullOrWhiteSpace(questionId)
            && (NumericFields.ContainsKey(questionId!) || TextFields.ContainsKey(questionId!));

        /// <summary>
        /// Turns an answer into a patch, or null when the answer cannot be read as one — an
        /// unparseable reply must fall through to a normal turn rather than silently writing
        /// nothing.
        /// </summary>
        public static DraftPatchOp? Bind(string? questionId, string? answer)
        {
            if (string.IsNullOrWhiteSpace(questionId) || string.IsNullOrWhiteSpace(answer))
                return null;

            if (NumericFields.TryGetValue(questionId!, out var numericPath))
            {
                var value = ParseNumber(answer!);
                if (value is null) return null;
                return new DraftPatchOp
                {
                    Op = "SetField",
                    Path = numericPath,
                    Value = JsonSerializer.SerializeToElement(value.Value, Json)
                };
            }

            if (TextFields.TryGetValue(questionId!, out var textPath))
            {
                return new DraftPatchOp
                {
                    Op = "SetField",
                    Path = textPath,
                    Value = JsonSerializer.SerializeToElement(answer!.Trim(), Json)
                };
            }

            return null;
        }

        /// <summary>
        /// Pulls the number out of a human answer: "225", "תעריף 225", "225 ש\"ח", "223.97".
        /// Takes the FIRST number — "225 ש\"ח לשעה" must not resolve to some later digit.
        /// </summary>
        private static decimal? ParseNumber(string answer)
        {
            var match = Regex.Match(answer.Replace(",", ""), @"-?\d+(\.\d+)?");
            if (!match.Success) return null;
            return decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
    }
}
