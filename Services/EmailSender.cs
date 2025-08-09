using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;

namespace BestStoreApi.Services
{
    public class EmailSender
    {

        private readonly string apiKey;
        private readonly string FromEmail;
        private readonly string SenderName;

        public EmailSender(IConfiguration configuration)
        {
            apiKey = configuration["EmailSender:ApiKey"]!;
            FromEmail = configuration["EmailSender:FromEmail"]!;
            SenderName = configuration["EmailSender:SenderName"]!;

        }

        //private static void Main()
        //{
        //    SendEmail().Wait();
        //}

        public async Task SendEmail(string EmailSubject, string toEmail, string MailRecievingUser, string EmailMessage)
        {
           
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(FromEmail, SenderName);
            var to = new EmailAddress(toEmail, MailRecievingUser);
            var plainTextContent = EmailMessage;
            var htmlContent = "";
            var msg = MailHelper.CreateSingleEmail(from, to, EmailSubject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
