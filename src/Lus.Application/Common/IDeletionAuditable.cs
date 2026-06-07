namespace Lus.Application.Common
{
    public interface IDeletionAuditable
    {
        bool IsDeleted { get; set; }

        DateTime? DeletedOn { get; set; }

        int? DeletedById { get; set; }
    }
}
