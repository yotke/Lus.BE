namespace Lus.NotificationCenter.TemplateServices
{
    public interface ITemplateService
    {
        string GenerateHtmlMailTemplate(string templateData);

        Dictionary<string, byte[]> GetMailAttachments(string templateData);
    }
}
