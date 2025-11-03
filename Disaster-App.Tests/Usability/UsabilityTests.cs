using Disaster_App.Controllers;
using Disaster_App.Data;
using Disaster_App.Models;
using Disaster_App.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Disaster_App.Tests.Usability
{
    /// <summary>
    /// Usability tests to verify user flows and form interactions work correctly
    /// </summary>
    public class UsabilityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly HomeController _controller;

        public UsabilityTests()
        {
            _context = TestHelpers.CreateInMemoryDbContext();
            _loggerMock = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_loggerMock.Object, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Complete User Registration and Login Flow

        [Fact]
        public async Task Usability_CompleteRegistrationAndLoginFlow_Success()
        {
            // Step 1: User accesses registration page
            var registerGetResult = _controller.Register();
            Assert.IsType<ViewResult>(registerGetResult);

            // Step 2: User submits registration form
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            MockSessionHelper.SetupControllerContext(_controller, mockHttpContext);
            
            var newUser = new User
            {
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = "SecurePass123!",
                Role = "User"
            };

            var registerPostResult = await _controller.Register(newUser);
            var redirectToLogin = Assert.IsType<RedirectToActionResult>(registerPostResult);
            Assert.Equal("Login", redirectToLogin.ActionName);

            // Verify user was created
            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
            Assert.NotNull(userInDb);
            Assert.Equal("John Doe", userInDb.FullName);

            // Step 3: User accesses login page
            var loginGetResult = _controller.Login();
            Assert.IsType<ViewResult>(loginGetResult);

            // Step 4: User logs in with registered credentials
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "john@example.com",
                PasswordHash = "SecurePass123!"
            };

            var loginPostResult = await _controller.Login(loginUser);
            var redirectToHome = Assert.IsType<RedirectToActionResult>(loginPostResult);
            Assert.Equal("UserHome", redirectToHome.ActionName);

            // Verify session was set
            Assert.Equal("john@example.com", mockHttpContext.Object.Session.GetString("UserEmail"));
            Assert.Equal("User", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        #endregion

        #region Complete Incident Reporting Flow

        [Fact]
        public async Task Usability_CompleteIncidentReportingFlow_Success()
        {
            // Step 1: User registers and logs in
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "user@test.com",
                userId: user.UserID,
                userName: "Test User"
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 2: User navigates to LogIncident page
            var logIncidentGetResult = _controller.LogIncident();
            Assert.IsType<ViewResult>(logIncidentGetResult);

            // Step 3: User submits incident form
            var incident = new Incident
            {
                Title = "Flood in Downtown",
                Description = "Severe flooding reported in downtown area",
                Location = "123 Main St, Downtown"
            };

            var logIncidentPostResult = await _controller.LogIncident(incident);
            var redirectToHome = Assert.IsType<RedirectToActionResult>(logIncidentPostResult);
            Assert.Equal("UserHome", redirectToHome.ActionName);

            // Step 4: Verify incident was saved
            var incidentInDb = await _context.Incidents.FirstOrDefaultAsync(i => i.Title == "Flood in Downtown");
            Assert.NotNull(incidentInDb);
            Assert.Equal(user.UserID, incidentInDb.ReportedBy);
        }

        #endregion

        #region Complete Donation Flow

        [Fact]
        public async Task Usability_CompleteDonationFlow_Success()
        {
            // Step 1: User logs in
            var user = TestHelpers.CreateTestUser("donor@test.com", "Password123!", "User", "Jane Donor");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "donor@test.com",
                userId: user.UserID,
                userName: "Jane Donor"
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 2: User navigates to donation page (form should be pre-filled)
            var logDonationGetResult = _controller.LogDonation();
            var donationView = Assert.IsType<ViewResult>(logDonationGetResult);
            var donationModel = Assert.IsType<Donation>(donationView.Model);
            Assert.Equal("Jane Donor", donationModel.DonorName);
            Assert.Equal("donor@test.com", donationModel.Email);

            // Step 3: User submits donation form
            var donation = new Donation
            {
                DonorName = "Jane Donor",
                Email = "donor@test.com",
                ResourceType = "Food",
                Quantity = 50,
                Description = "Canned goods and non-perishable items",
                ContactNumber = "555-1234",
                PickupAddress = "456 Donor St"
            };

            var logDonationPostResult = await _controller.LogDonation(donation);
            var redirectToHome = Assert.IsType<RedirectToActionResult>(logDonationPostResult);
            Assert.Equal("UserHome", redirectToHome.ActionName);

            // Step 4: Verify donation was saved
            var donationInDb = await _context.Donations.FirstOrDefaultAsync(d => d.Email == "donor@test.com");
            Assert.NotNull(donationInDb);
            Assert.Equal("Food", donationInDb.ResourceType);
            Assert.Equal(50, donationInDb.Quantity);
            Assert.Equal("Pending", donationInDb.Status);
        }

        #endregion

        #region Complete Volunteer Registration Flow

        [Fact]
        public async Task Usability_CompleteVolunteerRegistrationFlow_Success()
        {
            // Step 1: User registers as regular user
            var user = TestHelpers.CreateTestUser("volunteer@test.com", "Password123!", "User");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Step 2: User logs in
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "volunteer@test.com",
                userId: user.UserID
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 3: User navigates to volunteer registration page
            var volunteerssGetResult = _controller.Volunteerss();
            var volunteerView = Assert.IsType<ViewResult>(volunteerssGetResult);
            var volunteerModel = Assert.IsType<Volunteer>(volunteerView.Model);
            Assert.Equal(user.UserID, volunteerModel.UserID);

            // Step 4: User submits volunteer registration form
            var volunteer = new Volunteer
            {
                UserID = user.UserID,
                Skills = "First Aid, Search & Rescue, Communication",
                Availability = "Weekends and evenings"
            };

            var volunteerssPostResult = await _controller.Volunteerss(volunteer);
            var redirectToHome = Assert.IsType<RedirectToActionResult>(volunteerssPostResult);
            Assert.Equal("UserHome", redirectToHome.ActionName);

            // Step 5: Verify volunteer profile was created and user role updated
            var volunteerInDb = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserID == user.UserID);
            Assert.NotNull(volunteerInDb);
            Assert.Equal("First Aid, Search & Rescue, Communication", volunteerInDb.Skills);

            var updatedUser = await _context.Users.FindAsync(user.UserID);
            Assert.NotNull(updatedUser);
            Assert.Equal("Volunteer", updatedUser.Role);
            Assert.Equal("Volunteer", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        #endregion

        #region Complete Volunteer Task Creation Flow

        [Fact]
        public async Task Usability_CompleteVolunteerTaskCreationFlow_Success()
        {
            // Step 1: User logs in
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 2: User navigates to volunteer task creation page
            var volunteerTaskGetResult = _controller.VolunteerTask();
            Assert.IsType<ViewResult>(volunteerTaskGetResult);

            // Step 3: User creates a volunteer task
            var volunteerTask = new VolunteerTask
            {
                TaskName = "Emergency Food Distribution",
                Description = "Distribute food packages to affected families",
                Status = "Open",
                AssignedTo = null
            };

            var volunteerTaskPostResult = await _controller.VolunteerTask(volunteerTask);
            var redirectToVolunteerHome = Assert.IsType<RedirectToActionResult>(volunteerTaskPostResult);
            Assert.Equal("VolunteerHome", redirectToVolunteerHome.ActionName);

            // Step 4: Verify task was created
            var taskInDb = await _context.VolunteerTasks.FirstOrDefaultAsync(t => t.TaskName == "Emergency Food Distribution");
            Assert.NotNull(taskInDb);
            Assert.Equal("Open", taskInDb.Status);
        }

        #endregion

        #region Session Management and Security Tests

        [Fact]
        public void Usability_SessionExpired_RedirectsToLogin()
        {
            // Step 1: User tries to access protected page without session
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 2: Attempt to access UserHome without login
            var userHomeResult = _controller.UserHome();
            var redirectToLogin1 = Assert.IsType<RedirectToActionResult>(userHomeResult);
            Assert.Equal("Login", redirectToLogin1.ActionName);

            // Step 3: Attempt to access LogIncident without login
            var logIncidentResult = _controller.LogIncident();
            var redirectToLogin2 = Assert.IsType<RedirectToActionResult>(logIncidentResult);
            Assert.Equal("Login", redirectToLogin2.ActionName);

            // Step 4: Attempt to access LogDonation without login
            var logDonationResult = _controller.LogDonation();
            var redirectToLogin3 = Assert.IsType<RedirectToActionResult>(logDonationResult);
            Assert.Equal("Login", redirectToLogin3.ActionName);
        }

        [Fact]
        public void Usability_AdminPage_OnlyAccessibleByAdmins()
        {
            // Step 1: Regular user tries to access admin page
            var userHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(role: "User");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = userHttpContext.Object
            };

            var userResult = _controller.AdminHome();
            var redirectToLogin1 = Assert.IsType<RedirectToActionResult>(userResult);
            Assert.Equal("Login", redirectToLogin1.ActionName);

            // Step 2: Volunteer tries to access admin page
            var volunteerHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(role: "Volunteer");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = volunteerHttpContext.Object
            };

            var volunteerResult = _controller.AdminHome();
            var redirectToLogin2 = Assert.IsType<RedirectToActionResult>(volunteerResult);
            Assert.Equal("Login", redirectToLogin2.ActionName);

            // Step 3: Admin successfully accesses admin page
            var adminHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(role: "Admin");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = adminHttpContext.Object
            };

            var adminResult = _controller.AdminHome();
            Assert.IsType<ViewResult>(adminResult);
        }

        #endregion

        #region Form Validation Tests

        [Fact]
        public async Task Usability_FormValidation_ShowsErrorsForInvalidInput()
        {
            // Test Register form validation
            var invalidUser = new User
            {
                FullName = "", // Invalid
                Email = "not-an-email", // Invalid
                PasswordHash = "", // Invalid
                Role = "User"
            };

            var registerResult = await _controller.Register(invalidUser);
            var registerView = Assert.IsType<ViewResult>(registerResult);
            Assert.False(_controller.ModelState.IsValid);

            // Test Donation form validation
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var invalidDonation = new Donation
            {
                DonorName = "", // Invalid
                Email = "invalid-email", // Invalid
                ResourceType = "", // Invalid
                Quantity = -5 // Invalid
            };

            var donationResult = await _controller.LogDonation(invalidDonation);
            var donationView = Assert.IsType<ViewResult>(donationResult);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task Usability_DataIntegrity_DuplicateEmailPrevented()
        {
            // Step 1: First user registers
            var user1 = TestHelpers.CreateTestUser("duplicate@test.com", "Password123!");
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            // Step 2: Second user tries to register with same email
            var user2 = new User
            {
                FullName = "Another User",
                Email = "duplicate@test.com",
                PasswordHash = "DifferentPass123!",
                Role = "User"
            };

            var result = await _controller.Register(user2);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("Email"));

            // Verify only one user exists
            var userCount = await _context.Users.CountAsync(u => u.Email == "duplicate@test.com");
            Assert.Equal(1, userCount);
        }

        [Fact]
        public async Task Usability_DataIntegrity_DuplicateVolunteerRegistrationPrevented()
        {
            // Step 1: User registers as volunteer
            var user = TestHelpers.CreateTestUser("volunteer@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var volunteer1 = new Volunteer
            {
                UserID = user.UserID,
                Skills = "First Aid"
            };
            _context.Volunteers.Add(volunteer1);
            await _context.SaveChangesAsync();

            // Step 2: User tries to register as volunteer again
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(userId: user.UserID);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var volunteer2 = new Volunteer
            {
                UserID = user.UserID,
                Skills = "Different Skills"
            };

            var result = await _controller.Volunteerss(volunteer2);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);

            // Verify only one volunteer profile exists
            var volunteerCount = await _context.Volunteers.CountAsync(v => v.UserID == user.UserID);
            Assert.Equal(1, volunteerCount);
        }

        #endregion

        #region Password Security Tests

        [Fact]
        public async Task Usability_PasswordSecurity_PasswordsAreHashed()
        {
            // Step 1: User registers with plain password
            var plainPassword = "MySecurePassword123!";
            var newUser = new User
            {
                FullName = "Security Test User",
                Email = "security@test.com",
                PasswordHash = plainPassword,
                Role = "User"
            };

            await _controller.Register(newUser);

            // Step 2: Verify password is hashed in database
            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "security@test.com");
            Assert.NotNull(userInDb);
            Assert.NotEqual(plainPassword, userInDb.PasswordHash);
            Assert.True(userInDb.PasswordHash.Length > 20); // Base64 encoded SHA256 hash

            // Step 3: User can still login with plain password (controller hashes it)
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "security@test.com",
                PasswordHash = plainPassword
            };

            var loginResult = await _controller.Login(loginUser);
            Assert.IsType<RedirectToActionResult>(loginResult);
        }

        #endregion

        #region Navigation Flow Tests

        [Fact]
        public async Task Usability_NavigationFlow_UserCanNavigateBetweenPagesAfterLogin()
        {
            // Step 1: User logs in
            var user = TestHelpers.CreateTestUser("nav@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "nav@test.com",
                userId: user.UserID,
                userName: "Navigation User"
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Step 2: User can access UserHome
            var userHomeResult = _controller.UserHome();
            Assert.IsType<ViewResult>(userHomeResult);

            // Step 3: User can navigate to LogIncident
            var logIncidentResult = _controller.LogIncident();
            Assert.IsType<ViewResult>(logIncidentResult);

            // Step 4: User can navigate to LogDonation
            var logDonationResult = _controller.LogDonation();
            Assert.IsType<ViewResult>(logDonationResult);

            // Step 5: User can navigate to Volunteer registration
            var volunteerResult = _controller.Volunteerss();
            Assert.IsType<ViewResult>(volunteerResult);
        }

        #endregion
    }
}

