using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("range-slider")]
public class RangeSliderTagHelper : TagHelper
{
    // ===== Model binding (optional)
    [HtmlAttributeName("from-for")]
    public ModelExpression? FromFor { get; set; }

    [HtmlAttributeName("to-for")]
    public ModelExpression? ToFor { get; set; }

    // ===== Fallback literal values
    [HtmlAttributeName("from")]
    public int? From { get; set; }

    [HtmlAttributeName("to")]
    public int? To { get; set; }

    public int Min { get; set; }
    public int Max { get; set; }

    // ===== Accept names for from and to input fields
    [HtmlAttributeName("from-name")]
    public string? FromName { get; set; }

    [HtmlAttributeName("to-name")]
    public string? ToName { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Resolve final values
        var fromValue = FromFor?.Model as int? ?? From ?? Min;
        var toValue   = ToFor?.Model as int? ?? To ?? Max;

        var fromName = FromName ?? FromFor?.Name;
        var toName   = ToName ?? ToFor?.Name;

        output.TagName = "div";
        output.Attributes.SetAttribute("class", "range_container");
        output.Attributes.SetAttribute("data-range-slider", "");

        output.Content.SetHtmlContent($@"
<div class='sliders_control'>
    <input type='range'
           class='fromSlider'
           min='{Min}'
           max='{Max}'
           value='{fromValue}' />

    <input type='range'
           class='toSlider'
           min='{Min}'
           max='{Max}'
           value='{toValue}' />
</div>

<div class='form_control'>
    <div class='form_control_container'>
        <div>Min</div>
        <input type='number'
               class='fromInput'
               {(fromName != null ? $"name='{fromName}'" : "")}
               value='{fromValue}'
               min='{Min}'
               max='{Max}' />
    </div>

    <div class='form_control_container'>
        <div>Max</div>
        <input type='number'
               class='toInput'
               {(toName != null ? $"name='{toName}'" : "")}
               value='{toValue}'
               min='{Min}'
               max='{Max}' />
    </div>
</div>");
    }
}
