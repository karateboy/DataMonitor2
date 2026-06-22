namespace DataMonitor2.Models;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_settings.SmtpServer, _settings.Port);
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        client.EnableSsl = _settings.EnableSsl;

        var mail = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(to);

        await client.SendMailAsync(mail);
    }
}
