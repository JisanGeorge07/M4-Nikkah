using Application.Interfaces.Persistence;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class RegistrationRepository : IRegistrationRepository
    {
        private readonly AppDbContext _dbContext;

        public RegistrationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Registration> GetRegistrationByIdAsync(long userId)
        {
            return await _dbContext.Registration.FindAsync(userId);
        }

        public async Task UpdateRegistrationAsync(Registration registration)
        {
            _dbContext.Registration.Update(registration);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<Registration> UpdateProfilePicAsync(long userId, string imagePath)
        {
            var registration = await _dbContext.Registration.FindAsync(userId);
            if (registration == null)
            {
                throw new Exception("User not found");
            }

            registration.ImagePath = imagePath;
            await _dbContext.SaveChangesAsync();
            return registration;
        }
    }
}
