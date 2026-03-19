using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class GuestLinkCreateViewModel
{
    [Required]
    [Display(Name = "School")]
    public int SchoolId { get; set; }

    [Required]
    [Display(Name = "Class")]
    public int ClassId { get; set; }

    [Display(Name = "Expiry Date")]
    public DateTime? ExpiryDate { get; set; }

    [Display(Name = "Maximum Uses (Optional)")]
    [Range(1, int.MaxValue, ErrorMessage = "Must be a positive number")]
    public int? MaxUses { get; set; }

    public IEnumerable<SelectListItem> Schools { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Classes { get; set; } = new List<SelectListItem>();
}
