namespace Application.Models.Common;

public abstract class OrderableDto : BaseDto
{
    public long DisplayOrder { get; set; }
}