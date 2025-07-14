Update :

1. Codebase adapter web service - done merge dalam 1 project service.
2. Validasi input dan cycle day - done.
3. Logic & handler token - done.
4. Logic & handler smart search - done
5. Logic & handler Custom Report (Generate Report, Get Report) - done.
6. Logic & handler Other Data & PDF Report - done.
7. New Logic & process logging comprehensive table - done

- **pefindo.bk_log_entries**: Tabel utama untuk menyimpan log proses bisnis secara keseluruhan, terjadi dalam sistem
- **pefindo.bk_http_request_logs**: Tabel khusus untuk menyimpan detail log komunikasi HTTP, termasuk request/response, status code, headers, dan waktu eksekusi setiap API call
- **pefindo.bk_process_step_logs**: Tabel untuk mencatat setiap langkah dalam alur proses bisnis, memungkinkan tracking progress dan debugging pada setiap tahapan workflow
- **pefindo.bk_error_logs**: Tabel dedicated untuk menyimpan informasi error dan exception yang terjadi, dilengkapi dengan stack trace, error code, dan context untuk troubleshooting

Todo :

1. Refactor function proses summary data menjadi bagian bagian
2. Integrasi hasil refactor function dengan web adapter

Catatan : keseluruhan proses sudah ditest flownya menggunakan value dummy response (generate sendiri based on strukturjson response api pefindo), karena belum bisa actual hit ke pbk.
