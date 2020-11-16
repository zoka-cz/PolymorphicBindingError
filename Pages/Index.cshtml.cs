using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using aspnetcore_PolymorphicBinding.ViewModels;

namespace aspnetcore_PolymorphicBinding.Pages
{
	public class IndexModel : PageModel
	{
		private readonly ILogger<IndexModel> _logger;

		public IndexModel(ILogger<IndexModel> logger)
		{
			_logger = logger;
		}

		[BindProperty]
		public TestPolymorphicModels PolymorphicModels { get; set; }


		public void OnGet()
		{
			PolymorphicModels = new TestPolymorphicModels();
		}

		public IActionResult OnPost()
		{
			if (!ModelState.IsValid)
				return Page();

			return RedirectToPage("/Success");
		}
	}
}
