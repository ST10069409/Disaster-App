# Disaster App Unit Tests

This directory contains comprehensive unit tests for the Disaster App application.

## Test Coverage

### Controller Tests (`HomeControllerTests.cs`)
Tests all controller actions including:
- **Register**: User registration with validation, duplicate email detection
- **Login**: Authentication with role-based redirects (User, Volunteer, Admin)
- **LogIncident**: Incident reporting form functionality
- **LogDonation**: Donation form with pre-filled user data
- **VolunteerTask**: Volunteer task creation
- **Volunteerss**: Volunteer registration with role updates
- **View Actions**: UserHome, VolunteerHome, AdminHome with proper authorization

### Usability Tests (`UsabilityTests.cs`)
End-to-end user flow tests including:
- Complete registration and login flow
- Complete incident reporting workflow
- Complete donation submission workflow
- Complete volunteer registration workflow
- Session management and security
- Form validation
- Data integrity checks
- Password security
- Navigation flow

## Running the Tests

### Using .NET CLI
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~HomeControllerTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~Register_POST_ValidUser_CreatesUserAndRedirectsToLogin"
```

### Using Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Build the solution (Ctrl+Shift+B)
3. Run all tests or select specific tests to run

### Using Rider
1. Open Unit Tests window
2. Build the solution
3. Run all tests or select specific tests

## Test Structure

### Helpers
- **TestHelpers.cs**: Utility methods for creating test data and in-memory database contexts
- **MockSessionHelper.cs**: Helper for mocking HTTP sessions and contexts

### Test Categories
- **Controller Tests**: Unit tests for individual controller actions
- **Usability Tests**: Integration-style tests for complete user workflows

## Technologies Used

- **xUnit**: Testing framework
- **Moq**: Mocking framework for dependencies
- **Entity Framework Core InMemory**: In-memory database for testing
- **Microsoft.AspNetCore.Mvc.Testing**: ASP.NET Core testing support

## Test Statistics

- **Total Test Classes**: 2
- **Total Test Methods**: ~50+ tests
- **Coverage Areas**: 
  - All forms (Register, Login, LogIncident, LogDonation, VolunteerTask, Volunteerss)
  - Authentication and authorization
  - Session management
  - Data validation
  - Error handling

## Notes

- All tests use in-memory database for isolation
- Each test cleans up after itself using `IDisposable`
- Session is properly mocked to test authentication flows
- Tests verify both success and failure scenarios

