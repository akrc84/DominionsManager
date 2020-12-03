using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DominionsManager
{
    public static class GmailClient
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        static string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend };
        static string ApplicationName = "Gmail API .NET Quickstart";

        public static List<GameTurnInfo> GetMail(string mailSubject, int? turn = null)
        {
            List<GameTurnInfo> gameTurnsList = new List<GameTurnInfo>();
            GmailService service = GetService();

            UsersResource.MessagesResource.ListRequest listRequest = service.Users.Messages.List("me");
            listRequest.LabelIds = "INBOX";
            //re.Q = "is:unread"; //only get unread;
            listRequest.Q = "from:" + Properties.Settings.Default.TurnMail + " subject:" + mailSubject;

            if (turn == 1)
                listRequest.Q = listRequest.Q + " started! First turn attached";
            else if (turn != null)
                listRequest.Q = listRequest.Q + " AND subject:turn " + turn.ToString() + "  has:attachment";
            else
                listRequest.Q = listRequest.Q + "  has:attachment";

            ListMessagesResponse listMessagesResponse = listRequest.Execute();

            if (listMessagesResponse != null && listMessagesResponse.Messages != null)
            {
                foreach (var email in listMessagesResponse.Messages)
                {
                    UsersResource.MessagesResource.GetRequest emailInfoReq = service.Users.Messages.Get("me", email.Id);
                    //TODO get only what we need
                    //emailInfoReq.Fields = "subject:"; 

                    var emailInfoResponse = emailInfoReq.Execute();

                    if (emailInfoResponse != null)
                    {
                        String from = "";
                        String date = "";
                        String subject = "";
                        String body = "";
                        string data = "";
                        //loop through the headers and get the fields we need...
                        foreach (var mParts in emailInfoResponse.Payload.Headers)
                        {
                            if (mParts.Name == "Date")
                            {
                                date = mParts.Value;
                            }
                            else if (mParts.Name == "From")
                            {
                                from = mParts.Value;
                            }
                            else if (mParts.Name == "Subject")
                            {
                                subject = mParts.Value;
                            }

                            if (date != "" && from != "")
                            {
                                if (emailInfoResponse.Payload.Parts == null && emailInfoResponse.Payload.Body != null)
                                    data = DecodeBase64String(emailInfoResponse.Payload.Body.Data);
                                else
                                    body = GetNestedBodyParts(emailInfoResponse.Payload.Parts, "");
                            }

                        }
                        GameAttachment gameAttachment = null;

                        foreach (var mParts in emailInfoResponse.Payload.Parts)
                        {
                            if (!mParts.Filename.Contains(".trn"))
                                continue;

                            string attachId = mParts.Body.AttachmentId;

                            gameAttachment = new GameAttachment();

                            UsersResource.MessagesResource.AttachmentsResource.GetRequest emailAtachReq = service.Users.Messages.Attachments.Get("me", email.Id, attachId);
                            MessagePartBody emailAtachResponse = emailAtachReq.Execute();
                            gameAttachment.data = DecodeBase64StringToByte(emailAtachResponse.Data);
                            gameAttachment.fileName = mParts.Filename;
                        }

                        gameTurnsList.Add(new GameTurnInfo(mailSubject, subject, gameAttachment));
                    }
                }
            }

            return gameTurnsList;
        }

        internal static void SendMail(GameTurnInfo gameTurnInfo)
        {
            GmailService service = GetService();

            MailMessage mail = new MailMessage();
            mail.Subject = gameTurnInfo.GameName + " Turn " + (gameTurnInfo.LastTurn).ToString();
            mail.Body = string.Empty;
            mail.From = new MailAddress(Properties.Settings.Default.MyMail);
            mail.IsBodyHtml = true;

            string attImg = gameTurnInfo.GameTurnFile.FullName;

            mail.Attachments.Add(new Attachment(attImg));
#if (DEBUG)
            #warning fill with test mail
            mail.To.Add(new MailAddress(""));
#else
            mail.To.Add(new MailAddress(Properties.Settings.Default.TurnMail));
#endif

            MimeKit.MimeMessage mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mail);


            Message message = new Message();

            message.Raw = Base64UrlEncode(mimeMessage.ToString());

            UsersResource.MessagesResource.SendRequest re = service.Users.Messages.Send(message, "me");

            var res = re.Execute();
        }

        private static GmailService GetService()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + new FileDataStore(credPath, true).FolderPath);
            }

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            return service;
        }

        static byte[] DecodeBase64StringToByte(string s)
        {
            var ts = s.Replace("-", "+");
            ts = ts.Replace("_", "/");
            return Convert.FromBase64String(ts);
        }

        static String DecodeBase64String(string s)
        {
            var ts = s.Replace("-", "+");
            ts = ts.Replace("_", "/");
            var bc = Convert.FromBase64String(ts);
            var tts = Encoding.UTF8.GetString(bc);

            return tts;
        }

        static String GetNestedBodyParts(IList<MessagePart> part, string curr)
        {
            string str = curr;
            if (part == null)
            {
                return str;
            }
            else
            {
                foreach (var parts in part)
                {
                    if (parts.Parts == null)
                    {
                        if (parts.Body != null && parts.Body.Data != null)
                        {
                            var ts = DecodeBase64String(parts.Body.Data);
                            str += ts;
                        }
                    }
                    else
                    {
                        return GetNestedBodyParts(parts.Parts, str);
                    }
                }

                return str;
            }
        }

        public static string Base64UrlEncode(string s)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(inputBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}