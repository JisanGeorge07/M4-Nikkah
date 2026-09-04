using Application.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class CityDto : BaseDto
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public long StateId { get; set; }
        public long DistrictId { get; set; }
        public string Name { get; set; }
    }
}
