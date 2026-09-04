using Application.Helpers;
using Application.Interfaces.Persistence;
using Application.Models;
using Application.Models.Transactions;
using AutoMapper;
using Domain;
using Domain.Framework;
using Identity.Migrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Persistence.Repositories;
using Serilog;
using System.Net.Mail;
using URMARRY.Models;
using URMARRY.Services;
using Microsoft.Extensions.Configuration;

namespace URMARRY.Controllers
{
    public class TransactionController : Controller
    {
        public IHttpContextAccessor ContextAccessor { get; }
        private readonly IRepository<Registration> _userRepo;
        private readonly IRepository<PlanPurchase> _planPurchaseRepo;
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private readonly PayUSettings _payUSettings;
        private readonly string _merchantKey;
        private readonly string _merchantSalt;
        private readonly string _paymentUrl;
        private readonly string _amount;
        private readonly string _productInfo;
        private readonly string _paymentGateway;
        private readonly string _generatedBy;
        
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<UserController> _logger;
		private readonly IRepository<Registration> _registrationRepo;
		private readonly IMapper _mapper;
        private readonly EmailNotificationHelper _emailNotificationHelper;
        private readonly CookieHelper _cookieHelper;
        private readonly IConfiguration _configuration;
        private readonly IRepository<FollowUp> _followUpRepo;
        private readonly IRepository<FollowUpTimeline> _followUpTimelineRepo;

        public TransactionController(IOptions<PayUSettings> payUSettings, IWebHostEnvironment env,
            ITransactionRepository transactionRepository,
            IHttpContextAccessor httpContextAccessor,
            IRepository<Registration> userRepo,
            EmailNotificationHelper emailNotificationHelper,
            IRepository<Registration> registrationRepo,
			IMapper mapper, 
            IRepository<PlanPurchase> planPurchaseRepo,
            CookieHelper cookieHelper,
            IConfiguration configuration,
            IRepository<FollowUp> followUpRepo,
            IRepository<FollowUpTimeline> followUpTimelineRepo)
        {
            _payUSettings = payUSettings.Value;
            _transactionRepository = transactionRepository;
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepo;
			_mapper = mapper;
            _planPurchaseRepo = planPurchaseRepo;
            _registrationRepo = registrationRepo;
            _emailNotificationHelper = emailNotificationHelper;
            _cookieHelper = cookieHelper;
            _configuration = configuration;
            _followUpRepo = followUpRepo;
            _followUpTimelineRepo = followUpTimelineRepo;

            if (env.IsDevelopment())
            {
                _merchantKey = _payUSettings.Development.MerchantKey;
                _merchantSalt = _payUSettings.Development.MerchantSalt;
                _paymentUrl = _payUSettings.Development.PaymentUrl;
                _amount = _payUSettings.Development.Amount;
                _productInfo = _payUSettings.Development.ProductInfo;
                _paymentGateway = _payUSettings.Development.PaymentGateway;
                _generatedBy = _payUSettings.Development.GeneratedBy;
            }
            else
            {
                _merchantKey = _payUSettings.Production.MerchantKey;
                _merchantSalt = _payUSettings.Production.MerchantSalt;
                _paymentUrl = _payUSettings.Production.PaymentUrl;
                _amount = _payUSettings.Production.Amount;
                _productInfo = _payUSettings.Production.ProductInfo;
                _paymentGateway = _payUSettings.Production.PaymentGateway;
                _generatedBy = _payUSettings.Production.GeneratedBy;
            }
        }

        public IActionResult Index()
        {
            var paymentGatewayRequest = new PaymentGatewayRequest();
            return View();
        }

        [HttpGet]
        [Route("transactions/{userId}")]
        public async Task<IActionResult> GetTransactionByUserId(long userId)
        {
            var transaction = await _transactionRepository.GetTransactionByUserIdAsync(userId);

            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found." });
            }

            return Ok(transaction);
        }


