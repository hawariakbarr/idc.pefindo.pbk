# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Prompt Rules

- Always maintain professional and precise communication
- Focus on code quality, readability, and maintainability
- Provide clear, concise, and actionable recommendations
- Respect existing architectural patterns and design principles
- Prioritize secure, efficient, and well-documented solutions

## Project Overview

This is the **IDC Pefindo PBK API** - a .NET 8 Web API that serves as middleware between Core Banking Decision Engine and Pefindo PBK credit bureau services. The API processes individual credit assessment requests through a comprehensive 9-step workflow including cycle day validation, token management, smart search, similarity validation, report generation, and data aggregation.

**Key Business Process**: `CYCLE_DAY_VALIDATION` → `GET_PEFINDO_TOKEN` → `SMART_SEARCH` → `SIMILARITY_CHECK_SEARCH` → `GENERATE_REPORT` → `SIMILARITY_CHECK_REPORT` → `STORE_REPORT_DATA` → `DOWNLOAD_PDF_REPORT` → `DATA_AGGREGATION`

[... rest of the existing content remains unchanged ...]