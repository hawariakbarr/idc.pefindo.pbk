using System.Text.Json.Serialization;

namespace idc.pefindo.pbk.Models;

#region Authentication Models

/// <summary>
/// Updated Pefindo token response model with data object
/// </summary>
public class PefindoTokenResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PefindoTokenData? Data { get; set; }
}

/// <summary>
/// Token data object from new API response format
/// </summary>
public class PefindoTokenData
{
    [JsonPropertyName("valid_date")]
    public string ValidDate { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

}

/// <summary>
/// In-memory token cache entry
/// </summary>
public class TokenCacheEntry
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime CachedAt { get; set; }
    public string ValidDateOriginal { get; set; } = string.Empty;

    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;

    public TimeSpan TimeUntilExpiry => ExpiryDate - DateTime.UtcNow;
}

#endregion

#region Search Models

/// <summary>
/// Pefindo search request model
/// </summary>
public class PefindoSearchRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "PERSONAL";
    
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; } = 1;
    
    [JsonPropertyName("inquiry_reason")]
    public int InquiryReason { get; set; } = 1;
    
    [JsonPropertyName("reference_code")]
    public string ReferenceCode { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public List<PefindoSearchParam> Params { get; set; } = new();
}

/// <summary>
/// Search parameter for individual debtor
/// </summary>
public class PefindoSearchParam
{
    [JsonPropertyName("id_type")]
    public string IdType { get; set; } = "KTP";
    
    [JsonPropertyName("id_no")]
    public string IdNo { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("date_of_birth")]
    public string DateOfBirth { get; set; } = string.Empty;
    
    [JsonPropertyName("report_date")]
    public string? ReportDate { get; set; }
}

/// <summary>
/// Pefindo search response model
/// </summary>
public class PefindoSearchResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("inquiry_id")]
    public int InquiryId { get; set; }
    
    [JsonPropertyName("data")]
    public List<PefindoSearchData> Data { get; set; } = new();
    
    [JsonPropertyName("response_status")]
    public string ResponseStatus { get; set; } = string.Empty;
}

/// <summary>
/// Individual search result data
/// </summary>
public class PefindoSearchData
{
    [JsonPropertyName("similarity_score")]
    public decimal SimilarityScore { get; set; }
    
    [JsonPropertyName("id_pefindo")]
    public long IdPefindo { get; set; }
    
    [JsonPropertyName("id_type")]
    public string IdType { get; set; } = string.Empty;
    
    [JsonPropertyName("id_no")]
    public string IdNo { get; set; } = string.Empty;
    
    [JsonPropertyName("id_tipe_debitur")]
    public string IdTipeDebitur { get; set; } = string.Empty;
    
    [JsonPropertyName("nama_debitur")]
    public string NamaDebitur { get; set; } = string.Empty;
    
    [JsonPropertyName("tanggal_lahir")]
    public string TanggalLahir { get; set; } = string.Empty;
    
    [JsonPropertyName("nama_gadis_ibu_kandung")]
    public string NamaGadisIbuKandung { get; set; } = string.Empty;
    
    [JsonPropertyName("alamat")]
    public string Alamat { get; set; } = string.Empty;
    
    [JsonPropertyName("npwp")]
    public string Npwp { get; set; } = string.Empty;
    
    [JsonPropertyName("response_status")]
    public string ResponseStatus { get; set; } = string.Empty;
}

#endregion

#region Report Generation Models

/// <summary>
/// Pefindo report generation request
/// </summary>
public class PefindoReportRequest
{
    [JsonPropertyName("inquiry_id")]
    public int InquiryId { get; set; }
    
    [JsonPropertyName("ids")]
    public List<PefindoReportIdParam> Ids { get; set; } = new();
    
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("generate_pdf")]
    public string GeneratePdf { get; set; } = "1";
    
    [JsonPropertyName("language")]
    public string Language { get; set; } = "01";
}

/// <summary>
/// Report ID parameter
/// </summary>
public class PefindoReportIdParam
{
    [JsonPropertyName("id_type")]
    public string IdType { get; set; } = string.Empty;
    
    [JsonPropertyName("id_no")]
    public string IdNo { get; set; } = string.Empty;
    
    [JsonPropertyName("id_pefindo")]
    public long IdPefindo { get; set; }
}

/// <summary>
/// Pefindo report generation response
/// </summary>
public class PefindoReportResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Pefindo get report response
/// </summary>
public class PefindoGetReportResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("report")]
    public PefindoReportData? Report { get; set; }
}

/// <summary>
/// Comprehensive Pefindo report data structure
/// </summary>
public class PefindoReportData
{
    [JsonPropertyName("header")]
    public PefindoReportHeader Header { get; set; } = new();
    
    [JsonPropertyName("debitur")]
    public PefindoDebiturInfo Debitur { get; set; } = new();
    
    [JsonPropertyName("facilities")]
    public List<PefindoFacility> Facilities { get; set; } = new();
    
    [JsonPropertyName("fasilitas")]
    public List<PefindoFasilitas> Fasilitas { get; set; } = new();
    
    [JsonPropertyName("collaterals")]
    public List<PefindoCollateral> Collaterals { get; set; } = new();
    
    [JsonPropertyName("score_info")]
    public PefindoScoreInfo? ScoreInfo { get; set; }
}

/// <summary>
/// Report header information
/// </summary>
public class PefindoReportHeader
{
    [JsonPropertyName("id_report")]
    public string IdReport { get; set; } = string.Empty;
    
    [JsonPropertyName("idscore_id")]
    public string IdscoreId { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("tgl_permintaan")]
    public DateTime TglPermintaan { get; set; }
    
    [JsonPropertyName("no_referensi_dokumen")]
    public string NoReferensiDokumen { get; set; } = string.Empty;
    
