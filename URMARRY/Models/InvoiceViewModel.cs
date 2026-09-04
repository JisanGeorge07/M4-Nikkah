using Application.Models;
using Application.Models.Transactions;

namespace URMARRY.Models
{
    public class InvoiceViewModel
    {
        public RegistrationDto Registration { get; set; }
        public Transaction Transaction { get; set; }
    }
}
