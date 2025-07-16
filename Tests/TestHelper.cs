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
            TypeData = "PERSONAL",
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

    /// <summary>
    /// Creates test database configuration
    /// </summary>
    public static Dictionary<string, string?> CreateTestDatabaseConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["DBEncryptedPassword"] = "chp6Ov+o88fjesa58tlMCXYPm6QZYpmWJVRgWfLfhlo=",
            ["DatabaseConfiguration:Names:idccore"] = "idc.core",
            ["DatabaseConfiguration:Names:idcen"] = "idc.en",
            ["DatabaseConfiguration:Names:idcbk"] = "idc.bk",
            ["DatabaseConfiguration:Names:idccust"] = "idc.cust",
            ["DatabaseConfiguration:ConnectionStrings:idccore"] = "Host=localhost;Database=idc.core_test;Username=test_user;Password={DBEncryptedPassword};",
            ["DatabaseConfiguration:ConnectionStrings:idcen"] = "Host=localhost;Database=idc.en_test;Username=test_user;Password={DBEncryptedPassword};",
            ["DatabaseConfiguration:ConnectionStrings:idcbk"] = "Host=localhost;Database=idc.bk_test;Username=test_user;Password={DBEncryptedPassword};",
            ["DatabaseConfiguration:ConnectionStrings:idccust"] = "Host=localhost;Database=idc.cust_test;Username=test_user;Password={DBEncryptedPassword};",
        };
    }
}
