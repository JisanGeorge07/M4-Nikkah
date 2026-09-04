using Application.Interfaces.Persistence;
using Application.Models;
using AutoMapper;
using Domain;
using Microsoft.AspNetCore.Mvc;
using URMARRY.Models;

namespace URMARRY.Controllers
{
	public class AboutController : Controller
	{
		private readonly IRepository<About> _repo;
		private readonly IMapper _mapper;

		public AboutController(IRepository<About> repo, IMapper mapper)
		{
			_repo = repo;
			_mapper = mapper;
		}
		[HttpGet("/about")]
		public async Task<IActionResult> Index()
		{
			return View(new HomeViewModel
			{ 
				About = _mapper.Map<AboutDto>(await _repo.FirstActive())
			});
		}
	}
}
