using Application.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class UserStarProfileDto: BaseDto
    {
        
        public long Id { get; set; }
        public long UserId { get; set; }
        public long StarId { get; set; }
        public bool? IsStarred { get; set; }
    }
}
