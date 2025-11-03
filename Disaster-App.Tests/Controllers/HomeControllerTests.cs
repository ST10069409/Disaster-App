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

namespace Disaster_App.Tests.Controllers
{
    public class HomeControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly HomeController _controller;

        public HomeControllerTests()
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

        #region Register Tests

        [Fact]
        public void Register_GET_ReturnsView()
        {
            // Act
            var result = _controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        [Fact]
        public async Task Register_POST_ValidUser_CreatesUserAndRedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            MockSessionHelper.SetupControllerContext(_controller, mockHttpContext);
            
            var newUser = new User
            {
                FullName = "John Doe",
                Email = "newuser@test.com",
                PasswordHash = "Password123!",
                Role = "User"
            };

            // Act
            var result = await _controller.Register(newUser);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            
            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
            Assert.NotNull(userInDb);
            Assert.Equal("John Doe", userInDb.FullName);
            Assert.NotEqual("Password123!", userInDb.PasswordHash); // Should be hashed
        }

        [Fact]
        public async Task Register_POST_DuplicateEmail_ReturnsViewWithError()
        {
            // Arrange
            var existingUser = TestHelpers.CreateTestUser("existing@test.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var duplicateUser = new User
            {
                FullName = "Another User",
                Email = "existing@test.com",
                PasswordHash = "Password123!",
                Role = "User"
            };

            // Act
            var result = await _controller.Register(duplicateUser);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Email"));
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Register_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var invalidUser = new User
            {
                FullName = "", // Invalid: empty name
                Email = "invalid-email", // Invalid: not a valid email
                PasswordHash = "", // Invalid: empty password
                Role = "User"
            };

            // Act
            var result = await _controller.Register(invalidUser);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region Login Tests

        [Fact]
        public void Login_GET_ReturnsView()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        [Fact]
        public async Task Login_POST_ValidUserWithRoleUser_RedirectsToUserHome()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!", "User");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "user@test.com",
                PasswordHash = "Password123!"
            };

            // Act
            var result = await _controller.Login(loginUser);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirectResult.ActionName);
            
            // Verify session was set
            Assert.Equal("user@test.com", mockHttpContext.Object.Session.GetString("UserEmail"));
            Assert.Equal("User", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        [Fact]
        public async Task Login_POST_ValidUserWithRoleVolunteer_RedirectsToVolunteerHome()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("volunteer@test.com", "Password123!", "Volunteer");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "volunteer@test.com",
                PasswordHash = "Password123!"
            };

            // Act
            var result = await _controller.Login(loginUser);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VolunteerHome", redirectResult.ActionName);
            Assert.Equal("Volunteer", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        [Fact]
        public async Task Login_POST_ValidUserWithRoleAdmin_RedirectsToAdminHome()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("admin@test.com", "Password123!", "Admin");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "admin@test.com",
                PasswordHash = "Password123!"
            };

            // Act
            var result = await _controller.Login(loginUser);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AdminHome", redirectResult.ActionName);
            Assert.Equal("Admin", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        [Fact]
        public async Task Login_POST_InvalidEmail_ReturnsViewWithError()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "wrong@test.com",
                PasswordHash = "Password123!"
            };

