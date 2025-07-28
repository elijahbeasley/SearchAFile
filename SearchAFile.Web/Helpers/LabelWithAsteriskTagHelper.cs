using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SearchAFile.Web.TagHelpers
{
    [HtmlTargetElement("label", Attributes = "asp-for, show-asterisk")]
    public class LabelWithAsteriskTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        [HtmlAttributeName("show-asterisk")]
        public bool ShowAsterisk { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var metadata = For.Metadata;
            var displayName = metadata.DisplayName ?? metadata.PropertyName;
            var isRequired = metadata.ValidatorMetadata.Any(v => v is RequiredAttribute);

            // Get the child content (e.g., any custom inner HTML inside <label>...</label>)
            var childContent = output.GetChildContentAsync().Result;
            var customContent = childContent.GetContent();
            var hasCustomContent = !string.IsNullOrWhiteSpace(customContent);

            if (!hasCustomContent)
            {
                output.Content.SetHtmlContent(displayName);
            }
            else
            {
                output.Content.SetHtmlContent(customContent); // reapply user content
            }

            if (ShowAsterisk && isRequired)
            {
                output.Content.AppendHtml("<span class='text-danger'>*</span>");
            }
        }
    }
}