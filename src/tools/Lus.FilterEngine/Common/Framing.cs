namespace Lus.FilterEngine.Common
{
    public sealed class Framing
    {
        public Framing()
        {
            Take = 10;
        }

        public Framing(int take, int skip)
        {
            Take = take;
            Skip = skip;
        }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}
