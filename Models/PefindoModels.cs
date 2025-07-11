using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;

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
    public string IdPefindo { get; set; } = string.Empty;

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

    [JsonPropertyName("npwp")]
    public string Npwp { get; set; } = string.Empty;

    [JsonPropertyName("tgl_pendirian")]
    public string? TglPendirian { get; set; }

    [JsonPropertyName("tempat_pendirian")]
    public string? TempatPendirian { get; set; }

    [JsonPropertyName("alamat")]
    public string Alamat { get; set; } = string.Empty;

    [JsonPropertyName("nama_gadis_ibu_kandung")]
    public string NamaGadisIbuKandung { get; set; } = string.Empty;

    [JsonPropertyName("tempat_lahir")]
    public string TempatLahir { get; set; } = string.Empty;

    [JsonPropertyName("jenis_kelamin")]
    public int JenisKelamin { get; set; }

    [JsonPropertyName("kode_kota")]
    public string KodeKota { get; set; } = string.Empty;

    [JsonPropertyName("kode_pos")]
    public string KodePos { get; set; } = string.Empty;

    [JsonPropertyName("is_current")]
    public int IsCurrent { get; set; }

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
    public string IdPefindo { get; set; } = string.Empty;
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

    [JsonPropertyName("scoring")]
    public List<PefindoScoring> Scoring { get; set; } = new();
}

/// <summary>
/// Custom parameter for Pefindo header
/// </summary>
public class PefindoCustomParam
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
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

    [JsonPropertyName("pengurus")]
    public List<PefindoPengurus> Pengurus { get; set; } = new();

    [JsonPropertyName("fasilitas")]
    public List<PefindoFasilitas> Fasilitas { get; set; } = new();

    [JsonPropertyName("otherdata")]
    public PefindoOtherData? OtherData { get; set; }

    [JsonPropertyName("permintaan_data")]
    public List<PefindoPermintaanData> PermintaanData { get; set; } = new();

    [JsonPropertyName("summary_permintaan_data")]
    public List<PefindoSummaryPermintaanData> SummaryPermintaanData { get; set; } = new();

    [JsonPropertyName("summary_riwayat_debitur")]
    public List<PefindoSummaryRiwayatDebitur> SummaryRiwayatDebitur { get; set; } = new();

    [JsonPropertyName("riwayat_identitas_debitur")]
    public List<PefindoRiwayatIdentitasDebitur> RiwayatIdentitasDebitur { get; set; } = new();
}

/// <summary>
/// Report header information
/// </summary>
public class PefindoReportHeader
{
    [JsonPropertyName("ktp")]
    public string Ktp { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("npwp")]
    public string Npwp { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("id_report")]
    public string IdReport { get; set; } = string.Empty;

    [JsonPropertyName("id_pefindo")]
    public string IdPefindo { get; set; } = string.Empty;

    [JsonPropertyName("idscore_id")]
    public string IdscoreId { get; set; } = string.Empty;

    [JsonPropertyName("keterangan")]
    public string Keterangan { get; set; } = string.Empty;

    [JsonPropertyName("nama_debitur")]
    public string NamaDebitur { get; set; } = string.Empty;

    [JsonPropertyName("tempat_lahir")]
    public string TempatLahir { get; set; } = string.Empty;

    [JsonPropertyName("tanggal_lahir")]
    public DateTime? TanggalLahir { get; set; }

    [JsonPropertyName("tgl_permintaan")]
    public DateTime TglPermintaan { get; set; }

    [JsonPropertyName("id_tipe_debitur")]
    public short IdTipeDebitur { get; set; }

    [JsonPropertyName("tempat_pendirian")]
    public string? TempatPendirian { get; set; }

    [JsonPropertyName("tanggal_pendirian")]
    public DateTime? TanggalPendirian { get; set; }

    [JsonPropertyName("id_tujuan_permintaan")]
    public string IdTujuanPermintaan { get; set; } = string.Empty;

    [JsonPropertyName("no_referensi_dokumen")]
    public string NoReferensiDokumen { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_lain_tidak_tampil")]
    public string FasilitasLainTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_kredit_tidak_tampil")]
    public string FasilitasKreditTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_garansi_tidak_tampil")]
    public string FasilitasGaransiTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_joint_account_tidak_tampil")]
    public string FasilitasJointAccountTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_irrevocable_lc_tidak_tampil")]
    public string FasilitasIrrevocableLcTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("fasilitas_surat_berharga_tidak_tampil")]
    public string FasilitasSuratBerhargaTidakTampil { get; set; } = string.Empty;

    [JsonPropertyName("custom_param")]
    public List<PefindoCustomParam> CustomParam { get; set; } = new();
}

/// <summary>
/// Debtor information from report
/// </summary>
public class PefindoDebiturInfo
{
    [JsonPropertyName("npwp")]
    public string Npwp { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("telepon")]
    public string Telepon { get; set; } = string.Empty;

    [JsonPropertyName("go_public")]
    public short? GoPublic { get; set; }

    [JsonPropertyName("id_lokasi")]
    public int IdLokasi { get; set; }

    [JsonPropertyName("id_negara")]
    public short IdNegara { get; set; }

    [JsonPropertyName("kecamatan")]
    public string Kecamatan { get; set; } = string.Empty;

