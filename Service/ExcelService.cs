using ClosedXML.Excel;
using System.Data;

namespace NewPinpadApi.Services
{
    public interface IExcelService
    {
        DataTable ReadExcelToDataTable(Stream excelStream);
        Dictionary<string, string> BuildHeaderMap(IEnumerable<string> headers);
        string GetCellValue(DataRow row, Dictionary<string, string> headerMap, string standardKey);
        string MapRemarkToStatus(string remark);
        byte[] CreateExcelTemplate();
    }

    public class ExcelService : IExcelService
    {
        private const bool FIRST_ROW_IS_HEADER = true;

        public DataTable ReadExcelToDataTable(Stream excelStream)
        {
            try
            {
                // Aman untuk stream non-seekable & mencegah file lock
                using var ms = new MemoryStream();
                excelStream.CopyTo(ms);
                ms.Position = 0;

                using var workbook = new XLWorkbook(ms);
                var worksheet = workbook.Worksheet(1);
                var range = worksheet.RangeUsed();

                var dt = new DataTable();
                if (range == null) return dt;

                var rows = range.RowsUsed().ToList();
                if (!rows.Any()) return dt;

                var firstRow = rows.First();
                var headerCells = firstRow.CellsUsed().ToList();
                int colCount = headerCells.Count;

                // ==== Buat kolom ====
                if (FIRST_ROW_IS_HEADER)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in headerCells)
                    {
                        // pakai formatted utk konsistensi tampilan
                        var raw = c.GetFormattedString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(raw)) raw = "Column";

                        // hindari duplikat header
                        var name = raw;
                        int i = 2;
                        while (seen.Contains(name))
                            name = $"{raw}_{i++}";
                        seen.Add(name);

                        dt.Columns.Add(name);
                    }
                }
                else
                {
                    for (int i = 1; i <= colCount; i++)
                        dt.Columns.Add($"Column{i}");
                }

                // ==== Isi baris ====
                var dataRows = FIRST_ROW_IS_HEADER ? rows.Skip(1) : rows;
                foreach (var r in dataRows)
                {
                    var dr = dt.NewRow();
                    for (int i = 0; i < colCount; i++)
                    {
                        // pakai formatted supaya 0510 & tanggal aman
                        dr[i] = r.Cell(i + 1).GetFormattedString() ?? "";
                    }
                    dt.Rows.Add(dr);
                }

                // ==== Buang baris kosong ====
                var toRemove = new List<DataRow>();
                foreach (DataRow rr in dt.Rows)
                {
                    bool empty = dt.Columns.Cast<DataColumn>()
                        .All(c => string.IsNullOrWhiteSpace(rr[c]?.ToString()));
                    if (empty) toRemove.Add(rr);
                }
                foreach (var rr in toRemove) dt.Rows.Remove(rr);
                dt.AcceptChanges();

