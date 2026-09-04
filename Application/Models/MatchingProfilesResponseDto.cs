using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class MatchingProfilesResponseDto
    {
        public long Id { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public int age { get; set; }
        public string height { get; set; }
        //public string weight { get; set; }
        public string financial_status {  get; set; }

		public string education { get; set; }
        public string landline_number { get; set; }
        public string profile_picture { get; set; }
        public string highest_education { get; set; }
        public bool IsLiked { get; set; }
        public bool IsStarred { get; set; }
        public int? starred_id { get; set; }
        public int? favourite_id { get; set; }
        public string profession { get; set; }
        public string community { get; set; }
        public string maritalstatus { get; set; }
        public string district { get; set; }
        public string village { get; set; }
        public int age_matching_score { get; set; }
        public int height_matching_score { get; set; }
        //public int weight_matching_score { get; set; }
        public int financial_status_score { get; set; }

		public int education_type_matching_score { get; set; }
        public int profession_matching_score { get; set; }
       // public int community_matching_score { get; set; }
        public int maritalstatus_matching_score { get; set; }
        public int location_matching_score { get; set; }
        //public int village_matching_score { get; set; }
        public int total_matching_score { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime Starred_Created_On {  get; set; }

	}
}
