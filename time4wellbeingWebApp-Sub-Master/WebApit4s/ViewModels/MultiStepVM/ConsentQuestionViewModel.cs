using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels.MultiStepVM
{
    public class ConsentQuestionViewModel
    {
        public int ConsentQuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        [RegularExpression("Yes|No", ErrorMessage = "Answer must be 'Yes' or 'No'.")]
        public string Answer { get; set; } = string.Empty;
    }
}