    [JsonPropertyName("kelurahan")]
    public string Kelurahan { get; set; } = string.Empty;

    [JsonPropertyName("jml_plafon")]
    public decimal JmlPlafon { get; set; }

    [JsonPropertyName("nama_alias")]
    public string? NamaAlias { get; set; }

    [JsonPropertyName("nama_group")]
    public string? NamaGroup { get; set; }

    [JsonPropertyName("jml_pelapor")]
    public short JmlPelapor { get; set; }

    [JsonPropertyName("jml_penjamin")]
    public short JmlPenjamin { get; set; }

    [JsonPropertyName("tempat_lahir")]
    public string TempatLahir { get; set; } = string.Empty;

    [JsonPropertyName("jml_fasilitas")]
    public short JmlFasilitas { get; set; }

    [JsonPropertyName("jml_tunggakan")]
    public decimal JmlTunggakan { get; set; }

    [JsonPropertyName("mf_jml_plafon")]
    public decimal MfJmlPlafon { get; set; }

    [JsonPropertyName("tanggal_lahir")]
    public DateTime TanggalLahir { get; set; }

    [JsonPropertyName("alamat_debitur")]
    public string AlamatDebitur { get; set; } = string.Empty;

    [JsonPropertyName("mob_newest_fac")]
    public int MobNewestFac { get; set; }

    [JsonPropertyName("p2p_jml_plafon")]
    public decimal P2pJmlPlafon { get; set; }

    [JsonPropertyName("tempat_bekerja")]
    public string? TempatBekerja { get; set; }

    [JsonPropertyName("bank_jml_plafon")]
    public decimal BankJmlPlafon { get; set; }

    [JsonPropertyName("id_status_gelar")]
    public short IdStatusGelar { get; set; }

    [JsonPropertyName("id_tipe_debitur")]
    public short IdTipeDebitur { get; set; }

    [JsonPropertyName("nbfi_jml_plafon")]
    public decimal NbfiJmlPlafon { get; set; }

    [JsonPropertyName("nomor_identitas")]
    public string NomorIdentitas { get; set; } = string.Empty;

    [JsonPropertyName("pawn_jml_plafon")]
    public decimal PawnJmlPlafon { get; set; }

    [JsonPropertyName("telepon_seluler")]
    public string TeleponSeluler { get; set; } = string.Empty;

    [JsonPropertyName("coorp_jml_plafon")]
    public decimal CoorporateJmlPlafon { get; set; }

    [JsonPropertyName("id_jenis_kelamin")]
    public int IdJenisKelamin { get; set; }

    [JsonPropertyName("id_kantor_cabang")]
    public long IdKantorCabang { get; set; }

    [JsonPropertyName("jml_fasilitas_cc")]
    public short JmlFasilitasCc { get; set; }

    [JsonPropertyName("jml_fasilitas_pl")]
    public short JmlFasilitasPl { get; set; }

    [JsonPropertyName("jml_nilai_agunan")]
    public decimal JmlNilaiAgunan { get; set; }

    [JsonPropertyName("mf_jml_fasilitas")]
    public short MfJmlFasilitas { get; set; }

    [JsonPropertyName("mf_jml_tunggakan")]
    public decimal MfJmlTunggakan { get; set; }

    [JsonPropertyName("n_good_after_bad")]
    public int NGoodAfterBad { get; set; }

    [JsonPropertyName("nama_badan_usaha")]
    public string? NamaBadanUsaha { get; set; }

    [JsonPropertyName("status_tunggakan")]
    public short StatusTunggakan { get; set; }

    [JsonPropertyName("tempat_pendirian")]
    public string? TempatPendirian { get; set; }

    [JsonPropertyName("id_kabupaten_kota")]
    public short? IdKabupatenKota { get; set; }

    [JsonPropertyName("id_sektor_ekonomi")]
    public int IdSektorEkonomi { get; set; }

    [JsonPropertyName("jml_fasilitas_kpr")]
    public short JmlFasilitasKpr { get; set; }

    [JsonPropertyName("jml_fasilitas_kta")]
    public short JmlFasilitasKta { get; set; }

    [JsonPropertyName("p2p_jml_fasilitas")]
    public short P2pJmlFasilitas { get; set; }

    [JsonPropertyName("p2p_jml_tunggakan")]
    public decimal P2pJmlTunggakan { get; set; }

    [JsonPropertyName("bank_jml_fasilitas")]
    public short BankJmlFasilitas { get; set; }

    [JsonPropertyName("bank_jml_tunggakan")]
    public decimal BankJmlTunggakan { get; set; }

    [JsonPropertyName("id_jenis_identitas")]
    public int IdJenisIdentitas { get; set; }

    [JsonPropertyName("jml_fasilitas_bnpl")]
    public short JmlFasilitasBnpl { get; set; }

    [JsonPropertyName("jml_hari_tunggakan")]
    public short JmlHariTunggakan { get; set; }

    [JsonPropertyName("jml_saldo_terutang")]
    public decimal JmlSaldoTerutang { get; set; }

    [JsonPropertyName("nbfi_jml_fasilitas")]
    public short NbfiJmlFasilitas { get; set; }

