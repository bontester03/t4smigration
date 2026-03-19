using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Time4Wellbeing", _configuration["EmailSettings:SenderEmail"]));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlMessage };

        using var client = new SmtpClient();
        await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:Port"]), false);
        await client.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"], _configuration["EmailSettings:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }



    public string LoadTemplate(string templateFileName, Dictionary<string, string> replacements)
    {
        // Get the absolute path to wwwroot
        var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var templatePath = Path.Combine(wwwRootPath, "Templates", "Emails", templateFileName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found: {templatePath}");
        }

        var templateContent = File.ReadAllText(templatePath);

        foreach (var pair in replacements)
        {
            templateContent = templateContent.Replace($"{{{{{pair.Key}}}}}", pair.Value);
        }

        return templateContent;
    }



}
