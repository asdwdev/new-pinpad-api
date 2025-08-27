using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using NewPinpadApi.Models;
using System.Globalization;
using System.Text;

namespace NewPinpadApi.Services
{
    public interface IExportService
    {
        byte[] ExportToExcel(IEnumerable<APIReqLog> data);
        byte[] ExportToPdf(IEnumerable<APIReqLog> data);
        byte[] ExportToDetailedPdf(IEnumerable<APIReqLog> data);
        byte[] ExportToCsv(IEnumerable<APIReqLog> data);
    }

    public class ExportService : IExportService
    {
        public byte[] ExportToExcel(IEnumerable<APIReqLog> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("API Request Logs");

            // Set column widths
            worksheet.Column(1).Width = 8;   // ID
            worksheet.Column(2).Width = 25;  // Proses
            worksheet.Column(3).Width = 30;  // Request
            worksheet.Column(4).Width = 30;  // Result
            worksheet.Column(5).Width = 12;  // StatusCode
            worksheet.Column(6).Width = 25;  // Remark
            worksheet.Column(7).Width = 20;  // ReqBy
            worksheet.Column(8).Width = 18;  // ReqDate
            worksheet.Column(9).Width = 12;  // Method
            worksheet.Column(10).Width = 25; // Endpoint
            worksheet.Column(11).Width = 15; // IpAddress
            worksheet.Column(12).Width = 15; // ResponseTime

            // Title
            var titleCell = worksheet.Cell(1, 1);
            titleCell.Value = "API REQUEST LOGS REPORT";
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontColor = XLColor.DarkGray;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, 12).Merge();

            // Subtitle with date
            var subtitleCell = worksheet.Cell(2, 1);
            subtitleCell.Value = $"Generated on: {DateTime.Now:dddd, dd MMMM yyyy 'at' HH:mm:ss}";
            subtitleCell.Style.Font.FontSize = 12;
            subtitleCell.Style.Font.FontColor = XLColor.Gray;
            subtitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(2, 1, 2, 12).Merge();

            // Summary info
            var totalRecords = data.Count();
            var successCount = data.Count(x => x.StatusCode == "200");
            var successRate = totalRecords > 0 ? (double)successCount / totalRecords * 100 : 0;

            var summaryCell1 = worksheet.Cell(3, 1);
            summaryCell1.Value = $"Total Records: {totalRecords:N0}";
            summaryCell1.Style.Font.FontSize = 10;
            summaryCell1.Style.Font.FontColor = XLColor.Gray;

            var summaryCell2 = worksheet.Cell(3, 12);
            summaryCell2.Value = $"Success Rate: {successRate:F1}%";
            summaryCell2.Style.Font.FontSize = 10;
            summaryCell2.Style.Font.FontColor = XLColor.Gray;
            summaryCell2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Header row (row 5)
            var headerRow = worksheet.Row(5);
            headerRow.Height = 25;

            // Header titles
            string[] headers = { "ID", "Process", "Request", "Result", "Status Code", "Remark", "Requested By", "Request Date", "Method", "Endpoint", "IP Address", "Response Time (ms)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = worksheet.Cell(5, i + 1);
                headerCell.Value = headers[i];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontSize = 11;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 73, 94); // Dark blue-gray
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                headerCell.Style.Border.BottomBorderColor = XLColor.White;
            }

