using Domain.Common;
using System;

namespace Domain
{
    public class PremiumPackage : BaseEntity
    {
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}
