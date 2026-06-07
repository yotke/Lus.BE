namespace Lus.Contracts.Notifications
{
    public class AdditionalNotificationDataDto
    {
        public string CommitteeSigningMemberUrl { get; set; }

        public int SummonId { get; set; }

        public Dictionary<string, byte[]>? fileList { get; set; }
    }
}
