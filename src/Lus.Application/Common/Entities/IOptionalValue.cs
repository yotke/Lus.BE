namespace Lus.Application.Common.Entities
{
    public interface IOptionalValue<TEntityKey>
    {
        public Settings? Settings { get; set; }

        public string Note { get; set; }

        public int GroupId { get; set; }
    }
}