                return dt;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal membaca file Excel: {ex.Message}", ex);
            }
        }

        public Dictionary<string, string> BuildHeaderMap(IEnumerable<string> headers)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;

                var key = header.Trim()
                                .Replace(" ", "_")
                                .Replace("-", "_")
                                .ToUpperInvariant();

                // SERIAL_NUMBER
                if (key is "SERIAL_NUMBER" or "SERIALNUMBER" or "SN")
                    map["SERIAL_NUMBER"] = header;

                // KODE_OUTLET
                else if (key is "KODE_OUTLET" or "OUTLET" or "CABANG_OUTLET" or "CODE")
                    map["KODE_OUTLET"] = header;

                // REMARK
                else if (key is "REMARK" or "KETERANGAN" or "CATATAN")
                    map["REMARK"] = header;
            }
            return map;
        }

        public string GetCellValue(DataRow row, Dictionary<string, string> headerMap, string standardKey)
        {
            if (!headerMap.TryGetValue(standardKey, out var actual)) return "";
            return row[actual]?.ToString()?.Trim() ?? "";
        }

        public string MapRemarkToStatus(string remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
                return "Data tidak valid";

            var s = remark.Trim().ToLowerInvariant();

            // "Data Sesuai" - paling spesifik dulu
            if (s.Contains("data sesuai"))
                return "Data Sesuai";

            // "SN Sudah Terdaftar, Outlet Terdaftar" 
            if (s.Contains("sudah terdaftar") && s.Contains("sn") && s.Contains("outlet terdaftar"))
                return "SN sudah terdaftar";

            // "SN Belum Terdaftar, Outlet Terdaftar"
            if (s.Contains("belum terdaftar") && s.Contains("sn") && s.Contains("outlet terdaftar"))
                return "Data Sesuai";

            // "SN Belum Terdaftar, Outlet Tidak Terdaftar"
            if (s.Contains("belum terdaftar") && s.Contains("sn") && s.Contains("outlet tidak terdaftar"))
                return "Outlet tidak terdaftar";

            // "SN sudah terdaftar" (tanpa detail outlet)
            if (s.Contains("sudah terdaftar") && s.Contains("sn"))
                return "SN sudah terdaftar";

            // "Outlet tidak terdaftar" (tanpa detail SN)
            if (s.Contains("outlet") && s.Contains("tidak terdaftar"))
                return "Outlet tidak terdaftar";

            // "SN Belum Terdaftar" (tanpa detail outlet)
            if (s.Contains("belum terdaftar") && s.Contains("sn"))
                return "SN belum terdaftar";

            // Jika tidak ada yang cocok, return default
            return "Data tidak valid";
        }

        public byte[] CreateExcelTemplate()
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Template");

                // Add headers dengan styling
                worksheet.Cell("A1").Value = "SERIAL_NUMBER";
                worksheet.Cell("B1").Value = "KODE_OUTLET";
                worksheet.Cell("C1").Value = "REMARK";
                worksheet.Cell("D1").Value = "KETERANGAN";

                // Style headers
                var headerRange = worksheet.Range("A1:D1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
                headerRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                // Add sample data dengan berbagai skenario
                var sampleData = new[]
                {
                    new { SN = "277115", Outlet = "1302", Remark = "SN Belum Terdaftar, Outlet Tidak Terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "297867", Outlet = "1301", Remark = "SN Sudah Terdaftar, Outlet Terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "298717", Outlet = "1302", Remark = "SN Belum Terdaftar, Outlet Tidak Terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "277114", Outlet = "0510", Remark = "Data Sesuai", Keterangan = "Data TIDAK masuk ke DB" },
                    new { SN = "277116", Outlet = "0043", Remark = "Data Sesuai", Keterangan = "Data TIDAK masuk ke DB" },
                    new { SN = "299999", Outlet = "9999", Remark = "SN Belum Terdaftar, Outlet Terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "300000", Outlet = "8888", Remark = "Outlet tidak terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "300001", Outlet = "7777", Remark = "SN Belum Terdaftar", Keterangan = "Data akan masuk ke DB" },
                    new { SN = "300002", Outlet = "6666", Remark = "SN sudah terdaftar", Keterangan = "Data akan masuk ke DB" }
                };

                // Fill sample data
                for (int i = 0; i < sampleData.Length; i++)
                {
                    var row = i + 2; // Start from row 2
                    worksheet.Cell($"A{row}").Value = sampleData[i].SN;
                    worksheet.Cell($"B{row}").Value = sampleData[i].Outlet;
                    worksheet.Cell($"C{row}").Value = sampleData[i].Remark;
                    worksheet.Cell($"D{row}").Value = sampleData[i].Keterangan;
                }

                // Add data validation untuk kolom REMARK
                var remarkColumn = worksheet.Range("C2:C100");
                var validation = remarkColumn.CreateDataValidation();
                validation.List("SN Belum Terdaftar, Outlet Tidak Terdaftar,SN Sudah Terdaftar, Outlet Terdaftar,SN Belum Terdaftar, Outlet Terdaftar,Data Sesuai,Outlet tidak terdaftar,SN Belum Terdaftar,SN sudah terdaftar", true);

                // Add conditional formatting untuk status
                var statusColumn = worksheet.Range("C:C");
                var cf = statusColumn.AddConditionalFormat();
                cf.WhenContains("Data Sesuai").Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.LightGreen);
                cf.WhenContains("SN sudah terdaftar").Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.LightYellow);
                cf.WhenContains("Outlet tidak terdaftar").Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.LightCoral);

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Set column widths
                worksheet.Column("A").Width = 15; // SERIAL_NUMBER
                worksheet.Column("B").Width = 12; // KODE_OUTLET
                worksheet.Column("C").Width = 40; // REMARK
                worksheet.Column("D").Width = 25; // KETERANGAN

                // Add instructions
                worksheet.Cell("A10").Value = "INSTRUKSI:";
                worksheet.Cell("A10").Style.Font.Bold = true;
                worksheet.Cell("A10").Style.Font.FontSize = 12;

                worksheet.Cell("A11").Value = "1. SERIAL_NUMBER: Masukkan Serial Number Pinpad";
                worksheet.Cell("A12").Value = "2. KODE_OUTLET: Masukkan kode outlet/cabang";
                worksheet.Cell("A13").Value = "3. REMARK: Pilih dari dropdown atau ketik manual";
                worksheet.Cell("A14").Value = "4. Data dengan status 'Data Sesuai' TIDAK akan masuk ke database";
                worksheet.Cell("A15").Value = "5. Data dengan status lain AKAN masuk ke database dengan status 'NotReady'";

                // Style instructions
                var instructionRange = worksheet.Range("A11:A15");
                instructionRange.Style.Font.FontSize = 10;
                instructionRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Create memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal membuat template Excel: {ex.Message}", ex);
            }
        }
    }
}
