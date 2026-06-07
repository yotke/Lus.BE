namespace Lus.Contracts.Options
{
    public class SmsNotificationOptions
    {
        public int SmsCodeLength { get; set; }

        public string SmsManagerProvider { get; set; }

        public int HeaderCustomer { get; set; }

        public int HeaderRecipient { get; set; }

        public int HeaderSender { get; set; }

        public string HeaderUserId { get; set; }

        public string HeaderUserPass { get; set; }

        public string Sender { get; set; }

        public string Signature { get; set; }

        public bool OnlyIsraelPhone { get; set; }

        public bool IsDebugMode { get; set; }

        public string DebugModePhones { get; set; }
    }
}
