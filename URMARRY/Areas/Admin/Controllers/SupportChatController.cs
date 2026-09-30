using Application.Constants;
using Application.Interfaces.Persistence;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace URMARRY.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class SupportChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IRepository<Registration> _userRepo;

        public SupportChatController(IChatService chatService, IRepository<Registration> userRepo)
        {
            _chatService = chatService;
            _userRepo = userRepo;
        }

        [HttpGet("/admin/support-chat")]
        [HttpGet("/admin/support-chat/{conversationId:long?}")]
        public async Task<IActionResult> Index(long? conversationId = null)
        {
            ViewBag.SelectedConversationId = conversationId ?? 0;
            return View();
        }
    }
}
