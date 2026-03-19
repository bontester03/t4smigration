using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApit4s.ViewModels
{
    public class CreateParentRewardViewModel
    {
        
            // ✅ POSTED fields
            public string Title { get; set; }
            public string Description { get; set; }
            public int CoinCost { get; set; }
            public bool RequiresParentApproval { get; set; }
            public DateTime? ValidFromUtc { get; set; }
            public DateTime? ValidToUtc { get; set; }
            public int? CooldownDaysPerChild { get; set; }

            public bool IsCommon { get; set; } // true = apply to all children
            public int? ChildId { get; set; }  // optional if IsCommon is true

            // ❌ Do NOT bind this on POST (avoid validation errors)
            [BindNever]
            public List<SelectListItem> Children { get; set; } = new();
     

    }
}
