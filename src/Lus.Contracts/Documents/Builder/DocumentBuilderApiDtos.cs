namespace Lus.Contracts.Documents.Builder
{
    /// <summary>
    /// One catalog agent as the UI needs it. Served from the C# catalog so the client's
    /// pipeline cannot drift from what the server actually dispatches.
    /// </summary>
    public sealed class DocumentAgentDto
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public string InputKind { get; set; } = "";
        public string Icon { get; set; } = "";
        public string DisplayNameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public bool Enabled { get; set; }
        /// <summary>Content agents only — dependency wave. Null for every other kind.</summary>
        public int? Wave { get; set; }
    }

    /// <summary>
    /// A human edit on the canvas. Same op shape an agent emits: there is exactly one
    /// write path into a draft, so undo/redo and the version guard behave identically
    /// whether a person or an agent produced the change.
    /// </summary>
    public sealed class CanvasEditRequestDto
    {
        public int Version { get; set; }
        public List<DraftPatchOp> Ops { get; set; } = new();
    }

    /// <summary>Result of learning an uploaded exemplar workbook.</summary>
    public sealed class TemplateImportResponseDto
    {
        public int Version { get; set; }
        public List<DraftPatchOp> Ops { get; set; } = new();
        public string SheetName { get; set; } = "";
        /// <summary>Direction the DOCUMENT is written in — not the app chrome's direction.</summary>
        public bool Rtl { get; set; }
        public int MergeCount { get; set; }
        public int DataBandStartRow { get; set; }
    }
}
