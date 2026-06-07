using Microsoft.Extensions.Options;
using Lus.Contracts.Options;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using System.Reflection;

namespace Lus.NotificationCenter.CalendarServices
{
    public class CalendarService : ICalendarService
    {
        private readonly MailNotificationOptions mailNotificationOptions;
        private readonly ILogger<CalendarService> logger;

        public CalendarService(IOptions<MailNotificationOptions> options, ILogger<CalendarService> logger)
        {
            this.logger = logger;
            this.mailNotificationOptions = options.Value;
        }

        private SmtpClient CreateSmtpClient(bool IsSendMailAsync)
        {
            SmtpClient _Client = new SmtpClient(this.mailNotificationOptions.Host, this.mailNotificationOptions.Port);
            _Client.DeliveryMethod = this.mailNotificationOptions.DeliveryMethod;
            _Client.EnableSsl = this.mailNotificationOptions.EnableSsl;
            _Client.Timeout = this.mailNotificationOptions.Timeout;
            _Client.UseDefaultCredentials = this.mailNotificationOptions.DefaultCredentials;
            _Client.SendCompleted += IsSendMailAsync ? new SendCompletedEventHandler(ClientSendCompleted) : null;
            return _Client;
        }

        private void ClientSendCompleted(object sender, AsyncCompletedEventArgs e)
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

        private MailAddressCollection ParseMails(string mailAddress)
        {
            string[] tos = mailAddress.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            MailAddressCollection collection = new MailAddressCollection();

            foreach (string to in tos)
            {
                try
                {
                    collection.Add(new MailAddress(to));
                }
                catch { }
            }
            return collection;
        }

        private MailMessage CreateMailMessage()
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            mailMessage.BodyEncoding = UTF8Encoding.UTF8;
            mailMessage.BodyTransferEncoding = TransferEncoding.Base64;
            mailMessage.SubjectEncoding = UTF8Encoding.UTF8;
            return mailMessage;
        }

        private MailAddress SetUpFromMail(string from = null, string displayName = null)
        {
            MailAddress From;
            if (string.IsNullOrWhiteSpace(from))
                from = this.mailNotificationOptions.EmailFrom;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = this.mailNotificationOptions.DisplayName;
            try
            {
                From = new MailAddress(from, displayName);
            }
            catch
            {
                try
                {
                    From = new MailAddress("no_reply@" + from, displayName);
                }
                catch
                {
                    From = new MailAddress("no_reply@" + from, displayName);
                }
            }
            return From;
        }

        private void SendCalanderMail(string recipients, string meetingIcs, string messageBody = "", string subject = "", string displayName = "", string from = "", string ccMail = "", string bccMail = "", string replytoMail = "")
        {
            if (string.IsNullOrWhiteSpace(recipients))
            {
                return;
            }

            #region set up MailMessage
            MailMessage mailMessage = CreateMailMessage();
            mailMessage.Headers.Add("Content-class", "urn:content-classes:calendarmessage");
            mailMessage.From = SetUpFromMail(from, displayName);
            mailMessage.Subject = subject.Replace('\r', ' ').Replace('\n', ' ');
            mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            mailMessage.Body = messageBody;
            #endregion

            #region mail Addressees
            /*`to` Mails*/
            foreach (MailAddress mailAddress in ParseMails(recipients))
            {
                mailMessage.To.Add(mailAddress);
            }
            /*`cc` Mails*/
            if (!string.IsNullOrWhiteSpace(ccMail))
                foreach (MailAddress mailAddress in ParseMails(ccMail))
                {
                    mailMessage.CC.Add(mailAddress);
                }
            /*`bcc` Mails*/
            if (!string.IsNullOrWhiteSpace(bccMail))
                foreach (MailAddress mailAddress in ParseMails(bccMail))
                {
                    mailMessage.Bcc.Add(mailAddress);
                }
            /*`replyto` Mails*/
            if (!string.IsNullOrWhiteSpace(replytoMail))
                foreach (MailAddress mailAddress in ParseMails(replytoMail))
                {
                    mailMessage.ReplyToList.Add(mailAddress);
                }
            #endregion

            #region create calander and insert as view
            ContentType contype = new ContentType("text/calendar");
            contype.Parameters.Add("method", "REQUEST");
            contype.Parameters.Add("name", "Meeting.ics");
            AlternateView avCal = AlternateView.CreateAlternateViewFromString(meetingIcs.ToString(), contype);
            mailMessage.AlternateViews.Add(avCal);
            #endregion

            Send(mailMessage);
        }

