namespace Lus.Application.Common
{
    public interface IModificationAuditable
    {
        int? UpdatedById { get; set; }

        public DateTime UpdatedOn { get; set; }
    }
}
