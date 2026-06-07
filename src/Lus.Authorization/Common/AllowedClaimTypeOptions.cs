namespace Lus.Authorization.Common
{
    public class AllowedClaimTypeOptions
    {
        private readonly List<string> allowedClaims = new List<string>();

        public AllowedClaimTypeOptions WithClaimType(string claimType)
        {
            this.allowedClaims.Add(claimType);
            return this;
        }

        public IReadOnlyCollection<string> GetClaims() => this.allowedClaims;
    }
}
