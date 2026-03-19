using Microsoft.AspNetCore.Mvc.Rendering;
using WebApit4s.Models;

public class GuestLinkListViewModel
{
    public int? SchoolId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public IEnumerable<SelectListItem> Schools { get; set; }
    public List<GuestRegistrationLink> GuestLinks { get; set; } = new();
}
