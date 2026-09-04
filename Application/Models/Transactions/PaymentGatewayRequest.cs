using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Transactions
{
    public class PaymentGatewayRequest
    {
        public string Key { get; set; }
        public string TxnId { get; set; }
        public string Amount { get; set; }
        public string ProductInfo { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Udf1 { get; set; }
        public string Udf2 { get; set; }
        public string Udf3 { get; set; }
        public string Udf4 { get; set; }
        public string Udf5 { get; set; }
        public string Surl { get; set; }
        public string Furl { get; set; }
        public string Hash { get; set; }
    }
}