            // Data rows with alternating colors
            var row = 6;
            foreach (var item in data)
            {
                // Alternate row colors
                var rowStyle = row % 2 == 0 ? XLColor.FromArgb(248, 249, 250) : XLColor.White;
                worksheet.Row(row).Style.Fill.BackgroundColor = rowStyle;

                // ID
                worksheet.Cell(row, 1).Value = item.Id;
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Process
                worksheet.Cell(row, 2).Value = item.Proses;

                // Request (truncated if too long)
                worksheet.Cell(row, 3).Value = TruncateText(item.Request ?? "", 50);

                // Result (truncated if too long)
                worksheet.Cell(row, 4).Value = TruncateText(item.Result ?? "", 50);

                // Status Code with color coding
                var statusCell = worksheet.Cell(row, 5);
                statusCell.Value = item.StatusCode ?? "-";
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                statusCell.Style.Font.Bold = true;

                // Color coding for status codes
                if (item.StatusCode == "200")
                    statusCell.Style.Font.FontColor = XLColor.FromArgb(39, 174, 96); // Green for success
                else if (item.StatusCode != null && item.StatusCode.StartsWith("4"))
                    statusCell.Style.Font.FontColor = XLColor.FromArgb(243, 156, 18); // Orange for client errors
                else if (item.StatusCode != null && item.StatusCode.StartsWith("5"))
                    statusCell.Style.Font.FontColor = XLColor.FromArgb(231, 76, 60); // Red for server errors

                // Remark
                worksheet.Cell(row, 6).Value = TruncateText(item.Remark ?? "-", 40);

                // Requested By
                worksheet.Cell(row, 7).Value = item.ReqBy ?? "-";

                // Request Date
                worksheet.Cell(row, 8).Value = item.ReqDate.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Method
                worksheet.Cell(row, 9).Value = item.Method ?? "-";
                worksheet.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Endpoint
                worksheet.Cell(row, 10).Value = TruncateText(item.Endpoint ?? "-", 40);

                // IP Address
                worksheet.Cell(row, 11).Value = item.IpAddress ?? "-";

                // Response Time
                worksheet.Cell(row, 12).Value = item.ResponseTime;
                worksheet.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add borders to all cells in the row
                for (int col = 1; col <= 12; col++)
                {
                    worksheet.Cell(row, col).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(row, col).Style.Border.BottomBorderColor = XLColor.LightGray;
                }

                row++;
            }

            // Auto-fit columns for better readability
            worksheet.Columns().AdjustToContents();