    [JsonPropertyName("nbfi_jml_tunggakan")]
    public decimal NbfiJmlTunggakan { get; set; }

    [JsonPropertyName("pawn_jml_fasilitas")]
    public short PawnJmlFasilitas { get; set; }

    [JsonPropertyName("pawn_jml_tunggakan")]
    public decimal PawnJmlTunggakan { get; set; }

    [JsonPropertyName("tunggakan_terburuk")]
    public decimal TunggakanTerburuk { get; set; }

    [JsonPropertyName("coorp_jml_fasilitas")]
    public short CoorporateJmlFasilitas { get; set; }

    [JsonPropertyName("coorp_jml_tunggakan")]
    public decimal CoorporateJmlTunggakan { get; set; }

    [JsonPropertyName("id_golongan_debitur")]
    public int IdGolonganDebitur { get; set; }

    [JsonPropertyName("jml_aktif_fasilitas")]
    public short JmlAktifFasilitas { get; set; }

    [JsonPropertyName("jml_fasilitas_12bln")]
    public short JmlFasilitas12Bln { get; set; }

    [JsonPropertyName("jml_tutup_fasilitas")]
    public short JmlTutupFasilitas { get; set; }

    [JsonPropertyName("mf_status_tunggakan")]
    public short MfStatusTunggakan { get; set; }

    [JsonPropertyName("id_jenis_badan_usaha")]
    public int? IdJenisBadanUsaha { get; set; }

    [JsonPropertyName("id_status_perkawinan")]
    public short IdStatusPerkawinan { get; set; }

    [JsonPropertyName("jml_fasilitas_kredit")]
    public short JmlFasilitasKredit { get; set; }

    [JsonPropertyName("jml_fasilitas_others")]
    public short JmlFasilitasOthers { get; set; }

    [JsonPropertyName("jml_pengurus_pemilik")]
    public short JmlPengurusPemilik { get; set; }

    [JsonPropertyName("nama_lengkap_debitur")]
    public string NamaLengkapDebitur { get; set; } = string.Empty;

    [JsonPropertyName("nomor_akta_pendirian")]
    public string? NomorAktaPendirian { get; set; }

    [JsonPropertyName("p2p_status_tunggakan")]
    public short P2pStatusTunggakan { get; set; }

    [JsonPropertyName("alamat_tempat_bekerja")]
    public string? AlamatTempatBekerja { get; set; }

    [JsonPropertyName("bank_status_tunggakan")]
    public short BankStatusTunggakan { get; set; }

    [JsonPropertyName("jml_fasilitas_lainnya")]
    public short JmlFasilitasLainnya { get; set; }

    [JsonPropertyName("mf_jml_hari_tunggakan")]
    public short MfJmlHariTunggakan { get; set; }

    [JsonPropertyName("mf_jml_saldo_terutang")]
    public decimal MfJmlSaldoTerutang { get; set; }

    [JsonPropertyName("mf_tunggakan_terburuk")]
    public decimal MfTunggakanTerburuk { get; set; }

    [JsonPropertyName("nama_sesuai_identitas")]
    public string NamaSesuaiIdentitas { get; set; } = string.Empty;

    [JsonPropertyName("nbfi_status_tunggakan")]
    public short NbfiStatusTunggakan { get; set; }

    [JsonPropertyName("pawn_status_tunggakan")]
    public short PawnStatusTunggakan { get; set; }

    [JsonPropertyName("tanggal_pemeringkatan")]
    public string? TanggalPemeringkatan { get; set; }

    [JsonPropertyName("coorp_status_tunggakan")]
    public short CoorporateStatusTunggakan { get; set; }

    [JsonPropertyName("id_lembaga_pemeringkat")]
    public short? IdLembagaPemeringkat { get; set; }

    [JsonPropertyName("jml_fasilitas_konsumsi")]
    public short JmlFasilitasKonsumsi { get; set; }

    [JsonPropertyName("mf_jml_aktif_fasilitas")]
    public short MfJmlAktifFasilitas { get; set; }

    [JsonPropertyName("mf_jml_tutup_fasilitas")]
    public short MfJmlTutupFasilitas { get; set; }

    [JsonPropertyName("nama_gadis_ibu_kandung")]
    public string NamaGadisIbuKandung { get; set; } = string.Empty;

    [JsonPropertyName("p2p_jml_hari_tunggakan")]
    public short P2pJmlHariTunggakan { get; set; }

    [JsonPropertyName("p2p_jml_saldo_terutang")]
    public decimal P2pJmlSaldoTerutang { get; set; }

    [JsonPropertyName("p2p_tunggakan_terburuk")]
    public decimal P2pTunggakanTerburuk { get; set; }

    [JsonPropertyName("tanggal_akta_pendirian")]
    public DateTime? TanggalAktaPendirian { get; set; }

    [JsonPropertyName("tgl_tunggakan_terakhir")]
    public DateTime? TglTunggakanTerakhir { get; set; }

    [JsonPropertyName("bank_jml_hari_tunggakan")]
    public short BankJmlHariTunggakan { get; set; }

    [JsonPropertyName("bank_jml_saldo_terutang")]
    public decimal BankJmlSaldoTerutang { get; set; }

    [JsonPropertyName("bank_tunggakan_terburuk")]
    public decimal BankTunggakanTerburuk { get; set; }

