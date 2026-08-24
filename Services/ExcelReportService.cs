using ClosedXML.Excel;
using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.Services;

public class ExcelReportService
{
    public byte[] GenerateReport(
        List<Transaction> transactions,
        string userName,
        DateTime? startDate,
        DateTime? endDate)
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Transactions");


        // =====================================================
        // Title
        // =====================================================

        worksheet.Cell("A1").Value = "Expense & Income Report";

        worksheet.Range("A1:E1").Merge();

        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 18;

        worksheet.Cell("A1").Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;


        // =====================================================
        // User
        // =====================================================

        worksheet.Cell("A3").Value = "User";
        worksheet.Cell("B3").Value = userName;

        worksheet.Cell("A3").Style.Font.Bold = true;


        // =====================================================
        // Report Period
        // =====================================================

        worksheet.Cell("A4").Value = "Report Period";

        string period;

        if (startDate.HasValue && endDate.HasValue)
        {
            period =
                $"{startDate.Value:dd MMM yyyy} - " +
                $"{endDate.Value:dd MMM yyyy}";
        }
        else if (startDate.HasValue)
        {
            period =
                $"From {startDate.Value:dd MMM yyyy}";
        }
        else if (endDate.HasValue)
        {
            period =
                $"Until {endDate.Value:dd MMM yyyy}";
        }
        else
        {
            period = "All Dates";
        }

        worksheet.Cell("B4").Value = period;

        worksheet.Cell("A4").Style.Font.Bold = true;


        // =====================================================
        // Summary
        // =====================================================

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpense;


        worksheet.Cell("A6").Value = "Total Income";
        worksheet.Cell("B6").Value = totalIncome;

        worksheet.Cell("A7").Value = "Total Expense";
        worksheet.Cell("B7").Value = totalExpense;

        worksheet.Cell("A8").Value = "Balance";
        worksheet.Cell("B8").Value = balance;


        worksheet.Range("A6:A8")
            .Style.Font.Bold = true;


        worksheet.Range("B6:B8")
            .Style.NumberFormat.Format = "₹#,##0.00";


        // =====================================================
        // Transaction Table
        // =====================================================

        var tableStartRow = 10;

        worksheet.Cell(tableStartRow, 1).Value = "Date";
        worksheet.Cell(tableStartRow, 2).Value = "Type";
        worksheet.Cell(tableStartRow, 3).Value = "Category";
        worksheet.Cell(tableStartRow, 4).Value = "Description";
        worksheet.Cell(tableStartRow, 5).Value = "Amount";


        var headerRange = worksheet.Range(
            tableStartRow,
            1,
            tableStartRow,
            5);

        headerRange.Style.Font.Bold = true;

        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightGray;


        // =====================================================
        // Transaction Rows
        // =====================================================

        var row = tableStartRow + 1;

        foreach (var transaction in transactions)
        {
            worksheet.Cell(row, 1)
                .Value = transaction.TransactionDate;

            worksheet.Cell(row, 1)
                .Style.DateFormat.Format = "dd MMM yyyy";


            worksheet.Cell(row, 2)
                .Value = transaction.Type.ToString();


            worksheet.Cell(row, 3)
                .Value = transaction.Category?.Name ?? "Unknown";


            worksheet.Cell(row, 4)
                .Value = transaction.Description ?? "";


            worksheet.Cell(row, 5)
                .Value = transaction.Amount;

            worksheet.Cell(row, 5)
                .Style.NumberFormat.Format = "₹#,##0.00";


            row++;
        }


        // =====================================================
        // Table
        // =====================================================

        if (row > tableStartRow + 1)
        {
            var tableRange = worksheet.Range(
                tableStartRow,
                1,
                row - 1,
                5);

            tableRange.CreateTable("TransactionsTable");
        }


        // =====================================================
        // Column Width
        // =====================================================

        worksheet.Column(1).Width = 16;
        worksheet.Column(2).Width = 14;
        worksheet.Column(3).Width = 20;
        worksheet.Column(4).Width = 40;
        worksheet.Column(5).Width = 18;


        // =====================================================
        // Freeze Header
        // =====================================================

        worksheet.SheetView.FreezeRows(tableStartRow);


        // =====================================================
        // Generate Excel File
        // =====================================================

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}