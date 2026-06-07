using System.Text;

namespace Lus.Application.Configuration.Models
{
    public class CertificateConfiguration
    {
        public string CertificateBase64 { get; set; }

        public byte[] CertificateData { get; private set; }

        public string PasswordBase64 { get; set; }

        public string Password { get; private set; }

        public bool Decode()
        {
            if (string.IsNullOrEmpty(CertificateBase64) || CertificateBase64 == "null")
            {
                return false;
            }

            CertificateData = Convert.FromBase64String(CertificateBase64);
            var decodedBytes = Convert.FromBase64String(PasswordBase64);
            Password = Encoding.UTF8.GetString(decodedBytes);
            return true;
        }
    }
}