            // Add footer
            var footerRow = row + 1;
            var footerCell = worksheet.Cell(footerRow, 1);
            footerCell.Value = $"Total Records: {totalRecords:N0} | Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            footerCell.Style.Font.FontSize = 10;
            footerCell.Style.Font.FontColor = XLColor.Gray;
            footerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(footerRow, 1, footerRow, 12).Merge();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToPdf(IEnumerable<APIReqLog> data)
        {
            using var memoryStream = new MemoryStream();
            Document document = new Document(PageSize.A4.Rotate(), 10f, 10f, 10f, 10f);
            PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph("API Request Logs", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            document.Add(title);
            document.Add(new Paragraph(" ")); // Spacing

            // Create table
            PdfPTable table = new PdfPTable(8);
            table.WidthPercentage = 100;

            // Header
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            table.AddCell(new PdfPCell(new Phrase("ID", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Proses", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("StatusCode", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Remark", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("ReqBy", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("ReqDate", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Method", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("ResponseTime", headerFont)));

            // Data
            foreach (var item in data)
            {
                table.AddCell(new PdfPCell(new Phrase(item.Id.ToString(), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Proses, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.StatusCode ?? "", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Remark ?? "", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.ReqBy ?? "", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.ReqDate.ToString("yyyy-MM-dd HH:mm:ss"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Method ?? "", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.ResponseTime?.ToString() ?? "", cellFont)));
            }

            document.Add(table);
            document.Close();

            return memoryStream.ToArray();
        }

        public byte[] ExportToDetailedPdf(IEnumerable<APIReqLog> data)
        {
            using var memoryStream = new MemoryStream();
            Document document = new Document(PageSize.A4, 15f, 15f, 20f, 15f);
            PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Add custom fonts
            BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            Font titleFont = new Font(baseFont, 20, Font.BOLD, BaseColor.DARK_GRAY);
            Font subtitleFont = new Font(baseFont, 14, Font.NORMAL, BaseColor.GRAY);
            Font headerFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
            Font cellFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);
            Font smallFont = new Font(baseFont, 9, Font.NORMAL, BaseColor.GRAY);
            Font highlightFont = new Font(baseFont, 11, Font.BOLD, BaseColor.DARK_GRAY);

            // Header with company branding
            var headerTable = new PdfPTable(1);
            headerTable.WidthPercentage = 100;
            headerTable.DefaultCell.Border = Rectangle.NO_BORDER;
            headerTable.DefaultCell.PaddingBottom = 15f;

            // Company title
            var title = new Paragraph("API REQUEST LOGS DETAILED REPORT", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            headerTable.AddCell(new PdfPCell(title) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER });

            // Subtitle with date
            var subtitle = new Paragraph($"Generated on: {DateTime.Now:dddd, dd MMMM yyyy 'at' HH:mm:ss}", subtitleFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            headerTable.AddCell(new PdfPCell(subtitle) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER });

            document.Add(headerTable);

            // Executive Summary Section
            var totalRecords = data.Count();
            var successCount = data.Count(x => x.StatusCode == "200");
            var errorCount = totalRecords - successCount;
            var successRate = totalRecords > 0 ? (double)successCount / totalRecords * 100 : 0;

            var summaryTitle = new Paragraph("EXECUTIVE SUMMARY", highlightFont);
            summaryTitle.Alignment = Element.ALIGN_LEFT;
            summaryTitle.SpacingAfter = 10f;
            document.Add(summaryTitle);

            // Summary table
            var summaryTable = new PdfPTable(2);
            summaryTable.WidthPercentage = 100;
            summaryTable.DefaultCell.Padding = 8f;
            summaryTable.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
            summaryTable.DefaultCell.BorderColor = BaseColor.LIGHT_GRAY;

            // Summary data
            AddSummaryRow(summaryTable, "Total API Requests", $"{totalRecords:N0}", cellFont);
            AddSummaryRow(summaryTable, "Successful Requests (200)", $"{successCount:N0}", cellFont);
            AddSummaryRow(summaryTable, "Failed Requests", $"{errorCount:N0}", cellFont);
            AddSummaryRow(summaryTable, "Success Rate", $"{successRate:F1}%", cellFont);

            // Performance metrics
            var responseTimes = data.Where(x => x.ResponseTime.HasValue).Select(x => x.ResponseTime.Value).ToList();
            if (responseTimes.Any())
            {
                AddSummaryRow(summaryTable, "Average Response Time", $"{responseTimes.Average():F0} ms", cellFont);
                AddSummaryRow(summaryTable, "Min Response Time", $"{responseTimes.Min()} ms", cellFont);
                AddSummaryRow(summaryTable, "Max Response Time", $"{responseTimes.Max()} ms", cellFont);
            }

            document.Add(summaryTable);
            document.Add(new Paragraph(" "));

            // Top Processes Section
            var topProcesses = data.GroupBy(x => x.Proses)
                .Select(g => new { Process = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            if (topProcesses.Any())
            {
                var processTitle = new Paragraph("TOP 5 PROCESSES BY REQUEST COUNT", highlightFont);
                processTitle.Alignment = Element.ALIGN_LEFT;
                processTitle.SpacingAfter = 10f;
                document.Add(processTitle);

                var processTable = new PdfPTable(2);
                processTable.WidthPercentage = 100;
                processTable.DefaultCell.Padding = 6f;
                processTable.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                processTable.DefaultCell.BorderColor = BaseColor.LIGHT_GRAY;

                // Process table header
                var processHeaderCell = new PdfPCell();
                processHeaderCell.BackgroundColor = new BaseColor(52, 73, 94);
                processHeaderCell.Padding = 8f;
                processHeaderCell.Border = Rectangle.BOTTOM_BORDER;
                processHeaderCell.BorderColor = BaseColor.WHITE;
                processHeaderCell.BorderWidth = 2f;

                processHeaderCell.Phrase = new Phrase("Process Name", headerFont);
                processTable.AddCell(processHeaderCell);
                processHeaderCell.Phrase = new Phrase("Request Count", headerFont);
                processTable.AddCell(processHeaderCell);

                // Process data
                foreach (var process in topProcesses)
                {
                    processTable.AddCell(new PdfPCell(new Phrase(process.Process, cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY });
                    processTable.AddCell(new PdfPCell(new Phrase(process.Count.ToString("N0"), cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
                }

                document.Add(processTable);
                document.Add(new Paragraph(" "));
            }

            // Status Code Distribution
            var statusDistribution = data.GroupBy(x => x.StatusCode ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (statusDistribution.Any())
            {
                var statusTitle = new Paragraph("STATUS CODE DISTRIBUTION", highlightFont);
                statusTitle.Alignment = Element.ALIGN_LEFT;
                statusTitle.SpacingAfter = 10f;
                document.Add(statusTitle);

                var statusTable = new PdfPTable(3);
                statusTable.WidthPercentage = 100;
                statusTable.DefaultCell.Padding = 6f;
                statusTable.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                statusTable.DefaultCell.BorderColor = BaseColor.LIGHT_GRAY;

                // Status table header
                var statusHeaderCell = new PdfPCell();
                statusHeaderCell.BackgroundColor = new BaseColor(52, 73, 94);
                statusHeaderCell.Padding = 8f;
                statusHeaderCell.Border = Rectangle.BOTTOM_BORDER;
                statusHeaderCell.BorderColor = BaseColor.WHITE;
                statusHeaderCell.BorderWidth = 2f;

                statusHeaderCell.Phrase = new Phrase("Status Code", headerFont);
                statusTable.AddCell(statusHeaderCell);
                statusHeaderCell.Phrase = new Phrase("Count", headerFont);
                statusTable.AddCell(statusHeaderCell);
                statusHeaderCell.Phrase = new Phrase("Percentage", headerFont);
                statusTable.AddCell(statusHeaderCell);

                // Status data
                foreach (var status in statusDistribution)
                {
                    var percentage = totalRecords > 0 ? (double)status.Count / totalRecords * 100 : 0;
                    
                    statusTable.AddCell(new PdfPCell(new Phrase(status.Status, cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY });
                    statusTable.AddCell(new PdfPCell(new Phrase(status.Count.ToString("N0"), cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
                    statusTable.AddCell(new PdfPCell(new Phrase($"{percentage:F1}%", cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
                }

                document.Add(statusTable);
                document.Add(new Paragraph(" "));
            }

            // Detailed Data Section
            var dataTitle = new Paragraph("DETAILED REQUEST DATA", highlightFont);
            dataTitle.Alignment = Element.ALIGN_LEFT;
            dataTitle.SpacingAfter = 10f;
            document.Add(dataTitle);

            // Create detailed data table
            PdfPTable table = new PdfPTable(6);
            table.WidthPercentage = 100;
            table.DefaultCell.Padding = 5f;
            table.DefaultCell.MinimumHeight = 20f;

            // Set column widths
            float[] widths = { 0.15f, 0.25f, 0.15f, 0.20f, 0.15f, 0.10f };
            table.SetWidths(widths);

            // Table header with styling
            var headerCell = new PdfPCell();
            headerCell.BackgroundColor = new BaseColor(52, 73, 94);
            headerCell.Padding = 8f;
            headerCell.Border = Rectangle.BOTTOM_BORDER;
            headerCell.BorderColor = BaseColor.WHITE;
            headerCell.BorderWidth = 2f;

            // Add headers
            string[] headers = { "Process", "Status Code", "Requested By", "Date", "Method", "Response Time" };
            foreach (string header in headers)
            {
                headerCell.Phrase = new Phrase(header, headerFont);
                table.AddCell(headerCell);
            }

            // Data rows with alternating colors (limit to first 50 for detailed report)
            int rowCount = 0;
            foreach (var item in data.Take(50))
            {
                // Alternate row colors
                if (rowCount % 2 == 0)
                {
                    table.DefaultCell.BackgroundColor = new BaseColor(248, 249, 250);
                }
                else
                {
                    table.DefaultCell.BackgroundColor = BaseColor.WHITE;
                }

                // Status code color coding
                BaseColor statusColor = BaseColor.BLACK;
                if (item.StatusCode == "200")
                    statusColor = new BaseColor(39, 174, 96);
                else if (item.StatusCode != null && item.StatusCode.StartsWith("4"))
                    statusColor = new BaseColor(243, 156, 18);
                else if (item.StatusCode != null && item.StatusCode.StartsWith("5"))
                    statusColor = new BaseColor(231, 76, 60);

                // Add cells
                table.AddCell(new PdfPCell(new Phrase(item.Proses, cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY });
                
                var statusCell = new PdfPCell(new Phrase(item.StatusCode ?? "-", new Font(baseFont, 10, Font.BOLD, statusColor))) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER };
                table.AddCell(statusCell);

                table.AddCell(new PdfPCell(new Phrase(item.ReqBy ?? "-", cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY });
                
                var dateCell = new PdfPCell(new Phrase(item.ReqDate.ToString("dd/MM/yyyy\nHH:mm:ss"), cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER };
                table.AddCell(dateCell);

                var methodCell = new PdfPCell(new Phrase(item.Method ?? "-", cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER };
                table.AddCell(methodCell);

                var responseTimeCell = new PdfPCell(new Phrase(item.ResponseTime?.ToString() ?? "-", cellFont)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER };
                table.AddCell(responseTimeCell);

                rowCount++;
            }

            document.Add(table);

            // Note about data limitation
            if (data.Count() > 50)
            {
                var note = new Paragraph($"Note: Detailed data table shows first 50 records. Total records: {totalRecords:N0}", smallFont);
                note.Alignment = Element.ALIGN_CENTER;
                note.SpacingBefore = 10f;
                document.Add(note);
            }

            // Add footer with page info
            document.Add(new Paragraph(" "));
            var footer = new Paragraph($"Page {writer.PageNumber} - Total Records: {totalRecords:N0} | Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}", smallFont);
            footer.Alignment = Element.ALIGN_CENTER;
            document.Add(footer);

            document.Close();
            return memoryStream.ToArray();
        }

        private void AddSummaryRow(PdfPTable table, string label, string value, Font font)
        {
            table.AddCell(new PdfPCell(new Phrase(label, font)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(value, font)) { Border = Rectangle.BOTTOM_BORDER, BorderColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }

        public byte[] ExportToCsv(IEnumerable<APIReqLog> data)
        {
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("ID,Proses,Request,Result,StatusCode,Remark,ReqBy,ReqDate,Method,Endpoint,IpAddress,ResponseTime");

            // Data
            foreach (var item in data)
            {
                csv.AppendLine($"{item.Id}," +
                              $"\"{EscapeCsvField(item.Proses)}\"," +
                              $"\"{EscapeCsvField(item.Request)}\"," +
                              $"\"{EscapeCsvField(item.Result)}\"," +
                              $"\"{EscapeCsvField(item.StatusCode)}\"," +
                              $"\"{EscapeCsvField(item.Remark)}\"," +
                              $"\"{EscapeCsvField(item.ReqBy)}\"," +
                              $"\"{item.ReqDate:yyyy-MM-dd HH:mm:ss}\"," +
                              $"\"{EscapeCsvField(item.Method)}\"," +
                              $"\"{EscapeCsvField(item.Endpoint)}\"," +
                              $"\"{EscapeCsvField(item.IpAddress)}\"," +
                              $"{item.ResponseTime}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Replace double quotes with two double quotes and wrap in quotes if contains comma, quote, or newline
            field = field.Replace("\"", "\"\"");
            
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                field = $"\"{field}\"";
            }

            return field;
        }
    }
}
