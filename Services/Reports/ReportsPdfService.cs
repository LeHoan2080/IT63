using System.Globalization;
using BookSalesCore.ViewModels.Customers;
using BookSalesCore.ViewModels.Reports;
using BookSalesCore.ViewModels.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BookSalesCore.Services.Reports;

public class ReportsPdfService : IReportsPdfService
{
    private static readonly CultureInfo VietnameseCulture =
        CultureInfo.GetCultureInfo("vi-VN");

    public byte[] GenerateBusinessReportPdf(ReportsIndexViewModel model)
    {
        var dateSummary = model.FromDate.HasValue && model.ToDate.HasValue
            ? $"Khoảng thời gian: {model.FromDate.Value:dd/MM/yyyy} - {model.ToDate.Value:dd/MM/yyyy}"
            : "Khoảng thời gian: Không xác định";

        return Document.Create(document => document.Page(page =>
        {
            ConfigurePage(page, "Báo cáo kinh doanh", dateSummary);

            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.Spacing(8);
                    SummaryCard(row.RelativeItem(), "Giá trị đơn đã xác nhận", FormatCurrency(model.TotalSales));
                    SummaryCard(row.RelativeItem(), "Đơn xác nhận theo ngày xác nhận", model.SalesConfirmedOrderCount.ToString("N0", VietnameseCulture));
                    SummaryCard(row.RelativeItem(), "Tổng chứng từ", model.TotalOrderCount.ToString("N0", VietnameseCulture));
                    SummaryCard(row.RelativeItem(), "Đơn đã hủy", model.CancelledCount.ToString("N0", VietnameseCulture));
                });

                column.Item().Text("Số chứng từ theo trạng thái").FontSize(11).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(110);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Trạng thái");
                        HeaderCell(header.Cell(), "Số chứng từ", true);
                    });

