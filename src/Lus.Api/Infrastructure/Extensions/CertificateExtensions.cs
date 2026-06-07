using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Lus.Application.Configuration.Models;

namespace Lus.Infrastructure.Extensions
{
    public static class CertificateExtensions
    {
        public static IIdentityServerBuilder AddSignInCertificates(this IIdentityServerBuilder identityBuilder, IConfiguration configuration)
        {
            identityBuilder
                .AddSigningCredential(GetProtectCertificate(configuration, "Certificate"))
                .AddValidationKey(GetProtectCertificate(configuration, "Certificate"), SecurityAlgorithms.RsaSsaPssSha256);

            var oldCertificate = GetProtectCertificate(configuration, "OldCertificate");
            if (oldCertificate != null)
            {
                identityBuilder.AddValidationKey(oldCertificate);
            }

            var futureCertificate = GetProtectCertificate(configuration, "FutureCertificate");
            if (futureCertificate != null)
            {
                identityBuilder.AddValidationKey(futureCertificate);
            }

            return identityBuilder;
        }

        private static X509Certificate2? GetProtectCertificate(IConfiguration configuration, string section)
        {
            var certificateSection = configuration.GetSection("IdentityCertificateConfig:" + section)
                .Get<CertificateConfiguration>();

            return certificateSection.Decode() ? new X509Certificate2(certificateSection.CertificateData, certificateSection.Password) : null;
        }
    }
}
