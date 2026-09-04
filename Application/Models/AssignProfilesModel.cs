using System.Collections.Generic;

namespace Application.Models
{
    public class AssignProfilesModel
    {
        public List<long>? ProfileIds { get; set; }
        public long StaffId { get; set; }
    }
}
