using Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface IMatchingProfileRepo
    {
        Task<List<MatchingProfilesResponseDto>> GetMatchingUsersWithPercentage(int userId);
    }
}