    [JsonPropertyName("jml_fasilitas_investasi")]
    public short JmlFasilitasInvestasi { get; set; }

    [JsonPropertyName("jml_fasilitas_kkb_mobil")]
    public short JmlFasilitasKkbMobil { get; set; }

    [JsonPropertyName("jml_fasilitas_kkb_motor")]
    public short JmlFasilitasKkbMotor { get; set; }

    [JsonPropertyName("jml_fasilitas_kpr_sd_21")]
    public short JmlFasilitasKprSd21 { get; set; }

    [JsonPropertyName("jml_fasilitas_kpr_up_70")]
    public short JmlFasilitasKprUp70 { get; set; }

    [JsonPropertyName("kolektabilitas_terburuk")]
    public short KolektabilitasTerburuk { get; set; }

    [JsonPropertyName("nbfi_jml_hari_tunggakan")]
    public short NbfiJmlHariTunggakan { get; set; }

    [JsonPropertyName("nbfi_jml_saldo_terutang")]
    public decimal NbfiJmlSaldoTerutang { get; set; }

    [JsonPropertyName("nbfi_tunggakan_terburuk")]
    public decimal NbfiTunggakanTerburuk { get; set; }

    [JsonPropertyName("p2p_jml_aktif_fasilitas")]
    public short P2pJmlAktifFasilitas { get; set; }

    [JsonPropertyName("p2p_jml_tutup_fasilitas")]
    public short P2pJmlTutupFasilitas { get; set; }

    [JsonPropertyName("pawn_jml_hari_tunggakan")]
    public short PawnJmlHariTunggakan { get; set; }

    [JsonPropertyName("pawn_jml_saldo_terutang")]
    public decimal PawnJmlSaldoTerutang { get; set; }

    [JsonPropertyName("pawn_tunggakan_terburuk")]
    public decimal PawnTunggakanTerburuk { get; set; }

    [JsonPropertyName("bank_jml_aktif_fasilitas")]
    public short BankJmlAktifFasilitas { get; set; }

    [JsonPropertyName("bank_jml_tutup_fasilitas")]
    public short BankJmlTutupFasilitas { get; set; }

    [JsonPropertyName("coorp_jml_hari_tunggakan")]
    public short CoorporateJmlHariTunggakan { get; set; }

    [JsonPropertyName("coorp_jml_saldo_terutang")]
    public decimal CoorporateJmlSaldoTerutang { get; set; }

    [JsonPropertyName("coorp_tunggakan_terburuk")]
    public decimal CoorporateTunggakanTerburuk { get; set; }

    [JsonPropertyName("id_debitur_golden_record")]
    public long IdDebiturGoldenRecord { get; set; }

    [JsonPropertyName("jml_fasilitas_dgn_agunan")]
    public short JmlFasilitasDgnAgunan { get; set; }

