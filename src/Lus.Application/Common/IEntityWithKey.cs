namespace Lus.Application.Common
{
    public interface IEntityWithKey<TPrimaryKey>
    {
        TPrimaryKey Id { get; set; }
    }
}
