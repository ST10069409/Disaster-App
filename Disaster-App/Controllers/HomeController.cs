using Disaster_App.Data;
using Disaster_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Disaster_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ======================
        // Register
        // ======================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == user.Email);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Email already registered.");
                        return View(user);
                    }

                    // Hash the password before saving
                    user.PasswordHash = HashPassword(user.PasswordHash);
                    user.CreatedAt = DateTime.Now;

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    _logger.LogError(ex, "Database error during registration");
                }
            }
            return View(user);
        }

        // ======================
        // Login
        // ======================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User user)
        {
            var hashedPassword = HashPassword(user.PasswordHash);

            var dbUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email && u.PasswordHash == hashedPassword);

            if (dbUser != null)
            {
                HttpContext.Session.SetString("UserEmail", dbUser.Email);
                HttpContext.Session.SetString("UserRole", dbUser.Role);
                HttpContext.Session.SetInt32("UserId", dbUser.UserID);
                HttpContext.Session.SetString("UserName", dbUser.FullName);

                // Redirect based on user role
                if (dbUser.Role == "User")
                {
                    return RedirectToAction("UserHome", "Home");
                }
                else if (dbUser.Role == "Volunteer")
                {
                    return RedirectToAction("VolunteerHome", "Home"); // FIXED: Now goes to VolunteerHome
                }
                else if (dbUser.Role == "Admin")
                {
                    return RedirectToAction("AdminHome", "Home");
                }
                else
                {
                    return RedirectToAction("UserHome", "Home");
                }
            }

            ViewBag.Error = "Invalid login credentials";
            return View(user);
        }
        // ======================
        // Dashboard Pages
        // ======================
        public IActionResult UserHome()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            return View();
        }

        public async Task<IActionResult> VolunteerHome()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Home");
            }

            // REMOVE this check - let users with Volunteer role access the page
            // var isVolunteer = await _context.Volunteers.AnyAsync(v => v.UserID == userId.Value);
            // if (!isVolunteer)
            // {
            //     TempData["ErrorMessage"] = "You need to register as a volunteer first.";
            //     return RedirectToAction("Volunteerss", "Home");
            // }

            // Get volunteer's assigned tasks (only if they exist in Volunteers table)
            var volunteer = await _context.Volunteers
                .Include(v => v.Tasks)
                .FirstOrDefaultAsync(v => v.UserID == userId.Value);

            ViewBag.VolunteerTasks = volunteer?.Tasks?.Where(t => t.Status != "Completed").ToList() ?? new List<VolunteerTask>();

            return View();
        }

        public IActionResult AdminHome()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")) ||
                HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Home");
            }
            return View();
        }

        // ======================
        // Log Incident
        // ======================
        public IActionResult LogIncident()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogIncident(Incident incident)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid)
                return View(incident);

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    ModelState.AddModelError("", "User session expired. Please log in again.");
                    return View(incident);
                }

                // Verify user exists in database
                var userExists = await _context.Users.AnyAsync(u => u.UserID == userId.Value);
                if (!userExists)
                {
                    ModelState.AddModelError("", "User not found. Please log in again.");
                    return View(incident);
                }

                // Create new incident to avoid navigation property issues
                var newIncident = new Incident
                {
                    Title = incident.Title,
                    Description = incident.Description,
                    Location = incident.Location,
                    DateReported = DateTime.Now,
                    ReportedBy = userId.Value
                };

                _context.Incidents.Add(newIncident);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Incident reported successfully!";
                return RedirectToAction("UserHome", "Home");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "A database error occurred while saving the incident. Please try again.");
                _logger.LogError(ex, "Database error in LogIncident");
                Console.WriteLine($"DB Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                _logger.LogError(ex, "Unexpected error in LogIncident");
                Console.WriteLine($"General Error: {ex.Message}");
            }

            return View(incident);
        }

        // ======================
        // Log Donation
        // ======================
        public IActionResult LogDonation()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            // Pre-fill form with user data from session
            var donation = new Donation
            {
                DonorName = HttpContext.Session.GetString("UserName") ?? "",
                Email = HttpContext.Session.GetString("UserEmail") ?? ""
            };

            return View(donation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogDonation(Donation donation)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            // Auto-set some fields from session
            if (string.IsNullOrEmpty(donation.DonorName))
            {
                donation.DonorName = HttpContext.Session.GetString("UserName") ?? "";
            }

            if (string.IsNullOrEmpty(donation.Email))
            {
                donation.Email = HttpContext.Session.GetString("UserEmail") ?? "";
            }

            donation.DonationDate = DateTime.Now;
            donation.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Donations.Add(donation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "🎉 Thank you for your donation! We will contact you soon.";
                    return RedirectToAction("UserHome", "Home");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "A database error occurred. Please try again.");
                    _logger.LogError(ex, "Database error in LogDonation");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                    _logger.LogError(ex, "Unexpected error in LogDonation");
                }
            }

            return View(donation);
        }

        // ======================
        // Volunteer Registration
        // ======================
        public IActionResult Volunteerss()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            // Pre-fill form with user data from session
            var volunteer = new Volunteer
            {
                UserID = HttpContext.Session.GetInt32("UserId") ?? 0
            };

            return View(volunteer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Volunteerss(Volunteer volunteer)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            // Set automatic fields
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                ModelState.AddModelError("", "User session expired. Please log in again.");
                return View(volunteer);
            }

            volunteer.UserID = userId.Value;
            volunteer.JoinedAt = DateTime.Now;

            // Check if user is already registered as a volunteer
            var existingVolunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserID == userId.Value);

            if (existingVolunteer != null)
            {
                ModelState.AddModelError("", "You are already registered as a volunteer.");
                return View(volunteer);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verify user exists in database
                    var userExists = await _context.Users.AnyAsync(u => u.UserID == userId.Value);
                    if (!userExists)
                    {
                        ModelState.AddModelError("", "User not found. Please log in again.");
                        return View(volunteer);
                    }

                    _context.Volunteers.Add(volunteer);
                    await _context.SaveChangesAsync();

                    // Update user role to Volunteer
                    var user = await _context.Users.FindAsync(userId.Value);
                    if (user != null && user.Role != "Admin") // Don't change admin role
                    {
                        user.Role = "Volunteer";
                        await _context.SaveChangesAsync();

                        // Update session with new role
                        HttpContext.Session.SetString("UserRole", "Volunteer");
                    }

                    TempData["SuccessMessage"] = "🎉 Thank you for registering as a volunteer! You can now access volunteer features.";
                    return RedirectToAction("UserHome", "Home");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "A database error occurred while registering as a volunteer. Please try again.");
                    _logger.LogError(ex, "Database error in Volunteerss");
                    Console.WriteLine($"DB Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                    _logger.LogError(ex, "Unexpected error in Volunteerss");
                    Console.WriteLine($"General Error: {ex.Message}");
                }
            }

            return View(volunteer);
        }

        // ======================
        // Other Actions
        // ======================
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ======================
        // Utility - Hash password
        // ======================
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // ======================
        // Debug/Test Actions
        // ======================
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();
                var incidentCount = await _context.Incidents.CountAsync();

                return Content($"DB Connected: {canConnect}, Users: {userCount}, Incidents: {incidentCount}");
            }
            catch (Exception ex)
            {
                return Content($"Connection failed: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }

        public async Task<IActionResult> DebugUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Json(users.Select(u => new { u.UserID, u.Email, u.Role }));
        }
        public IActionResult VolunteerTask()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VolunteerTask(VolunteerTask volunteerTask)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            // Set automatic fields
            volunteerTask.CreatedAt = DateTime.Now;

            // If AssignedTo is empty, set to null
            if (volunteerTask.AssignedTo == 0)
            {
                volunteerTask.AssignedTo = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.VolunteerTasks.Add(volunteerTask);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "✅ Volunteer task created successfully!";
                    return RedirectToAction("VolunteerHome", "Home");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "A database error occurred while creating the task. Please try again.");
                    _logger.LogError(ex, "Database error in VolunteerTask");
                    Console.WriteLine($"DB Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                    _logger.LogError(ex, "Unexpected error in VolunteerTask");
                    Console.WriteLine($"General Error: {ex.Message}");
                }
            }

            return View(volunteerTask);
        }

        // ======================
        // Get Volunteers for Dropdown
        // ======================
        public async Task<IActionResult> GetVolunteers()
        {
            try
            {
                var volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    .Select(v => new
                    {
                        volunteerID = v.VolunteerID,
                        fullName = v.User.FullName,
                        skills = v.Skills
                    })
                    .ToListAsync();

                return Json(volunteers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading volunteers");
                return Json(new List<object>());
            }
        }
        // ======================
        // View Volunteer Tasks
        // ======================
        public async Task<IActionResult> ViewVolunteerTasks()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var volunteerTasks = await _context.VolunteerTasks
                    .Include(t => t.AssignedVolunteer)
                        .ThenInclude(v => v.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return View(volunteerTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading volunteer tasks");
                TempData["ErrorMessage"] = "An error occurred while loading volunteer tasks.";
                return View(new List<VolunteerTask>());
            }
        }
        public async Task<IActionResult> ViewVolunteers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var volunteers = await _context.Volunteers
                    .Include(v => v.User)
                    .Include(v => v.Tasks)
                    .OrderByDescending(v => v.JoinedAt)
                    .ToListAsync();

                return View(volunteers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading volunteers");
                TempData["ErrorMessage"] = "An error occurred while loading volunteers.";
                return View(new List<Volunteer>());
            }
        }
        public async Task<IActionResult> ViewDonations()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var donations = await _context.Donations
                    .OrderByDescending(d => d.DonationDate)
                    .ToListAsync();

                return View(donations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading donations");
                TempData["ErrorMessage"] = "An error occurred while loading donations.";
                return View(new List<Donation>());
            }
        }
        public async Task<IActionResult> ViewIncidents()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var incidents = await _context.Incidents
                    .OrderByDescending(i => i.DateReported)
                    .ToListAsync();

                return View(incidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading incidents");
                TempData["ErrorMessage"] = "An error occurred while loading incidents.";
                return View(new List<Incident>());
            }
        }
    }
}
    
