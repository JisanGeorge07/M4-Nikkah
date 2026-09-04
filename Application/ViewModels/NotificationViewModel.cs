using Application.Models;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ViewModels
{
    public class NotificationViewModel
    {
        public List<NotificationDto> notificationDto { get; set; }
        public RegistrationDto Registration { get; set; }

        public List<RegistrationDto> AllRegistrations { get; set; } = new List<RegistrationDto>();
    }
}
