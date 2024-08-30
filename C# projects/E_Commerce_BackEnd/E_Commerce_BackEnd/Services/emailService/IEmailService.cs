namespace E_Commerce_BackEnd.Services.emailService;

public interface IEmailService
{
    public Task SendEmailAsync(string toEmail, string subject, string body);
}