using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class District : OrderableBaseEntity
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public long StateId { get; set; }
        public string Name { get; set; }
        

    }
}
