using Xunit;
using FluentValidation.TestHelper;
using idc.pefindo.pbk.Models.Validators;
using idc.pefindo.pbk.Tests;

namespace idc.pefindo.pbk.Tests.Unit;

public class IndividualRequestValidatorTests
{
    private readonly IndividualRequestValidator _validator;

    public IndividualRequestValidatorTests()
    {
        _validator = new IndividualRequestValidator();
    }

    [Fact]
    public void ValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("X")]
    [InlineData("ThisNameIsTooLongAndExceedsTheMaximumAllowedLengthForTheNameFieldWhichShouldBeValidated")]
    public void InvalidName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.Name = name;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345")]
    [InlineData("12345678901234567")]
    [InlineData("123456789012345a")]
    public void InvalidIdNumber_ShouldHaveValidationError(string idNumber)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.IdNumber = idNumber;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-date")]
    [InlineData("2025-01-01")] // Future date
    [InlineData("1900-01-01")] // Too old
    public void InvalidDateOfBirth_ShouldHaveValidationError(string dateOfBirth)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.DateOfBirth = dateOfBirth;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void InvalidTolerance_ShouldHaveValidationError(int tolerance)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.Tolerance = tolerance;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tolerance);
    }
}
