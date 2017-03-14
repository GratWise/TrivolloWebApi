using System;
using System.Net.Mail;
using System.Text;

namespace TrivolloWebApi.Config
{
    public class Utilities
    {
        public const string WS_API_URL = "http://trivollo.gratwise.com/";

        private const string MAIL_HEAD = "Trivollo";
        private const string MAIL_FROM = "no-reply@trivollo.com";

        private const int PASSWORD_RESET_CODE_LENGTH = 6;

        public static string generatePasswordResetCode()
        {
            string input = "0123456789";
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < Utilities.PASSWORD_RESET_CODE_LENGTH; i++)
            {
                ch = input[random.Next(0, input.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public static void sendMail(string recipient, string subject, string emailBody)
        {
            MailMessage mail = new MailMessage();
            mail.To.Add(recipient);
            mail.From = new MailAddress(MAIL_FROM, MAIL_HEAD, System.Text.Encoding.UTF8);
            mail.Subject = subject;
            mail.SubjectEncoding = System.Text.Encoding.UTF8;
            mail.Body = emailBody;
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = true;
            mail.Priority = MailPriority.High;
            SmtpClient client = new SmtpClient();
            client.Send(mail);
        }
    }
}