using System.ComponentModel.DataAnnotations;

namespace WebApit4s.ViewModels.MultiStepVM
{
    public class GuestRegistrationStep3ViewModel
    {
        public GuestRegistrationStep3ViewModel()
        {
            PhysicalActivityScore = -1;
            BreakfastScore = -1;
            FruitVegScore = -1;
            SweetSnacksScore = -1;
            FattyFoodsScore = -1;
        }

        [Required]
        public string Code { get; set; }

        [Required(ErrorMessage = "Please select a physical activity score.")]
        [Range(0, 4, ErrorMessage = "Invalid selection.")]
        public int PhysicalActivityScore { get; set; }

        [Required(ErrorMessage = "Please select a breakfast score.")]
        [Range(0, 4, ErrorMessage = "Invalid selection.")]
        public int BreakfastScore { get; set; }

        [Required(ErrorMessage = "Please select a fruit/veg score.")]
        [Range(0, 4, ErrorMessage = "Invalid selection.")]
        public int FruitVegScore { get; set; }

        [Required(ErrorMessage = "Please select a sweet snacks score.")]
        [Range(0, 4, ErrorMessage = "Invalid selection.")]
        public int SweetSnacksScore { get; set; }

        [Required(ErrorMessage = "Please select a fatty foods score.")]
        [Range(0, 4, ErrorMessage = "Invalid selection.")]
        public int FattyFoodsScore { get; set; }
    }

}