        private void Send(MailMessage message)
        {
            try
            {
                SmtpClient client = CreateSmtpClient(false);
                client.Send(message);
            }
            catch (SmtpFailedRecipientsException ex)
            {
                this.logger.LogError(ex, "CalendarAdapter-SmtpFailedRecipientsException");
            }
            catch (SmtpException ex)
            {
                this.logger.LogError(ex, "CalendarAdapter-SmtpException");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "CalendarAdapter-Exception");
            }
        }

        private string CreateCalanderInvite(DateTime MeetingStart, DateTime MeetingEnd, string organizerName, string organizerAddress, CalendarMethodTypes methodEnum, string description = "", string summary = "", string location = "")
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("PRODID:-//Schedule a Meeting");
            str.AppendLine("VERSION:2.0");
            str.AppendLine($"METHOD:{methodEnum.ToString()}");
            str.AppendLine("BEGIN:VEVENT");
            str.AppendLine($"DTSTART:{MeetingStart.ToUniversalTime():yyyyMMddTHHmmssZ}");
            str.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            str.AppendLine($"DTEND:{MeetingEnd.ToUniversalTime():yyyyMMddTHHmmssZ}");
            if (!string.IsNullOrEmpty(location)) str.AppendLine($"LOCATION: {location}");
            str.AppendLine($"UID:{Guid.NewGuid()}");
            if (!string.IsNullOrEmpty(description))
            {
                str.AppendLine($"DESCRIPTION:{description}");
                str.AppendLine($"X-ALT-DESC;FMTTYPE=text/html:{description}");
            }
            if (!string.IsNullOrEmpty(summary)) str.AppendLine($"SUMMARY:{summary}");
            str.AppendLine($"ORGANIZER:MAILTO:{organizerAddress}");
            str.AppendLine($"ATTENDEE;CN=\"{organizerName}\";RSVP=TRUE:mailto:{organizerAddress}");
            str.AppendLine("BEGIN:VALARM");
            str.AppendLine("TRIGGER:-PT15M");
            str.AppendLine("ACTION:DISPLAY");
            str.AppendLine("DESCRIPTION:Reminder");
            str.AppendLine("END:VALARM");
            str.AppendLine("END:VEVENT");
            str.AppendLine("END:VCALENDAR");
            return str.ToString();
        }

        private void SendMailWithIcsAttachment()
        {
            MailMessage msg = new MailMessage();
            msg.Headers.Add("Content-class", "urn:content-classes:calendarmessage");
            //Now we have to set the value to Mail message properties

            //Note Please change it to correct mail-id to use this in your application
            msg.From = new MailAddress("Lus@iula.org.il", "הארגון המזמין");
            //msg.To.Add(new MailAddress("galr@onecity.co.il", "מוזמן לוועדה"));
            msg.To.Add(new MailAddress("automationlcd@gmail.com", "מזמין לוועדה"));

            msg.CC.Add(new MailAddress("galr@onecity.co.il", "מוזמן לוועדה"));

            msg.Subject = "הזמנה לראיון עבודה מיוחד";
            msg.Body = "הנך מוזמן לוועדה מיוחדת";

            // Now Contruct the ICS file using string builder
            StringBuilder str = new StringBuilder();
            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("PRODID:-//Schedule a Meeting");
            str.AppendLine("VERSION:2.0");
            str.AppendLine("METHOD:REQUEST");
            str.AppendLine("BEGIN:VEVENT");
            str.AppendLine($"DTSTART:{new DateTime(2022, 10, 25, 09, 00, 0):yyyyMMddTHHmmssZ}");
            str.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            str.AppendLine($"DTEND:{new DateTime(2022, 10, 25, 12, 30, 0):yyyyMMddTHHmmssZ}");
            str.AppendLine("LOCATION: " + "מיקום הפגישה - כתובת");
            str.AppendLine($"UID:{Guid.NewGuid()}");
            str.AppendLine($"DESCRIPTION:{msg.Body}");
            str.AppendLine($"X-ALT-DESC;FMTTYPE=text/html:{msg.Body}");
            str.AppendLine($"SUMMARY:{msg.Subject}");
            str.AppendLine($"ORGANIZER:MAILTO:{"galr@onecity.co.il"}");

            str.AppendLine($"ATTENDEE;CN=\"{msg.To[0].DisplayName}\";RSVP=TRUE:mailto:{msg.To[0].Address}");

            str.AppendLine("BEGIN:VALARM");
            str.AppendLine("TRIGGER:-PT15M");
            str.AppendLine("ACTION:DISPLAY");
            str.AppendLine("DESCRIPTION:Reminder");
            str.AppendLine("END:VALARM");
            str.AppendLine("END:VEVENT");
            str.AppendLine("END:VCALENDAR");

            //Now sending a mail with attachment ICS file.                     
            SmtpClient client = CreateSmtpClient(false);

            ContentType contype = new ContentType("text/calendar");
            contype.Parameters.Add("method", "REQUEST");
            contype.Parameters.Add("name", "Meeting.ics");

            //File.WriteAllText(@"D:\LADPC-Workspace\HR-Payroll\Net\Lus\Lus\Logs_Data\Meeting.ics", str.ToString());

            AlternateView avCal = AlternateView.CreateAlternateViewFromString(str.ToString(), contype);
            msg.AlternateViews.Add(avCal);
            client.Send(msg);
        }

        public async Task SendCalendarInviteAsync(CalendarNotificationDto calendarNotificationDto)
        {
            var calendarInvite = CreateCalanderInvite(
                calendarNotificationDto.StartDate,
                calendarNotificationDto.StartDate.AddMinutes(calendarNotificationDto.Interval),
                $"{calendarNotificationDto.User.FirstName} {calendarNotificationDto.User.LastName}",
                !string.IsNullOrWhiteSpace(this.mailNotificationOptions.DebugCalendarEmails) ? this.mailNotificationOptions.DebugCalendarEmails : calendarNotificationDto.User.Email,
                calendarNotificationDto.CalendarMethod,
            calendarNotificationDto.CalendarMethod == CalendarMethodTypes.REQUEST ? $"למכרז {calendarNotificationDto.TenderName}" : $"ביטול זימון למכרז '{calendarNotificationDto.TenderName}'",
            calendarNotificationDto.CalendarMethod == CalendarMethodTypes.REQUEST ? $"זימון לוועדה - {calendarNotificationDto.CommitteeName}" : $"ביטול זימון לוועדה - '{calendarNotificationDto.CommitteeName}'",
                calendarNotificationDto.SummonAddress
             );

            var recipientEmail = !string.IsNullOrWhiteSpace(this.mailNotificationOptions.DebugEmails)
                ? this.mailNotificationOptions.DebugEmails
                : calendarNotificationDto.SendUserEmail;

            SendCalanderMail(recipientEmail, calendarInvite,
                calendarNotificationDto.CalendarMethod == CalendarMethodTypes.REQUEST ? $"הנך מוזמנ/ת לוודעה לבדיקת התאמתך למשרה - {calendarNotificationDto.TenderName}" : $"ביטול זימון למשרה - '{calendarNotificationDto.TenderName}'",
                calendarNotificationDto.CalendarMethod == CalendarMethodTypes.REQUEST ? $"זימון לוועדה - {calendarNotificationDto.CommitteeName}" : $"ביטול זימון לוועדה - '{calendarNotificationDto.CommitteeName}'",
                calendarNotificationDto.OrganizationName, "Lus@iula.org.il", null, null, calendarNotificationDto.User.Email);

        }
    }
}