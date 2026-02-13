using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.infra.Configuration;

namespace soat.eleven.kutcut.infra.Services
{
    public class EmailNotificationService : IUserNotificaton
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IOptions<EmailSettings> settings,
            ILogger<EmailNotificationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public bool NotifyUser(UserDto user, NotifyMessage message)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                emailMessage.To.Add(new MailboxAddress(user.Name, user.Email));
                emailMessage.Subject = message.Title;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                        <body>
                            <h2>{message.Title}</h2>
                            <p>Olá {user.Name},</p>
                            <p>{message.Body}</p>
                            <br/>
                            <p>Atenciosamente,<br/>Equipe KutCut</p>
                        </body>
                        </html>",
                    TextBody = $"Olá {user.Name},\n\n{message.Body}\n\nAtenciosamente,\nEquipe KutCut"
                };

                emailMessage.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect(_settings.SmtpServer, _settings.Port, _settings.UseSsl);
                    client.Authenticate(_settings.UserName, _settings.Password);
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }

                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", 
                    user.Email, message.Title);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
                return false;
            }
        }
    }
}
