using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace E_Commerce_BackEnd.Services.emailService;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// async email sending 
    /// </summary>
    /// <param name="toEmail">Receiver email</param>
    /// <param name="subject">subject of the email</param>
    /// <param name="body">body of the email</param>
    
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        Console.WriteLine("IN SENDEMAILASYNC");
        Console.WriteLine("IN SENDEMAILASYNC");
        Console.WriteLine("IN SENDEMAILASYNC");
        Console.WriteLine("IN SENDEMAILASYNC");
        Console.WriteLine(_emailSettings.SenderEmail);
        email.From.Add(new MailboxAddress( _emailSettings.SenderName, _emailSettings.SenderEmail));
        email.To.Add(new MailboxAddress(toEmail,toEmail));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };
        
        var expirationDate = DateTime.UtcNow.AddHours(1);
        email.Headers.Add("Expiry-Date", expirationDate.ToString("R"));
        
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
        
       
    }
}