    [JsonPropertyName("jml_fasilitas_tertunggak")]
    public short JmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("jml_fasilitas_whitegoods")]
    public short JmlFasilitasWhitegoods { get; set; }

    [JsonPropertyName("nbfi_jml_aktif_fasilitas")]
    public short NbfiJmlAktifFasilitas { get; set; }

    [JsonPropertyName("nbfi_jml_tutup_fasilitas")]
    public short NbfiJmlTutupFasilitas { get; set; }

    [JsonPropertyName("pawn_jml_aktif_fasilitas")]
    public short PawnJmlAktifFasilitas { get; set; }

    [JsonPropertyName("pawn_jml_tutup_fasilitas")]
    public short PawnJmlTutupFasilitas { get; set; }

    [JsonPropertyName("tunggakan_terburuk_12bln")]
    public decimal TunggakanTerburuk12Bln { get; set; }

    [JsonPropertyName("coorp_jml_aktif_fasilitas")]
    public short CoorporateJmlAktifFasilitas { get; set; }

    [JsonPropertyName("coorp_jml_tutup_fasilitas")]
    public short CoorporateJmlTutupFasilitas { get; set; }

    [JsonPropertyName("jml_fasilitas_kredit_toko")]
    public short JmlFasilitasKreditToko { get; set; }

    [JsonPropertyName("jml_fasilitas_modal_kerja")]
    public short JmlFasilitasModalKerja { get; set; }

    [JsonPropertyName("jml_tutup_fasilitas_12bln")]
    public short JmlTutupFasilitas12Bln { get; set; }

    [JsonPropertyName("mf_tgl_tunggakan_terakhir")]
    public DateTime? MfTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("jml_fasilitas_kpr_sd_22_70")]
    public short JmlFasilitasKprSd2270 { get; set; }

    [JsonPropertyName("jml_fasilitas_tanpa_agunan")]
    public short JmlFasilitasTanpaAgunan { get; set; }

    [JsonPropertyName("mf_kolektabilitas_terburuk")]
    public short MfKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("p2p_tgl_tunggakan_terakhir")]
    public DateTime? P2pTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("bank_tgl_tunggakan_terakhir")]
    public DateTime? BankTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("jml_hari_tunggakan_terburuk")]
    public short JmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("mf_jml_fasilitas_tertunggak")]
    public short MfJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("nbfi_tgl_tunggakan_terakhir")]
    public DateTime? NbfiTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("p2p_kolektabilitas_terburuk")]
    public short P2pKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("pawn_tgl_tunggakan_terakhir")]
    public DateTime? PawnTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("tgl_buka_fasilitas_terakhir")]
    public DateTime? TglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("bank_kolektabilitas_terburuk")]
    public short BankKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("coorp_tgl_tunggakan_terakhir")]
    public DateTime? CoorporateTglTunggakanTerakhir { get; set; }

    [JsonPropertyName("jml_fasilitas_irrevocable_lc")]
    public short JmlFasilitasIrrevocableLc { get; set; }

    [JsonPropertyName("jml_fasilitas_surat_berharga")]
    public short JmlFasilitasSuratBerharga { get; set; }

    [JsonPropertyName("nbfi_kolektabilitas_terburuk")]
    public short NbfiKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("p2p_jml_fasilitas_tertunggak")]
    public short P2pJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("pawn_kolektabilitas_terburuk")]
    public short PawnKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("tgl_tutup_fasilitas_terakhir")]
    public DateTime? TglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("bank_jml_fasilitas_tertunggak")]
    public short BankJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("coorp_kolektabilitas_terburuk")]
    public short CoorporateKolektabilitasTerburuk { get; set; }

    [JsonPropertyName("kolektabilitas_terburuk_12bln")]
    public short KolektabilitasTerburuk12Bln { get; set; }

    [JsonPropertyName("nbfi_jml_fasilitas_tertunggak")]
    public short NbfiJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("nomor_akta_perubahan_terakhir")]
    public string? NomorAktaPerubahanTerakhir { get; set; }

    [JsonPropertyName("pawn_jml_fasilitas_tertunggak")]
    public short PawnJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("peringkat_atau_rating_debitur")]
    public string? PeringkatAtauRatingDebitur { get; set; }

    [JsonPropertyName("coorp_jml_fasilitas_tertunggak")]
    public short CoorporateJmlFasilitasTertunggak { get; set; }

    [JsonPropertyName("jml_fasilitas_perluasan_mandat")]
    public short JmlFasilitasPerluasanMandat { get; set; }

    [JsonPropertyName("mf_jml_hari_tunggakan_terburuk")]
    public short MfJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("mf_tgl_buka_fasilitas_terakhir")]
    public DateTime? MfTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("mf_tgl_tutup_fasilitas_terakhir")]
    public DateTime? MfTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("p2p_jml_hari_tunggakan_terburuk")]
    public short P2pJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("p2p_tgl_buka_fasilitas_terakhir")]
    public DateTime? P2pTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("tanggal_akta_perubahan_terakhir")]
    public DateTime? TanggalAktaPerubahanTerakhir { get; set; }

    [JsonPropertyName("bank_jml_hari_tunggakan_terburuk")]
    public short BankJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("bank_tgl_buka_fasilitas_terakhir")]
    public DateTime? BankTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("jml_bln_dgn_riwayat_kredit_12bln")]
    public short JmlBlnDgnRiwayatKredit12Bln { get; set; }

    [JsonPropertyName("nbfi_jml_hari_tunggakan_terburuk")]
    public short NbfiJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("nbfi_tgl_buka_fasilitas_terakhir")]
    public DateTime? NbfiTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("p2p_tgl_tutup_fasilitas_terakhir")]
    public DateTime? P2pTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("pawn_jml_hari_tunggakan_terburuk")]
    public short PawnJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("pawn_tgl_buka_fasilitas_terakhir")]
    public DateTime? PawnTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("bank_tgl_tutup_fasilitas_terakhir")]
    public DateTime? BankTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("coorp_jml_hari_tunggakan_terburuk")]
    public short CoorporateJmlHariTunggakanTerburuk { get; set; }

    [JsonPropertyName("coorp_tgl_buka_fasilitas_terakhir")]
    public DateTime? CoorporateTglBukaFasilitasTerakhir { get; set; }

    [JsonPropertyName("jml_hari_tunggakan_terburuk_12bln")]
    public short JmlHariTunggakanTerburuk12Bln { get; set; }

    [JsonPropertyName("nbfi_tgl_tutup_fasilitas_terakhir")]
    public DateTime? NbfiTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("pawn_tgl_tutup_fasilitas_terakhir")]
    public DateTime? PawnTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("coorp_tgl_tutup_fasilitas_terakhir")]
    public DateTime? CoorporateTglTutupFasilitasTerakhir { get; set; }

    [JsonPropertyName("jml_fasilitas_kredit_joint_account")]
    public short JmlFasilitasKreditJointAccount { get; set; }

    [JsonPropertyName("jml_fasilitas_garansi_yang_diberikan")]
    public short JmlFasilitasGaransiYangDiberikan { get; set; }

    [JsonPropertyName("perc_total_bln_tanpa_tunggakan_12bln")]
    public short? PercTotalBlnTanpaTunggakan12Bln { get; set; }
}

/// <summary>
/// Pengurus information
/// </summary>
public class PefindoPengurus
{
    [JsonPropertyName("id_pengurus")]
    public string IdPengurus { get; set; } = string.Empty;

    [JsonPropertyName("nama_pengurus")]
    public string NamaPengurus { get; set; } = string.Empty;

