using ContactBook.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ContactBook.Services
{
    public class EmailService : IEmailSender
    {
        private readonly MailSettings _mailSettings;
        //going to config to get settings then pass here
        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        //create an email message (from, to, subject, and body)
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            //?? = null colelesing operator
            //email sender gets value but if null it will get right side of code 
            //Username
            var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");
            //using mimekit to handle email format ==> Message
            MimeMessage newEmail = new();

            //create the new email [Sending]
            newEmail.Sender = MailboxAddress.Parse(emailSender);

            //splits the to list ==> loops over email address sent in 
            //one or many emails
            foreach (var emailAddress in email.Split(";"))
            {
                //taking each string and adding to the "to list"
                newEmail.To.Add(MailboxAddress.Parse(emailAddress));
            }

            //email subject
            newEmail.Subject = subject;

            //formats msg for us / creating our msg
            BodyBuilder emailBody = new();
            emailBody.HtmlBody = htmlMessage;
            //added to our email
            newEmail.Body = emailBody.ToMessageBody();

            //At this point lets log into our smtp client "google"
            using SmtpClient smtpClient = new();

            try
            {
                var host = _mailSettings.Host;
                var port = _mailSettings.Port;
                var password = _mailSettings.Password;

                //var host = _mailSettings.MailHost ?? Environment.GetEnvironmentVariable("MailHost");

                //if not = 0 look local else look into the environment and turn into int
                //var port = _mailSettings.MailPort != 0 ? _mailSettings.MailPort : int.Parse(Environment.GetEnvironmentVariable("Port"));
                //var password = _mailSettings.MailPassword ?? Environment.GetEnvironmentVariable("MailPassword");

                await smtpClient.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(emailSender, password);

                //sending email we made / then we disconnect 
                await smtpClient.SendAsync(newEmail);
                await smtpClient.DisconnectAsync(true);

            }
            catch (Exception ex)
            {
                var message = ex.Message;
                throw;
            }


        }
    }
}