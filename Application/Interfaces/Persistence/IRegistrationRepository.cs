using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface IRegistrationRepository
    {
        Task<Registration> UpdateProfilePicAsync(long userId, string imagePath);
        Task UpdateRegistrationAsync(Registration registration);
        Task<Registration> GetRegistrationByIdAsync(long userId);
    }
}
