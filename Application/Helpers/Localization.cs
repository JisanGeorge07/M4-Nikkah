using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Helpers;

public static class Localization
{
    public static string? GetEnglish(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        try
        {
            var data = JsonSerializer.Deserialize<CultureHelper>(text);
            return data?.English;
        }
        catch
        {
            return "";
        }
    }

    public static string? GetArabic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        try
        {
            var data = JsonSerializer.Deserialize<CultureHelper>(text);
            return data?.Arabic;
        }
        catch
        {
            return "";
        }
    }

    public static string Serialize(string? englishText, string? arabicText)
    {
        var result = new CultureHelper
        {
            English = englishText ?? "",
            Arabic = arabicText ?? ""
        };
        return JsonSerializer.Serialize(result);
    }

    public static string? GetLocalisationValue(string culture, string text)
    {
        return culture.ToLower() == "ar" ? GetArabic(text) : GetEnglish(text);
    }
}

public class CultureHelper
{
    [JsonPropertyName("en")] public string English { get; set; } = "";

    [JsonPropertyName("ar")] public string Arabic { get; set; } = "";
}