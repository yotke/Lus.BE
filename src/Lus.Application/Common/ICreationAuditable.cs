namespace Lus.Application.Common
{
    public interface ICreationAuditable
    {
        int? CreatedById { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
