namespace Domain.Common;

public abstract class OrderableBaseEntity : BaseEntity
{
    public long DisplayOrder { get; set; }
}