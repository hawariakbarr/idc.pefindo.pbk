using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of data aggregation service
/// </summary>
public class DataAggregationService : IDataAggregationService
{
    private readonly ILogger<DataAggregationService> _logger;

    public DataAggregationService(ILogger<DataAggregationService> logger)
    {
        _logger = logger;
    }

    public async Task<IndividualData> AggregateIndividualDataAsync(
        IndividualRequest request,
        PefindoSearchResponse searchResponse,
        PefindoGetReportResponse reportResponse,
        ProcessingContext processingContext)
    {
        try
        {
            _logger.LogDebug("Aggregating individual data for app_no: {AppNo}", request.CfLosAppNo);
            
            var reportData = reportResponse.Report;
            var debiturInfo = reportData?.Debitur;
            var searchData = searchResponse.Data.FirstOrDefault();
            
            // Map data to individual response format
            var individualData = new IndividualData
            {
                AppNo = request.CfLosAppNo,
                IdNumber = request.IdNumber,
                CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                
                // Search-related data
                SearchId = searchResponse.InquiryId.ToString(),
                PefindoId = searchData?.IdPefindo.ToString() ?? string.Empty,
                
                // Report-related data from Pefindo
                Score = reportData?.ScoreInfo?.Score ?? "0",
                MaxOverdue = debiturInfo?.MaxCurrDpd.ToString() ?? "0",
                MaxOverdueLast12Months = debiturInfo?.MaxOverdueLast12Months.ToString() ?? "0",
                TotalFacilities = debiturInfo?.JmlFasilitas.ToString() ?? "0",
                Plafon = debiturInfo?.JmlPlafon.ToString() ?? "0",
                TotalAngsuranAktif = debiturInfo?.TotalAngsuranAktif.ToString() ?? "0",
                BakiDebetNonAgunan = debiturInfo?.BakiDebetNonAgunan.ToString() ?? "0",
                FasilitasAktif = debiturInfo?.JmlFasilitas.ToString() ?? "0",
                
                // Credit quality analysis
                KualitasKreditTerburuk = AnalyzeWorstCreditQuality(reportData?.Facilities),
                BulanKualitasTerburuk = AnalyzeWorstCreditQualityMonth(reportData?.Facilities),
                KualitasKreditTerakhir = AnalyzeLatestCreditQuality(reportData?.Facilities),
                BulanKualitasKreditTerakhir = AnalyzeLatestCreditQualityMonth(reportData?.Facilities),
                
                // Write-off analysis
                WoContract = AnalyzeWriteOffContracts(reportData?.Facilities),
                WoAgunan = AnalyzeWriteOffCollateral(reportData?.Collaterals),
                
                // Overdue analysis  
                WorstOvd = CalculateWorstOverdue(reportData?.Facilities),
                TotBakidebet3160dpd = CalculateOutstandingInDpdRange(reportData?.Facilities, 31, 60),
                NoKol1Active = CountActiveKol1Facilities(reportData?.Facilities),
                Nom0312mthAll = CalculateNominal0312Months(reportData?.Facilities),
                
                // Processing status
                Status = "SUCCESS",
                ResponseStatus = reportResponse.Status?.ToUpper() ?? "SUCCESS",
                ResponseMessage = reportResponse.Message ?? "Processing completed successfully",
                Message = "Individual credit assessment completed"
            };
            
            _logger.LogInformation("Data aggregation completed for app_no: {AppNo}", request.CfLosAppNo);
            return await Task.FromResult(individualData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating individual data for app_no: {AppNo}", request.CfLosAppNo);
            throw;
        }
    }

    private string AnalyzeWorstCreditQuality(List<PefindoFacility>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";
            
        // Map credit quality to numeric values (5=worst, 1=best)
        var worstQuality = facilities
            .Select(f => MapCreditQualityToNumber(f.KualitasKredit))
            .DefaultIfEmpty(0)
            .Max();
            
        return worstQuality.ToString();
    }

    private string AnalyzeWorstCreditQualityMonth(List<PefindoFacility>? facilities)
    {
        // This would typically come from historical data in the report
        // For now, return current month if there are any facilities with bad quality
        if (facilities?.Any(f => MapCreditQualityToNumber(f.KualitasKredit) >= 3) == true)
        {
            return DateTime.Now.ToString("yyyy-MM");
        }
        return string.Empty;
    }

    private string AnalyzeLatestCreditQuality(List<PefindoFacility>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";
            
        // Get the most recent facility's credit quality
        var latestFacility = facilities.OrderByDescending(f => f.FacilityId).FirstOrDefault();
        return MapCreditQualityToNumber(latestFacility?.KualitasKredit).ToString();
    }

    private string AnalyzeLatestCreditQualityMonth(List<PefindoFacility>? facilities)
    {
        // Similar to worst quality month but for latest
        return DateTime.Now.ToString("yyyy-MM");
    }

    private string AnalyzeWriteOffContracts(List<PefindoFacility>? facilities)
    {
        if (facilities == null)
            return "0";
            
        var writeOffCount = facilities.Count(f => 
            f.KualitasKredit?.ToUpperInvariant().Contains("MACET") == true ||
            f.KualitasKredit?.ToUpperInvariant().Contains("WRITE") == true);
            
        return writeOffCount.ToString();
    }

    private string AnalyzeWriteOffCollateral(List<PefindoCollateral>? collaterals)
    {
        // Analyze collateral write-offs (implementation depends on data structure)
        return "0";
    }

    private string CalculateWorstOverdue(List<PefindoFacility>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";
            
        var worstDpd = facilities.Max(f => f.CurrentDpd);
        return worstDpd.ToString();
    }

    private string CalculateOutstandingInDpdRange(List<PefindoFacility>? facilities, int minDpd, int maxDpd)
    {
        if (facilities == null)
            return "0";
            
        var totalOutstanding = facilities
            .Where(f => f.CurrentDpd >= minDpd && f.CurrentDpd <= maxDpd)
            .Sum(f => f.BakiDebet);
            
        return totalOutstanding.ToString();
    }

    private string CountActiveKol1Facilities(List<PefindoFacility>? facilities)
    {
        if (facilities == null)
            return "0";
            
        var kol1Count = facilities.Count(f => 
            f.KualitasKredit?.ToUpperInvariant() == "LANCAR" ||
            f.KualitasKredit?.ToUpperInvariant() == "KOL 1");
            
        return kol1Count.ToString();
    }

    private string CalculateNominal0312Months(List<PefindoFacility>? facilities)
    {
        // Calculate nominal for 0-3 and 12 months range
        // Implementation depends on historical data structure
        return "0";
    }

    private int MapCreditQualityToNumber(string? kualitasKredit)
    {
        if (string.IsNullOrEmpty(kualitasKredit))
            return 0;
            
        return kualitasKredit.ToUpperInvariant() switch
        {
            "LANCAR" or "KOL 1" => 1,
            "DPK" or "KOL 2" => 2,
            "KURANG LANCAR" or "KOL 3" => 3,
            "DIRAGUKAN" or "KOL 4" => 4,
            "MACET" or "KOL 5" => 5,
            _ => 0
        };
    }
}
