using System.Text.Json;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Services
{
    public class DraftVersionConflictException : Exception
    {
        public int Expected { get; }
        public int Actual { get; }

        public DraftVersionConflictException(int expected, int actual)
            : base($"Draft version conflict: expected {expected}, actual {actual}.")
        {
            Expected = expected;
            Actual = actual;
        }
    }

    /// <summary>
    /// The only mutation path for a document draft. Applies a batch atomically under
    /// an optimistic version guard and returns the inverse batch for undo.
    /// </summary>
    public static class DraftPatcher
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static (DocumentDraftDto Next, IReadOnlyList<DraftPatchOp> Inverse) Apply(
            DocumentDraftDto draft,
            int expectedVersion,
            IReadOnlyList<DraftPatchOp> ops)
        {
            if (draft.Version != expectedVersion)
                throw new DraftVersionConflictException(expectedVersion, draft.Version);

            var next = Clone(draft);
            var inverse = new List<DraftPatchOp>(ops.Count);
            foreach (var op in ops)
                ApplyOne(next, op, inverse);

            next.Version = draft.Version + 1;
            return (next, inverse);
        }

        /// <summary>
        /// Apply ops to a clone without bumping <see cref="DocumentDraftDto.Version"/>.
        /// Used to let a deterministic follow-up (totals) see the pending row patches
        /// before they are committed as one versioned batch.
        /// </summary>
        public static DocumentDraftDto Preview(
            DocumentDraftDto draft,
            IReadOnlyList<DraftPatchOp> ops)
        {
            var (next, _) = Apply(draft, draft.Version, ops);
            next.Version = draft.Version;
            return next;
        }

        /// <summary>Apply inverse ops and restore the previous version number.</summary>
        public static DocumentDraftDto Revert(
            DocumentDraftDto draft,
            IReadOnlyList<DraftPatchOp> inverse)
        {
            var next = Clone(draft);
            var discarded = new List<DraftPatchOp>();

            // REVERSE order. Inverses are recorded as the forward batch is applied, so each
            // one restores the state its own op saw. Replaying them forwards undoes the
            // earliest change first and the later inverses then write back state that already
            // contains the change being undone — e.g. setting the rate emits [rate -> null,
            // totals -> {rate: 225}], which forwards leaves the rate at 225.
            for (var i = inverse.Count - 1; i >= 0; i--)
                ApplyOne(next, inverse[i], discarded);

            next.Version = Math.Max(0, draft.Version - 1);
            return next;
        }

        public static DocumentDraftDto Clone(DocumentDraftDto draft) =>
            JsonSerializer.Deserialize<DocumentDraftDto>(JsonSerializer.Serialize(draft, Json), Json)
            ?? new DocumentDraftDto();

        private static void ApplyOne(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            switch (op.Op)
            {
                case "SetField":
                    ApplySetField(draft, op, inverse);
                    break;
                case "AddRow":
                    ApplyAddRow(draft, op, inverse);
                    break;
                case "UpdateRow":
                    ApplyUpdateRow(draft, op, inverse);
                    break;
                case "RemoveRow":
                    ApplyRemoveRow(draft, op, inverse);
                    break;
                case "SetTotals":
                    inverse.Add(new DraftPatchOp
                    {
                        Op = "SetTotals",
                        Path = "totals",
                        Value = JsonSerializer.SerializeToElement(draft.Totals, Json)
                    });
                    draft.Totals = op.Value?.Deserialize<DocumentTotalsDto>(Json) ?? new DocumentTotalsDto();
                    break;
                default:
                    throw new ArgumentException($"Unknown patch op '{op.Op}'.");
            }
        }

        private static void ApplySetField(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            switch (op.Path)
            {
                case "lastUtterance":
                    inverse.Add(Set("lastUtterance", draft.LastUtterance));
                    draft.LastUtterance = ReadString(op.Value) ?? "";
                    break;
                case "letterhead.accountNumber":
                case "accountNumber":
                    inverse.Add(Set("accountNumber", draft.AccountNumber));
                    draft.AccountNumber = ReadString(op.Value);
                    break;
                case "totals.hourlyRate":
                    inverse.Add(SetNullableDecimal(op.Path, draft.Totals.HourlyRate));
                    draft.Totals.HourlyRate = ReadDecimal(op.Value);
                    break;
                case "totals.carryIn":
                    inverse.Add(SetDecimal(op.Path, draft.Totals.CarryIn));
                    draft.Totals.CarryIn = ReadDecimal(op.Value) ?? 0m;
                    break;
                case "totals.vatPercent":
                    inverse.Add(SetDecimal(op.Path, draft.Totals.VatPercent));
                    draft.Totals.VatPercent = ReadDecimal(op.Value) ?? 0m;
                    break;
                case "totals.plotsPercent":
                    inverse.Add(SetNullableDecimal(op.Path, draft.Totals.PlotsPercent));
                    draft.Totals.PlotsPercent = ReadDecimal(op.Value);
                    break;
                default:
                    if (op.Path.StartsWith("template.", StringComparison.Ordinal))
                    {
                        ApplyTemplateField(draft, op, inverse);
                        break;
                    }
                    throw new ArgumentException($"Unknown SetField path '{op.Path}'.");
            }
        }

        /// <summary>
        /// Template fields the Importer discovers (`template.rtl`, `template.dataBandStartRow`,
        /// `template.mergePolicy`, ...). Kept as an explicit whitelist rather than reflection so
        /// an agent cannot invent a path that silently writes nothing.
        /// </summary>
        private static void ApplyTemplateField(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            draft.Template ??= new DocumentTemplateDto();
            var tpl = draft.Template;
            var field = op.Path["template.".Length..];

            switch (field)
            {
                case "sheetName":
                    inverse.Add(Set(op.Path, tpl.SheetName));
                    tpl.SheetName = ReadString(op.Value);
                    break;
                case "rtl":
                    inverse.Add(SetBool(op.Path, tpl.Rtl));
                    tpl.Rtl = op.Value?.ValueKind == JsonValueKind.True;
                    break;
                case "mergePolicy":
                    inverse.Add(Set(op.Path, tpl.MergePolicy));
                    tpl.MergePolicy = ReadString(op.Value);
                    break;
                case "mergeCount":
                    inverse.Add(SetInt(op.Path, tpl.MergeCount));
                    tpl.MergeCount = ReadInt(op.Value) ?? 0;
                    break;
                case "dataBandStartRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.DataBandStartRow));
                    tpl.DataBandStartRow = ReadInt(op.Value);
                    break;
                case "tableHeaderRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.TableHeaderRow));
                    tpl.TableHeaderRow = ReadInt(op.Value);
                    break;
                case "titleRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.TitleRow));
                    tpl.TitleRow = ReadInt(op.Value);
                    break;
                case "totalsRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.TotalsRow));
                    tpl.TotalsRow = ReadInt(op.Value);
                    break;
                case "billingStartRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.BillingStartRow));
                    tpl.BillingStartRow = ReadInt(op.Value);
                    break;
                case "declarationStartRow":
                    inverse.Add(SetNullableInt(op.Path, tpl.DeclarationStartRow));
                    tpl.DeclarationStartRow = ReadInt(op.Value);
                    break;
                case "columnWidths":
                    inverse.Add(new DraftPatchOp
                    {
                        Op = "SetField",
                        Path = op.Path,
                        Value = JsonSerializer.SerializeToElement(tpl.ColumnWidths, Json)
                    });
                    tpl.ColumnWidths = op.Value?.Deserialize<Dictionary<string, double>>(Json) ?? new();
                    break;
                case "orgName":
                    inverse.Add(Set(op.Path, tpl.OrgName));
                    tpl.OrgName = ReadString(op.Value);
                    break;
                case "title":
                    inverse.Add(Set(op.Path, tpl.Title));
                    tpl.Title = ReadString(op.Value);
                    break;
                case "plannerName":
                    inverse.Add(Set(op.Path, tpl.PlannerName));
                    tpl.PlannerName = ReadString(op.Value);
                    break;
                case "clientName":
                    inverse.Add(Set(op.Path, tpl.ClientName));
                    tpl.ClientName = ReadString(op.Value);
                    break;
                case "declarationText":
                    inverse.Add(Set(op.Path, tpl.DeclarationText));
                    tpl.DeclarationText = ReadString(op.Value);
                    break;
                case "billingLabels":
                    inverse.Add(new DraftPatchOp
                    {
                        Op = "SetField",
                        Path = op.Path,
                        Value = JsonSerializer.SerializeToElement(tpl.BillingLabels, Json)
                    });
                    tpl.BillingLabels = op.Value?.Deserialize<List<string>>(Json) ?? new();
                    break;
                case "headers":
                    inverse.Add(new DraftPatchOp
                    {
                        Op = "SetField",
                        Path = op.Path,
                        Value = JsonSerializer.SerializeToElement(tpl.Headers, Json)
                    });
                    tpl.Headers = op.Value?.Deserialize<List<string>>(Json) ?? new();
                    break;
                default:
                    throw new ArgumentException($"Unknown SetField path '{op.Path}'.");
            }
        }

        private static DraftPatchOp SetBool(string path, bool value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, Json)
        };

        private static DraftPatchOp SetInt(string path, int value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, Json)
        };

        private static DraftPatchOp SetNullableInt(string path, int? value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, Json)
        };

        private static DraftPatchOp SetDecimal(string path, decimal value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, Json)
        };

        private static DraftPatchOp SetNullableDecimal(string path, decimal? value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, Json)
        };

        private static decimal? ReadDecimal(JsonElement? value) =>
            value is { ValueKind: JsonValueKind.Number } n && n.TryGetDecimal(out var d) ? d : null;

        private static int? ReadInt(JsonElement? value) =>
            value is { ValueKind: JsonValueKind.Number } n && n.TryGetInt32(out var i) ? i : null;

        private static void ApplyAddRow(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            var row = op.Value?.Deserialize<DocumentDraftRowDto>(Json) ?? new DocumentDraftRowDto();
            DeriveDayOfWeek(row);
            draft.Rows.Add(row);
            inverse.Add(new DraftPatchOp { Op = "RemoveRow", Path = $"rows[{draft.Rows.Count - 1}]", Value = null });
        }

        private static void ApplyUpdateRow(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            var index = ParseRowIndex(op.Path);
            var current = draft.Rows[index];
            inverse.Add(new DraftPatchOp
            {
                Op = "UpdateRow",
                Path = $"rows[{index}]",
                Value = JsonSerializer.SerializeToElement(current, Json)
            });
            var patch = op.Value?.Deserialize<DocumentDraftRowDto>(Json) ?? new DocumentDraftRowDto();
            MergeRow(current, patch, op.Path);
            // Provenance is copied outside MergeRow because its field-specific branches return
            // early: a `rows[0].hours` edit must still record WHO made it.
            if (patch.Source is not null)
            {
                current.Source = patch.Source;
                current.ChangedAt = patch.ChangedAt;
            }
            DeriveDayOfWeek(current);
        }

        private static void ApplyRemoveRow(DocumentDraftDto draft, DraftPatchOp op, List<DraftPatchOp> inverse)
        {
            var index = ParseRowIndex(op.Path);
            var removed = draft.Rows[index];
            inverse.Add(new DraftPatchOp
            {
                Op = "AddRow",
                Path = "rows",
                Value = JsonSerializer.SerializeToElement(removed, Json)
            });
            draft.Rows.RemoveAt(index);
        }

        private static void MergeRow(DocumentDraftRowDto target, DocumentDraftRowDto patch, string path)
        {
            if (path.EndsWith(".hours", StringComparison.OrdinalIgnoreCase))
            {
                target.Hours = patch.Hours;
                return;
            }
            if (path.EndsWith(".subject", StringComparison.OrdinalIgnoreCase))
            {
                target.Subject = patch.Subject;
                return;
            }
            if (path.EndsWith(".location", StringComparison.OrdinalIgnoreCase))
            {
                target.Location = patch.Location;
                return;
            }
            if (path.EndsWith(".date", StringComparison.OrdinalIgnoreCase))
            {
                target.Date = patch.Date;
                return;
            }

            if (patch.Date is not null) target.Date = patch.Date;
            if (patch.Hours is not null) target.Hours = patch.Hours;
            if (patch.Location is not null) target.Location = patch.Location;
            if (patch.Subject is not null) target.Subject = patch.Subject;
        }

        /// <summary>
        /// 1-based, Sunday = 1 — matching the document's own day letters (א'=ראשון) and the
        /// canvas that renders them. .NET's DayOfWeek is 0-based, so writing it straight
        /// through made Sunday render blank and every other day show the previous day's
        /// letter.
        /// </summary>
        private static void DeriveDayOfWeek(DocumentDraftRowDto row)
        {
            if (row.Date is { } date)
                row.DayOfWeek = (int)date.DayOfWeek + 1;
        }

        private static int ParseRowIndex(string path)
        {
            var start = path.IndexOf('[') + 1;
            var end = path.IndexOf(']');
            if (start <= 0 || end <= start)
                throw new ArgumentException($"Row path '{path}' is missing an index.");
            return int.Parse(path[start..end]);
        }

        private static DraftPatchOp Set(string path, string? value) => new()
        {
            Op = "SetField",
            Path = path,
            Value = JsonSerializer.SerializeToElement(value ?? "", Json)
        };

        private static string? ReadString(JsonElement? value)
        {
            if (value is null) return null;
            var el = value.Value;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText().Trim('"');
        }
    }
}