        // [HttpPost]
        public async Task<ActionResult>  MakePayment()
        {
            try
            {
                var userIdValue = _cookieHelper.GetUserIdFromCookie(HttpContext);
                if (!userIdValue.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }
                var userIdString = userIdValue.Value.ToString();
                var pgRequestmodel = new PaymentGatewayRequest();
                var profile = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdValue.Value));

                pgRequestmodel.Amount = _amount;
                pgRequestmodel.ProductInfo = _productInfo;
                pgRequestmodel.FirstName = profile.Name;
                pgRequestmodel.Email = profile.Email;
                pgRequestmodel.Phone = profile.Phone;
                pgRequestmodel.Key = _merchantKey;
                pgRequestmodel.TxnId = PaymentGatewayHelper.GenerateTransactionId(14);
                pgRequestmodel.Udf1 = userIdString;
                pgRequestmodel.Udf5 = string.IsNullOrEmpty(pgRequestmodel.Udf5) ? "" : pgRequestmodel.Udf5;
                pgRequestmodel.Hash = PaymentGatewayHelper.GenerateHash(pgRequestmodel, _merchantSalt);
                pgRequestmodel.Udf2 = _paymentUrl; // temporary use only. Don't send for hash generation or to gateway
                // Always use HTTPS for callback URLs to prevent the browser
                // "not secure" warning when the payment gateway POSTs results back.
                // Request.Scheme can resolve to "http" behind reverse proxies.
                pgRequestmodel.Surl = Url.Action("PaymentSuccess", "Transaction", null, "https");
                pgRequestmodel.Furl = Url.Action("PaymentFailed", "Transaction", null, "https");
                ViewBag.UserName = profile.Name;
                ViewBag.RegisterNumber = profile.RegisterNumber;

