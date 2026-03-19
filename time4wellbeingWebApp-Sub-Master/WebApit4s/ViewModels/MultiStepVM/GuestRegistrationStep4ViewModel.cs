using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels.MultiStepVM
{
    public class GuestRegistrationStep4ViewModel
    {
        public string Code { get; set; }

        [Display(Name = "Would you like to register another child?")]
        public bool AddAnotherChild { get; set; }

        public List<ConsentQuestionViewModel> ConsentQuestions { get; set; } = new();
    }
}
