using BookSalesCore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSalesCore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261027220000_AddBookCodeSequenceAndCustomerSnapshot")]
partial class AddBookCodeSequenceAndCustomerSnapshot
{
}
