using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Notification:BaseEntity
    {
        public int Id { get; set; }
        public string Notify_Message { get; set; }
        public long UserId { get; set; }
        public long Notify_Id { get; set; }
        public int Is_Read { get; set; } = 0;
    }
}
