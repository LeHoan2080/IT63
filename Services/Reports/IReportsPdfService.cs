using BookSalesCore.ViewModels.Reports;
using BookSalesCore.ViewModels.Sales;
using BookSalesCore.ViewModels.Customers;

namespace BookSalesCore.Services.Reports;

public interface IReportsPdfService
{
    byte[] GenerateBusinessReportPdf(ReportsIndexViewModel model);

    byte[] GenerateSalesOrdersPdf(SalesOrderListViewModel model);

    byte[] GenerateCustomersPdf(CustomerListViewModel model);
}
