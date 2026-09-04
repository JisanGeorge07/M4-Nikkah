using Application.Helpers;
using Application.Models.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Models;

public class PageSettingsDto : OrderableDto
{
    public string? Name { get; set; }
    public string? ParentName { get; set; }
    public long? EntityId { get; set; }

    public string? Title { get; set; }

    public string? BannerTitle { get; set; }
    public string? BannerSubtitle { get; set; }
    public bool ShowOnFooter { get; set; }


    /// Resolution: 1721 x 441
    public string? BannerImagePath { get; set; }

    public IFormFile? BannerImage { get; set; }
    //public string? BannerArabicImagePath { get; set; }
    //public IFormFile? BannerArabicImage { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    //public string? TitleEnglish
    //{
    //    get => Localization.GetEnglish(Title);
    //    set => Title = Localization.Serialize(value, TitleArabic);
    //}

    //public string? TitleArabic
    //{
    //    get => Localization.GetArabic(Title);
    //    set => Title = Localization.Serialize(TitleEnglish, value);
    //}


    //public string? BannerTitleEnglish
    //{
    //    get => Localization.GetEnglish(BannerTitle);
    //    set => BannerTitle = Localization.Serialize(value, BannerTitleArabic);
    //}

    //public string? BannerTitleArabic
    //{
    //    get => Localization.GetArabic(BannerTitle);
    //    set => BannerTitle = Localization.Serialize(BannerTitleEnglish, value);
    //}

    //public string? BannerSubtitleEnglish
    //{
    //    get => Localization.GetEnglish(BannerSubtitle);
    //    set => BannerSubtitle = Localization.Serialize(value, BannerSubtitleArabic);
    //}

    //public string? BannerSubtitleArabic
    //{
    //    get => Localization.GetArabic(BannerSubtitle);
    //    set => BannerSubtitle = Localization.Serialize(BannerSubtitleEnglish, value);
    //}

    //public string? SeoTitleEnglish
    //{
    //    get => Localization.GetEnglish(SeoTitle);
    //    set => SeoTitle = Localization.Serialize(value, SeoTitleArabic);
    //}

    //public string? SeoTitleArabic
    //{
    //    get => Localization.GetArabic(SeoTitle);
    //    set => SeoTitle = Localization.Serialize(SeoTitleEnglish, value);
    //}

    //public string? SeoDescriptionEnglish
    //{
    //    get => Localization.GetEnglish(SeoDescription);
    //    set => SeoDescription = Localization.Serialize(value, SeoDescriptionArabic);
    //}

    //public string? SeoDescriptionArabic
    //{
    //    get => Localization.GetArabic(SeoDescription);
    //    set => SeoDescription = Localization.Serialize(SeoDescriptionEnglish, value);
    //}
}