using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using idc.pefindo.pbk.Utilities;
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
                PefindoId = searchData?.IdPefindo.ToString() ?? "N/A",

                // Report-related data from Pefindo
                Score = GetScoreFromReport(reportResponse) ?? "0",
                MaxOverdue = debiturInfo?.TunggakanTerburuk.ToString() ?? "0",
                MaxOverdueLast12Months = debiturInfo?.TunggakanTerburuk12Bln.ToString() ?? "0",
                TotalFacilities = debiturInfo?.JmlFasilitas.ToString() ?? "0",
                Plafon = debiturInfo?.JmlPlafon.ToString() ?? "0",
                TotalAngsuranAktif = debiturInfo?.JmlAktifFasilitas.ToString() ?? "0",
                BakiDebetNonAgunan = debiturInfo?.JmlSaldoTerutang.ToString() ?? "0",
                FasilitasAktif = debiturInfo?.JmlAktifFasilitas.ToString() ?? "0",

                // Credit quality analysis
                KualitasKreditTerburuk = debiturInfo?.KolektabilitasTerburuk.ToString() ?? "0",
                BulanKualitasTerburuk = GetWorstCreditQualityMonth(reportData?.Fasilitas),
                KualitasKreditTerakhir = debiturInfo?.KolektabilitasTerburuk.ToString() ?? "0",
                BulanKualitasKreditTerakhir = DateTime.Now.ToString("yyyy-MM"),

                // Write-off analysis
                WoContract = AnalyzeWriteOffContracts(reportData?.Fasilitas),
                WoAgunan = AnalyzeWriteOffCollateral(reportData?.Fasilitas),

                // Overdue analysis
                WorstOvd = CalculateWorstOverdue(reportData?.Fasilitas),
                TotBakidebet3160dpd = CalculateOutstandingInDpdRange(reportData?.Fasilitas, 31, 60),
                NoKol1Active = CountActiveKol1Facilities(reportData?.Fasilitas),
                Nom0312mthAll = CalculateNominal0312Months(reportData?.Fasilitas),

                // Processing status
                Status = "SUCCESS",
                ResponseStatus = reportResponse.Status?.ToUpper() ?? "SUCCESS",
                ResponseMessage = searchData == null ? (searchResponse.Message ?? "Processing completed successfully") : (reportResponse.Message ?? "Processing completed successfully"),
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

    public async Task<IndividualData> AggregateIndividualDataWithJsonAsync(
        IndividualRequest request,
        PefindoSearchResponse searchResponse,
        JsonNode? reportResponseJson,
        ProcessingContext processingContext)
    {
        try
        {
            _logger.LogDebug("Aggregating individual data with JSON for app_no: {AppNo}", request.CfLosAppNo);

            var searchData = searchResponse.Data.FirstOrDefault();

            // Extract data menggunakan JsonNode
            var reportData = reportResponseJson?["report"];
            var debiturInfo = reportData?["debitur"];
            var fasilitasArray = reportData?["fasilitas"]?.AsArray();
            var scoringArray = reportResponseJson?["scoring"]?.AsArray();

            // Map data to individual response format
            var individualData = new IndividualData
            {
                AppNo = request.CfLosAppNo,
                IdNumber = request.IdNumber,
                CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                // Search-related data
                SearchId = searchResponse.InquiryId.ToString(),
                PefindoId = searchData?.IdPefindo.ToString() ?? "N/A",

                // Report-related data from Pefindo using JsonNode
                Score = GetScoreFromJsonReport(scoringArray) ?? "0",
                MaxOverdue = Helper.SafeGetString(debiturInfo?["tunggakan_terburuk"]) ?? "0",
                MaxOverdueLast12Months = Helper.SafeGetString(debiturInfo?["tunggakan_terburuk_12_bln"]) ?? "0",
                TotalFacilities = Helper.SafeGetString(debiturInfo?["jml_fasilitas"]) ?? "0",
                Plafon = Helper.SafeGetString(debiturInfo?["jml_plafon"]) ?? "0",
                TotalAngsuranAktif = Helper.SafeGetString(debiturInfo?["jml_aktif_fasilitas"]) ?? "0",
                BakiDebetNonAgunan = Helper.SafeGetString(debiturInfo?["jml_saldo_terutang"]) ?? "0",
                FasilitasAktif = Helper.SafeGetString(debiturInfo?["jml_aktif_fasilitas"]) ?? "0",

                // Credit quality analysis using JsonNode
                KualitasKreditTerburuk = Helper.SafeGetString(debiturInfo?["kolektabilitas_terburuk"]) ?? "0",
                BulanKualitasTerburuk = GetWorstCreditQualityMonthFromJson(fasilitasArray),
                KualitasKreditTerakhir = Helper.SafeGetString(debiturInfo?["kolektabilitas_terburuk"]) ?? "0",
                BulanKualitasKreditTerakhir = DateTime.Now.ToString("yyyy-MM"),

                // Write-off analysis using JsonNode
                WoContract = AnalyzeWriteOffContractsFromJson(fasilitasArray),
                WoAgunan = AnalyzeWriteOffCollateralFromJson(fasilitasArray),

                // Overdue analysis using JsonNode
                WorstOvd = CalculateWorstOverdueFromJson(fasilitasArray),
                TotBakidebet3160dpd = CalculateOutstandingInDpdRangeFromJson(fasilitasArray, 31, 60),
                NoKol1Active = CountActiveKol1FacilitiesFromJson(fasilitasArray),
                Nom0312mthAll = CalculateNominal0312MonthsFromJson(fasilitasArray),

                // Processing status
                Status = "SUCCESS",
                ResponseStatus = Helper.SafeGetString(reportResponseJson?["status"])?.ToUpper() ?? "SUCCESS",
                ResponseMessage = Helper.SafeGetString(reportResponseJson?["message"]) ?? "Processing completed successfully",
                Message = "Individual credit assessment completed"
            };

            _logger.LogInformation("Data aggregation with JSON completed for app_no: {AppNo}", request.CfLosAppNo);
            return await Task.FromResult(individualData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating individual data with JSON for app_no: {AppNo}", request.CfLosAppNo);
            throw;
        }
    }

    public async Task<JsonNode?> AggregateIndividualWithReturnJsonAsync(
        IndividualRequest request,
        PefindoSearchResponse searchResponse,
        JsonNode? reportResponseJson,
        ProcessingContext processingContext)
    {
        try
        {
            _logger.LogDebug("Aggregating individual data with JSON for app_no: {AppNo}", request.CfLosAppNo);

            var searchData = searchResponse.Data.FirstOrDefault();

            // Extract data menggunakan JsonNode
            var reportData = reportResponseJson?["report"];
            var debiturInfo = reportData?["debitur"];
            var fasilitasArray = reportData?["fasilitas"]?.AsArray();
            var scoringArray = reportResponseJson?["scoring"]?.AsArray();

            // Map data to individual response format
            var individualData = new IndividualData
            {
                AppNo = request.CfLosAppNo,
                IdNumber = request.IdNumber,
                CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                // Search-related data
                SearchId = searchResponse.InquiryId.ToString(),
                PefindoId = searchData?.IdPefindo.ToString() ?? "N/A",

                // Report-related data from Pefindo using JsonNode
                Score = GetScoreFromJsonReport(scoringArray) ?? "0",
                MaxOverdue = Helper.SafeGetString(debiturInfo?["tunggakan_terburuk"]) ?? "0",
                MaxOverdueLast12Months = Helper.SafeGetString(debiturInfo?["tunggakan_terburuk_12_bln"]) ?? "0",
                TotalFacilities = Helper.SafeGetString(debiturInfo?["jml_fasilitas"]) ?? "0",
                Plafon = Helper.SafeGetString(debiturInfo?["jml_plafon"]) ?? "0",
                TotalAngsuranAktif = Helper.SafeGetString(debiturInfo?["jml_aktif_fasilitas"]) ?? "0",
                BakiDebetNonAgunan = Helper.SafeGetString(debiturInfo?["jml_saldo_terutang"]) ?? "0",
                FasilitasAktif = Helper.SafeGetString(debiturInfo?["jml_aktif_fasilitas"]) ?? "0",

                // Credit quality analysis using JsonNode
                KualitasKreditTerburuk = Helper.SafeGetString(debiturInfo?["kolektabilitas_terburuk"]) ?? "0",
                BulanKualitasTerburuk = GetWorstCreditQualityMonthFromJson(fasilitasArray),
                KualitasKreditTerakhir = Helper.SafeGetString(debiturInfo?["kolektabilitas_terakhir"]) ?? "0",
                BulanKualitasKreditTerakhir = DateTime.Now.ToString("yyyy-MM"),

                // Write-off analysis using JsonNode
                WoContract = AnalyzeWriteOffContractsFromJson(fasilitasArray),
                WoAgunan = AnalyzeWriteOffCollateralFromJson(fasilitasArray),

                // Overdue analysis using JsonNode
                WorstOvd = CalculateWorstOverdueFromJson(fasilitasArray),
                TotBakidebet3160dpd = CalculateOutstandingInDpdRangeFromJson(fasilitasArray, 31, 60),
                NoKol1Active = CountActiveKol1FacilitiesFromJson(fasilitasArray),
                Nom0312mthAll = CalculateNominal0312MonthsFromJson(fasilitasArray),

                // Processing status
                Status = "SUCCESS",
                ResponseStatus = Helper.SafeGetString(reportResponseJson?["status"])?.ToUpper() ?? "SUCCESS",
                ResponseMessage = Helper.SafeGetString(reportResponseJson?["message"]) ?? "Processing completed successfully",
                Message = "Individual credit assessment completed"
            };

            _logger.LogInformation("Data aggregation with JSON completed for app_no: {AppNo}", request.CfLosAppNo);

            var result = JsonSerializer.SerializeToNode(individualData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating individual data with JSON for app_no: {AppNo}", request.CfLosAppNo);
            throw;
        }
    }

    private string? GetScoreFromReport(PefindoGetReportResponse reportResponse)
    {
        // Try to get score from scoring array first
        var latestScoring = reportResponse.Scoring?.OrderByDescending(s => s.Period).FirstOrDefault();
        if (latestScoring != null)
        {
            return latestScoring.Score.ToString();
        }

        // Fallback to report data if available
        return "0";
    }

    private string? GetScoreFromJsonReport(JsonArray? scoringArray)
    {
        if (scoringArray == null || !scoringArray.Any())
            return "0";

        // Get latest scoring by period
        var latestScoring = scoringArray
            .Where(s => s?["period"] != null)
            .OrderByDescending(s => Helper.SafeGetString(s?["period"]))
            .FirstOrDefault();

        return Helper.SafeGetString(latestScoring?["score"]) ?? "0";
    }

    private string AnalyzeWriteOffContractsFromJson(JsonArray? fasilitasArray)
    {
        if (fasilitasArray == null)
            return "0";

        var writeOffCount = fasilitasArray.Count(f => Helper.SafeGetInt(f?["kolektabilitas_terburuk"]) == 5);
        return writeOffCount.ToString();
    }

    private string AnalyzeWriteOffCollateralFromJson(JsonArray? fasilitasArray)
    {
        if (fasilitasArray == null)
            return "0";

        var writeOffCollateralCount = fasilitasArray.Count(f =>
            Helper.SafeGetBool(f?["has_collateral"]) && Helper.SafeGetInt(f?["kolektabilitas_terburuk"]) == 5);

        return writeOffCollateralCount.ToString();
    }

    private string AnalyzeWorstCreditQuality(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";

        // Map credit quality to numeric values (5=worst, 1=best)
        var worstQuality = facilities
            .Select(f => MapCreditQualityToNumber(f.KolektabilitasTerburuk))
            .DefaultIfEmpty(0)
            .Max();

        return worstQuality.ToString();
    }

    private string AnalyzeWorstCreditQualityMonth(List<PefindoFasilitas>? facilities)
    {
        // This would typically come from historical data in the report
        // For now, return current month if there are any facilities with bad quality
        if (facilities?.Any(f => MapCreditQualityToNumber(f.KolektabilitasTerburuk) >= 3) == true)
        {
            return DateTime.Now.ToString("yyyy-MM");
        }
        return string.Empty;
    }

    private string AnalyzeLatestCreditQuality(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";

        // Get the most recent facility's credit quality
        var latestFacility = facilities.OrderByDescending(f => f.TanggalAwalKreditAtauPembiayaan ?? DateTime.MinValue).FirstOrDefault();
        return latestFacility != null ? MapCreditQualityToNumber(latestFacility.KolektabilitasTerburuk).ToString() : "0";
    }

    private string AnalyzeLatestCreditQualityMonth(List<PefindoFasilitas>? facilities)
    {
        // Similar to worst quality month but for latest
        return DateTime.Now.ToString("yyyy-MM");
    }

    private string AnalyzeWriteOffContracts(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null)
            return "0";

        var writeOffCount = facilities.Count(f =>
            f.KolektabilitasTerburuk == 5);  // Kolektabilitas 5 = MACET

        return writeOffCount.ToString();
    }

    private string AnalyzeWriteOffCollateral(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null)
            return "0";

        // Count facilities with collateral that are written off
        var writeOffCollateralCount = facilities.Count(f =>
            f.HasCollateral && f.KolektabilitasTerburuk == 5);  // Kolektabilitas 5 = MACET

        return writeOffCollateralCount.ToString();
    }

    private string CalculateWorstOverdue(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return "0";

        var worstDpd = facilities
            .Select(f => (int)f.TunggakanTerburuk)
            .DefaultIfEmpty(0)
            .Max();
        return worstDpd.ToString();
    }

    private string CalculateOutstandingInDpdRange(List<PefindoFasilitas>? facilities, int minDpd, int maxDpd)
    {
        if (facilities == null)
            return "0";

        var totalOutstanding = facilities
            .Where(f => (int)f.TunggakanTerburuk >= minDpd && (int)f.TunggakanTerburuk <= maxDpd)
            .Sum(f => f.SaldoTerutang);

        return totalOutstanding.ToString();
    }

    private string CountActiveKol1Facilities(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null)
            return "0";

        var kol1Count = facilities.Count(f =>
            f.KolektabilitasTerburuk == 1);  // Kolektabilitas 1 = LANCAR/KOL 1

        return kol1Count.ToString();
    }

    private string CalculateNominal0312Months(List<PefindoFasilitas>? facilities)
    {
        // Calculate nominal for 0-3 and 12 months range
        // Implementation depends on historical data structure
        return "0";
    }

    private int MapCreditQualityToNumber(short? kualitasKredit)
    {
        return kualitasKredit switch
        {
            1 => 1,  // LANCAR/KOL 1
            2 => 2,  // DPK/KOL 2
            3 => 3,  // KURANG LANCAR/KOL 3
            4 => 4,  // DIRAGUKAN/KOL 4
            5 => 5,  // MACET/KOL 5
            null => 0,
            _ => kualitasKredit.Value
        };
    }

    private string GetWorstCreditQualityMonth(List<PefindoFasilitas>? facilities)
    {
        if (facilities == null || !facilities.Any())
            return string.Empty;

        // Find the facility with worst credit quality and return its month
        var worstFacility = facilities
            .OrderByDescending(f => MapCreditQualityToNumber(f.KolektabilitasTerburuk))
            .FirstOrDefault();

        if (worstFacility != null)
        {
            return worstFacility.TahunBulanData?.ToString("yyyy-MM") ?? DateTime.Now.ToString("yyyy-MM");
        }

        return DateTime.Now.ToString("yyyy-MM");
    }

    private string GetWorstCreditQualityMonthFromJson(JsonArray? fasilitasArray)
    {
        if (fasilitasArray == null || !fasilitasArray.Any())
            return string.Empty;

        var worstFacility = fasilitasArray
            .Where(f => f?["kolektabilitas_terburuk"] != null)
            .OrderByDescending(f => Helper.SafeGetInt(f?["kolektabilitas_terburuk"]))
            .FirstOrDefault();

        if (worstFacility != null)
        {
            var tahunBulanData = Helper.SafeGetString(worstFacility?["tahun_bulan_data"]);
            if (!string.IsNullOrEmpty(tahunBulanData) && DateTime.TryParse(tahunBulanData, out var date))
            {
                return date.ToString("yyyy-MM");
            }
        }

        return DateTime.Now.ToString("yyyy-MM");
    }

    private string CalculateWorstOverdueFromJson(JsonArray? fasilitasArray)
    {
        if (fasilitasArray == null || !fasilitasArray.Any())
            return "0";

        var worstDpd = fasilitasArray
            .Select(f => Helper.SafeGetInt(f?["tunggakan_terburuk"]))
            .DefaultIfEmpty(0)
            .Max();

        return worstDpd.ToString();
    }

    private string CalculateOutstandingInDpdRangeFromJson(JsonArray? fasilitasArray, int minDpd, int maxDpd)
    {
        if (fasilitasArray == null)
            return "0";

        var totalOutstanding = fasilitasArray
            .Where(f =>
            {
                var dpd = Helper.SafeGetInt(f?["tunggakan_terburuk"]);
                return dpd >= minDpd && dpd <= maxDpd;
            })
            .Sum(f => Helper.SafeGetDecimal(f?["saldo_terutang"]));

        return totalOutstanding.ToString();
    }

    private string CountActiveKol1FacilitiesFromJson(JsonArray? fasilitasArray)
    {
        if (fasilitasArray == null)
            return "0";

        var kol1Count = fasilitasArray.Count(f => Helper.SafeGetInt(f?["kolektabilitas_terburuk"]) == 1);
        return kol1Count.ToString();
    }

    private string CalculateNominal0312MonthsFromJson(JsonArray? fasilitasArray)
    {
        // Implement based on business logic requirements
        return "0";
    }
}
