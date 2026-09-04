using Domain.Common;

namespace Domain
{
    public class UserContactView : BaseEntity
    {
        public long ViewerUserId { get; set; }
        public virtual Registration? ViewerUser { get; set; }
        
        public long ViewedUserId  { get; set; }
        public virtual Registration? ViewedUser { get; set; }
        
        
    }
}
