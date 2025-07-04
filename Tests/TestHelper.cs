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
            Name = "Test User",
            DateOfBirth = "1990-01-01",
            IdNumber = "1234567890123456",
            CfLosAppNo = "TEST-001",
            Action = "perorangan",
            MotherName = "Test Mother",
            Tolerance = 0,
            FacilityLimit = 10000,
            SimilarityCheckVersion = 3,
            TableVersion = 2
        };
    }

    public static IndividualRequest CreateInvalidIndividualRequest()
    {
        return new IndividualRequest
        {
            TypeData = "",
            Name = "",
            DateOfBirth = "invalid-date",
            IdNumber = "123", // Too short
            CfLosAppNo = "",
            MotherName = "",
            FacilityLimit = -1000 // Negative
        };
    }
}
