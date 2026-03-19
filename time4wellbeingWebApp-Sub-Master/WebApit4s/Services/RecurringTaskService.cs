using WebApit4s.DAL;
using WebApit4s.Models;
using Microsoft.EntityFrameworkCore;
using System;


namespace WebApit4s.Services
{
    public class RecurringTaskService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public RecurringTaskService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[RecurringTaskService] Running at {DateTime.UtcNow}");

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TimeContext>();



                var recurringTemplates = await db.ChildGameTasks
     .Where(t => t.IsRecurringDaily && t.IsGenerated == false)
     .ToListAsync();

                foreach (var template in recurringTemplates)
                {
                    var today = DateTime.UtcNow.Date;

                    bool alreadyExists = await db.ChildGameTasks.AnyAsync(t =>
                        t.ChildId == template.ChildId &&
                        t.GameTaskId == template.GameTaskId &&
                        t.AssignedDate.Date == today);

                    if (!alreadyExists)
                    {
                        db.ChildGameTasks.Add(new ChildGameTask
                        {
                            ChildId = template.ChildId,
                            GameTaskId = template.GameTaskId,
                            AssignedDate = today,
                            IsRecurringDaily = true,
                            IsGenerated = true, // ✅ mark as auto-generated
                            ExpiryDate = today.AddDays(1).AddTicks(-1)
                        });

                        Console.WriteLine($"[RecurringTaskService] ✅ New recurring task assigned to child {template.ChildId}");
                    }
                }



                await db.SaveChangesAsync();

                Console.WriteLine($"[RecurringTaskService] Sleeping until next run...");

                // For testing: run every 30 seconds
               // await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                //Wait until next day(24 hours)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }

}
