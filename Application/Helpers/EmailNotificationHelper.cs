using Application.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class EmailNotificationHelper
    {
        private readonly SmtpSettings _smtpsettings;

        public EmailNotificationHelper(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpsettings = smtpSettings.Value;
        }
        public void SendEmail(string email, AlternateView htmlcontentview, string subject)
        {
            try
            {

                MailMessage message = new MailMessage();
                if (string.IsNullOrEmpty(subject))
                {
                    subject = "M4nikah";
                }
                using (var client = new SmtpClient(_smtpsettings.Host, _smtpsettings.Port))
                {
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    message.AlternateViews.Add(htmlcontentview);

                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpsettings.Username, _smtpsettings.Password);

                    switch (_smtpsettings.Security?.ToLower())
                    {
                        case "ssl":
                            client.EnableSsl = true;
                            break;
                        case "none":
                            client.EnableSsl = false;
                            break;
                        case "auto":
                            client.EnableSsl = true; // Default to SSL if auto

                            break;
                        default:
                            client.EnableSsl = true; // Default to SSL if no valid option is provided
                            break;
                    }

                    message.From = new MailAddress(_smtpsettings.SenderEmail);
                    message.Subject = subject;
                    message.IsBodyHtml = true;
                    message.To.Add(email);
                    client.Send(message);
                    Console.WriteLine("Email sent successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }


        }

    }
}
