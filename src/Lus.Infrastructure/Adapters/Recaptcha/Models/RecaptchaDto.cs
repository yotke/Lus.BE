using System.Runtime.Serialization;

namespace Lus.Contracts.Common.Ports
{
    [DataContract]
    public class RecaptchaDto
    {
        [DataMember]
        public bool success { get; set; }   // whether this request was a valid reCAPTCHA token for your site
        [DataMember]
        public decimal score { get; set; }  // the score for this request (0.0 - 1.0)
        [DataMember]
        public string action { get; set; }  // the action name for this request (important to verify)
        [DataMember]
        public string challenge_ts { get; set; }    // timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ)
        [DataMember]
        public string hostname { get; set; }    // the hostname of the site where the reCAPTCHA was solved
        [DataMember(Name = "error-codes")]
        public string[] error_codes { get; set; }// optional - Could create a child object for error-codes
    }
}
