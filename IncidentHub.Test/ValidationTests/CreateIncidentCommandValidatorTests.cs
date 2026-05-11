using FluentValidation.TestHelper;
using IncidentHub.Api.Features.Incidents.Commands.CreateIncident;

namespace IncidentHub.Tests.ValidationTests;

public class CreateIncidentCommandValidatorTests
{
    private readonly CreateIncidentCommandValidator _validator;

    public CreateIncidentCommandValidatorTests()
    {
        _validator = new CreateIncidentCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "Valid Title",
            "Valid description",
            Severity.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TitleIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "", // Empty title
            "Valid description",
            Severity.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            new string('a', 251), // Too long
            "Valid description",
            Severity.Medium);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 250 characters.");
    }

    [Fact]
    public void Validate_ValidSeverity_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "Valid Title",
            "Valid description",
            Severity.Critical);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Severity);
    }

    [Fact]
    public void Validate_InvalidSeverity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "Valid Title",
            "Valid description",
            (Severity)999); // Invalid severity

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Severity)
            .WithErrorMessage("Severity must be a valid value: Low, Medium, High, or Critical.");
    }
}