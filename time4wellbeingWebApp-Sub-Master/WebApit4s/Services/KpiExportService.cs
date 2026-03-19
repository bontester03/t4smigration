using ClosedXML.Excel;
using WebApit4s.Models;
using System.IO;

namespace WebApit4s.Services
{
    public class KpiExportService
    {
        public byte[] GenerateKpiExcelReport(List<KpiReport> kpis, int year)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("KPI Report");

            int currentRow = 1;

            // Title - Merged across all columns
            var titleCell = worksheet.Range(currentRow, 1, currentRow, 9).Merge();
            titleCell.Value = $"Family Weight Management Service KPI & Service Levels – Performance Report Year 3: {year - 1}–{year}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            currentRow += 2;

            // Section 1: Key Performance Indicators
            worksheet.Cell(currentRow, 1).Value = "Key Performance Indicators";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
            currentRow++;

            // Headers
            string[] headers = { "Contract Ref", "Measure", "Target", "Information Required", "Previous Year", "Q1", "Q2", "Q3", "Q4" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.WrapText = true;
            }
            currentRow++;

            // KPI Data Rows
            foreach (var kpi in kpis.OrderBy(k => k.ContractRef))
            {
                worksheet.Cell(currentRow, 1).Value = kpi.ContractRef;
                worksheet.Cell(currentRow, 2).Value = kpi.Measure;
                worksheet.Cell(currentRow, 2).Style.Alignment.WrapText = true;
                worksheet.Cell(currentRow, 3).Value = kpi.Target ?? "N/A";
                worksheet.Cell(currentRow, 4).Value = kpi.InformationRequired;
                worksheet.Cell(currentRow, 4).Style.Alignment.WrapText = true;
                worksheet.Cell(currentRow, 5).Value = kpi.PreviousYear?.ToString() ?? "-";
                worksheet.Cell(currentRow, 6).Value = kpi.Q1?.ToString() ?? "-";
                worksheet.Cell(currentRow, 7).Value = kpi.Q2?.ToString() ?? "-";
                worksheet.Cell(currentRow, 8).Value = kpi.Q3?.ToString() ?? "-";
                worksheet.Cell(currentRow, 9).Value = kpi.Q4?.ToString() ?? "-";

                // Apply borders
                for (int col = 1; col <= 9; col++)
                {
                    worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;
            }

            currentRow += 2;

            // Section 2: Service Levels
            worksheet.Cell(currentRow, 1).Value = "Service Levels";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
            currentRow++;

            // Service Level Headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.WrapText = true;
            }
            currentRow++;

            // Service Level Data (604 CYP supported)
            int totalChildren = kpis.FirstOrDefault()?.Q1 ?? 0;
            worksheet.Cell(currentRow, 1).Value = "SL 1";
            worksheet.Cell(currentRow, 2).Value = "Number of CYP supported";
            worksheet.Cell(currentRow, 3).Value = "604";
            worksheet.Cell(currentRow, 4).Value = "Total children and young people supported by the service";
            worksheet.Cell(currentRow, 5).Value = totalChildren.ToString();
            worksheet.Cell(currentRow, 6).Value = "?";
            worksheet.Cell(currentRow, 7).Value = "?";
            worksheet.Cell(currentRow, 8).Value = "?";
            worksheet.Cell(currentRow, 9).Value = "?";

            for (int col = 1; col <= 9; col++)
            {
                worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            currentRow += 2;

            // Section 3: Checklist
            worksheet.Cell(currentRow, 1).Value = "Checklist";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
            currentRow++;

            // Checklist Headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            currentRow++;

            // Checklist items
            string[] checklistItems = {
                "Quarterly monitoring reports submitted",
                "Annual service review completed",
                "Safeguarding protocols followed"
            };

            foreach (var item in checklistItems)
            {
                worksheet.Cell(currentRow, 1).Value = "";
                worksheet.Cell(currentRow, 2).Value = item;
                worksheet.Cell(currentRow, 3).Value = "";
                worksheet.Cell(currentRow, 4).Value = "";
                worksheet.Cell(currentRow, 5).Value = "";
                worksheet.Cell(currentRow, 6).Value = "?";
                worksheet.Cell(currentRow, 7).Value = "?";
                worksheet.Cell(currentRow, 8).Value = "?";
                worksheet.Cell(currentRow, 9).Value = "?";

                for (int col = 1; col <= 9; col++)
                {
                    worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            worksheet.Column(2).Width = 40; // Measure column wider
            worksheet.Column(4).Width = 50; // Information Required wider

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static string CalculatePercentage(int? numerator, int? denominator)
        {
            if (!numerator.HasValue || !denominator.HasValue || denominator == 0)
                return "-";

            var percentage = (numerator.Value / (double)denominator.Value) * 100;
            return $"{percentage:F1}%";
        }
    }
}
