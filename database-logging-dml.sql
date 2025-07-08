
Implementation :

-- =====================================
-- 1. DATABASE SCHEMA (PostgreSQL, pefindo schema, bk_ prefix)
-- =====================================

-- Master log table for correlation
CREATE TABLE pefindo.bk_log_entries (
    id BIGSERIAL PRIMARY KEY,
    correlation_id VARCHAR(50) NOT NULL,
    request_id VARCHAR(50) NOT NULL,
    user_id VARCHAR(50),
    session_id VARCHAR(50),
    process_name VARCHAR(100) NOT NULL,
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP,
    status VARCHAR(20), -- Success, Failed, InProgress
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bk_log_entries_correlation_id ON pefindo.bk_log_entries(correlation_id);
CREATE INDEX idx_bk_log_entries_request_id ON pefindo.bk_log_entries(request_id);
CREATE INDEX idx_bk_log_entries_created_at ON pefindo.bk_log_entries(created_at);

-- Example data for bk_log_entries
INSERT INTO pefindo.bk_log_entries (correlation_id, request_id, user_id, session_id, process_name, start_time, end_time, status)
VALUES
    ('corr-123', 'req-456', 'user-789', 'sess-101', 'CreditReportGeneration', '2023-10-01 10:00:00', '2023-10-01 10:00:05', 'Success'),
    ('corr-124', 'req-457', 'user-790', 'sess-102', 'CreditReportGeneration', '2023-10-01 10:01:00', '2023-10-01 10:01:05', 'Failed'),
    ('corr-125', 'req-458', 'user-791', 'sess-103', 'CreditReportGeneration', '2023-10-01 10:02:00', '2023-10-01 10:02:05', 'InProgress');



-- HTTP requests to external services
CREATE TABLE pefindo.bk_http_request_logs (
    id BIGSERIAL PRIMARY KEY,
    correlation_id VARCHAR(50) NOT NULL,
    request_id VARCHAR(50) NOT NULL,
    service_name VARCHAR(100) NOT NULL, -- 'PefindoIDScore', 'PefindoPBK'
    method VARCHAR(10) NOT NULL,
    url TEXT NOT NULL,
    request_headers TEXT,
    request_body TEXT,
    response_status_code INTEGER,
    response_headers TEXT,
    response_body TEXT,
    duration_ms INTEGER, -- milliseconds
    request_time TIMESTAMP NOT NULL,
    response_time TIMESTAMP,
    is_successful BOOLEAN,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bk_http_logs_correlation_id ON pefindo.bk_http_request_logs(correlation_id);
CREATE INDEX idx_bk_http_logs_service_time ON pefindo.bk_http_request_logs(service_name, request_time);
CREATE INDEX idx_bk_http_logs_successful ON pefindo.bk_http_request_logs(is_successful);

-- Example data for bk_http_request_logs
INSERT INTO pefindo.bk_http_request_logs (correlation_id, request_id, service_name, method, url, request_headers, request_body, response_status_code, response_headers, response_body, duration_ms, request_time, response_time, is_successful, error_message)
VALUES
    ('corr-123', 'req-456', 'PefindoIDScore', 'POST', 'https://api.pefindo.com/idscore', '{"Authorization": "Bearer token"}', '{"id": "123"}', 200, '{"Content-Type": "application/json"}', '{"score": 750}', 100, '2023-10-01 10:00:00', '2023-10-01 10:00:01', TRUE, NULL),
    ('corr-124', 'req-457', 'PefindoPBK', 'GET', 'https://api.pefindo.com/pbk', '{"Authorization": "Bearer token"}', NULL, 404, '{"Content-Type": "application/json"}', '{"error": "Not Found"}', 200, '2023-10-01 10:01:00', '2023-10-01 10:01:01', FALSE, 'Not Found'),
    ('corr-125', 'req-458', 'PefindoIDScore', 'POST', 'https://api.pefindo.com/idscore', '{"Authorization": "Bearer token"}', '{"id": "456"}', 500, '{"Content-Type": "application/json"}', '{"error": "Internal Server Error"}', 300, '2023-10-01 10:02:00', '2023-10-01 10:02:01', FALSE, 'Internal Server Error');

-- Process step logging
CREATE TABLE pefindo.bk_process_step_logs (
    id BIGSERIAL PRIMARY KEY,
    correlation_id VARCHAR(50) NOT NULL,
    request_id VARCHAR(50) NOT NULL,
    step_name VARCHAR(100) NOT NULL,
    step_order INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL, -- Started, Completed, Failed
    input_data TEXT,
    output_data TEXT,
    error_details TEXT,
    duration_ms INTEGER, -- milliseconds
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bk_process_logs_correlation_order ON pefindo.bk_process_step_logs(correlation_id, step_order);
CREATE INDEX idx_bk_process_logs_step_name ON pefindo.bk_process_step_logs(step_name);

-- Example data for bk_process_step_logs
INSERT INTO pefindo.bk_process_step_logs (correlation_id, request_id, step_name, step_order, status, input_data, output_data, error_details, duration_ms, start_time, end_time)
VALUES
    ('corr-123', 'req-456', 'ValidateRequest', 1, 'Completed', '{"id": "123"}', '{"isValid": true}', NULL, 100, '2023-10-01 10:00:00', '2023-10-01 10:00:01'),
    ('corr-124', 'req-457', 'FetchCreditScore', 2, 'Failed', '{"id": "124"}', NULL, 'Service Unavailable', 200, '2023-10-01 10:01:00', '2023-10-01 10:01:01'),
    ('corr-125', 'req-458', 'GenerateReport', 3, 'InProgress', '{"id": "125"}', NULL, NULL, 300, '2023-10-01 10:02:00', NULL);

-- Error and exception logging
CREATE TABLE pefindo.bk_error_logs (
    id BIGSERIAL PRIMARY KEY,
    correlation_id VARCHAR(50),
    request_id VARCHAR(50),
    log_level VARCHAR(20) NOT NULL, -- Error, Warning, Critical
    source VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    exception TEXT,
    stack_trace TEXT,
    user_id VARCHAR(50),
    additional_data TEXT, -- JSON
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bk_error_logs_correlation_id ON pefindo.bk_error_logs(correlation_id);
CREATE INDEX idx_bk_error_logs_level_time ON pefindo.bk_error_logs(log_level, created_at);

-- Example data for bk_error_logs
INSERT INTO pefindo.bk_error_logs (correlation_id, request_id, log_level, source, message, exception, stack_trace, user_id, additional_data)
VALUES
    ('corr-123', 'req-456', 'Error', 'CreditReportService', 'Failed to generate credit report', 'NullReferenceException', 'at CreditReportService.GenerateReport() in /src/Services/CreditReportService.cs:line 42', 'user-789', '{"requestId": "req-456"}'),
    ('corr-124', 'req-457', 'Warning', 'CreditScoreService', 'Credit score service took too long to respond', NULL, NULL, 'user-790', '{"requestId": "req-457"}'),
    ('corr-125', 'req-458', 'Critical', 'ReportGenerator', 'Unexpected error occurred', 'InvalidOperationException', 'at ReportGenerator.Generate() in /src/Services/ReportGenerator.cs:line 30', 'user-791', '{"requestId": "req-458"}');

-- Business audit trail (for compliance)
CREATE TABLE pefindo.bk_audit_logs (
    id BIGSERIAL PRIMARY KEY,
    correlation_id VARCHAR(50) NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    action VARCHAR(100) NOT NULL, -- 'CreditReportRequested', 'DataRetrieved'
    entity_type VARCHAR(50), -- 'Customer', 'CreditReport'
    entity_id VARCHAR(50),
    old_value TEXT,
    new_value TEXT,
    timestamp TIMESTAMP NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bk_audit_logs_user_time ON pefindo.bk_audit_logs(user_id, timestamp);
CREATE INDEX idx_bk_audit_logs_entity ON pefindo.bk_audit_logs(entity_type, entity_id);


-- Example data for bk_audit_logs
INSERT INTO pefindo.bk_audit_logs (correlation_id, user_id, action, entity_type, entity_id, old_value, new_value, timestamp, ip_address, user_agent)
VALUES
    ('corr-123', 'user-789', 'CreditReportRequested', 'Customer', 'cust-001', NULL, '{"creditScore": 750}', '2023-10-01 10:00:00', '192.168.1.1', 'Mozilla/5.0'),
    ('corr-124', 'user-790', 'DataRetrieved', 'CreditReport', 'report-001', '{"status": "Pending"}', '{"status": "Completed"}', '2023-10-01 10:01:00', '192.168.1.2', 'Mozilla/5.0'),
    ('corr-125', 'user-791', 'CreditReportRequested', 'Customer', 'cust-002', NULL, '{"creditScore": 680}', '2023-10-01 10:02:00', '192.168.1.3', 'Mozilla/5.0');

-- =====================================