    [JsonPropertyName("jabatan")]
    public string Jabatan { get; set; } = string.Empty;

    [JsonPropertyName("nomor_identitas")]
    public string NomorIdentitas { get; set; } = string.Empty;
}

/// <summary>
/// Facility information (Fasilitas)
/// </summary>
public class PefindoFasilitas
{
    [JsonPropertyName("keterangan")]
    public string Keterangan { get; set; } = string.Empty;

    [JsonPropertyName("keterangan_sebab_macet")]
    public string KeteranganSebabMacet { get; set; } = string.Empty;

    [JsonPropertyName("kolektabilitas_terburuk")]
    public short KolektabilitasTerburuk { get; set; }

    [JsonPropertyName("kolektabilitas_terburuk_12bln")]
    public short KolektabilitasTerburuk12Bln { get; set; }

    [JsonPropertyName("listing")]
    public string Listing { get; set; } = string.Empty;

    [JsonPropertyName("has_collateral")]
    public bool HasCollateral { get; set; }

    [JsonPropertyName("nama_yang_dijamin")]
    public string NamaYangDijamin { get; set; } = string.Empty;

    [JsonPropertyName("nilai_dalam_mata_uang_asal")]
    public decimal NilaiDalamMataUangAsal { get; set; }

    [JsonPropertyName("nilai_pasar")]
    public decimal NilaiPasar { get; set; }

    [JsonPropertyName("nilai_perolehan")]
    public decimal NilaiPerolehan { get; set; }

    [JsonPropertyName("nilai_proyek")]
    public decimal NilaiProyek { get; set; }

    [JsonPropertyName("nominal")]
    public decimal Nominal { get; set; }

    [JsonPropertyName("nominal_tunggakan")]
    public decimal NominalTunggakan { get; set; }

    [JsonPropertyName("nomor_akad_akhir")]
    public string NomorAkadAkhir { get; set; } = string.Empty;

    [JsonPropertyName("nomor_akad_awal")]
    public string NomorAkadAwal { get; set; } = string.Empty;

    [JsonPropertyName("nomor_rekening_fasilitas")]
    public string NomorRekeningFasilitas { get; set; } = string.Empty;

    [JsonPropertyName("peringkat_surat_berharga")]
    public string PeringkatSuratBerharga { get; set; } = string.Empty;

    [JsonPropertyName("plafon")]
    public decimal Plafon { get; set; }

    [JsonPropertyName("plafon_awal")]
    public decimal PlafonAwal { get; set; }

    [JsonPropertyName("realisasi_atau_pencairan_bulan_berjalan")]
    public decimal RealisasiAtauPencairanBulanBerjalan { get; set; }

    [JsonPropertyName("saldo_terutang")]
    public decimal SaldoTerutang { get; set; }

    [JsonPropertyName("sequence_debitur_anggota_joint_account")]
    public short? SequenceDebiturAnggotaJointAccount { get; set; }

    [JsonPropertyName("setoran_jaminan")]
    public decimal SetoranJaminan { get; set; }

    [JsonPropertyName("sovereign_rate")]
    public string SovereignRate { get; set; } = string.Empty;

    [JsonPropertyName("suku_bunga_atau_imbalan")]
    public decimal SukuBungaAtauImbalan { get; set; }

    [JsonPropertyName("syndicated_loan")]
    public short? SyndicatedLoan { get; set; }

    [JsonPropertyName("tahun_bulan_data")]
    public DateTime? TahunBulanData { get; set; }

    [JsonPropertyName("tanggal_akad_akhir")]
    public DateTime? TanggalAkadAkhir { get; set; }

    [JsonPropertyName("tanggal_akad_awal")]
    public DateTime? TanggalAkadAwal { get; set; }

    [JsonPropertyName("tanggal_akhir")]
    public DateTime? TanggalAkhir { get; set; }

    [JsonPropertyName("tanggal_awal_kredit_atau_pembiayaan")]
    public DateTime? TanggalAwalKreditAtauPembiayaan { get; set; }

    [JsonPropertyName("tanggal_jatuh_tempo")]
    public DateTime? TanggalJatuhTempo { get; set; }

    [JsonPropertyName("tanggal_keluar")]
    public DateTime? TanggalKeluar { get; set; }

    [JsonPropertyName("tanggal_kondisi")]
    public DateTime? TanggalKondisi { get; set; }

    [JsonPropertyName("tanggal_macet")]
    public DateTime? TanggalMacet { get; set; }

    [JsonPropertyName("tanggal_mulai")]
    public DateTime? TanggalMulai { get; set; }

    [JsonPropertyName("tanggal_pembelian")]
    public DateTime? TanggalPembelian { get; set; }

    [JsonPropertyName("tanggal_penerbitan")]
    public DateTime? TanggalPenerbitan { get; set; }

    [JsonPropertyName("tanggal_restrukturisasi_akhir")]
    public DateTime? TanggalRestrukturisasiAkhir { get; set; }

    [JsonPropertyName("tanggal_restrukturisasi_awal")]
    public DateTime? TanggalRestrukturisasiAwal { get; set; }

    [JsonPropertyName("tanggal_tunggakan")]
    public DateTime? TanggalTunggakan { get; set; }

