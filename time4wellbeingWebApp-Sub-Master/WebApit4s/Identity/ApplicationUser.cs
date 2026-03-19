using Microsoft.AspNetCore.Identity;

using WebApit4s.Models;

namespace WebApit4s.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public int? ReferralTypeId { get; set; }
        public ReferralType ReferralType { get; set; } = null!;

        // Navigation properties from your existing model
        public PersonalDetails? PersonalDetails { get; set; }
        public ICollection<QuestionAnswer>? QuestionAnswers { get; set; }
        public ICollection<WeeklyMeasurements>? WeeklyMeasurements { get; set; } = new List<WeeklyMeasurements>();
        public ICollection<HealthScore> HealthScores { get; set; } = new List<HealthScore>();
        public ICollection<Child> Children { get; set; } = new List<Child>();
        public ICollection<ConsentAnswer> ConsentAnswers { get; set; } = new List<ConsentAnswer>();
        public ICollection<RegistrationReminder> RegistrationReminders { get; set; } = new List<RegistrationReminder>();

        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        public ICollection<GameTask> CreatedGameTasks { get; set; } = new List<GameTask>();

        public UserType UserType { get; set; }

        public bool IsApprovedByAdmin { get; set; } = true; // Set to false for pending Employees or Guests

        public bool EnableAIGoals { get; set; } = true;

        public bool IsGuestUser { get; set; } = true; // mark as guest-only registration
        public bool IsLoginEnabled { get; set; } = false; // optional future login

    }

    public enum UserType
    {
        Parent = 1,
        Employee = 2,
        Guest = 3,
        Admin = 4
    }

}
