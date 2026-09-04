using Domain.Common;

namespace Domain;

public class PageSettings : OrderableBaseEntity
{
    public string? Name { get; set; }
    public string? ParentName { get; set; }
    public long? EntityId { get; set; }

    public string? Title { get; set; }

    public string? BannerTitle { get; set; }
    public string? BannerSubtitle { get; set; }
    public string? BannerImagePath { get; set; }
    public bool ShowOnFooter { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}