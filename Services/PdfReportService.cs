using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.Services;

public class PdfReportService
{
    public byte[] GenerateReport(
        List<Transaction> transactions,
        string userName,
        DateTime? startDate,
        DateTime? endDate)
    {
        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpense;


        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.DefaultTextStyle(x =>
                    x.FontSize(10));


                // =================================================
                // Header
                // =================================================

                page.Header()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("Expense & Income Report")
                            .FontSize(22)
                            .Bold();

                        column.Item()
                            .Text($"User: {userName}")
                            .FontSize(11);

                        column.Item()
                            .Text(GetReportPeriod(
                                startDate,
                                endDate))
                            .FontSize(10);

                        column.Item()
                            .PaddingTop(10)
                            .LineHorizontal(1);
                    });


                // =================================================
                // Content
                // =================================================

                page.Content()
                    .PaddingVertical(15)
                    .Column(column =>
                    {
                        // -----------------------------------------
                        // Summary
                        // -----------------------------------------

                        column.Item()
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Border(1)
                                    .Padding(10)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text("Total Income")
                                            .Bold();

                                        c.Item()
                                            .Text(
                                                $"₹ {totalIncome:N2}")
                                            .FontSize(14)
                                            .Bold();
                                    });


                                row.ConstantItem(10);


                                row.RelativeItem()
                                    .Border(1)
                                    .Padding(10)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text("Total Expense")
                                            .Bold();

                                        c.Item()
                                            .Text(
                                                $"₹ {totalExpense:N2}")
                                            .FontSize(14)
                                            .Bold();
                                    });


                                row.ConstantItem(10);


                                row.RelativeItem()
                                    .Border(1)
                                    .Padding(10)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text("Balance")
                                            .Bold();

                                        c.Item()
                                            .Text(
                                                $"₹ {balance:N2}")
                                            .FontSize(14)
                                            .Bold();
                                    });
                            });


                        column.Item()
                            .PaddingTop(20);


                        // -----------------------------------------
                        // Transaction Count
                        // -----------------------------------------

                        column.Item()
                            .Text(
                                $"Total Transactions: {transactions.Count}")
                            .Bold();


                        column.Item()
                            .PaddingTop(10);


                        // -----------------------------------------
                        // Empty State
                        // -----------------------------------------

                        if (!transactions.Any())
                        {
                            column.Item()
                                .PaddingTop(30)
                                .AlignCenter()
                                .Text("No transactions found.")
                                .FontSize(14);
                        }
                        else
                        {
                            // -------------------------------------
                            // Transaction Table
                            // -------------------------------------

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(80);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(80);
                                    });


                                    // Header

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text("Date")
                                            .Bold();

                                        header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text("Type")
                                            .Bold();

                                        header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text("Category")
                                            .Bold();

                                        header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text("Description")
                                            .Bold();

                                        header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text("Amount")
                                            .Bold();
                                    });


                                    // Rows

                                    foreach (var transaction in transactions)
                                    {
                                        table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(
                                                transaction.TransactionDate
                                                    .ToString("dd MMM yyyy"));


                                        table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(
                                                transaction.Type.ToString());


                                        table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(
                                                transaction.Category?.Name
                                                ?? "Unknown");


                                        table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(
                                                string.IsNullOrWhiteSpace(
                                                    transaction.Description)
                                                    ? "-"
                                                    : transaction.Description);


                                        table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .AlignRight()
                                            .Text(
                                                $"₹ {transaction.Amount:N2}");
                                    }
                                });
                        }
                    });


                // =================================================
                // Footer
                // =================================================

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated on ");
                        text.Span(
                            DateTime.Now.ToString(
                                "dd MMM yyyy HH:mm"));
                    });
            });
        });


        return document.GeneratePdf();
    }


    private static string GetReportPeriod(
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return
                $"Period: {startDate.Value:dd MMM yyyy} - " +
                $"{endDate.Value:dd MMM yyyy}";
        }

        if (startDate.HasValue)
        {
            return
                $"Period: From {startDate.Value:dd MMM yyyy}";
        }

        if (endDate.HasValue)
        {
            return
                $"Period: Until {endDate.Value:dd MMM yyyy}";
        }

        return "Period: All Dates";
    }
}