                    BodyCell(table.Cell(), "Nháp (Draft)");
                    BodyCell(table.Cell(), model.DraftCount.ToString("N0", VietnameseCulture), true);
                    BodyCell(table.Cell(), "Đã gửi (Sent)");
                    BodyCell(table.Cell(), model.SentCount.ToString("N0", VietnameseCulture), true);
                    BodyCell(table.Cell(), "Đã xác nhận (Confirmed)");
                    BodyCell(table.Cell(), model.ConfirmedOrderCount.ToString("N0", VietnameseCulture), true);
                    BodyCell(table.Cell(), "Đã hủy (Cancelled)");
                    BodyCell(table.Cell(), model.CancelledCount.ToString("N0", VietnameseCulture), true);
                });

                column.Item().Text("Doanh số theo ngày xác nhận").FontSize(11).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Ngày xác nhận");
                        HeaderCell(header.Cell(), "Số đơn", true);
                        HeaderCell(header.Cell(), "Tổng giá trị", true);
                    });

                    foreach (var item in model.DailySales)
                    {
                        BodyCell(table.Cell(), item.Date.ToString("dd/MM/yyyy"));
                        BodyCell(table.Cell(), item.OrderCount.ToString("N0", VietnameseCulture), true);
                        BodyCell(table.Cell(), FormatCurrency(item.TotalSales), true);
                    }

                    table.Footer(footer =>
                    {
                        FooterCell(footer.Cell(), "Tổng cộng");
                        FooterCell(footer.Cell(), model.DailySales.Sum(item => item.OrderCount).ToString("N0", VietnameseCulture), true);
                        FooterCell(footer.Cell(), FormatCurrency(model.TotalSales), true);
                    });
                });

                column.Item().Text("Top 10 sách bán chạy").FontSize(11).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(38);
                        columns.ConstantColumn(90);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Hạng");
                        HeaderCell(header.Cell(), "Mã sách");
                        HeaderCell(header.Cell(), "Tên sách");
                        HeaderCell(header.Cell(), "Số lượng", true);
                        HeaderCell(header.Cell(), "Giá trị", true);
                    });

                    for (var index = 0; index < model.TopBooks.Count; index++)
                    {
                        var book = model.TopBooks[index];
                        BodyCell(table.Cell(), (index + 1).ToString(VietnameseCulture));
                        BodyCell(table.Cell(), book.SKU);
                        BodyCell(table.Cell(), book.BookTitle);
                        BodyCell(table.Cell(), book.TotalQuantity.ToString("N0", VietnameseCulture), true);
                        BodyCell(table.Cell(), FormatCurrency(book.TotalSales), true);
                    }
                });

                column.Item().Text(
                    "Số chứng từ theo trạng thái được lọc theo ngày lập; doanh số và số đơn xác nhận được lọc theo ngày xác nhận. Giá trị chưa bao gồm thuế, chiết khấu, thanh toán hoặc công nợ.")
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken1);
            });

            AddFooter(page.Footer());
        })).GeneratePdf();
    }

    public byte[] GenerateSalesOrdersPdf(SalesOrderListViewModel model)
    {
        var filterSummary = BuildSalesFilterSummary(model);
        var totalAmount = model.SalesOrders.Sum(order => order.TotalAmount);

        return Document.Create(document => document.Page(page =>
        {
            ConfigurePage(page, "Báo cáo chứng từ bán hàng", filterSummary);

            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.Spacing(8);
                    SummaryCard(row.RelativeItem(), "Số chứng từ", model.TotalItems.ToString("N0", VietnameseCulture));
                    SummaryCard(row.RelativeItem(), "Tổng giá trị danh sách", FormatCurrency(totalAmount));
                    SummaryCard(row.RelativeItem(), "Đã xác nhận", model.SalesOrders.Count(order => order.Status == "Confirmed").ToString("N0", VietnameseCulture));
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2.4f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Số chứng từ");
                        HeaderCell(header.Cell(), "Ngày lập");
                        HeaderCell(header.Cell(), "Khách hàng");
                        HeaderCell(header.Cell(), "Trạng thái");
                        HeaderCell(header.Cell(), "Tổng tiền", true);
                    });

                    foreach (var order in model.SalesOrders)
                    {
                        BodyCell(table.Cell(), order.OrderNumber);
                        BodyCell(table.Cell(), order.OrderDate.ToString("dd/MM/yyyy HH:mm"));
                        BodyCell(table.Cell(), $"{order.CustomerNameSnapshot}\n{order.Customer.CustomerCode}");
                        BodyCell(table.Cell(), GetStatusLabel(order.Status));
                        BodyCell(table.Cell(), FormatCurrency(order.TotalAmount), true);
                    }

                    table.Footer(footer =>
                    {
                        footer.Cell().ColumnSpan(4).Element(container => FooterCell(container, "Tổng cộng"));
                        FooterCell(footer.Cell(), FormatCurrency(totalAmount), true);
                    });
                });
            });

            AddFooter(page.Footer());
        })).GeneratePdf();
    }

    public byte[] GenerateCustomersPdf(CustomerListViewModel model)
    {
        var filterSummary = BuildCustomerFilterSummary(model);

        return Document.Create(document => document.Page(page =>
        {
            ConfigurePage(page, "Danh bạ khách hàng", filterSummary);

            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.Spacing(8);
                    SummaryCard(row.RelativeItem(), "Khách hàng trong kết quả", model.TotalItems.ToString("N0", VietnameseCulture));
                    SummaryCard(row.RelativeItem(), "Đang hoạt động", model.Customers.Count(customer => customer.IsActive).ToString("N0", VietnameseCulture));
                    SummaryCard(row.RelativeItem(), "Ngừng hoạt động", model.Customers.Count(customer => !customer.IsActive).ToString("N0", VietnameseCulture));
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.3f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Mã khách hàng");
                        HeaderCell(header.Cell(), "Tên khách hàng");
                        HeaderCell(header.Cell(), "Số điện thoại");
                        HeaderCell(header.Cell(), "Email");
                        HeaderCell(header.Cell(), "Trạng thái");
                    });

                    foreach (var customer in model.Customers)
                    {
                        BodyCell(table.Cell(), customer.CustomerCode);
                        BodyCell(table.Cell(), customer.CustomerName);
                        BodyCell(table.Cell(), customer.Phone ?? "-");
                        BodyCell(table.Cell(), customer.Email ?? "-");
                        BodyCell(table.Cell(), customer.IsActive ? "Đang hoạt động" : "Ngừng hoạt động");
                    }
                });
            });

            AddFooter(page.Footer());
        })).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page, string title, string filterSummary)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(24);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(8));
        page.Header().Column(column =>
        {
            column.Item().Text(title)
                .FontSize(18)
                .Bold()
                .FontColor(Colors.BlueGrey.Darken3);
            column.Item().Text($"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}")
                .FontSize(8)
                .FontColor(Colors.Grey.Darken1);
            column.Item().Text(filterSummary)
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
            column.Item().PaddingTop(5).LineHorizontal(1)
                .LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void SummaryCard(IContainer container, string label, string value)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text(label)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
                column.Item().Text(value)
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.BlueGrey.Darken3);
            });
    }

    private static void HeaderCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(5)
            .PaddingHorizontal(4);

        var descriptor = alignRight
            ? cell.AlignRight().Text(text)
            : cell.Text(text);

        descriptor.FontSize(7).Bold().FontColor(Colors.Grey.Darken3);
    }

    private static void BodyCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .AlignMiddle();

        var descriptor = alignRight
            ? cell.AlignRight().Text(text)
            : cell.Text(text);

        descriptor.FontSize(7);
    }

    private static void FooterCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(5)
            .PaddingHorizontal(4);

        var descriptor = alignRight
            ? cell.AlignRight().Text(text)
            : cell.Text(text);

        descriptor.FontSize(7).Bold();
    }

    private static void AddFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .AlignCenter()
            .Text(text =>
            {
                text.Span("Trang ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
    }

    private static string BuildSalesFilterSummary(SalesOrderListViewModel model)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            filters.Add($"Từ khóa: {model.SearchTerm}");
        }

        if (!string.IsNullOrWhiteSpace(model.Status))
        {
            filters.Add($"Trạng thái: {GetStatusLabel(model.Status)}");
        }

        if (model.FromDate.HasValue)
        {
            filters.Add($"Từ ngày: {model.FromDate.Value:dd/MM/yyyy}");
        }

        if (model.ToDate.HasValue)
        {
            filters.Add($"Đến ngày: {model.ToDate.Value:dd/MM/yyyy}");
        }

        if (model.CustomerId.HasValue)
        {
            filters.Add($"Mã khách hàng: {model.CustomerId.Value}");
        }

        return filters.Count == 0
            ? "Bộ lọc: Tất cả chứng từ"
            : $"Bộ lọc: {string.Join(" | ", filters)}";
    }

    private static string BuildCustomerFilterSummary(CustomerListViewModel model)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            filters.Add($"Từ khóa: {model.SearchTerm}");
        }

        if (model.Status == "active")
        {
            filters.Add("Trạng thái: Đang hoạt động");
        }
        else if (model.Status == "inactive")
        {
            filters.Add("Trạng thái: Ngừng hoạt động");
        }

        return filters.Count == 0
            ? "Bộ lọc: Tất cả khách hàng"
            : $"Bộ lọc: {string.Join(" | ", filters)}";
    }

    private static string GetStatusLabel(string status) => status switch
    {
        "Draft" => "Bản nháp",
        "Sent" => "Đã gửi",
        "Confirmed" => "Đã xác nhận",
        "Cancelled" => "Đã hủy",
        _ => status
    };

    private static string FormatCurrency(decimal value) =>
        $"{value.ToString("N0", VietnameseCulture)} ₫";
}
