using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Persistence;
using Application.Models;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Services
{
    public class MatchingProfilesRepo : IMatchingProfileRepo
    {
        private readonly AppDbContext _context;
        public MatchingProfilesRepo(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<MatchingProfilesResponseDto>> GetMatchingUsersWithPercentage(int userId)
        {
            var matchingProfiles = new List<MatchingProfilesResponseDto>();

            var connection = _context.Database.GetDbConnection();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "GetMatchingUsersWithPercentage";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@userid";
                userIdParam.Value = userId;
                command.Parameters.Add(userIdParam);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var matchingProfile = new MatchingProfilesResponseDto
                        {
                            Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                            name = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString(reader.GetOrdinal("name")),
                            gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? string.Empty : reader.GetString(reader.GetOrdinal("gender")),
                            age = reader.IsDBNull(reader.GetOrdinal("age")) ? 0 : reader.GetInt32(reader.GetOrdinal("age")),
                            height = reader.IsDBNull(reader.GetOrdinal("height")) ? string.Empty : reader.GetString(reader.GetOrdinal("height")),
							//weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? string.Empty : reader.GetString(reader.GetOrdinal("weight")),
							financial_status = reader.IsDBNull(reader.GetOrdinal("height")) ? string.Empty : reader.GetString(reader.GetOrdinal("height")),
							education = reader.IsDBNull(reader.GetOrdinal("education")) ? string.Empty : reader.GetString(reader.GetOrdinal("education")),
                            landline_number = reader.IsDBNull(reader.GetOrdinal("landline_number")) ? string.Empty : reader.GetString(reader.GetOrdinal("landline_number")),
                            profile_picture = reader.IsDBNull(reader.GetOrdinal("profile_picture")) ? string.Empty : reader.GetString(reader.GetOrdinal("profile_picture")),
                            highest_education = reader.IsDBNull(reader.GetOrdinal("highest_education")) ? string.Empty : reader.GetString(reader.GetOrdinal("highest_education")),
                            IsLiked = reader.IsDBNull(reader.GetOrdinal("IsLiked")) ? false : reader.GetBoolean(reader.GetOrdinal("IsLiked")),
                            IsStarred = reader.IsDBNull(reader.GetOrdinal("IsStarred")) ? false : reader.GetBoolean(reader.GetOrdinal("IsStarred")),
                            starred_id = reader.IsDBNull(reader.GetOrdinal("starred_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("starred_id")),
                            favourite_id = reader.IsDBNull(reader.GetOrdinal("favourite_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("favourite_id")),
                            profession = reader.IsDBNull(reader.GetOrdinal("profession")) ? string.Empty : reader.GetString(reader.GetOrdinal("profession")),
                            community = reader.IsDBNull(reader.GetOrdinal("community")) ? string.Empty : reader.GetString(reader.GetOrdinal("community")),
                            maritalstatus = reader.IsDBNull(reader.GetOrdinal("maritalstatus")) ? string.Empty : reader.GetString(reader.GetOrdinal("maritalstatus")),
                            district = reader.IsDBNull(reader.GetOrdinal("district")) ? string.Empty : reader.GetString(reader.GetOrdinal("district")),
                            village = reader.IsDBNull(reader.GetOrdinal("village")) ? string.Empty : reader.GetString(reader.GetOrdinal("village")),
                            age_matching_score = reader.IsDBNull(reader.GetOrdinal("age_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("age_matching_score")),
                            height_matching_score = reader.IsDBNull(reader.GetOrdinal("height_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("height_matching_score")),
                           // weight_matching_score = reader.IsDBNull(reader.GetOrdinal("weight_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("weight_matching_score")),
                            education_type_matching_score = reader.IsDBNull(reader.GetOrdinal("education_type_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("education_type_matching_score")),
                            financial_status_score= reader.IsDBNull(reader.GetOrdinal("financial_status_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("financial_status_score")),
							profession_matching_score = reader.IsDBNull(reader.GetOrdinal("profession_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("profession_matching_score")),
                            //community_matching_score = reader.IsDBNull(reader.GetOrdinal("community_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("community_matching_score")),
                            maritalstatus_matching_score = reader.IsDBNull(reader.GetOrdinal("maritalstatus_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("maritalstatus_matching_score")),
                            location_matching_score = reader.IsDBNull(reader.GetOrdinal("location_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("location_matching_score")),
                           // village_matching_score = reader.IsDBNull(reader.GetOrdinal("village_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("village_matching_score")),
                            total_matching_score = reader.IsDBNull(reader.GetOrdinal("total_matching_score")) ? 0 : reader.GetInt32(reader.GetOrdinal("total_matching_score")),
                            CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("created_on")),
                            Starred_Created_On= reader.IsDBNull(reader.GetOrdinal("starred_created_on")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("starred_created_on")),
						};

                        matchingProfiles.Add(matchingProfile);
                    }
                }
            }

            return matchingProfiles;
        }
    }
}
