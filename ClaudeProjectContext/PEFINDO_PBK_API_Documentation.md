# Dokumentasi API PEFINDO PBK

Dokumen ini menjelaskan dua jenis layanan API yang disediakan oleh PEFINDO Biro Kredit (PBK): **New Report API** dan **Old Report API**.

- **New Report API**: Dirancang dengan teknologi terkini (RESTful, JSON) untuk fleksibilitas dan kemudahan integrasi.
- **Old Report API**: Dibuat untuk menjaga kompatibilitas dengan sistem yang sudah berjalan (SOAP, XML).

---

## New Report API (JSON)

API ini adalah versi terbaru dan yang direkomendasikan. Menggunakan format JSON yang lebih ringan dan modern.

### Alur Proses API

1.  **getToken**: Dapatkan token otentikasi menggunakan username dan password. Token memiliki masa berlaku.
2.  **validateToken (Opsional)**: Periksa validitas token yang ada.
3.  **search**: Lakukan pencarian data debitur berdasarkan parameter produk yang dipilih.
4.  **generateReport**: Inisiasi proses pembuatan laporan untuk satu atau lebih debitur yang ditemukan dari hasil `search`. Proses ini membutuhkan `event_id` unik dari client.
5.  **getReport**: Ambil laporan yang telah selesai dibuat menggunakan `event_id`.
6.  **downloadReport**: Jika laporan berukuran besar (big report), gunakan method ini untuk mengunduh laporan dalam bentuk file JSON.
7.  **downloadPdfReport**: Unduh laporan dalam format PDF jika opsi ini dipilih saat `generateReport`.
8.  **bulk**: Kirim beberapa permintaan pencarian sekaligus.

---

### 1. getToken

Fungsi untuk mendapatkan token otentikasi (JWT) sebagai izin akses ke API lainnya.

- **Method**: `GET`
- **Endpoint**: `https://[domain]/api/v1/getToken`
- **Authorization**: `Basic Auth` (base64encode({username}:{password}))
- **Prekondisi**:
  - Username dan password sudah terdaftar.
  - IP client sudah di-whitelist.

**Header**

| Key             | Value              |
| --------------- | ------------------ |
| `Content-Type`  | `application/json` |
| `Authorization` | `Basic {base64}`   |

**Contoh cURL**

```bash
curl --location 'https://[domain]/api/v1/getToken' \
--header 'Content-Type: application/json' \
--header 'Authorization: Basic cGJrX3VzZXI6UEBzc3cwcmQxMjM0'
```

**Respon Sukses (200 OK)**

```json
{
  "code": "01",
  "status": "success",
  "message": "Token aktif",
  "data": {
    "valid_date": "2024261509242633",
    "token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICI5SFh... (token panjang) ...K5mfUGQ"
  }
}
```

| Key               | Tipe Data | Deskripsi                                               |
| ----------------- | --------- | ------------------------------------------------------- |
| `code`            | String    | `01` menandakan sukses.                                 |
| `status`          | String    | `success`.                                              |
| `message`         | String    | `Token aktif`.                                          |
| `data.valid_date` | String    | Tanggal dan waktu kedaluwarsa token (yyyymmddHH24miss). |
| `data.token`      | String    | JSON Web Token (JWT) untuk otentikasi.                  |

**Respon Gagal**

- **403 Forbidden (Username/Password Salah)**
  ```json
  {
    "code": "13",
    "status": "failed",
    "message": "username atau password salah"
  }
  ```
- **403 Forbidden (IP tidak terdaftar)**
  ```json
  {
    "code": "17",
    "status": "failed",
    "message": "akses ditolak"
  }
  ```
- **500 Internal Server Error**
  ```json
  {
    "code": "99",
    "status": "failed",
    "message": "[Pesan error dari sistem]"
  }
  ```

---

### 2. validateToken

Fungsi untuk memvalidasi token yang sedang aktif.

- **Method**: `GET`
- **Endpoint**: `https://[domain]/api/v1/validateToken`
- **Authorization**: `Bearer Token`

**Header**

