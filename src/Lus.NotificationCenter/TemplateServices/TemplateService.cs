using System.Text.RegularExpressions;

namespace Lus.NotificationCenter.TemplateServices
{
    public class TemplateService : ITemplateService
    {
        private readonly string match = @"(\$.*?\$)";
        private readonly string fixImages = "(src=\"Home.*?\")";

        public string GenerateHtmlMailTemplate(string templateData)
        {
            if (string.IsNullOrWhiteSpace(templateData))
            {
                return null;
            }

            string mainUserTemplate = @"{1}<table style='direction:rtl;width:100%;border:0px;'><tr><td>&nbsp;</td><td>{0}</td><td>&nbsp;</td></tr></table>";
            string mainUserCss = @"<style>table {direction: rtl;text-align: right;font-size: 17px;}tr {direction: rtl;text-align: right;font-size: 17px;}th, td {direction: rtl;text-align: right;font-size: 17px;}</style>";

            string userTemplate = string.Empty;

            userTemplate = string.Format(mainUserTemplate, templateData, mainUserCss);
            userTemplate = cleanTemplate(userTemplate);

            return userTemplate;
        }

        public Dictionary<string, byte[]> GetMailAttachments(string templateData)
        {
            Dictionary<string, byte[]> images = new Dictionary<string, byte[]>();
            Match number;

            Match maches = Regex.Match(templateData, fixImages);
            while (maches != null && !string.IsNullOrWhiteSpace(maches.Value))
            {
                number = Regex.Match(maches.Value, "([^\\/]+)$");
                templateData = templateData.Replace(maches.Value, "");
                maches = Regex.Match(templateData, fixImages);
            }
            return images;
        }

        private string cleanTemplate(string Template)
        {
            Match maches = Regex.Match(Template, match);
            while (maches != null && !string.IsNullOrWhiteSpace(maches.Value))
            {
                Template = Template.Replace(maches.Value, "");
                maches = Regex.Match(Template, match);
            }
            return Template;
        }
    }
}
