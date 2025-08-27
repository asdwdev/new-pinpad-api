# Enhanced PDF Export Features

## Overview
The API Request Logs system now includes enhanced PDF export capabilities with professional styling, color coding, and detailed analytics reports.

## Available Export Formats

### 1. Standard PDF Export (`/api/APIReqLog/export/pdf`)
- **Features:**
  - Professional header with company branding
  - Color-coded status codes (Green for 200, Orange for 4xx, Red for 5xx)
  - Alternating row colors for better readability
  - Summary statistics (Total records, Success rate)
  - Formatted date and time display
  - Professional table styling with borders and spacing

### 2. Detailed PDF Export (`/api/APIReqLog/export/pdf/detailed`)
- **Features:**
  - Executive summary with key metrics
  - Performance analytics (Response time statistics)
  - Top 5 processes by request count
  - Status code distribution with percentages
  - Detailed data table (limited to first 50 records for readability)
  - Professional layout with section headers
  - Comprehensive statistics and insights

### 3. Enhanced Excel Export (`/api/APIReqLog/export/excel`)
- **Features:**
  - Professional header and title
  - Color-coded status codes
  - Alternating row colors
  - Summary statistics
  - Optimized column widths
  - Professional formatting and borders

## Design Features

### Color Scheme
- **Header Background:** Dark Blue-Gray (#34495E)
- **Success Status:** Green (#27AE60)
- **Client Errors (4xx):** Orange (#F39C12)
- **Server Errors (5xx):** Red (#E74C3C)
- **Alternating Rows:** Light Gray (#F8F9FA) and White

### Typography
- **Title Font:** 18-20pt Bold Dark Gray
- **Subtitle Font:** 12-14pt Normal Gray
- **Header Font:** 10-12pt Bold White
- **Data Font:** 9-10pt Normal Black
- **Footer Font:** 8-9pt Normal Gray

### Layout
- **Page Orientation:** Landscape for standard PDF, Portrait for detailed PDF
- **Margins:** Optimized for professional printing
- **Spacing:** Consistent padding and margins throughout
- **Borders:** Subtle borders with professional styling

## API Endpoints

```http
GET /api/APIReqLog/export/pdf?{filter_params}
GET /api/APIReqLog/export/pdf/detailed?{filter_params}
GET /api/APIReqLog/export/excel?{filter_params}
GET /api/APIReqLog/export/csv?{filter_params}
```

## Filter Parameters
All export endpoints support the same filtering options:
- `startDate`: Filter by start date
- `endDate`: Filter by end date
- `search`: General search across multiple fields
- `proses`: Filter by specific process
- `reqBy`: Filter by requester
- `statusCode`: Filter by status code
- `page`: Page number (for pagination)
- `pageSize`: Page size (for pagination)

## Technical Implementation

### Dependencies
- **iTextSharp:** PDF generation and styling
- **ClosedXML:** Excel file generation and formatting
- **System.Text:** CSV generation

### Key Methods
- `ExportToPdf()`: Standard PDF export with enhanced styling
- `ExportToDetailedPdf()`: Comprehensive PDF report with analytics
- `ExportToExcel()`: Enhanced Excel export with professional formatting
- `ExportToCsv()`: Standard CSV export

### Helper Methods
- `TruncateText()`: Text truncation for long content
- `AddSummaryRow()`: Consistent summary table row creation

## Usage Examples

### Basic PDF Export
```csharp
var result = await _exportService.ExportToPdf(logData);
return File(result, "application/pdf", "API_Logs.pdf");
```

### Detailed PDF Export
```csharp
var result = await _exportService.ExportToDetailedPdf(logData);
return File(result, "application/pdf", "API_Logs_Detailed.pdf");
```

### Excel Export
```csharp
var result = await _exportService.ExportToExcel(logData);
return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "API_Logs.xlsx");
```

## Benefits

1. **Professional Appearance:** Corporate-ready reports with consistent branding
2. **Better Readability:** Color coding and alternating row colors
3. **Comprehensive Analytics:** Detailed insights in the detailed PDF format
4. **Consistent Formatting:** Standardized styling across all export formats
5. **Performance Optimized:** Efficient data processing and export generation
6. **Filter Support:** All export formats support the same filtering capabilities

## Future Enhancements

- Chart generation for visual analytics
- Custom branding and logo support
- Multi-language support
- Template customization options
- Batch export capabilities
- Email integration for automated report delivery
