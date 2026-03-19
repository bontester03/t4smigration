using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApit4s.Models
{
    public class AdminChildFullDetailsViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string ReferralType { get; set; }
        public int ReferralTypeId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public string ChildName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }

        public string ParentGuardianName { get; set; }
        public string RelationshipToChild { get; set; }
        public string TeleNumber { get; set; }
        public string ParentEmail { get; set; }
        public string Postcode { get; set; }
        public string School { get; set; }
        public string Class { get; set; }
        public string GPPractice { get; set; }

        public int ChildId { get; set; } // ✅ Added for context-specific child updates

        public EngagementStatus EngagementStatus { get; set; }

        public int TotalPoints { get; set; }

        public int? SelectedGameTaskId { get; set; } // For dropdown
        public DateTime? AssignDate { get; set; } = DateTime.Today;
        public List<SelectListItem> GameTaskList { get; set; } = new();
        public bool RequiresSchoolSelection { get; set; }
        public ReferralCategory ReferralTypeCategory { get; set; }

        
        public List<WeeklyMeasurementViewModel> Measurements { get; set; } = new();
        public List<AdminHealthScoreViewModel> HealthScores { get; set; } = new();

        public List<NotificationViewModel> Notifications { get; set; } = new();

        // ✅ Add this
        public string NotificationMessage { get; set; } = string.Empty;

        // ✅ New for Admin Notes
        public List<AdminNoteViewModel> AdminNotes { get; set; } = new();
        public string NewNoteText { get; set; } = string.Empty;

        public string ParentSchool { get; set; } = "N/A";
        public string ParentClass { get; set; } = "N/A";


        public List<AdminConsentAnswerViewModel> ConsentAnswers { get; set; } = new();

    }
}
