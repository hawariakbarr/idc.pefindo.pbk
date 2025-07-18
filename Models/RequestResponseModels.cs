using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace idc.pefindo.pbk.Models;

/// <summary>
/// Main request model for individual credit assessment
/// </summary>
public class IndividualRequest
{
    [JsonPropertyName("type_data")]
    public string TypeData { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dob")]
    public string DateOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("id_number")]
    public string IdNumber { get; set; } = string.Empty;

    [JsonPropertyName("cf_los_app_no")]
    public string CfLosAppNo { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = "perorangan";

    [JsonPropertyName("mother_name")]
    public string MotherName { get; set; } = string.Empty;

    [JsonPropertyName("tolerance")]
    public int Tolerance { get; set; }

    [JsonPropertyName("facility_limit")]
    public decimal FacilityLimit { get; set; }

    [JsonPropertyName("similarity_check_version")]
    public int SimilarityCheckVersion { get; set; }

    [JsonPropertyName("table_version")]
    public int TableVersion { get; set; }
}

/// <summary>
/// Response model for individual credit assessment
/// </summary>
public class IndividualResponse
{
    [JsonPropertyName("data")]
    public IndividualData Data { get; set; } = new();
}

/// <summary>
/// Detailed response data for individual assessment
/// </summary>
public class IndividualData
{
    [JsonPropertyName("max_overdue")]
    public string MaxOverdue { get; set; } = string.Empty;

    [JsonPropertyName("max_overdue_last12months")]
    public string MaxOverdueLast12Months { get; set; } = string.Empty;

    [JsonPropertyName("pefindo_id")]
    public string PefindoId { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public string Score { get; set; } = string.Empty;

    [JsonPropertyName("search_id")]
    public string SearchId { get; set; } = string.Empty;

    [JsonPropertyName("total_angsuran_aktif")]
    public string TotalAngsuranAktif { get; set; } = string.Empty;

    [JsonPropertyName("wo_contract")]
    public string WoContract { get; set; } = string.Empty;

    [JsonPropertyName("wo_agunan")]
    public string WoAgunan { get; set; } = string.Empty;

    [JsonPropertyName("baki_debet_non_agunan")]
    public string BakiDebetNonAgunan { get; set; } = string.Empty;

    [JsonPropertyName("plafon")]
    public string Plafon { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_aktif")]
    public string FasilitasAktif { get; set; } = string.Empty;

    [JsonPropertyName("total_facilities")]
    public string TotalFacilities { get; set; } = string.Empty;

    [JsonPropertyName("kualitas_kredit_terburuk")]
    public string KualitasKreditTerburuk { get; set; } = string.Empty;

    [JsonPropertyName("bulan_kualitas_terburuk")]
    public string BulanKualitasTerburuk { get; set; } = string.Empty;

    [JsonPropertyName("kualitas_kredit_terakhir")]
    public string KualitasKreditTerakhir { get; set; } = string.Empty;

    [JsonPropertyName("bulan_kualitas_kredit_terakhir")]
    public string BulanKualitasKreditTerakhir { get; set; } = string.Empty;

    [JsonPropertyName("worst_ovd")]
    public string WorstOvd { get; set; } = string.Empty;

    [JsonPropertyName("tot_baki_debet_31_60_dpd")]
    public string TotBakidebet3160dpd { get; set; } = string.Empty;

    [JsonPropertyName("no_kol1_active")]
    public string NoKol1Active { get; set; } = string.Empty;

    [JsonPropertyName("nom_03_12_mth_all")]
    public string Nom0312mthAll { get; set; } = string.Empty;



    [JsonPropertyName("created_date")]
    public string CreatedDate { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("response_status")]
    public string ResponseStatus { get; set; } = string.Empty;

    [JsonPropertyName("response_message")]
    public string ResponseMessage { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("app_no")]
    public string AppNo { get; set; } = string.Empty;

    [JsonPropertyName("id_number")]
    public string IdNumber { get; set; } = string.Empty;
}

// Additional models for Pefindo API integration would go here...
// (Include all the Pefindo models from the previous artifacts)
