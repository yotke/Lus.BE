namespace Lus.Contracts.Documents
{
    public enum DocumentSourceFormat
    {
        Xlsx = 0,
        Xls = 1
    }

    public enum DocumentInstanceStatus
    {
        Draft = 0,
        Committed = 1,
        Rendered = 2
    }

    public enum DocumentRepeatPolicy
    {
        OnePerSheet = 0,
        StackedPerSheet = 1
    }
}