| Key             | Value              |
| --------------- | ------------------ |
| `Content-Type`  | `application/json` |
| `Authorization` | `Bearer {token}`   |

**Contoh cURL**

```bash
curl --location --request GET 'https://[domain]/api/v1/validateToken' \
--header 'Content-Type: application/json' \
--header 'Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICI5SFh... (token panjang) ...K5mfUGQ'
```

**Respon Sukses (200 OK)**

```json
{
  "code": "01",
  "status": "success",
  "message": "authorized"
}
```

**Respon Gagal (403 Forbidden - Token tidak valid)**

```json
{
  "code": "06",
  "status": "failed",
  "message": "Invalid Token"
}
```

---

### 3. search

Fungsi untuk melakukan pencarian data debitur berdasarkan produk yang dipilih.

- **Method**: `POST`
- **Endpoint**: `https://[domain]/api/v1/product/search`
- **Authorization**: `Bearer Token`

**Header**

| Key             | Value            |
| --------------- | ---------------- |
| `Authorization` | `Bearer {token}` |

**Body Parameters**

| Key                    | Wajib/Kondisional | Tipe Data     | Deskripsi                                                                              |
| ---------------------- | ----------------- | ------------- | -------------------------------------------------------------------------------------- |
| `type`                 | M                 | String        | Jenis pencarian. Alternatif: `PERSONAL` atau `CORPORATE`.                              |
| `product_id`           | M                 | Numeric       | ID produk yang akan dicari (lihat lampiran).                                           |
| `inquiry_reason`       | M                 | Numeric       | Alasan permintaan (lihat lampiran).                                                    |
| `reference_code`       | C                 | String        | Kode referensi dari client.                                                            |
| `params`               | M                 | Array<Object> | Kumpulan parameter pencarian. Setiap objek dalam array mewakili satu subjek pencarian. |
| `params.id_types`      | M                 | String        | Jenis identitas. Contoh: `KTP`, `NPWP`, `PHONE`, `PASSPORT`.                           |
| `params.id_no`         | M                 | String        | Nomor identitas.                                                                       |
| `params.name`          | M                 | String        | Nama lengkap debitur.                                                                  |
| `params.date_of_birth` | C                 | String        | Tanggal lahir debitur (format: `YYYY-MM-DD`).                                          |
| `params.report_date`   | C                 | String        | Untuk _backdated report_, tanggal laporan yang diinginkan (format: `YYYY-MM-DD`).      |

**Contoh Body Request**

```json
{
  "type": "PERSONAL",
  "product_id": 93,
  "inquiry_reason": 48,
  "reference_code": "xxxxxx",
  "params": [
    {
      "id_type": "KTP",
      "id_no": "3150972902880002",
      "name": "INDIVIDU",
      "date_of_birth": "1967-09-11",
      "report_date": "2024-03-06"
    }
  ]
}
```

**Respon Sukses (200 OK)**

```json
{
  "code": "01",
  "status": "Success",
  "message": "Data ditemukan",
  "inquiry_id": 318,
  "data": [
    {
      "similarity_score": 100.0,
      "id_pefindo": 101110967000498,
      "id_type": "KTP",
      "id_no": "3150972902880002",
      "id_tipe_debitur": "PERSONAL",
      "nama_debitur": "INDIVIDU 101110967000498",
      "tanggal_lahir": "1967-09-11",
      "npwp": "101110967000498",
      "alamat": "ALAMAT 101110967000498",
      "nama_gadis_ibu_kandung": "IBU 101110967000498",
      "response_status": "ALL_CORRECT"
    }
  ]
}
```

**Respon Gagal (404 Not Found)**

```json
{
  "code": "31",
  "status": "failed",
  "message": "Data tidak ditemukan"
}
```

---

### 4. generateReport

Fungsi untuk memulai proses pembuatan laporan berdasarkan hasil dari `search`.

- **Method**: `POST`
- **Endpoint**: `https://[domain]/api/v1/product/generateReport`
- **Authorization**: `Bearer Token`

**Body Parameters**

