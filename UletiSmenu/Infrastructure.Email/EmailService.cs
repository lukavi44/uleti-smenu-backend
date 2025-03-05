using Core.Interfaces;
using System.Net.Mail;

namespace Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;

    public EmailService(SmtpClient smtpClient, string fromEmail)
    {
        _smtpClient = smtpClient;
        _fromEmail = fromEmail;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var mailMessage = new MailMessage(_fromEmail, toEmail, subject, message);
        mailMessage.IsBodyHtml = true;
        _smtpClient.UseDefaultCredentials = false;
        _smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

        await _smtpClient.SendMailAsync(mailMessage);
    }
}
