using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using WebApit4s.DAL;
using WebApit4s.Models;

namespace WebApit4s.Services
{
    public class RegistrationReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegistrationReminderService> _logger;
    

        public RegistrationReminderService(
            IServiceProvider serviceProvider,
            IEmailSender emailSender,
            ILogger<RegistrationReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _emailSender = emailSender;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendReminders(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running registration reminder job.");
                }

                // Sleep for 24 hours (you can reduce this for testing)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            }
        }

        private async Task SendReminders(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TimeContext>(); // replace with your context name

            var cutoff = DateTime.UtcNow.AddHours(-24);

            var users = await context.Users
                .Where(u => u.RegistrationDate <= cutoff)
                .Where(u =>
                    !context.Children.Any(c => c.UserId == u.Id) ||
                    !context.PersonalDetails.Any(p => p.UserId == u.Id) ||
                    !context.HealthScores.Any(h => h.UserId == u.Id)
                )
                .Where(u => !context.RegistrationReminders.Any(r => r.UserId == u.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                var messageBody = $@"
                <p>Hello {user.Email},</p>
                <p>Please log in and complete your unfinished registration:</p>
                <ul>
                    <li>Child Details</li>
                    <li>Parent/Guardian Info</li>
                    <li>Initial Health Score</li>
                </ul>
                <p><a href='https://www.time4wellbeinguk.com/login'>Login Here</a></p>
                <p>This is a computer-generated email. Do not reply. For support, email <a href='mailto:info@time4sportuk.com'>info@time4sportuk.com</a>.</p>";

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Complete Your Registration - Time4Wellbeing",
                    messageBody);

                context.RegistrationReminders.Add(new RegistrationReminder
                {
                    UserId = user.Id,
                    SentAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation($"{users.Count} reminder email(s) sent at {DateTime.UtcNow}.");
        }
    }

}