| Key              | Wajib/Kondisional | Tipe Data     | Deskripsi                                                                                     |
| ---------------- | ----------------- | ------------- | --------------------------------------------------------------------------------------------- |
| `inquiry_id`     | M                 | Number        | `inquiry_id` yang diterima dari respon `/product/search`.                                     |
| `ids`            | M                 | Array<Object> | Data debitur yang dipilih untuk dibuatkan laporan. Diambil dari respon `/product/search`.     |
| `ids.id_type`    | M                 | String        | Tipe ID debitur.                                                                              |
| `ids.id_no`      | M                 | String        | Nomor ID debitur.                                                                             |
| `ids.id_pefindo` | M                 | Numeric       | ID Pefindo debitur.                                                                           |
| `event_id`       | M                 | String        | **UUID** unik yang dibuat oleh client untuk melacak permintaan ini.                           |
| `generate_pdf`   | C                 | String        | Opsi untuk membuat laporan PDF. Alternatif: `"1"` (Ya) atau `"0"` (Tidak).                    |
| `language`       | C                 | String        | Bahasa yang digunakan dalam PDF. Alternatif: `"01"` (Bahasa Indonesia) atau `"02"` (Inggris). |

**Contoh Body Request**

```json
{
  "inquiry_id": 243,
  "ids": [
    {
      "id_type": "KTP",
      "id_no": "3150972902880002",
      "id_pefindo": 101110967000498
    }
  ],
  "event_id": "451bd8bd-19dd-4605-bd05-a92971892a47",
  "generate_pdf": "1",
  "language": "01"
}
```

**Respon Sukses (200 OK)**

```json
{
  "code": "01",
  "status": "success",
  "event_id": "451bd8bd-19dd-4605-bd05-a92971892a47",
  "message": "Proses membuat report sedang dikerjakan"
}
```

**Respon Gagal (409 Conflict - Event ID sudah digunakan)**

```json
{
  "code": "35",
  "status": "failed",
  "event_id": "451bd8bd-19dd-4605-bd05-a92971892a47",
  "message": "event_id sudah ada, gunakan yang lain"
}
```

---

### 5. getReport

Fungsi untuk mendapatkan data laporan yang telah selesai diproses.

- **Method**: `GET`
- **Endpoint**: `https://[domain]/api/v1/product/getReport/eventId/{eventId}`
- **Authorization**: `Bearer Token`

**Path Parameters**

| Key       | Wajib/Kondisional | Tipe Data | Deskripsi                                                      |
| --------- | ----------------- | --------- | -------------------------------------------------------------- |
| `eventId` | M                 | String    | `event_id` (UUID) yang dikirim saat `/product/generateReport`. |

**Respon Sukses (200 OK - Laporan Selesai)**

- **Kode `01`**: Laporan normal berhasil dibuat dan data lengkap ada di dalam _response body_. (Lihat **Struktur Data Respon JSON** di bawah untuk detail).
- **Kode `36`**: Laporan termasuk kategori _big report_. Data fasilitas tidak disertakan dan harus diunduh menggunakan `/downloadReport`.

**Respon Lainnya**

- **200 OK (Kode `32`)**: Laporan masih dalam proses scoring.
- **404 Not Found (Kode `31`)**: Data tidak ditemukan.

---

### 6. downloadReport

Fungsi untuk mengunduh data laporan dalam bentuk file JSON, khusus untuk kategori _big report_.

- **Method**: `GET`
- **Endpoint**: `https://[domain]/api/v1/product/downloadReport/event_id/{event_id}`
- **Authorization**: `Bearer Token`

---

### 7. downloadPdfReport

Fungsi untuk mengunduh file laporan dalam format PDF.

- **Method**: `GET`
- **Endpoint**: `https://[domain]/api/v1/product/downloadPdfReport/event_id/{event_id}`
- **Authorization**: `Bearer Token`

---

### 8. bulk

Fungsi untuk membuat sejumlah laporan sekaligus.

- **Method**: `POST`
- **Endpoint**: `https://[domain]/api/v1/product/bulk`
- **Authorization**: `Bearer Token`

**Contoh Body Request**

