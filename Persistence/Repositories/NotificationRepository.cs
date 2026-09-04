using Application.Interfaces.Persistence;
using Domain;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class NotificationRepository : INotifcationRepository
    {
        private readonly AppDbContext _dbContext;

        public NotificationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<Notification>> GetNotificationsBasedOnNotifyId(long notifyId)
        {
            return await _dbContext.Notification.Where(x => !x.IsDeleted && (x.Notify_Id == notifyId)).OrderByDescending(x => x.CreatedOn).ToListAsync();
        }
        public async Task<List<Notification>> GetAllNotifications()
        {
            return await _dbContext.Notification
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();
        }
    }
}
