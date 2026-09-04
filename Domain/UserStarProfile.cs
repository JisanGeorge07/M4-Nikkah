using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class UserStarProfile:BaseEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long StarId { get; set; }
    }
}
