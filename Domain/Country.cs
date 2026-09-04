using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Country : OrderableBaseEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }

    }
}
