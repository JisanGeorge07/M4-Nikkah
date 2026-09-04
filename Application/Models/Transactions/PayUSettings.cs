using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Transactions
{
    public class PayUSettings
    {
        public EnvironmentSettings Development { get; set; }
        public EnvironmentSettings Production { get; set; }
    }
    public class EnvironmentSettings
    {
        public string MerchantKey { get; set; }
        public string MerchantSalt { get; set; }
        public string PaymentUrl { get; set; }
        public string Amount { get; set; }
        public string ProductInfo { get; set; }
        public string PaymentGateway { get; set; }
        public string GeneratedBy { get; set; }
    }
}
