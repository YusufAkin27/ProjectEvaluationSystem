using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectEvaluationSystem.Data;
using ProjectEvaluationSystem.Models;
using ProjectEvaluationSystem.Services;

namespace ProjectEvaluationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects.ToListAsync();
            return View(projects);
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvaluation(int projectId, Evaluation model)
        {
            if (ModelState.IsValid)
            {
                // Kullanıcı IP adresini al
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                // Daha önce bu IP'den bu projeye değerlendirme yapılmış mı kontrol et
                var existingEvaluation = await _context.Evaluations
                    .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.IpAddress == ipAddress);
                
                if (existingEvaluation != null)
                {
                    ModelState.AddModelError(string.Empty, "Bu projeyi daha önce değerlendirdiniz.");
                    var currentProject = await _context.Projects
                        .Include(p => p.Evaluations)
                        .FirstOrDefaultAsync(p => p.Id == projectId);
                    return View("Details", currentProject);
                }
                
                // Yeni değerlendirmeyi ekle
                model.ProjectId = projectId;
                model.IpAddress = ipAddress ?? "Unknown";
                model.EvaluationDate = DateTime.Now;
                
                _context.Evaluations.Add(model);
                await _context.SaveChangesAsync();
                
                // Projenin ortalama puanını güncelle
                var updatedProject = await _context.Projects
                    .Include(p => p.Evaluations)
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                
                if (updatedProject != null && updatedProject.Evaluations.Any())
                {
                    updatedProject.AverageRating = updatedProject.Evaluations.Average(e => e.Rating);
                    await _context.SaveChangesAsync();
                }
                
                // Proje katkıda bulunanlarına anonim değerlendirme bildirimi gönder
                try
                {
                    await _emailService.SendAnonymousEvaluationNotificationAsync(model.Id);
                }
                catch (Exception ex)
                {
                    // E-posta gönderimi başarısız olsa bile işleme devam et
                    _logger.LogError($"Değerlendirme bildirimi gönderilemedi: {ex.Message}");
                }
                
                TempData["SuccessMessage"] = "Değerlendirmeniz başarıyla kaydedildi.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
            
            // Form geçerli değilse
            var projectWithEvaluations = await _context.Projects
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            
            return View("Details", projectWithEvaluations);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
