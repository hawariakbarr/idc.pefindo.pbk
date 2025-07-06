using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Main service interface for orchestrating the complete individual processing workflow
/// 
/// WORKFLOW:
/// 1. Validate cycle day rules
/// 2. Authenticate with Pefindo API
/// 3. Perform smart search
/// 4. Validate search similarity
/// 5. Generate detailed report
/// 6. Validate report similarity  
/// 7. Aggregate and store summary data
/// 8. Log all processing steps
/// </summary>
public interface IIndividualProcessingService
{
    /// <summary>
    /// Processes a complete individual credit assessment request
    /// </summary>
    /// <param name="request">Individual request data</param>
    /// <returns>Processed individual response</returns>
    Task<IndividualResponse> ProcessIndividualRequestAsync(IndividualRequest request);
}
