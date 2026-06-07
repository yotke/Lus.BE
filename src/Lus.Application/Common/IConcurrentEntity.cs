namespace Lus.Application.Common
{
    public interface IConcurrentEntity
    {
        byte[] RowVersion { get; set; }
    }
}
