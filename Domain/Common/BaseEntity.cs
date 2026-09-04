namespace Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; set; }

    public DateTime CreatedOn { get; set; } 
    public string? CreatedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}