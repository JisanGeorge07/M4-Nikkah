using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
	public class MatchedProfiles
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Gender { get; set; }
		public int Age { get; set; }
		public string Weight { get; set; }
		public string Height { get; set; }
		public string Highest_Education { get; set; }
		public string Education { get; set; }
		public string Village { get; set; }
		public string District { get; set; }
		public string Community { get; set; }
		public string Landline_Number { get; set; }
		public string Profession { get; set; }
		public string MaritalStatus { get; set; }
		public string Profile_Picture { get; set; }
		public string Religiousness { get; set; }
		public string Financialstatus { get; set; }
		public int IsStarred { get; set; }
		public int IsLiked { get; set; }
		public int Starred_Id { get; set; }
		public int Favourite_Id { get; set; }
		public int Total_Matching_Score { get; set; }
	}
}
