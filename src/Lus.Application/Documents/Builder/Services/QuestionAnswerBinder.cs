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
            ["client_name"] = "template.clientName",
            ["planner_name"] = "template.plannerName",
        };

        /// <summary>
        /// Row-scoped questions ("which date is row 3?") arrive as `row_date:3`. The row index
        /// travels in the id because the planner picked the row — asking the user to restate
        /// which row they are answering about would be asking them to do the app's bookkeeping.
        /// </summary>
        private static readonly Dictionary<string, (string Field, bool Numeric)> RowFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["row_date"] = ("Date", false),
                ["row_hours"] = ("Hours", true),
                ["row_location"] = ("Location", false),
                ["row_subject"] = ("Subject", false),
            };

        private static (string Kind, int Index)? SplitRowQuestion(string questionId)
        {
            var separator = questionId.IndexOf(':');
            if (separator <= 0) return null;

            var kind = questionId[..separator];
            if (!RowFields.ContainsKey(kind)) return null;

            return int.TryParse(questionId[(separator + 1)..], out var index) && index >= 0
                ? (kind, index)
                : null;
        }

        /// <summary>
        /// True when this question's answer is a field value rather than free text. Questions
        /// like "first_row" are NOT bindable — their answer is dictation and belongs to the
        /// content wave.
        /// </summary>
        public static bool IsBindable(string? questionId) =>
            !string.IsNullOrWhiteSpace(questionId)
            && (NumericFields.ContainsKey(questionId!)
                || TextFields.ContainsKey(questionId!)
                || SplitRowQuestion(questionId!) is not null);

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

            if (SplitRowQuestion(questionId!) is { } row)
            {
                var (field, numeric) = RowFields[row.Kind];
                object? value = numeric ? ParseNumber(answer!) : answer!.Trim();
                if (value is null) return null;

                // A date answer only counts if it IS a date — "מחר" must fall through to a
                // normal turn rather than being written as a literal string.
                if (field == "Date")
                {
                    if (!TryParseDate(answer!, out var parsed)) return null;
                    value = parsed;
                }

                var patch = new Dictionary<string, object?> { [field] = value };
                return new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = $"rows[{row.Index}]",
                    Value = JsonSerializer.SerializeToElement(patch, Json)
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
        /// Accepts what a person actually types for a date: 5/3/2026, 5.3.2026, 2026-03-05.
        /// Day-first, because that is how the exemplar's users write dates.
        /// </summary>
        private static bool TryParseDate(string answer, out DateTime value)
        {
            var trimmed = answer.Trim();
            string[] formats =
            {
                "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy", "dd.MM.yyyy",
                "d-M-yyyy", "dd-MM-yyyy", "d/M/yy", "dd/MM/yy",
            };

            return DateTime.TryParseExact(
                       trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                   || DateTime.TryParse(
                       trimmed, CultureInfo.GetCultureInfo("he-IL"), DateTimeStyles.None, out value);
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
