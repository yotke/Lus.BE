namespace Lus.Contracts.Documents.Builder
{
    public sealed class DocumentDraftRowDto
    {
        public DateTime? Date { get; set; }
        public int? DayOfWeek { get; set; }
        public decimal? Hours { get; set; }
        public string? Location { get; set; }
        public string? Subject { get; set; }

        /// <summary>
        /// Who last wrote this row: "user" for a hand edit on the canvas, otherwise the agent
        /// that produced it ("doc.row_extractor", "doc.template_reader", ...).
        ///
        /// The user has to be able to tell their own corrections apart from what the AI filled
        /// in — a document you cannot audit is a document you cannot sign.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>When it was last written (UTC), so the canvas can show recency.</summary>
        public DateTime? ChangedAt { get; set; }
    }

    /// <summary>
    /// What the Importer learned from the uploaded exemplar: the document's geometry and
    /// intent, not its contents. This is what lets the canvas render an EMPTY document that
    /// already has the right shape, before a single row exists.
    ///
    /// Everything here is data, never a branch in the code — two archetypes with different
    /// merge policies and directions are the same renderer with a different template.
    /// </summary>
    public sealed class DocumentTemplateDto
    {
        public string? SheetName { get; set; }

        /// <summary>
        /// Direction the DOCUMENT is written in. A property of the template, NOT of the app
        /// chrome: an RTL sheet puts column A on the right regardless of the user's UI language.
        /// </summary>
        public bool Rtl { get; set; } = true;

        /// <summary>First row of the data band — where rows start below the table header.</summary>
        public int? DataBandStartRow { get; set; }
        public int? TableHeaderRow { get; set; }
        public int? TitleRow { get; set; }
        public int? TotalsRow { get; set; }
        public int? BillingStartRow { get; set; }
        public int? DeclarationStartRow { get; set; }

        public string? MergePolicy { get; set; }
        public int MergeCount { get; set; }

        /// <summary>Column letter -> width, as read from the exemplar.</summary>
        public Dictionary<string, double> ColumnWidths { get; set; } = new();

        /// <summary>Header labels in document order — the canvas's column headings.</summary>
        public List<string> Headers { get; set; } = new();

        // ── Letterhead / chrome, so the canvas can PREVIEW the finished document ────────
        // The user recognises their report by its title band and letterhead, not by its
        // data band. Without these the canvas is a bare grid that looks nothing like the
        // file they uploaded.

        /// <summary>Banner line above the title (the client organisation).</summary>
        public string? OrgName { get; set; }

        /// <summary>The report's own title line, e.g. "דוח ביצוע שעות עבודה מרץ 2026".</summary>
        public string? Title { get; set; }

        public string? PlannerName { get; set; }
        public string? ClientName { get; set; }

        /// <summary>
        /// The billing block's labels in document order (hours / rate / subtotal / VAT /
        /// total). Taken from the workbook so the preview uses the document's own wording
        /// instead of inventing its own.
        /// </summary>
        public List<string> BillingLabels { get; set; } = new();

        /// <summary>The employee declaration paragraph printed under the billing block.</summary>
        public string? DeclarationText { get; set; }
    }

    public sealed class DocumentDraftDto
    {
        public int Version { get; set; }
        public string LastUtterance { get; set; } = "";
        public string? AccountNumber { get; set; }
        public List<DocumentDraftRowDto> Rows { get; set; } = new();
        public DocumentTotalsDto Totals { get; set; } = new();

        /// <summary>Null until an exemplar has been imported.</summary>
        public DocumentTemplateDto? Template { get; set; }
    }

    public sealed class DocumentTotalsDto
    {
        public decimal Hours { get; set; }
        public decimal CarryIn { get; set; }
        public decimal Remaining { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal VatPercent { get; set; } = 18;
        public decimal? PlotsPercent { get; set; }
        public decimal? Total { get; set; }
    }

    public sealed class DocumentBuilderTurnRequestDto
    {
        public int Version { get; set; }
        public string? Text { get; set; }

        /// <summary>
        /// Id of the question this message answers, when it answers one. Without it the reply
        /// to "what is the hourly rate?" is indistinguishable from a new line of dictation.
        /// </summary>
        public string? QuestionId { get; set; }
    }

    public sealed class DocumentBuilderTurnResponseDto
    {
        public int Version { get; set; }
        public List<DraftPatchOp> Ops { get; set; } = new();

        /// <summary>
        /// The single next interview question, when the planner produced one. Additive —
        /// null for every caller that predates the planner running in a turn.
        /// </summary>
        public DocumentQuestionDto? Question { get; set; }

        /// <summary>Assistant chat lines produced during the turn (advisor answer, notes).</summary>
        public List<DocumentBuilderMessageDto> Messages { get; set; } = new();

        /// <summary>Validator findings for this turn. Empty when the document is clean.</summary>
        public List<DocumentWarningDto> Warnings { get; set; } = new();
    }

    /// <summary>One chat-rail line. Role mirrors the SignalR BuilderMessage event.</summary>
    public sealed class DocumentBuilderMessageDto
    {
        public string Role { get; set; } = "assistant";
        public string Text { get; set; } = "";
        public List<string> Suggestions { get; set; } = new();
    }

    public sealed class DocumentBuilderSessionDto
    {
        public int Version { get; set; }
        public DocumentDraftDto Draft { get; set; } = new();
    }

    public sealed class DocumentQuestionDto
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> Chips { get; set; } = new();
    }

    public sealed class DocumentWarningDto
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
