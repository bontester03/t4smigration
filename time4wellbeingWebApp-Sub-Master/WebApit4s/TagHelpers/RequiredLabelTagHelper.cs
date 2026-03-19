using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace WebApit4s.TagHelpers
{
    [HtmlTargetElement("label", Attributes = ForAttributeName)]
    public class RequiredLabelTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; } = null!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For?.Metadata?.ContainerType != null)
            {
                var property = For.Metadata.ContainerType.GetProperty(For.Metadata.PropertyName);
                var isRequired = property?.GetCustomAttribute<RequiredAttribute>() != null;

                if (isRequired)
                {
                    // Append a red asterisk after the label text
                    output.PostContent.AppendHtml(" <span class='text-danger'>*</span>");
                }
            }
        }
    }
}
