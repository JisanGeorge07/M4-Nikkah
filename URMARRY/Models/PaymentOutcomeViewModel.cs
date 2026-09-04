using Application.Models;
using Application.Models.Transactions;

namespace URMARRY.Models
{
    public class PaymentOutcomeViewModel
    {
        public PaymentGatewayResponse Response { get; set; }
        public RegistrationDto Registration { get; set; }
    }
}
