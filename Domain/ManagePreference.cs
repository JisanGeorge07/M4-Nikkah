using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class ManagePreference:BaseEntity
    {
        public long Id { get; set; }
        public long User_Id { get; set; }
        public long Notification_Alert { get; set; }
        public long Email_Alert { get; set; }
        public long Sms_Alert { get; set; }
    }
}