```json
{
    "data": [
        {
            "event_id": "451bd8bd-19dd-4605-bd05-a92971892a48",
            "type": "PERSONAL",
            "product_id": 93,
            "inquiry_reason": 48,
            "params": [ { ... } ]
        },
        {
            "event_id": "451bd8bd-19dd-4605-bd05-a92971892a49",
            "type": "PERSONAL",
            "product_id": 93,
            "inquiry_reason": 48,
            "params": [ { ... } ]
        }
    ]
}
```

**Respon Sukses (200 OK)**

```json
{
  "code": "01",
  "status": "success",
  "message": "Proses bulk sedang dikerjakan"
}
```

---

---

## Old Report API (Legacy - XML)

API ini dirancang untuk kompatibilitas dengan sistem lama dan menggunakan protokol SOAP dengan format data XML.

### 1. API Smartsearch Individu

- **URL Endpoint**: `https://[domain]/WsReport/v5.109/service.svc?wsdl`
- **Authorization**: Basic Authentication
- **Soapaction**: `http://[domain]/CB5/IReportPublicServiceBase/SmartSearchIndividual`

**Contoh Body Request (XML)**

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:cb5="http://[domain]/CB5" xmlns:smar="http://[domain]/CB5/v5.109/SmartSearch">
   <soapenv:Header/>
   <soapenv:Body>
      <cb5:SmartSearchIndividual>
         <cb5:query>
            <smar:Inquiry_reason>Providing Facilities</smar:Inquiry_reason>
            <smar:Parameters>
               <smar:DateOfBirth>1985-05-01</smar:DateOfBirth>
               <smar:FullName>Mike Tyson</smar:FullName>
               <smar:IdNumbers>
                  <smar:IdNumberPairIndividual>
                     <smar:IdNumber>1234567890123456</smar:IdNumber>
                     <smar:IdNumberType>KTP</smar:IdNumberType>
                  </smar:IdNumberPairIndividual>
               </smar:IdNumbers>
            </smar:Parameters>
            <smar:ReferenceCode>A123</smar:ReferenceCode>
         </cb5:query>
      </cb5:SmartSearchIndividual>
   </soapenv:Body>
</soapenv:Envelope>
```

### 2. API Smartsearch Company

- **URL Endpoint**: `https://[domain]/WsReport/v5.109/service.svc?wsdl`
- **Authorization**: Basic Authentication
- **Soapaction**: `http://[domain]/CB5/IReportPublicServiceBase/SmartSearchCompany`

**Contoh Body Request (XML)**

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:cb5="http://[domain]/CB5" xmlns:smar="http://[domain]/CB5/v5.109/SmartSearch">
   <soapenv:Header/>
   <soapenv:Body>
      <cb5:SmartSearchCompany>
         <cb5:query>
            <smar:Inquiry_reason>Providing Facilities</smar:Inquiry_reason>
            <smar:Parameters>
               <smar:CompanyName>PT Pertambangan</smar:CompanyName>
               <smar:IdNumbers>
                  <smar:IdNumberPairCompany>
                     <smar:IdNumber>555666777888999</smar:IdNumber>
                     <smar:IdNumberType>NPWP</smar:IdNumberType>
                  </smar:IdNumberPairCompany>
               </smar:IdNumbers>
            </smar:Parameters>
            <smar:ReferenceCode>A123</smar:ReferenceCode>
         </cb5:query>
      </cb5:SmartSearchCompany>
   </soapenv:Body>
</soapenv:Envelope>
```

### 3. API GetCustomReport

- **URL Endpoint**: `https://[domain]/WsReport/v5.109/service.svc?wsdl`
- **Authorization**: Basic Authentication
- **Soapaction**: `http://[domain]/CB5/IReportPublicServiceBase/GetCustomReport`

### 4. API GetPdfReport

- **URL Endpoint**: `https://[domain]/WsReport/v5.109/service.svc?wsdl`
- **Authorization**: Basic Authentication
- **Soapaction**: `http://[domain]/CB5/IReportPublicServiceBase/GetPdfReport`

---

---

## Struktur Data Respon JSON (getReport)

