using Application.Interfaces.Infrastructure.Email;
using Application.Models.Framework;
using System.Net.Mail;

namespace Application.Interfaces.Infrastructure;

public interface IEmailService
{
    public Task Send(Message message);

    public Task Authenticate(EmailDto email);

    public Task<bool> SendSmsAsync(string otp, string phoneNumber);

    public Task<bool> SendSmsForgotPasswordAsync(string password, string phoneNumber);

    public Task<bool> SendSmsPaymentLinkAsync(string paymentLink, string phoneNumber);
}