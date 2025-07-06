using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.Tests;

/// <summary>
/// Helper class for creating test data
/// </summary>
public static class TestHelper
{
    public static IndividualRequest CreateValidIndividualRequest()
    {
        return new IndividualRequest
        {
            TypeData = "Individual",
            Name = "John Doe", // Valid name (2+ chars, letters and space only)
            DateOfBirth = "1990-01-01", // Valid date, makes person 33-34 years old
            IdNumber = "1234567890123456", // Exactly 16 digits
            CfLosAppNo = "TEST-001", // 3+ characters
            Action = "perorangan",
            MotherName = "Jane Doe", // Valid mother name
            Tolerance = 0,
            FacilityLimit = 10000, // Positive value
            SimilarityCheckVersion = 3, // Valid version (1-3)
            TableVersion = 2 // Valid version (1-2)
        };
    }

    public static IndividualRequest CreateInvalidIndividualRequest()
    {
        return new IndividualRequest
        {
            TypeData = "", // Invalid: empty
            Name = "", // Invalid: empty
            DateOfBirth = "invalid-date", // Invalid format
            IdNumber = "123", // Invalid: too short
            CfLosAppNo = "", // Invalid: empty
            MotherName = "", // Invalid: empty
            FacilityLimit = -1000 // Invalid: negative
        };
    }
}
