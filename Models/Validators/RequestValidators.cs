using FluentValidation;
using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.Models.Validators;

/// <summary>
/// Validator for IndividualRequest with comprehensive business rules
/// </summary>
public class IndividualRequestValidator : AbstractValidator<IndividualRequest>
{
    public IndividualRequestValidator()
    {
        RuleFor(x => x.TypeData)
            .NotEmpty().WithMessage("Type data is required")
            .Must(x => x == "Individual" || x == "Corporate")
            .WithMessage("Type data must be 'Individual' or 'Corporate'");
        
        // Corrected Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Name must contain only letters and spaces");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(BeValidDate).WithMessage("Date of birth must be in YYYY-MM-DD format")
            .Must(BeValidAge).WithMessage("Date of birth must indicate person is at least 17 years old");

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required")
            .Length(16, 16).WithMessage("ID number must be exactly 16 digits")
            .Must(BeAllDigits).WithMessage("ID number must contain only digits");

        RuleFor(x => x.CfLosAppNo)
            .NotEmpty().WithMessage("Application number is required")
            .Length(3, 50).WithMessage("Application number must be between 3 and 50 characters");

        RuleFor(x => x.MotherName)
            .NotEmpty().WithMessage("Mother name is required")
            .Length(2, 100).WithMessage("Mother name must be between 2 and 100 characters");

        RuleFor(x => x.Tolerance)
            .GreaterThanOrEqualTo(0).WithMessage("Tolerance must be 0 or greater")
            .LessThanOrEqualTo(30).WithMessage("Tolerance cannot exceed 30 days");

        RuleFor(x => x.FacilityLimit)
            .GreaterThan(0).WithMessage("Facility limit must be greater than 0")
            .LessThanOrEqualTo(999999999999).WithMessage("Facility limit is too large");

        RuleFor(x => x.SimilarityCheckVersion)
            .InclusiveBetween(1, 3).WithMessage("Similarity check version must be 1, 2, or 3");

        RuleFor(x => x.TableVersion)
            .InclusiveBetween(1, 2).WithMessage("Table version must be 1 or 2");
    }

    private static bool BeValidDate(string dateString)
    {
        return DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, 
            System.Globalization.DateTimeStyles.None, out _);
    }

    private static bool BeValidAge(string dateString)
    {
        if (!DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, 
            System.Globalization.DateTimeStyles.None, out var birthDate))
            return false;

        var age = DateTime.Now.Year - birthDate.Year;
        if (birthDate.Date > DateTime.Now.AddYears(-age)) age--;

        return age >= 17 && age <= 100; // Reasonable age limits
    }

    private static bool BeAllDigits(string value)
    {
        return value.All(char.IsDigit);
    }
}