Berikut adalah rincian struktur data JSON yang diterima dari endpoint `/getReport`.

### `report.header`

| JSON Column                             | Tipe Data |
| --------------------------------------- | --------- |
| `id_report`                             | String    |
| `idscore_id`                            | String    |
| `username`                              | String    |
| `tgl_permintaan`                        | DateTime  |
| `id_tujuan_permintaan`                  | String    |
| `id_tipe_debitur`                       | int16     |
| `no_referensi_dokumen`                  | String    |
| `ktp`                                   | String    |
| `npwp`                                  | String    |
| `nama_debitur`                          | String    |
| `tanggal_lahir`                         | DateTime  |
| `tempat_lahir`                          | String    |
| `tanggal_pendirian`                     | DateTime  |
| `tempat_pendirian`                      | String    |
| `fasilitas_kredit_tidak_tampil`         | String    |
| `fasilitas_joint_account_tidak_tampil`  | String    |
| `fasilitas_surat_berharga_tidak_tampil` | String    |
| `fasilitas_irrevocable_lc_tidak_tampil` | String    |
| `fasilitas_garansi_tidak_tampil`        | String    |
| `fasilitas_lain_tidak_tampil`           | String    |

### `report.debitur`

| JSON Column                     | Tipe Data |
| ------------------------------- | --------- |
| `alamat_debitur`                | string    |
| `alamat_tempat_bekerja`         | string    |
| `email`                         | string    |
| `go_public`                     | int16?    |
| `id_debitur_golden_record`      | int64     |
| `id_golongan_debitur`           | int32     |
| `id_jenis_badan_usaha`          | int32     |
| `id_jenis_identitas`            | int32     |
| `id_jenis_kelamin`              | int32     |
| `id_kabupaten_kota`             | int16?    |
| `id_kantor_cabang`              | int64     |
| `id_lembaga_pemeringkat`        | int16     |
| `id_lokasi`                     | int32     |
| `id_negara`                     | int16     |
| `id_sektor_ekonomi`             | int32     |
| `id_status_gelar`               | int16     |
| `id_status_perkawinan`          | int16     |
| `id_tipe_debitur`               | int16     |
| `kecamatan`                     | string    |
| `kelurahan`                     | string    |
| `nama_alias`                    | string    |
| `nama_badan_usaha`              | string    |
| `nama_gadis_ibu_kandung`        | string    |
| `nama_group`                    | string    |
| `nama_lengkap_debitur`          | string    |
| `nama_sesuai_identitas`         | string    |
| `nomor_akta_pendirian`          | string    |
| `nomor_akta_perubahan_terakhir` | string    |
| `nomor_identitas`               | string    |
| `npwp`                          | string    |
| `peringkat_atau_rating_debitur` | string    |
| ... (dan field lainnya)         | ...       |

## Lampiran

### Kode Respon HTTP

| Kode | Keterangan                      |
| ---- | ------------------------------- |
| 200  | OK                              |
| 400  | Bad Request                     |
| 403  | Unauthorized / Forbidden Access |
| 404  | Data Not Found                  |
| 500  | Internal Server Error           |

### Kode Output Kesalahan Aplikasi

| Kode | Keterangan                                             |
| ---- | ------------------------------------------------------ |
| 01   | Sukses                                                 |
| 11   | Parameter username wajib diisi                         |
| 12   | Parameter password wajib diisi                         |
| 13   | Username atau password salah                           |
| 14   | Akun tidak aktif                                       |
| 15   | Token tidak valid                                      |
| 16   | Token expired                                          |
| 17   | Akses ditolak (IP tidak terdaftar)                     |
| 21   | Parameter wajib diisi                                  |
| 22   | Parameter tidak sesuai                                 |
| 31   | Data tidak ditemukan                                   |
| 32   | Laporan masih dalam proses scoring                     |
| 33   | Laporan untuk data similarity tidak diproses/diabaikan |
| 34   | Request id tidak ditemukan                             |
| 35   | Event_id sudah ada, gunakan yang lain                  |
| 36   | Kategori big report, gunakan method downloadReport     |
| 99   | Error lain-lain                                        |