    [JsonPropertyName("ktp")]
    public string Ktp { get; set; } = string.Empty;
    
    [JsonPropertyName("npwp")]
    public string Npwp { get; set; } = string.Empty;
    
    [JsonPropertyName("nama_debitur")]
    public string NamaDebitur { get; set; } = string.Empty;
    
    [JsonPropertyName("tanggal_lahir")]
    public DateTime TanggalLahir { get; set; }
    
    [JsonPropertyName("tempat_lahir")]
    public string TempatLahir { get; set; } = string.Empty;
}

/// <summary>
/// Debtor information from report
/// </summary>
public class PefindoDebiturInfo
{
    [JsonPropertyName("id_pefindo")]
    public long IdPefindo { get; set; }
    
    [JsonPropertyName("nama_debitur")]
    public string NamaDebitur { get; set; } = string.Empty;
    
    [JsonPropertyName("alamat_debitur")]
    public string AlamatDebitur { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("tanggal_lahir")]
    public string TanggalLahir { get; set; } = string.Empty;
    
    [JsonPropertyName("nama_gadis_ibu_kandung")]
    public string NamaGadisIbuKandung { get; set; } = string.Empty;
    
    [JsonPropertyName("jml_fasilitas")]
    public int JmlFasilitas { get; set; }
    
    [JsonPropertyName("jml_tunggakan")]
    public int JmlTunggakan { get; set; }
    
    [JsonPropertyName("jml_plafon")]
    public decimal JmlPlafon { get; set; }
    
    [JsonPropertyName("total_angsuran_aktif")]
    public decimal TotalAngsuranAktif { get; set; }
    
    [JsonPropertyName("baki_debet_non_agunan")]
    public decimal BakiDebetNonAgunan { get; set; }
    
    [JsonPropertyName("max_curr_dpd")]
    public int MaxCurrDpd { get; set; }
    
    [JsonPropertyName("max_overdue_last12months")]
    public int MaxOverdueLast12Months { get; set; }

    [JsonPropertyName("wo_contract")]
    public int WoContract { get; set; } 

    [JsonPropertyName("wo_agunan")]
    public int WoAgunan { get; set; }

    [JsonPropertyName("kualitas_kredit_terburuk")]
    public string KualitasKreditTerburuk { get; set; } = string.Empty;

    [JsonPropertyName("bulan_kualitas_terburuk")]
    public string BulanKualitasTerburuk { get; set; } = string.Empty;

    [JsonPropertyName("baki_debet_kualitas_terburuk")]
    public decimal BakiDebetKualitasTerburuk { get; set; }

    [JsonPropertyName("kualitas_kredit_terakhir")]
    public string KualitasKreditTerakhir { get; set; } = string.Empty;

    [JsonPropertyName("bulan_kualitas_kredit_terakhir")]
    public string BulanKualitasKreditTerakhir { get; set; } = string.Empty;

    [JsonPropertyName("total_baki_debet")]
    public decimal TotalBakiDebet { get; set; }

    [JsonPropertyName("total_nilai_agunan")]
    public decimal TotalNilaiAgunan { get; set; }

}

/// <summary>
/// Credit facility information
/// </summary>
public class PefindoFacility
{
    [JsonPropertyName("facility_id")]
    public string FacilityId { get; set; } = string.Empty;
    
    [JsonPropertyName("facility_type")]
    public string FacilityType { get; set; } = string.Empty;
    
    [JsonPropertyName("plafon")]
    public decimal Plafon { get; set; }
    
    [JsonPropertyName("baki_debet")]
    public decimal BakiDebet { get; set; }
    
    [JsonPropertyName("kualitas_kredit")]
    public string KualitasKredit { get; set; } = string.Empty;
    
    [JsonPropertyName("current_dpd")]
    public int CurrentDpd { get; set; }
}

/// <summary>
/// Indonesian facility information (Fasilitas)
/// </summary>
public class PefindoFasilitas
{
    [JsonPropertyName("id_fasilitas")]
    public long IdFasilitas { get; set; }
    
    [JsonPropertyName("jenis_fasilitas")]
    public string JenisFasilitas { get; set; } = string.Empty;
    
    [JsonPropertyName("nama_bank")]
    public string NamaBank { get; set; } = string.Empty;
    
    [JsonPropertyName("plafon")]
    public decimal Plafon { get; set; }
    
    [JsonPropertyName("baki_debet")]
    public decimal BakiDebet { get; set; }
    
    [JsonPropertyName("dpd_current")]
    public int DpdCurrent { get; set; }
    
    [JsonPropertyName("kualitas_kredit")]
    public string KualitasKredit { get; set; } = string.Empty;
    
    [JsonPropertyName("tanggal_mulai")]
    public DateTime TanggalMulai { get; set; }
    
    [JsonPropertyName("tanggal_berakhir")]
    public DateTime TanggalBerakhir { get; set; }
}

/// <summary>
/// Collateral information
/// </summary>
public class PefindoCollateral
{
    [JsonPropertyName("collateral_type")]
    public string CollateralType { get; set; } = string.Empty;
    
    [JsonPropertyName("collateral_value")]
    public decimal CollateralValue { get; set; }
}

/// <summary>
/// Credit score information
/// </summary>
public class PefindoScoreInfo
{
    [JsonPropertyName("score")]
    public string Score { get; set; } = string.Empty;
    
    [JsonPropertyName("risk_grade")]
    public string RiskGrade { get; set; } = string.Empty;
    
    [JsonPropertyName("risk_desc")]
    public string RiskDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("score_date")]
    public string ScoreDate { get; set; } = string.Empty;
}

#endregion

#region Error Models

/// <summary>
/// Standard Pefindo error response
/// </summary>
public class PefindoErrorResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

#endregion
