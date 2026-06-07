using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Options;
using Lus.Contracts.Users;
using System.ComponentModel;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;

namespace Lus.NotificationCenter.EmailServices
{
    public class EmailService : IEmailService
    {
        private readonly MailNotificationOptions mailNotificationOptions;
        private readonly Dictionary<MailType, string> emailTemplateNamesDictionary =
            new Dictionary<MailType, string>
            {
                { MailType.SiteRegistration, "ConfirmEmail"},
                { MailType.PasswordReset, "ResetPassword"},
                { MailType.LogInWithoutPassword, "LogInWithoutPassword"},
                { MailType.ApplicationConfirmation, "SendConfirmApplicationToTender"},
                { MailType.ApplicationModified, "SendChangedByAdminApplicationToTender"},
                { MailType.MembersSignatureMail, "SendSignatureToken"},
                { MailType.MembersProtocolMail, "SendProtocol"},
                { MailType.UserLoginInfoNotification, "UserLoginInfoNotification"}
            };

        private Dictionary<MailType, Func<UserDto, string, AdditionalNotificationDataDto, string>> mailTransformation;
        private readonly ILogger<EmailService> logger;

        public EmailService(IOptions<MailNotificationOptions> options, ILogger<EmailService> logger)
        {
            this.mailNotificationOptions = options.Value;
            this.logger = logger;
            AddMailTransformations();
        }

        public string GetEmailTemplate(MailType mailType)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith($"{this.emailTemplateNamesDictionary[mailType]}.html"));
            string result = string.Empty;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }

        public string GenerateMailByType(UserDto user, MailType mailType, string mailTemplate, AdditionalNotificationDataDto additionalNotificationData = null) => mailTransformation[mailType](user, mailTemplate, additionalNotificationData);

        private void AddMailTransformations()
        {
            mailTransformation =
                new Dictionary<MailType, Func<UserDto, string, AdditionalNotificationDataDto, string>>
                {
                    {
                        MailType.SiteRegistration, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@ConfirmVerificationToken", userDto.ConfirmationToken)
                                .Replace("@Domain", this.mailNotificationOptions.SiteDomain)
                    },{
                        MailType.LogInWithoutPassword, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@SmsVerificationToken", userDto.SmsVerificationToken)
                    }, {
                        MailType.PasswordReset, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@PasswordVerificationToken", userDto.PasswordVerificationToken)
                                .Replace("@Domain", this.mailNotificationOptions.SiteDomain)
                    }, {
                        MailType.ApplicationConfirmation, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@TenderName", userDto.TenderName ?? string.Empty)
                                .Replace("@OrganizationName", userDto.OrganizationName ?? "ארגון")
                    }, {
                        MailType.ApplicationModified, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@TenderName", userDto.TenderName ?? string.Empty)
                                .Replace("@OrganizationName", userDto.OrganizationName ?? "ארגון")
                    },{
                        MailType.UserLoginInfoNotification, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                    }, {
                        MailType.MembersSignatureMail, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@ConfirmVerificationToken", userDto.ConfirmationToken)
                                .Replace("@SummonId", additionalNotificationData.SummonId.ToString())
                                .Replace("@Domain", this.mailNotificationOptions.SiteDomain)
                    },{
                        MailType.MembersProtocolMail, (userDto, mailTemplate, additionalNotificationData) =>
                        mailTemplate
                                .Replace("@FullName", $"{userDto.FirstName} {userDto.LastName}")
                                .Replace("@TenderName", userDto.TenderName ?? string.Empty)
                                .Replace("@SummonId", additionalNotificationData.SummonId.ToString())
                                .Replace("@Domain", this.mailNotificationOptions.SiteDomain)
                                
                    }
                };
        }

        public async Task<bool> SendMailAsync(MailNotificationDto mailNotificationDto, string replyToMail = "", Dictionary<string, byte[]> fileList = null)
        {
            return await AsyncSendMail(mailNotificationDto.RecepientEmail, mailNotificationDto.FreeText,
                mailNotificationDto.Subject, fileList, !string.IsNullOrWhiteSpace(mailNotificationDto.DisplayName) ? mailNotificationDto.DisplayName : this.mailNotificationOptions.DisplayName,
                this.mailNotificationOptions.EmailFrom, replyToMail);
        }

        public async Task<bool> AsyncSendMail(string recipients, string htmlBody, string subject = "", Dictionary<string, byte[]> fileList = null, string displayName = "", string from = "", string replyToMail = "")
        {
            MailMessage mailMessage = new MailMessage
            {
                DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
                BodyEncoding = Encoding.UTF8,
                BodyTransferEncoding = TransferEncoding.Base64,
                SubjectEncoding = Encoding.UTF8
            };

            mailMessage.From = new MailAddress("no_reply@" + from, displayName);

            mailMessage.Subject = subject.Replace('\r', ' ').Replace('\n', ' ');
            mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            if (!string.IsNullOrWhiteSpace(replyToMail))
            {
                MailAddressCollection collectionReply = new MailAddressCollection();
                recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => new MailAddress(i)).ToList().ForEach(i => collectionReply.Add(i));
                foreach (var replyEmail in collectionReply)
                {
                    mailMessage.ReplyToList.Add(replyEmail);
                }
            }

            MailAddressCollection collection = new MailAddressCollection();
            recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => new MailAddress(i)).ToList().ForEach(i => collection.Add(i));

            foreach (MailAddress mailAddress in collection)
            {
                mailMessage.To.Add(mailAddress);
            }

            string textBody = htmlBody;
            AlternateView textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
            mailMessage.AlternateViews.Add(textView);

            StringBuilder htmlContent = new StringBuilder();
            htmlContent.AppendLine(htmlBody);

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlContent.ToString(), Encoding.UTF8, "text/html");
            mailMessage.AlternateViews.Add(htmlView);

            if (fileList != null)
            {
                AddAttachments(mailMessage, fileList);
            }

            return await SendMailAsync(mailMessage);
        }

        internal async Task<bool> SendMailAsync(MailMessage message)
        {
            try
            {
                SmtpClient _Client = new SmtpClient(this.mailNotificationOptions.Host, this.mailNotificationOptions.Port);
                _Client.DeliveryMethod = this.mailNotificationOptions.DeliveryMethod;
                _Client.EnableSsl = this.mailNotificationOptions.EnableSsl;
                _Client.Timeout = this.mailNotificationOptions.Timeout;
                _Client.UseDefaultCredentials = this.mailNotificationOptions.DefaultCredentials;
                _Client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
                await _Client.SendMailAsync(message);
                return true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                this.logger.LogError(ex, "SmtpFailedRecipientsException");
            }
            catch (SmtpException ex)
            {
                this.logger.LogError(ex, "SmtpException");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception");
            }
            return false;
        }

        internal void AddAttachments(MailMessage mailMessage, Dictionary<string, byte[]> fielsListArray)
        {
            if (!fielsListArray.Any())
                return;

            foreach (KeyValuePair<string, byte[]> file in fielsListArray)
            {
                Attachment data = new Attachment(new MemoryStream(file.Value), file.Key, MediaTypeNames.Application.Octet);
                mailMessage.Attachments.Add(data);
            }
        }

        private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            object token = (object)e.UserState;
            if (e.Cancelled)
            {
                this.logger.LogError("MailSender_Log_Cancelled", new { E = e, TOKEN = token });
            }
            if (e.Error != null)
            {
                this.logger.LogError("MailSender_Log_Error", new { E = e, TOKEN = token });
            }
        }
    }
}
