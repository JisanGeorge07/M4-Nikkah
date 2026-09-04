using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface IProfileService
    {
        Task<string> AddProfilePicAsync(long userId, IFormFile? image);
        Task<bool> NotLikeProfileAsync(long userId, long notLikedProfileId);
        Task<List<long>> GetNotLikedProfileIdsAsync(long userId);
    }

}