            // Act
            var result = await _controller.Login(loginUser);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.Error);
            Assert.Equal("Invalid login credentials", _controller.ViewBag.Error);
        }

        [Fact]
        public async Task Login_POST_InvalidPassword_ReturnsViewWithError()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var loginUser = new User
            {
                Email = "user@test.com",
                PasswordHash = "WrongPassword!"
            };

            // Act
            var result = await _controller.Login(loginUser);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.Error);
        }

        #endregion

        #region LogIncident Tests

        [Fact]
        public void LogIncident_GET_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.LogIncident();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void LogIncident_GET_WithSession_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.LogIncident();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task LogIncident_POST_ValidIncident_CreatesIncidentAndRedirects()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "user@test.com",
                userId: user.UserID
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var incident = new Incident
            {
                Title = "Test Incident",
                Description = "Test Description",
                Location = "Test Location"
            };

            // Act
            var result = await _controller.LogIncident(incident);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirectResult.ActionName);

            var incidentInDb = await _context.Incidents.FirstOrDefaultAsync(i => i.Title == "Test Incident");
            Assert.NotNull(incidentInDb);
            Assert.Equal(user.UserID, incidentInDb.ReportedBy);
        }

        [Fact]
        public async Task LogIncident_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var invalidIncident = new Incident
            {
                Title = "", // Invalid: empty title
                Description = "Test Description",
                Location = "Test Location"
            };

            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.LogIncident(invalidIncident);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task LogIncident_POST_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var incident = new Incident
            {
                Title = "Test Incident",
                Description = "Test Description",
                Location = "Test Location"
            };

            // Act
            var result = await _controller.LogIncident(incident);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        #endregion

        #region LogDonation Tests

        [Fact]
        public void LogDonation_GET_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.LogDonation();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void LogDonation_GET_WithSession_ReturnsViewWithPrefilledData()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "user@test.com",
                userName: "John Doe"
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.LogDonation();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Donation>(viewResult.Model);
            Assert.Equal("John Doe", model.DonorName);
            Assert.Equal("user@test.com", model.Email);
        }

        [Fact]
        public async Task LogDonation_POST_ValidDonation_CreatesDonationAndRedirects()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "user@test.com",
                userName: "John Doe"
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var donation = new Donation
            {
                DonorName = "John Doe",
                Email = "user@test.com",
                ResourceType = "Food",
                Quantity = 10,
                Description = "Canned goods"
            };

            // Act
            var result = await _controller.LogDonation(donation);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirectResult.ActionName);

            var donationInDb = await _context.Donations.FirstOrDefaultAsync(d => d.Email == "user@test.com");
            Assert.NotNull(donationInDb);
            Assert.Equal("Food", donationInDb.ResourceType);
            Assert.Equal(10, donationInDb.Quantity);
        }

        [Fact]
        public async Task LogDonation_POST_InvalidModel_ReturnsView()
        {
            // Arrange
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
                Quantity = -1 // Invalid
            };

            // Act
            var result = await _controller.LogDonation(invalidDonation);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region VolunteerTask Tests

        [Fact]
        public void VolunteerTask_GET_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.VolunteerTask();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void VolunteerTask_GET_WithSession_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.VolunteerTask();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task VolunteerTask_POST_ValidTask_CreatesTaskAndRedirects()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var volunteerTask = new VolunteerTask
            {
                TaskName = "Emergency Response",
                Description = "Help with emergency response",
                Status = "Open",
                AssignedTo = null
            };

            // Act
            var result = await _controller.VolunteerTask(volunteerTask);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VolunteerHome", redirectResult.ActionName);

            var taskInDb = await _context.VolunteerTasks.FirstOrDefaultAsync(t => t.TaskName == "Emergency Response");
            Assert.NotNull(taskInDb);
            Assert.Equal("Open", taskInDb.Status);
        }

        [Fact]
        public async Task VolunteerTask_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var invalidTask = new VolunteerTask
            {
                TaskName = "", // Invalid: empty name
                Description = "Test Description",
                Status = "Open"
            };

            _controller.ModelState.AddModelError("TaskName", "Task name is required");

            // Act
            var result = await _controller.VolunteerTask(invalidTask);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task VolunteerTask_POST_ZeroAssignedTo_SetsToNull()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var volunteerTask = new VolunteerTask
            {
                TaskName = "Test Task",
                Description = "Test Description",
                Status = "Open",
                AssignedTo = 0
            };

            // Act
            await _controller.VolunteerTask(volunteerTask);

            // Assert
            var taskInDb = await _context.VolunteerTasks.FirstOrDefaultAsync();
            Assert.NotNull(taskInDb);
            Assert.Null(taskInDb.AssignedTo);
        }

        #endregion

        #region Volunteerss (Volunteer Registration) Tests

        [Fact]
        public void Volunteerss_GET_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.Volunteerss();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void Volunteerss_GET_WithSession_ReturnsViewWithPrefilledUserId()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(userId: 1);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.Volunteerss();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Volunteer>(viewResult.Model);
            Assert.Equal(1, model.UserID);
        }

        [Fact]
        public async Task Volunteerss_POST_ValidVolunteer_CreatesVolunteerAndUpdatesUserRole()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!", "User");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "user@test.com",
                userId: user.UserID
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var volunteer = new Volunteer
            {
                UserID = user.UserID,
                Skills = "First Aid, Communication",
                Availability = "Weekends"
            };

            // Act
            var result = await _controller.Volunteerss(volunteer);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserHome", redirectResult.ActionName);

            var volunteerInDb = await _context.Volunteers.FirstOrDefaultAsync(v => v.UserID == user.UserID);
            Assert.NotNull(volunteerInDb);
            Assert.Equal("First Aid, Communication", volunteerInDb.Skills);

            // Verify user role was updated
            var updatedUser = await _context.Users.FindAsync(user.UserID);
            Assert.Equal("Volunteer", updatedUser?.Role);
            Assert.Equal("Volunteer", mockHttpContext.Object.Session.GetString("UserRole"));
        }

        [Fact]
        public async Task Volunteerss_POST_DuplicateVolunteer_ReturnsViewWithError()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("user@test.com", "Password123!", "User");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var existingVolunteer = new Volunteer
            {
                UserID = user.UserID,
                Skills = "Existing Skills"
            };
            _context.Volunteers.Add(existingVolunteer);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(userId: user.UserID);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var duplicateVolunteer = new Volunteer
            {
                UserID = user.UserID,
                Skills = "New Skills"
            };

            // Act
            var result = await _controller.Volunteerss(duplicateVolunteer);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(""));
        }

        [Fact]
        public async Task Volunteerss_POST_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var volunteer = new Volunteer
            {
                Skills = "Test Skills"
            };

            // Act
            var result = await _controller.Volunteerss(volunteer);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        #endregion

        #region View Actions Tests

        [Fact]
        public void UserHome_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.UserHome();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void UserHome_WithSession_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.UserHome();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void VolunteerHome_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.VolunteerHome();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task VolunteerHome_WithSession_ReturnsViewWithTasks()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("volunteer@test.com", "Password123!", "Volunteer");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var volunteer = new Volunteer
            {
                UserID = user.UserID,
                Skills = "Test Skills"
            };
            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            var task1 = new VolunteerTask
            {
                TaskName = "Task 1",
                Status = "Open",
                AssignedTo = volunteer.VolunteerID
            };
            var task2 = new VolunteerTask
            {
                TaskName = "Task 2",
                Status = "Completed",
                AssignedTo = volunteer.VolunteerID
            };
            _context.VolunteerTasks.AddRange(task1, task2);
            await _context.SaveChangesAsync();

            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(
                email: "volunteer@test.com",
                userId: user.UserID
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.VolunteerHome();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var tasks = _controller.ViewBag.VolunteerTasks as List<VolunteerTask>;
            Assert.NotNull(tasks);
            Assert.Single(tasks); // Only non-completed tasks
            Assert.Equal("Task 1", tasks[0].TaskName);
        }

        [Fact]
        public void AdminHome_WithoutSession_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.AdminHome();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void AdminHome_WithNonAdminRole_RedirectsToLogin()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(role: "User");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.AdminHome();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void AdminHome_WithAdminRole_ReturnsView()
        {
            // Arrange
            var mockHttpContext = MockSessionHelper.CreateMockHttpContextWithUser(role: "Admin");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.AdminHome();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
        }

        #endregion
    }
}

