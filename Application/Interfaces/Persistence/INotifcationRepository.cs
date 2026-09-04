using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface INotifcationRepository
    {
        Task<List<Notification>> GetNotificationsBasedOnNotifyId(long notifyId);
        Task<List<Notification>> GetAllNotifications();
    }
}
 