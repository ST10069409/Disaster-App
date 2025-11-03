using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Disaster_App.Tests.Helpers
{
    public static class MockSessionHelper
    {
        /// <summary>
        /// Creates a mock HttpContext with session support using TestSession
        /// </summary>
        public static Mock<HttpContext> CreateMockHttpContext()
        {
            var mockHttpContext = new Mock<HttpContext>();
            var testSession = new TestSession();

            mockHttpContext.Setup(c => c.Session).Returns(testSession);
            
            // Set up Request and Response
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost"));
            mockRequest.Setup(r => r.PathBase).Returns(PathString.Empty);
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            
            var mockResponse = new Mock<HttpResponse>();
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);

            return mockHttpContext;
        }

        /// <summary>
        /// Creates a mock HttpContext with logged-in user session
        /// </summary>
        public static Mock<HttpContext> CreateMockHttpContextWithUser(
            string email = "test@example.com",
            string role = "User",
            int userId = 1,
            string userName = "Test User")
        {
            var mockHttpContext = CreateMockHttpContext();
            var session = mockHttpContext.Object.Session;
            
            // Use the extension methods which will work with our TestSession implementation
            session.SetString("UserEmail", email);
            session.SetString("UserRole", role);
            session.SetInt32("UserId", userId);
            session.SetString("UserName", userName);

            return mockHttpContext;
        }

        /// <summary>
        /// Sets up the controller context for a controller
        /// </summary>
        public static void SetupControllerContext(Controller controller, Mock<HttpContext> mockHttpContext)
        {
            var actionContext = new ActionContext(
                mockHttpContext.Object,
                new RouteData(),
                new ControllerActionDescriptor()
            );

            controller.ControllerContext = new ControllerContext(actionContext);
        }
    }
}

