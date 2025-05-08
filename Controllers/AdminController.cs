using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectEvaluationSystem.Data;
using ProjectEvaluationSystem.Models;
using ProjectEvaluationSystem.Services;

namespace ProjectEvaluationSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context, 
            IEmailService emailService,
            ICloudinaryService cloudinaryService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _webHostEnvironment = webHostEnvironment;
        }

        // Admin giriş sayfası
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminId") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // Admin giriş işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Kullanıcı adı ve şifre gereklidir.";
                return View();
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password && u.IsAdmin);

            if (admin != null)
            {
                HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                TempData["SuccessMessage"] = "Giriş başarılı. Hoş geldiniz!";
                return RedirectToAction("Index");
            }

            ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        // Admin çıkış işlemi
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Admin sayfası - Proje listesi
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var projects = await _context.Projects.ToListAsync();
            return View(projects);
        }

        // Yeni proje ekleme sayfası
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // Yeni proje ekleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, IFormFile imageFile, string[] contributorFirstName, string[] contributorLastName, string[] contributorEmail)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                // Proje fotoğrafını yükle
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        project.ImagePath = await _cloudinaryService.UploadImageAsync(imageFile);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Resim yükleme işlemi başarısız: {ex.Message}");
                        return View(project);
                    }
                }

                // Projeyi ekle
                project.CreatedDate = DateTime.Now;
                _context.Add(project);
                await _context.SaveChangesAsync();

                // Katkıda bulunanları ekle
                if (contributorFirstName != null && contributorFirstName.Length > 0)
                {
                    for (int i = 0; i < contributorFirstName.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(contributorFirstName[i]) && !string.IsNullOrEmpty(contributorLastName[i]))
                        {
                            var contributor = new Contributor
                            {
                                FirstName = contributorFirstName[i],
                                LastName = contributorLastName[i],
                                Email = i < contributorEmail.Length ? contributorEmail[i] : null,
                                ProjectId = project.Id
                            };

                            _context.Contributors.Add(contributor);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Proje başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // Proje düzenleme sayfası
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .Include(p => p.Contributors)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // Proje düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, IFormFile imageFile, string[] contributorIds, string[] contributorFirstName, string[] contributorLastName, string[] contributorEmail)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects
                        .Include(p => p.Contributors)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    // Proje bilgilerini güncelle
                    existingProject.Name = project.Name;
                    existingProject.Description = project.Description;
                    existingProject.GithubLink = project.GithubLink;

                    // Proje fotoğrafını güncelle
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Eski fotoğrafı sil
                        if (!string.IsNullOrEmpty(existingProject.ImagePath))
                        {
                            await _cloudinaryService.DeleteImageAsync(existingProject.ImagePath);
                        }

                        // Yeni fotoğrafı yükle
                        existingProject.ImagePath = await _cloudinaryService.UploadImageAsync(imageFile);
                    }

                    // Katkıda bulunanları güncelle
                    if (contributorFirstName != null && contributorFirstName.Length > 0)
                    {
                        // Mevcut katkıda bulunanları temizle
                        _context.Contributors.RemoveRange(existingProject.Contributors);

                        // Yeni katkıda bulunanları ekle
                        for (int i = 0; i < contributorFirstName.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(contributorFirstName[i]) && !string.IsNullOrEmpty(contributorLastName[i]))
                            {
                                var contributor = new Contributor
                                {
                                    FirstName = contributorFirstName[i],
                                    LastName = contributorLastName[i],
                                    Email = i < contributorEmail.Length ? contributorEmail[i] : null,
                                    ProjectId = project.Id
                                };

                                _context.Contributors.Add(contributor);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proje başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(project);
        }

        // Proje silme onay sayfası
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // Proje silme işlemi
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .Include(p => p.Contributors)
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Proje fotoğrafını sil
            if (!string.IsNullOrEmpty(project.ImagePath))
            {
                await _cloudinaryService.DeleteImageAsync(project.ImagePath);
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proje başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Değerlendirme özetlerini e-posta ile gönder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEvaluationSummary(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            await _emailService.SendProjectEvaluationSummaryAsync(id);

            TempData["SuccessMessage"] = "Değerlendirme özeti e-posta ile gönderildi.";
            return RedirectToAction(nameof(Index));
        }

        // Değerlendirmeleri görüntüleme sayfası
        public async Task<IActionResult> ViewEvaluations(int? projectId)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var query = _context.Evaluations
                .Include(e => e.Project)
                .AsQueryable();

            // Proje ID'sine göre filtreleme
            if (projectId.HasValue)
            {
                query = query.Where(e => e.ProjectId == projectId.Value);
            }

            var evaluations = await query
                .OrderByDescending(e => e.EvaluationDate)
                .ToListAsync();
            
            // Dropdown için proje listesi
            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.Name)
                .ToListAsync();
            
            ViewBag.SelectedProjectId = projectId;

            return View(evaluations);
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(p => p.Id == id);
        }
    }
} 