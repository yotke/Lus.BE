using Lus.Contracts.Streets;

namespace Lus.Contracts.Cities
{
    public class CityDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string EnglishName { get; set; }

        public int PNH_EREH_K_NUMERI { get; set; }

        public ICollection<StreetDto> Streets { get; set; }
    }
}