    [JsonPropertyName("tanggal_wanprestasi")]
    public DateTime? TanggalWanprestasi { get; set; }

    [JsonPropertyName("tenor")]
    public short Tenor { get; set; }

    [JsonPropertyName("tgl_tunggakan_terakhir")]
    public DateTime? TglTunggakanTerakhir { get; set; }

    [JsonPropertyName("tunggakan_bunga_atau_imbalan")]
    public decimal TunggakanBungaAtauImbalan { get; set; }

    [JsonPropertyName("tunggakan_pokok")]
    public decimal TunggakanPokok { get; set; }

    [JsonPropertyName("tunggakan_terburuk")]
    public decimal TunggakanTerburuk { get; set; }

    [JsonPropertyName("tunggakan_terburuk_12bln")]
    public decimal TunggakanTerburuk12Bln { get; set; }

    [JsonPropertyName("agunan")]
    public List<PefindoAgunan> Agunan { get; set; } = new();

    [JsonPropertyName("penjamin")]
    public List<PefindoPenjamin> Penjamin { get; set; } = new();

    [JsonPropertyName("riwayat_fasilitas")]
    public List<PefindoRiwayatFasilitas> RiwayatFasilitas { get; set; } = new();
}

/// <summary>
/// Collateral information (Agunan)
/// </summary>
public class PefindoAgunan
{
    [JsonPropertyName("alamat_agunan")]
    public string AlamatAgunan { get; set; } = string.Empty;

    [JsonPropertyName("bukti_kepemilikan")]
    public string BuktiKepemilikan { get; set; } = string.Empty;

    [JsonPropertyName("diasuransikan")]
    public short Diasuransikan { get; set; }

    [JsonPropertyName("id_agunan")]
    public long IdAgunan { get; set; }

    [JsonPropertyName("id_jenis_agunan")]
    public short IdJenisAgunan { get; set; }

    [JsonPropertyName("id_jenis_pengikatan")]
    public short IdJenisPengikatan { get; set; }

    [JsonPropertyName("id_kabupaten_kota")]
    public short IdKabupatenKota { get; set; }

    [JsonPropertyName("id_kantor_cabang")]
    public int IdKantorCabang { get; set; }

    [JsonPropertyName("id_lembaga_pemeringkat")]
    public short IdLembagaPemeringkat { get; set; }

    [JsonPropertyName("id_status_agunan")]
    public short IdStatusAgunan { get; set; }

    [JsonPropertyName("keterangan")]
    public string Keterangan { get; set; } = string.Empty;

    [JsonPropertyName("kode_register_atau_nomor_agunan")]
    public string KodeRegisterAtauNomorAgunan { get; set; } = string.Empty;

    [JsonPropertyName("nama_pemilik_agunan")]
    public string NamaPemilikAgunan { get; set; } = string.Empty;

    [JsonPropertyName("nama_penilai_independen")]
    public string NamaPenilaiIndependen { get; set; } = string.Empty;

    [JsonPropertyName("nilai_agunan_menurut_pelapor")]
    public decimal NilaiAgunanMenurutPelapor { get; set; }

    [JsonPropertyName("nilai_agunan_menurut_penilai_independen")]
    public decimal NilaiAgunanMenurutPenilaiIndependen { get; set; }

    [JsonPropertyName("nilai_agunan_sesuai_njop")]
    public decimal NilaiAgunanSesuaiNjop { get; set; }

    [JsonPropertyName("peringkat_agunan")]
    public string PeringkatAgunan { get; set; } = string.Empty;

    [JsonPropertyName("persentase_paripasu")]
    public decimal? PersentaseParipasu { get; set; }

    [JsonPropertyName("status_kredit_joint_account")]
    public string StatusKreditJointAccount { get; set; } = string.Empty;

    [JsonPropertyName("status_paripasu")]
    public string StatusParipasu { get; set; } = string.Empty;

    [JsonPropertyName("tanggal_pengikatan")]
    public DateTime? TanggalPengikatan { get; set; }

    [JsonPropertyName("tanggal_penilaian_agunan_menurut_pelapor")]
    public DateTime? TanggalPenilaianAgunanMenurutPelapor { get; set; }

    [JsonPropertyName("tanggal_penilaian_agunan_menurut_penilai_independen")]
    public DateTime? TanggalPenilaianAgunanMenurutPenilaiIndependen { get; set; }
}

/// <summary>
/// Guarantor information (Penjamin)
/// </summary>
public class PefindoPenjamin
{
    [JsonPropertyName("alamat_penjamin")]
    public string AlamatPenjamin { get; set; } = string.Empty;

    [JsonPropertyName("id_golongan_debitur")]
    public int IdGolonganDebitur { get; set; }

    [JsonPropertyName("id_jenis_fasilitas")]
    public int IdJenisFasilitas { get; set; }

    [JsonPropertyName("id_jenis_identitas")]
    public short IdJenisIdentitas { get; set; }

    [JsonPropertyName("id_kantor_cabang")]
    public int IdKantorCabang { get; set; }

    [JsonPropertyName("id_penjamin")]
    public long IdPenjamin { get; set; }

    [JsonPropertyName("keterangan")]
    public string Keterangan { get; set; } = string.Empty;

