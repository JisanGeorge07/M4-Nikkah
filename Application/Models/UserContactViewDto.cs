using Application.Models.Common;
using Domain;
namespace Application.Models
{
    public class UserContactViewDto : BaseDto
    {
        public long ViewerUserId { get; set; }
        public Registration? ViewerUser { get; set; }
        
        public long ViewedUserId  { get; set; }
        public Registration? ViewedUser { get; set; }
    }
}
