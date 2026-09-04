using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Transactions
{
    public class Transaction : PaymentGatewayResponse
    {
        public long Id { get; set; }
        public long userId { get; set; }
        public string PaymentGateway { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public string? PaymentType { get; set; }
        public OfflinePaymentMethod? OfflinePaymentType { get; set; }
        public string? Source { get; set; }
        public Registration Registration { get; set; }
    }
}
 