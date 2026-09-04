using Domain.Common;

namespace Domain;

public class SocialMedia : OrderableBaseEntity
{
    public string? Name { get; set; }
    public string? ImagePath { get; set; }
    public string? Url { get; set; }
}