    [JsonPropertyName("nama_lengkap_penjamin")]
    public string NamaLengkapPenjamin { get; set; } = string.Empty;

    [JsonPropertyName("nama_penjamin_sesuai_identitas")]
    public string NamaPenjaminSesuaiIdentitas { get; set; } = string.Empty;

    [JsonPropertyName("nomor_identitas_penjamin")]
    public string NomorIdentitasPenjamin { get; set; } = string.Empty;

    [JsonPropertyName("npwp_penjamin")]
    public string NpwpPenjamin { get; set; } = string.Empty;

    [JsonPropertyName("persentase_fasilitas_yang_dijamin")]
    public decimal PersentaseFasilitasYangDijamin { get; set; }

    [JsonPropertyName("tahun_bulan_data")]
    public DateTime TahunBulanData { get; set; }
}

/// <summary>
/// Facility history information (Riwayat Fasilitas)
/// </summary>
public class PefindoRiwayatFasilitas
{
    [JsonPropertyName("baki_debet")]
    public decimal BakiDebet { get; set; }

    [JsonPropertyName("denda")]
    public decimal Denda { get; set; }

    [JsonPropertyName("id_kolektabilitas")]
    public short IdKolektabilitas { get; set; }

    [JsonPropertyName("jumlah_hari_tunggakan")]
    public short JumlahHariTunggakan { get; set; }

    [JsonPropertyName("nominal_tunggakan")]
    public decimal NominalTunggakan { get; set; }

    [JsonPropertyName("saldo_terutang")]
    public decimal SaldoTerutang { get; set; }

    [JsonPropertyName("snapshot_order")]
    public short SnapshotOrder { get; set; }

    [JsonPropertyName("tahun_bulan_data")]
    public DateTime TahunBulanData { get; set; }

    [JsonPropertyName("tunggakan_bunga_atau_imbalan")]
    public decimal? TunggakanBungaAtauImbalan { get; set; }
}

/// <summary>
/// Other data information
/// </summary>
public class PefindoOtherData
{
    [JsonPropertyName("key")]
    public List<string> Key { get; set; } = new();

    [JsonPropertyName("value")]
    public List<string> Value { get; set; } = new();

    [JsonPropertyName("id_pelapor")]
    public int IdPelapor { get; set; }
}

/// <summary>
/// Data request information (Permintaan Data)
/// </summary>
public class PefindoPermintaanData
{
    [JsonPropertyName("id_pelapor")]
    public int IdPelapor { get; set; }

    [JsonPropertyName("tgl_permintaan")]
    public DateTime TglPermintaan { get; set; }

    [JsonPropertyName("id_jenis_pelapor")]
    public int IdJenisPelapor { get; set; }

    [JsonPropertyName("id_tujuan_permintaan")]
    public int IdTujuanPermintaan { get; set; }
}

/// <summary>
/// Summary of data requests (Summary Permintaan Data)
/// </summary>
public class PefindoSummaryPermintaanData
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("jml_pelapor_1bln")]
    public short JmlPelapor1Bln { get; set; }

    [JsonPropertyName("jml_pelapor_3bln")]
    public short JmlPelapor3Bln { get; set; }

    [JsonPropertyName("jml_pelapor_6bln")]
    public short JmlPelapor6Bln { get; set; }

    [JsonPropertyName("jml_pelapor_12bln")]
    public short JmlPelapor12Bln { get; set; }

    [JsonPropertyName("jml_pelapor_24bln")]
    public short JmlPelapor24Bln { get; set; }
}

/// <summary>
/// Data request summary (Summary Riwayat Debitur)
/// </summary>
public class PefindoSummaryRiwayatDebitur
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("jml_fasilitas_aktif")]
    public short JmlFasilitasAktif { get; set; }

    [JsonPropertyName("total_plafon")]
    public decimal TotalPlafon { get; set; }

    [JsonPropertyName("total_saldo")]
    public decimal TotalSaldo { get; set; }

    [JsonPropertyName("kolektabilitas_terburuk")]
    public short KolektabilitasTerburuk { get; set; }
}

/// <summary>
/// Summary of debtor history data (Summary Identitas Debitur)
/// </summary>
public class PefindoRiwayatIdentitasDebitur
{
    [JsonPropertyName("periode")]
    public string Periode { get; set; } = string.Empty;

    [JsonPropertyName("nomor_identitas")]
    public string NomorIdentitas { get; set; } = string.Empty;

    [JsonPropertyName("nama_lengkap")]
    public string NamaLengkap { get; set; } = string.Empty;

    [JsonPropertyName("alamat")]
    public string Alamat { get; set; } = string.Empty;
}

#endregion

#region Pefindo Scoring

/// <summary>
/// Pefindo Scoring information
/// </summary>
public class PefindoScoring
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("id_pefindo")]
    public string IdPefindo { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("pod")]
    public decimal Pod { get; set; }

    [JsonPropertyName("reason_code")]
    public List<string> ReasonCode { get; set; } = new();

    [JsonPropertyName("reason_desc")]
    public List<string> ReasonDesc { get; set; } = new();

    [JsonPropertyName("risk_grade")]
    public string RiskGrade { get; set; } = string.Empty;

    [JsonPropertyName("risk_desc")]
    public string RiskDesc { get; set; } = string.Empty;
}
#endregion