                return View("PaymentGatewayRequestForm", pgRequestmodel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in MakePayment.");
                return StatusCode(500, "An error occurred while processing the payment.");
            }
        }

        [HttpGet("/Transaction/MakePaymentDirect")]
        public async Task<ActionResult> MakePaymentDirect(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest("Invalid token.");
                }

                // Retrieve encryption secret key from appsettings.json
                string secretKey = _configuration.GetValue<string>("PaymentLinkSettings:SecretKey") ?? "M4NikkahPaymentSecretKey#2026";
                
                var crypt = new RijndaelCrypt(secretKey);
                string decryptedPayload = crypt.Decrypt(token);
                if (string.IsNullOrEmpty(decryptedPayload))
                {
                    return BadRequest("Invalid payment link token.");
                }

                long userId = 0;
                DateTime? expiryTime = null;

                if (decryptedPayload.Contains('|'))
                {
                    var parts = decryptedPayload.Split('|');
                    if (parts.Length >= 2 && long.TryParse(parts[0], out long parsedId))
                    {
                        userId = parsedId;
                        if (DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsedExpiry))
                        {
                            expiryTime = parsedExpiry;
                        }
                    }
                }
                else
                {
                    long.TryParse(decryptedPayload, out userId);
                }

                if (userId == 0)
                {
                    return BadRequest("Invalid payment link token.");
                }

                if (expiryTime.HasValue && DateTime.UtcNow > expiryTime.Value)
                {
                    return Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Payment Link Expired - M4Nikah</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8f9fa; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
        .card { background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); text-align: center; max-width: 480px; }
        .icon { font-size: 48px; color: #dc3545; margin-bottom: 15px; }
        h2 { color: #333; margin-bottom: 10px; }
        p { color: #666; font-size: 15px; line-height: 1.5; margin-bottom: 25px; }
        .btn { display: inline-block; padding: 10px 24px; background-color: #7209b7; color: white; text-decoration: none; border-radius: 6px; font-weight: 600; }
    </style>
</head>
<body>
    <div class='card'>
        <div class='icon'>⏰</div>
        <h2>Payment Link Expired</h2>
        <p>This payment link has expired. For your security, direct payment links are valid for a limited time only. Please contact our support team or request a new link.</p>
        <a href='/' class='btn'>Return to Home</a>
    </div>
</body>
</html>", "text/html");
                }

                var profile = await _registrationRepo.Get(userId);
                if (profile == null || profile.IsDeleted)
                {
                    return NotFound("User not found.");
                }

                var profileDto = _mapper.Map<RegistrationDto>(profile);
                var pgRequestmodel = new PaymentGatewayRequest
                {
                    Amount = _amount,
                    ProductInfo = _productInfo,
                    FirstName = profileDto.Name,
                    Email = profileDto.Email,
                    Phone = profileDto.Phone,
                    Key = _merchantKey,
                    TxnId = PaymentGatewayHelper.GenerateTransactionId(14),
                    Udf1 = userId.ToString(),
                    Udf5 = ""
                };
                
                pgRequestmodel.Hash = PaymentGatewayHelper.GenerateHash(pgRequestmodel, _merchantSalt);
                pgRequestmodel.Udf2 = _paymentUrl; // Set after hash generation, used as form action in the View
                pgRequestmodel.Surl = Url.Action("PaymentSuccess", "Transaction", null, "https");
                pgRequestmodel.Furl = Url.Action("PaymentFailed", "Transaction", null, "https");
                
                ViewBag.UserName = profileDto.Name;
                ViewBag.RegisterNumber = profileDto.RegisterNumber;

                return View("PaymentGatewayRequestForm", pgRequestmodel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in MakePaymentDirect.");
                return StatusCode(500, "An error occurred while processing the payment.");
            }
        }

        public async Task<ActionResult> PaymentSuccess(PaymentGatewayResponse response)
        {
            try
            {
                Log.Information("PaymentSuccess callback received. TxnId={TxnId}, Status={Status}, Udf1={Udf1}",
                    response?.TxnId, response?.Status, response?.Udf1);

                await ProcessPayment(response, "Web");
                RegistrationDto? registration = null;
                if (long.TryParse(response?.Udf1, out long userIdLong))
                {
                    registration = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
                    if (registration != null)
                    {
                        registration.IsPremiumMember = true;
                    }
                }
                if (registration == null)
                {
                    registration = new RegistrationDto();
                }
                return View(new PaymentOutcomeViewModel { Response = response, Registration = registration });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in PaymentSuccess. TxnId={TxnId}, Udf1={Udf1}",
                    response?.TxnId, response?.Udf1);
                return StatusCode(500, "An error occurred while processing the payment success.");
            }
        }
        public async Task<ActionResult> PaymentOnMobile(PaymentGatewayResponse response)
        {
            try
			{
                Log.Information("PaymentOnMobile-START");
                await ProcessPayment(response, "Mobile");
                Log.Information("PaymentOnMobile-END");


				return View(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in PaymentOnMobile.");
                return StatusCode(500, "An error occurred while processing the payment on mobile.");
            }
        }

        private async Task ProcessPayment(PaymentGatewayResponse response, string source)
        {
            if (response == null)
            {
                Log.Error("ProcessPayment called with null response.");
                throw new ArgumentNullException(nameof(response), "Payment gateway response is null. The POST body may have been lost during an HTTP to HTTPS redirect.");
            }

            if (string.IsNullOrEmpty(response.Udf1) || !long.TryParse(response.Udf1, out long userId))
            {
                Log.Error("ProcessPayment: Udf1 is null/empty or not a valid user ID. Udf1={Udf1}, TxnId={TxnId}",
                    response.Udf1, response.TxnId);
                throw new InvalidOperationException(
                    $"Cannot process payment: User ID (Udf1) is missing or invalid. Udf1='{response.Udf1}'. " +
                    "This may indicate the POST body was lost during an HTTP to HTTPS redirect.");
            }

            var existingTxn = await _transactionRepository.GetTransactionByTxnIdAsync(response.TxnId);
            if (existingTxn != null)
            {
                Log.Information("ProcessPayment: Transaction {TxnId} has already been processed. Skipping duplicate processing.", response.TxnId);
                return;
            }

            Transaction transaction = new Transaction
            {
                userId = userId,
                Udf1 = response.Udf1,
                Status = response.Status,
                Key = response.Key,
                TxnId = response.TxnId,
                Amount = response.Amount,
                ProductInfo = response.ProductInfo,
                FirstName = response.FirstName,
                Email = response.Email,
                Phone = response.Phone,
                Hash = response.Hash,
                PaymentGateway = _paymentGateway,
                CreatedBy = _generatedBy,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now,
                ModifiedBy = _generatedBy,
                PaymentType = "Online",
                Source = source
            };

            await _transactionRepository.AddPaymentResultAsync(transaction);

            var plans = await _planPurchaseRepo.Where(x => x.UserId == userId);
            var latestPlan = plans.OrderByDescending(x => x.CreatedOn).FirstOrDefault();

            if (latestPlan != null && (latestPlan.ViewCreditsPurchased - latestPlan.ViewCreditsUsed) > 0 && latestPlan.ExpiresAt > DateTime.UtcNow)
            {
                latestPlan.ViewCreditsPurchased += 50;
                latestPlan.ExpiresAt = DateTime.UtcNow.AddDays(180);
                latestPlan.LowCreditNotificationSent = false;
                latestPlan.ExpiryNotificationSent = false;
                latestPlan.ModifiedOn = DateTime.Now;
                latestPlan.ModifiedBy = _generatedBy;
                await _planPurchaseRepo.Update(latestPlan);
            }
            else
            {
                PlanPurchase planPurchase = new PlanPurchase()
                {
                    UserId = userId,
                    ViewCreditsPurchased = 50,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now,
                    CreatedBy = _generatedBy,
                    ModifiedBy = _generatedBy
                };
                await _planPurchaseRepo.Add(planPurchase);
            }
            await _planPurchaseRepo.SaveChanges();

            // Update user premium status in Registration table
            var user = await _userRepo.Get(userId);

            if (user != null)
            {
                user.IsPremiumMember = true;
                await _userRepo.Update(user);
                await _userRepo.SaveChanges();
            }

            // Update follow-up status if an active follow-up exists for this user
            try
            {
                var userFollowUps = await _followUpRepo.Where(f => f.ProfileId == userId && !f.IsDeleted && (f.FollowUpType == FollowUpType.PremiumFollowUp || f.FollowUpType == FollowUpType.RenewalFollowUp));
                var activeFollowUp = userFollowUps.OrderByDescending(f => f.CreatedOn).FirstOrDefault();
                if (activeFollowUp != null)
                {
                    activeFollowUp.PaymentCompleted = true;
                    activeFollowUp.LatestAdminApprovalStatus = AdminApprovalStatus.Approved;
                    if (activeFollowUp.FollowUpType == FollowUpType.PremiumFollowUp)
                    {
                        activeFollowUp.LatestInterestStatus = PremiumInterestStatus.Converted;
                    }
                    else if (activeFollowUp.FollowUpType == FollowUpType.RenewalFollowUp)
                    {
                        activeFollowUp.LatestRenewalInterestStatus = RenewalInterestStatus.Renewed;
                    }

                    await _followUpRepo.Update(activeFollowUp);
                    await _followUpRepo.SaveChanges();

                    var timeline = new FollowUpTimeline
                    {
                        FollowUpId = activeFollowUp.Id,
                        StaffId = activeFollowUp.AssignedStaffId ?? 0,
                        StaffName = "System (Online Payment Gateway)",
                        ContactType = activeFollowUp.LatestContactType,
                        CallStatus = activeFollowUp.LatestCallStatus,
                        Remarks = $"Online payment completed successfully. TxnId: {transaction.TxnId}, Amount: {transaction.Amount}. Membership activated.",
                        NextFollowUpDate = null,
                        InterestStatus = activeFollowUp.LatestInterestStatus,
                        RenewalInterestStatus = activeFollowUp.LatestRenewalInterestStatus,
                        IsActive = true
                    };
                    await _followUpTimelineRepo.Add(timeline);
                    await _followUpTimelineRepo.SaveChanges();
                }
            }
            catch (Exception fuEx)
            {
                Log.Error(fuEx, "Error updating follow-up on successful payment for UserId={UserId}", userId);
            }
            
            // Send the payment success email (non-critical — don't let email failure crash the payment processing)
            try
            {
                await SendPaymentSuccessEmail(transaction.Email, transaction.FirstName, transaction.TxnId, transaction.Amount, transaction.CreatedOn);
            }
            catch (Exception emailEx)
            {
                Log.Error(emailEx, "Failed to send payment success email for TxnId={TxnId}, but payment was processed successfully.", transaction.TxnId);
            }
        }
        
        private async Task SendPaymentSuccessEmail(string email, string name, string txnId, string amount, DateTime dateTime)
        {
            try
            {
                string templatePath = "Templates/Payment/Paymentsuccessful.html";
                string emailContent = System.IO.File.ReadAllText(templatePath);

                // Replace placeholders with actual values
                emailContent = emailContent.Replace("[User's Name]", name);
                emailContent = emailContent.Replace("[Transaction ID]", txnId);
                emailContent = emailContent.Replace("[Payment Amount]", amount);
                emailContent = emailContent.Replace("[Payment Date]", dateTime.ToShortDateString());

                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                _emailNotificationHelper.SendEmail(email, htmlView, "Payment Successful - Welcome to Premium Membership on M4Nikah!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while sending payment success email.");
            }
        }

        public async Task<ActionResult> PaymentFailed(PaymentGatewayResponse response)
        {
            try
            {
                var _transaction = new Transaction();
                if (long.TryParse(response.Udf1, out long userId))
                {
                    _transaction.userId = userId;
                }
                else
                {
                    _transaction.userId = userId; // find out id using email from registration table;
                }

                _transaction.Udf1 = response.Udf1;
                _transaction.Status = response.Status;
                _transaction.Key = response.Key;
                _transaction.TxnId = response.TxnId;
                _transaction.Amount = response.Amount;
                _transaction.ProductInfo = response.ProductInfo;
                _transaction.FirstName = response.FirstName;
                _transaction.Email = response.Email;
                _transaction.Phone = response.Phone;
                _transaction.Hash = response.Hash;
                _transaction.PaymentGateway = _paymentGateway;
                _transaction.CreatedBy = _generatedBy;
                _transaction.CreatedOn = DateTime.Now;
                _transaction.ModifiedOn = DateTime.Now;
                _transaction.ModifiedBy = _generatedBy;
                _transaction.PaymentType = "Online";
                _transaction.Source = "Web";

                await _transactionRepository.AddPaymentResultAsync(_transaction);

                // Send the payment failed email
                await SendPaymentFailedEmail(_transaction.Email, _transaction.FirstName, _transaction.TxnId, _transaction.Amount);

                RegistrationDto? registration = null;
                if (long.TryParse(response.Udf1, out long userIdLong))
                {
                    registration = _mapper.Map<RegistrationDto>(await _registrationRepo.Get(userIdLong));
                }
                if (registration == null)
                {
                    registration = new RegistrationDto();
                }
                return View(new PaymentOutcomeViewModel { Response = response, Registration = registration });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in PaymentFailed.");
                return StatusCode(500, "An error occurred while processing the payment failure.");
            }
        }
        private async Task SendPaymentFailedEmail(string email, string name, string txnId, string amount)
        {
            try
            {
                string templatePath = "Templates/Payment/Paymentfailed.html";
                string emailContent = System.IO.File.ReadAllText(templatePath);

                // Replace placeholders with actual values
                emailContent = emailContent.Replace("[User Name]", name);
                emailContent = emailContent.Replace("[Transaction ID]", txnId);
                emailContent = emailContent.Replace("[Amount]", amount);
                emailContent = emailContent.Replace("[Support Email]", "support@m4nikkah.com");
                emailContent = emailContent.Replace("[Support Phone Number]", "123-456-7890");

                var htmlView = AlternateView.CreateAlternateViewFromString(emailContent, null, "text/html");
                _emailNotificationHelper.SendEmail(email, htmlView, "Payment Failed Notification");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while sending payment failed email.");
            }
        }

    }
}