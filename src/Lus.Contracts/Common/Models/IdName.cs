namespace Lus.Contracts.Common.Models
{
    public class IdName
    {
        public int id { get; set; }
        public string name { get; set; }

        public IdName(int id,string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
