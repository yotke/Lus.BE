using com.ladpc;
using com.ladpc.commons.services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lus.Authorization.Models;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Options;
using Lus.Contracts.Users;
using Newtonsoft.Json;

namespace Lus.NotificationCenter.SmsServices
{
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> logger;
        private readonly SmsNotificationOptions smsNotificationOptions;

        private static readonly Dictionary<SmsType, string> SMSTemplate = new Dictionary<SmsType, string>
        {
            { SmsType.LogIn, @"שלום, קוד אימות חד פעמי הינו:
{0}
OneCity" },
            { SmsType.ResetPassword, @"{0} {1} ,שלום רב !
קוד אימות לאתר Lus באינטרנט הוא: {2}
OneCity" },
            { SmsType.LogInWithoutPassword, @"{0} {1} ,שלום רב !
קוד אימות לאתר Lus באינטרנט הוא: {2}
OneCity" },
            { SmsType.SignatureResetPassword, @"{0} {1} שלום רב, לאחר דיון הוועדה הנך מתבקש לחתום על החלטת הוועדה.
על מנת להשלים את התהליך יש להיכנס ללינק המצורף.
{2}
,בברכה
Lus" },
            { SmsType.ApplicationVerification,  @"שלום {1} , 
הגשת מועמדותך למכרז {0} ב{2} התקבלה בהצלחה. 
Lus 
אתר העבודה והמכרזים של ישראל" },
                        { SmsType.ApplicationModified,  @"שלום {1} , 
בוצע שינוי במועמדותך למכרז {0} ב{2}. 
Lus 
אתר העבודה והמכרזים של ישראל" },
            { SmsType.ProtocolClosed,  @"שלום {1} , 
פרוטוקול ועדה נשלח אליך למייל. 
Lus - אתר העבודה והמכרזים של ישראל" }
        };


        private static readonly Dictionary<SmsType, Func<UserDto, SmsType, string?, AdditionalNotificationDataDto, string>> TemplateFormatter =
            new Dictionary<SmsType, Func<UserDto, SmsType, string?, AdditionalNotificationDataDto, string>>
            {
                { SmsType.LogIn, (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType], user.SmsVerificationToken) },
                { SmsType.ResetPassword,  (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType], user.FirstName, user.LastName, user.SmsVerificationToken) },
                { SmsType.LogInWithoutPassword,  (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType], user.FirstName, user.LastName, user.SmsVerificationToken) },
                { SmsType.SignatureResetPassword,  (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType], user.FirstName, user.LastName, additionalData?.CommitteeSigningMemberUrl + user.ConfirmationToken) },
                { SmsType.ApplicationVerification,  (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType],  user.TenderName ?? string.Empty, user.FirstName, user.OrganizationName ?? "ארגון") },
                { SmsType.ApplicationModified,  (user, smsType, alternativeMsg, additionalData) => alternativeMsg ?? string.Format(SMSTemplate[smsType],  user.TenderName ?? string.Empty, user.FirstName, user.OrganizationName ?? "ארגון") }
            };

        public SmsService(ILogger<SmsService> logger, IOptions<SmsNotificationOptions> options)
        {
            this.logger = logger;
            this.smsNotificationOptions = options.Value;
        }

        public string GenerateStringCode()
        {
            Random rnd = new Random();
            string code = string.Empty;
            int codeLength = rnd.Next(this.smsNotificationOptions.SmsCodeLength, (this.smsNotificationOptions.SmsCodeLength * 2));
            while (code.Length <= codeLength)
                code += rnd.Next(0, 111).ToString();

            return code.Substring(0, this.smsNotificationOptions.SmsCodeLength);
        }

        public string GetSmsTemplate(UserDto user, SmsType typeOfNotification, string alternativeMsg = null, AdditionalNotificationDataDto additionalData = null) => TemplateFormatter[typeOfNotification](user, typeOfNotification, alternativeMsg, additionalData);

        public async Task<string> SendSmsAsync(SmsNotificationDto smsNotificationDto, bool isError = false)
        {
            try
            {
                ReturnObject response = await doSend(smsNotificationDto.Message, smsNotificationDto.PhoneNumber);

                if (response.Msg.RcNumber > 0 && response.Msg.RcType == 9 && !isError)
                {
                    this.logger.LogError("ErrorToSendSms", ToJSON(response));
                    return await SendSmsAsync(smsNotificationDto, true);
                }

                return ToJSON(response);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "ErrorToSendSms");
                throw ex;
            }
        }

        private async Task<ReturnObject> doSend(string text, string phone)
        {
            var client = new PortalSmsManagerWSClient();
            try
            {
                var phones = new ArrayOfJavaLangstring_literal();
                phones.Add(phone);
                ;
                if (!string.IsNullOrWhiteSpace(this.smsNotificationOptions.DebugModePhones))
                {
                    phones = GetDebugPhones();
                }

                if (this.smsNotificationOptions.IsDebugMode)
                {
                    return new ReturnObject
                    {
                        Msg = new Msg
                        {
                            RcNumber = 0,
                            RcType = 1,
                            RcVersion = "1.0",
                            RcOwner = 0
                        },
                        Globals = new Globals()
                    };
                }
                else
                {
                    var obj = await client.addSmsRequestAsync(getHeader(), text,
                        this.smsNotificationOptions.OnlyIsraelPhone, phones, "", false, "now", "", "",
                        this.smsNotificationOptions.Sender, this.smsNotificationOptions.Signature, "", "", "");

                    return obj.Body.@return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private ArrayOfJavaLangstring_literal GetDebugPhones()
        {
            var phones = this.smsNotificationOptions.DebugModePhones.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var array = new ArrayOfJavaLangstring_literal();
            foreach (var phone in phones)
            {
                array.Add(phone);
            }

            return array;
        }

        private SystemHeader getHeader()
        {
            return new SystemHeader
            {
                Customer = this.smsNotificationOptions.HeaderCustomer,
                Recipient = this.smsNotificationOptions.HeaderRecipient,
                Sender = this.smsNotificationOptions.HeaderSender,
                UserId = this.smsNotificationOptions.HeaderUserId,
                UserPass = this.smsNotificationOptions.HeaderUserPass,
            };
        }

        private string ToJSON(object o)
        {
            var serializerSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            return JsonConvert.SerializeObject(o, Formatting.None, serializerSettings);
        }
    }
}
