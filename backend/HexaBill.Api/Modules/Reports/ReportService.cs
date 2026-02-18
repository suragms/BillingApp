/*
 * Purpose: Report service for analytics and AI suggestions
 * Single profit definition (PRODUCTION_MASTER_TODO #38): Profit = GrandTotal(Sales) - COGS - Expenses.
 * COGS = SaleItems (Qty × ConversionToBase × CostPrice). Use same in ProfitService and dashboard.
 */
using Microsoft.EntityFrameworkCore;
using HexaBill.Api.Data;
using HexaBill.Api.Models;
using HexaBill.Api.Shared.Extensions;
using HexaBill.Api.Modules.Payments;
using HexaBill.Api.Modules.Billing;
using HexaBill.Api.Modules.SuperAdmin;
using HexaBill.Api.Shared.Services;

namespace HexaBill.Api.Modules.Reports
{
    public interface IReportService
    {
        Task<SummaryReportDto> GetSummaryReportAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null, int? routeId = null, int? userIdForStaff = null, string? roleForStaff = null);
        Task<PagedResponse<SaleDto>> GetSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, int? customerId = null, string? status = null, int page = 1, int pageSize = 10, int? branchId = null, int? routeId = null, int? userIdForStaff = null, string? roleForStaff = null);
        Task<EnhancedSalesReportDto> GetEnhancedSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, string granularity = "day", int? productId = null, int? customerId = null, string? status = null, int page = 1, int pageSize = 50);
        Task<List<ProductSalesDto>> GetProductSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, int top = 20);
        Task<List<ProductSalesDto>> GetEnhancedProductSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, int? productId = null, string? unitType = null, bool lowStockOnly = false);
        Task<List<CustomerDto>> GetOutstandingCustomersAsync(int tenantId, int days = 30);
        Task<CustomerReportDto> GetCustomerReportAsync(int tenantId, DateTime fromDate, DateTime toDate, decimal? minOutstanding = null);
        Task<List<PaymentDto>> GetChequeReportAsync(int tenantId);
        Task<AISuggestionsDto> GetAISuggestionsAsync(int tenantId, int periodDays = 30);
        Task<List<PendingBillDto>> GetPendingBillsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null, string? search = null, string? status = null);
        Task<AgingReportDto> GetAgingReportAsync(int tenantId, DateTime asOfDate, int? customerId = null);
        Task<StockReportDto> GetStockReportAsync(int tenantId, bool lowOnly = false);
        Task<List<ExpenseByCategoryDto>> GetExpensesByCategoryAsync(int tenantId, DateTime fromDate, DateTime toDate, int? branchId = null);
        Task<List<SalesVsExpensesDto>> GetSalesVsExpensesAsync(int tenantId, DateTime fromDate, DateTime toDate, string groupBy = "day");
        Task<SalesLedgerReportDto> GetComprehensiveSalesLedgerAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null, int? routeId = null, int? staffId = null, int? userIdForStaff = null, string? roleForStaff = null);
        Task<List<StaffPerformanceDto>> GetStaffPerformanceAsync(int tenantId, DateTime fromDate, DateTime toDate, int? routeId = null); // FIX: Add route filter parameter
    }

    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly IRouteScopeService _routeScopeService;
        private readonly ISettingsService _settingsService;

        public ReportService(AppDbContext context, IRouteScopeService routeScopeService, ISettingsService settingsService)
        {
            _context = context;
            _routeScopeService = routeScopeService;
            _settingsService = settingsService;
        }

        public async Task<SummaryReportDto> GetSummaryReportAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null, int? routeId = null, int? userIdForStaff = null, string? roleForStaff = null)
        {
            try
            {
                // CRITICAL FIX: Never use .Date property, it creates Unspecified
                var utcNow = DateTime.UtcNow;
                var today = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                DateTime startDate;
                DateTime endDate;
                
                // CRITICAL: Handle date parsing and ensure UTC Kind for PostgreSQL
                if (fromDate.HasValue)
                {
                    startDate = new DateTime(fromDate.Value.Year, fromDate.Value.Month, fromDate.Value.Day, 0, 0, 0, DateTimeKind.Utc); // CRITICAL FIX: Never use .Date
                }
                else
                {
                    startDate = today;
                }
                
                if (toDate.HasValue)
                {
                    endDate = toDate.Value.AddDays(1).ToUtcKind(); // Include the entire day and convert to UTC Kind - FIX: Don't use .Date
                }
                else
                {
                    endDate = today.AddDays(1).ToUtcKind();
                }
                
                Console.WriteLine($"?? GetSummaryReportAsync called with tenantId={tenantId}, fromDate: {startDate:yyyy-MM-dd}, toDate: {endDate:yyyy-MM-dd}");
                Console.WriteLine($"?? Date range: {startDate:yyyy-MM-dd HH:mm:ss} to {endDate:yyyy-MM-dd HH:mm:ss}");

                decimal salesToday = 0;
                decimal purchasesToday = 0;
                decimal expensesToday = 0;

                try
                {
                    // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                    var salesQuery = _context.Sales
                        .Where(s => !s.IsDeleted && s.InvoiceDate >= startDate && s.InvoiceDate < endDate);
                    if (tenantId > 0)
                    {
                        salesQuery = salesQuery.Where(s => s.TenantId == tenantId);
                    }
                    if (branchId.HasValue) salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
                    if (routeId.HasValue) salesQuery = salesQuery.Where(s => s.RouteId == routeId.Value);
                    if (tenantId > 0 && userIdForStaff.HasValue && string.Equals(roleForStaff, "Staff", StringComparison.OrdinalIgnoreCase))
                    {
                        var restrictedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userIdForStaff.Value, tenantId, roleForStaff ?? "");
                        if (restrictedRouteIds != null && restrictedRouteIds.Length > 0)
                            salesQuery = salesQuery.Where(s => s.RouteId != null && restrictedRouteIds.Contains(s.RouteId.Value));
                        else if (restrictedRouteIds != null && restrictedRouteIds.Length == 0)
                            salesQuery = salesQuery.Where(s => false);
                    }
                    var salesCount = await salesQuery.CountAsync();
                    Console.WriteLine($"?? Found {salesCount} sales records in date range (SuperAdmin: {tenantId == 0})");
                    salesToday = await salesQuery.SumAsync(s => (decimal?)s.GrandTotal) ?? 0;
                    Console.WriteLine($"?? Total sales today: {salesToday}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error calculating salesToday: {ex.Message}");
                    Console.WriteLine($"? Stack trace: {ex.StackTrace}");
                    salesToday = 0;
                }

                try
                {
                    // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                    var purchasesQuery = _context.Purchases
                        .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate < endDate);
                    if (tenantId > 0)
                    {
                        purchasesQuery = purchasesQuery.Where(p => p.TenantId == tenantId);
                    }
                    var purchasesCount = await purchasesQuery.CountAsync();
                    Console.WriteLine($"?? Found {purchasesCount} purchase records in date range (SuperAdmin: {tenantId == 0})");
                    purchasesToday = await purchasesQuery.SumAsync(p => (decimal?)p.TotalAmount) ?? 0;
                    Console.WriteLine($"?? Total purchases today: {purchasesToday}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error calculating purchasesToday: {ex.Message}");
                    Console.WriteLine($"? Stack trace: {ex.StackTrace}");
                    purchasesToday = 0;
                }

                try
                {
                    // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                    var expensesQuery = _context.Expenses
                        .Where(e => e.Date >= startDate && e.Date < endDate);
                    if (tenantId > 0)
                    {
                        expensesQuery = expensesQuery.Where(e => e.TenantId == tenantId);
                    }
                    var expensesCount = await expensesQuery.CountAsync();
                    Console.WriteLine($"?? Found {expensesCount} expense records in date range (SuperAdmin: {tenantId == 0})");
                    expensesToday = await expensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0;
                    Console.WriteLine($"?? Total expenses today: {expensesToday}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error calculating expensesToday: {ex.Message}");
                    Console.WriteLine($"? Stack trace: {ex.StackTrace}");
                    expensesToday = 0;
                }

                // CRITICAL FIX: Calculate COGS (Cost of Goods Sold) from actual sales, not purchases
                // COGS = Sum of (SaleItem.Qty × Product.ConversionToBase × Product.CostPrice) for all items sold in period
                decimal cogsToday = 0;
                try
                {
                    // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                    var saleItemsQuery = _context.SaleItems
                        .Include(si => si.Sale)
                        .Include(si => si.Product)
                        .Where(si => si.Sale.InvoiceDate >= startDate 
                            && si.Sale.InvoiceDate < endDate 
                            && !si.Sale.IsDeleted);
                    
                    if (tenantId > 0)
                    {
                        saleItemsQuery = saleItemsQuery.Where(si => si.Sale.TenantId == tenantId);
                    }
                    if (branchId.HasValue)
                    {
                        saleItemsQuery = saleItemsQuery.Where(si => si.Sale.BranchId == branchId.Value);
                    }
                    if (routeId.HasValue)
                    {
                        saleItemsQuery = saleItemsQuery.Where(si => si.Sale.RouteId == routeId.Value);
                    }
                    if (tenantId > 0 && userIdForStaff.HasValue && string.Equals(roleForStaff, "Staff", StringComparison.OrdinalIgnoreCase))
                    {
                        var restrictedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userIdForStaff.Value, tenantId, roleForStaff ?? "");
                        if (restrictedRouteIds != null && restrictedRouteIds.Length > 0)
                            saleItemsQuery = saleItemsQuery.Where(si => si.Sale.RouteId != null && restrictedRouteIds.Contains(si.Sale.RouteId.Value));
                        else if (restrictedRouteIds != null && restrictedRouteIds.Length == 0)
                            saleItemsQuery = saleItemsQuery.Where(si => false);
                    }
                    
                    var saleItems = await saleItemsQuery.ToListAsync();
                    
                    // Calculate COGS: Convert sale quantity to base units, then multiply by cost price per base unit
                    cogsToday = saleItems.Sum(si =>
                    {
                        // Convert sale quantity to base units using ConversionToBase
                        var conversionFactor = si.Product.ConversionToBase > 0 ? si.Product.ConversionToBase : 1m;
                        var baseQty = si.Qty * conversionFactor;
                        
                        // COGS = base quantity × cost price per base unit
                        // CostPrice is already per base unit (stored when purchase is recorded)
                        var cogs = baseQty * si.Product.CostPrice;
                        return cogs;
                    });
                    
                    Console.WriteLine($"?? Calculated COGS from {saleItems.Count} sale items: {cogsToday:C}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error calculating COGS: {ex.Message}");
                    Console.WriteLine($"? Stack trace: {ex.StackTrace}");
                    cogsToday = 0;
                }

                // Single definition: Profit = GrandTotal(Sales) - COGS - Expenses (same as ProfitService; PRODUCTION_MASTER_TODO #38)
                var grossProfit = salesToday - cogsToday;
                var profitToday = grossProfit - expensesToday;
                
                Console.WriteLine($"\n========== REPORT SERVICE PROFIT CALCULATION (CORRECTED) ==========");
                Console.WriteLine($"?? Date Range: {startDate:yyyy-MM-dd HH:mm:ss} to {endDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"?? Sales (GrandTotal with VAT): {salesToday:C}");
                Console.WriteLine($"?? COGS (from SaleItems × Product.CostPrice): {cogsToday:C}");
                Console.WriteLine($"?? Purchases (for reference, not used in profit): {purchasesToday:C}");
                Console.WriteLine($"?? Gross Profit (Sales - COGS): {grossProfit:C}");
                Console.WriteLine($"?? Expenses: {expensesToday:C}");
                Console.WriteLine($"? NET PROFIT (Gross Profit - Expenses): {profitToday:C}");
                Console.WriteLine($"=========================================================================\n");

                List<ProductDto> lowStockProducts = new List<ProductDto>();
                try
                {
                    // #55: Per-product ReorderLevel or global fallback for ReorderLevel 0
                    int? globalThreshold = null;
                    if (tenantId > 0)
                    {
                        var settings = await _settingsService.GetOwnerSettingsAsync(tenantId);
                        if (settings.TryGetValue("LOW_STOCK_GLOBAL_THRESHOLD", out var v) && !string.IsNullOrWhiteSpace(v) && int.TryParse(v.Trim(), out int gt) && gt > 0)
                            globalThreshold = gt;
                    }
                    var productsQuery = _context.Products.AsQueryable();
                    if (tenantId > 0)
                        productsQuery = productsQuery.Where(p => p.TenantId == tenantId);
                    if (globalThreshold.HasValue && globalThreshold.Value > 0)
                        productsQuery = productsQuery.Where(p => (p.ReorderLevel > 0 && p.StockQty <= p.ReorderLevel) || (p.ReorderLevel == 0 && p.StockQty <= globalThreshold.Value));
                    else
                        productsQuery = productsQuery.Where(p => p.ReorderLevel > 0 && p.StockQty <= p.ReorderLevel);
                    
                    var products = await productsQuery
                        .Select(p => new ProductDto
                        {
                            Id = p.Id,
                            Sku = p.Sku,
                            NameEn = p.NameEn,
                            NameAr = p.NameAr,
                            UnitType = p.UnitType,
                            ConversionToBase = p.ConversionToBase,
                            CostPrice = p.CostPrice,
                            SellPrice = p.SellPrice,
                            StockQty = p.StockQty,
                            ReorderLevel = p.ReorderLevel,
                            DescriptionEn = p.DescriptionEn,
                            DescriptionAr = p.DescriptionAr
                        })
                        .ToListAsync();
                    
                    lowStockProducts = products
                        .OrderBy(p => p.StockQty)
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading low stock products: {ex.Message}");
                    lowStockProducts = new List<ProductDto>();
                }

                List<SaleDto> pendingInvoices = new List<SaleDto>();
                int pendingBillsCount = 0;
                decimal pendingBillsAmount = 0;
                int paidBillsCount = 0;
                decimal paidBillsAmount = 0;
                try
                {
                    // PERFORMANCE FIX: Filter in database instead of loading all sales into RAM
                    // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                    // Use actual balance calculation: GrandTotal - PaidAmount > 0.01m
                    
                    // Build base query for tenant filtering
                    var baseSalesQuery = _context.Sales.Where(s => !s.IsDeleted);
                    if (tenantId > 0)
                    {
                        baseSalesQuery = baseSalesQuery.Where(s => s.TenantId == tenantId);
                    }
                    
                    // Pending bills: Filter in database (balance > 0.01m)
                    var pendingBillsQuery = baseSalesQuery
                        .Where(s => (s.GrandTotal - s.PaidAmount) > 0.01m); // Filter in SQL
                    
                    pendingBillsCount = await pendingBillsQuery.CountAsync(); // Count in database
                    pendingBillsAmount = await pendingBillsQuery.SumAsync(s => (decimal?)(s.GrandTotal - s.PaidAmount)) ?? 0m; // Sum in database
                    
                    // Paid bills: Filter in database (balance <= 0.01m)
                    var paidBillsQuery = baseSalesQuery
                        .Where(s => (s.GrandTotal - s.PaidAmount) <= 0.01m); // Filter in SQL
                    
                    paidBillsCount = await paidBillsQuery.CountAsync(); // Count in database
                    paidBillsAmount = await paidBillsQuery.SumAsync(s => (decimal?)s.GrandTotal) ?? 0m; // Sum in database
                    
                    Console.WriteLine($"?? Pending Bills: {pendingBillsCount} invoices, Amount: {pendingBillsAmount:C}");
                    Console.WriteLine($"? Paid Bills: {paidBillsCount} invoices, Amount: {paidBillsAmount:C}");
                    
                    // Get pending invoices for display (with customer info) - Limited to top 10 for performance
                    // CRITICAL: Get pending invoices with actual balance calculation
                    var pendingInvoicesQuery = from s in _context.Sales
                                               join c in _context.Customers on s.CustomerId equals c.Id into customerGroup
                                               from c in customerGroup.DefaultIfEmpty()
                                               where !s.IsDeleted && 
                                                     (s.GrandTotal - s.PaidAmount) > 0.01m // Actual balance > 0
                                               select new { Sale = s, Customer = c };
                    
                    // Apply tenant filter if needed
                    if (tenantId > 0)
                    {
                        pendingInvoicesQuery = pendingInvoicesQuery.Where(x => x.Sale.TenantId == tenantId);
                    }
                    
                    // Apply branch/route filters if provided
                    if (branchId.HasValue)
                    {
                        pendingInvoicesQuery = pendingInvoicesQuery.Where(x => x.Sale.BranchId == branchId.Value);
                    }
                    if (routeId.HasValue)
                    {
                        pendingInvoicesQuery = pendingInvoicesQuery.Where(x => x.Sale.RouteId == routeId.Value);
                    }
                    if (tenantId > 0 && userIdForStaff.HasValue && string.Equals(roleForStaff, "Staff", StringComparison.OrdinalIgnoreCase))
                    {
                        var restrictedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userIdForStaff.Value, tenantId, roleForStaff ?? "");
                        if (restrictedRouteIds != null && restrictedRouteIds.Length > 0)
                            pendingInvoicesQuery = pendingInvoicesQuery.Where(x => x.Sale.RouteId != null && restrictedRouteIds.Contains(x.Sale.RouteId.Value));
                        else if (restrictedRouteIds != null && restrictedRouteIds.Length == 0)
                            pendingInvoicesQuery = pendingInvoicesQuery.Where(x => false);
                    }
                    
                    pendingInvoices = await pendingInvoicesQuery
                        .OrderByDescending(x => x.Sale.InvoiceDate)
                        .Take(10)
                        .Select(x => new SaleDto
                        {
                            Id = x.Sale.Id,
                            InvoiceNo = x.Sale.InvoiceNo,
                            InvoiceDate = x.Sale.InvoiceDate,
                            CustomerId = x.Sale.CustomerId,
                            CustomerName = x.Customer != null ? x.Customer.Name : null,
                            Subtotal = x.Sale.Subtotal,
                            VatTotal = x.Sale.VatTotal,
                            Discount = x.Sale.Discount,
                            GrandTotal = x.Sale.GrandTotal,
                            PaidAmount = x.Sale.PaidAmount,
                            PaymentStatus = x.Sale.PaymentStatus.ToString(),
                            Notes = x.Sale.Notes,
                            Items = new List<SaleItemDto>() // Empty list for summary view
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading pending invoices: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    pendingInvoices = new List<SaleDto>();
                    pendingBillsCount = 0;
                }

                // Calculate invoice counts
                // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                var invoicesTodayQuery = _context.Sales
                    .Where(s => !s.IsDeleted && s.InvoiceDate >= today && s.InvoiceDate < today.AddDays(1));
                if (tenantId > 0) invoicesTodayQuery = invoicesTodayQuery.Where(s => s.TenantId == tenantId);
                var invoicesToday = await invoicesTodayQuery.CountAsync();
                
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var invoicesWeeklyQuery = _context.Sales
                    .Where(s => !s.IsDeleted && s.InvoiceDate >= startOfWeek && s.InvoiceDate < today.AddDays(1));
                if (tenantId > 0) invoicesWeeklyQuery = invoicesWeeklyQuery.Where(s => s.TenantId == tenantId);
                var invoicesWeekly = await invoicesWeeklyQuery.CountAsync();
                
                var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var invoicesMonthlyQuery = _context.Sales
                    .Where(s => !s.IsDeleted && s.InvoiceDate >= startOfMonth && s.InvoiceDate < today.AddDays(1));
                if (tenantId > 0) invoicesMonthlyQuery = invoicesMonthlyQuery.Where(s => s.TenantId == tenantId);
                var invoicesMonthly = await invoicesMonthlyQuery.CountAsync();

                // Calculate branch breakdown (only if no specific branchId/routeId filter is applied)
                List<DashboardBranchSummaryDto> branchBreakdown = new List<DashboardBranchSummaryDto>();
                if (!branchId.HasValue && !routeId.HasValue && tenantId > 0) // Only show breakdown for owner view, not filtered
                {
                    try
                    {
                        var branches = await _context.Branches
                            .Where(b => b.TenantId == tenantId)
                            .ToListAsync();

                        foreach (var branch in branches)
                        {
                            // Sales for this branch in date range
                            var branchSalesQuery = _context.Sales
                                .Where(s => !s.IsDeleted 
                                    && s.BranchId == branch.Id 
                                    && s.InvoiceDate >= startDate 
                                    && s.InvoiceDate < endDate);
                            var branchSales = await branchSalesQuery.SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;
                            var branchInvoiceCount = await branchSalesQuery.CountAsync();

                            // Expenses for this branch in date range
                            var branchExpensesQuery = _context.Expenses
                                .Where(e => e.BranchId == branch.Id 
                                    && e.Date >= startDate 
                                    && e.Date < endDate);
                            var branchExpenses = await branchExpensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;

                            // Simplified profit (Sales - Expenses, no COGS per branch in this view)
                            var branchProfit = branchSales - branchExpenses;

                            branchBreakdown.Add(new DashboardBranchSummaryDto
                            {
                                BranchId = branch.Id,
                                BranchName = branch.Name,
                                Sales = branchSales,
                                Expenses = branchExpenses,
                                Profit = branchProfit,
                                InvoiceCount = branchInvoiceCount
                            });
                        }

                        // Order by sales descending
                        branchBreakdown = branchBreakdown.OrderByDescending(b => b.Sales).ToList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error calculating branch breakdown: {ex.Message}");
                        branchBreakdown = new List<DashboardBranchSummaryDto>();
                    }
                }

                // Calculate daily sales trend for last 7 days
                List<DailySalesDto> dailySalesTrend = new List<DailySalesDto>();
                try
                {
                    var sevenDaysAgo = today.AddDays(-7);
                    var dailySalesQuery = from s in _context.Sales
                                         where !s.IsDeleted 
                                             && s.InvoiceDate >= sevenDaysAgo 
                                             && s.InvoiceDate < today.AddDays(1)
                                         group s by s.InvoiceDate.Date into g
                                         select new
                                         {
                                             Date = g.Key,
                                             Sales = g.Sum(s => s.GrandTotal),
                                             InvoiceCount = g.Count()
                                         };
                    
                    if (tenantId > 0)
                    {
                        // Need to filter by tenant - adjust query
                        dailySalesQuery = from s in _context.Sales
                                         where !s.IsDeleted 
                                             && s.TenantId == tenantId
                                             && s.InvoiceDate >= sevenDaysAgo 
                                             && s.InvoiceDate < today.AddDays(1)
                                         group s by s.InvoiceDate.Date into g
                                         select new
                                         {
                                             Date = g.Key,
                                             Sales = g.Sum(s => s.GrandTotal),
                                             InvoiceCount = g.Count()
                                         };
                    }
                    
                    if (branchId.HasValue)
                    {
                        dailySalesQuery = from s in _context.Sales
                                         where !s.IsDeleted 
                                             && s.BranchId == branchId.Value
                                             && s.InvoiceDate >= sevenDaysAgo 
                                             && s.InvoiceDate < today.AddDays(1)
                                         group s by s.InvoiceDate.Date into g
                                         select new
                                         {
                                             Date = g.Key,
                                             Sales = g.Sum(s => s.GrandTotal),
                                             InvoiceCount = g.Count()
                                         };
                        if (tenantId > 0)
                        {
                            dailySalesQuery = from s in _context.Sales
                                             where !s.IsDeleted 
                                                 && s.TenantId == tenantId
                                                 && s.BranchId == branchId.Value
                                                 && s.InvoiceDate >= sevenDaysAgo 
                                                 && s.InvoiceDate < today.AddDays(1)
                                             group s by s.InvoiceDate.Date into g
                                             select new
                                             {
                                                 Date = g.Key,
                                                 Sales = g.Sum(s => s.GrandTotal),
                                                 InvoiceCount = g.Count()
                                             };
                        }
                    }
                    
                    var dailySalesData = await dailySalesQuery.ToListAsync();
                    
                    // Fill in missing days with zero sales
                    for (int i = 6; i >= 0; i--)
                    {
                        var date = today.AddDays(-i);
                        var dateStr = date.ToString("yyyy-MM-dd");
                        var dayData = dailySalesData.FirstOrDefault(d => d.Date.Date == date.Date);
                        
                        dailySalesTrend.Add(new DailySalesDto
                        {
                            Date = dateStr,
                            Sales = dayData != null ? dayData.Sales : 0m,
                            InvoiceCount = dayData != null ? dayData.InvoiceCount : 0
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating daily sales trend: {ex.Message}");
                    dailySalesTrend = new List<DailySalesDto>();
                }

                // Calculate top customers for the period
                List<TopCustomerDto> topCustomers = new List<TopCustomerDto>();
                try
                {
                    var topCustomersQuery = from s in _context.Sales
                                           join c in _context.Customers on s.CustomerId equals c.Id
                                         where !s.IsDeleted 
                                             && s.InvoiceDate >= startDate 
                                             && s.InvoiceDate < endDate
                                               && s.CustomerId != null
                                           group s by new { c.Id, c.Name } into g
                                           select new TopCustomerDto
                                           {
                                               CustomerId = g.Key.Id,
                                               CustomerName = g.Key.Name ?? "Unknown",
                                               TotalSales = g.Sum(s => s.GrandTotal),
                                               InvoiceCount = g.Count()
                                           };
                    
                    if (tenantId > 0)
                    {
                        topCustomersQuery = from s in _context.Sales
                                           join c in _context.Customers on s.CustomerId equals c.Id
                                           where !s.IsDeleted 
                                               && s.TenantId == tenantId
                                               && s.InvoiceDate >= startDate 
                                               && s.InvoiceDate < endDate
                                               && s.CustomerId != null
                                           group s by new { c.Id, c.Name } into g
                                           select new TopCustomerDto
                                           {
                                               CustomerId = g.Key.Id,
                                               CustomerName = g.Key.Name ?? "Unknown",
                                               TotalSales = g.Sum(s => s.GrandTotal),
                                               InvoiceCount = g.Count()
                                           };
                    }
                    
                    if (branchId.HasValue)
                    {
                        topCustomersQuery = topCustomersQuery.Where(tc => 
                            _context.Sales.Any(s => s.CustomerId == tc.CustomerId && s.BranchId == branchId.Value));
                    }
                    
                    topCustomers = await topCustomersQuery
                        .OrderByDescending(c => c.TotalSales)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating top customers: {ex.Message}");
                    topCustomers = new List<TopCustomerDto>();
                }

                // Calculate top products for the period
                List<TopProductDto> topProducts = new List<TopProductDto>();
                try
                {
                    var topProductsQuery = from si in _context.SaleItems
                                           join s in _context.Sales on si.SaleId equals s.Id
                                           join p in _context.Products on si.ProductId equals p.Id
                                         where !s.IsDeleted 
                                             && s.InvoiceDate >= startDate 
                                             && s.InvoiceDate < endDate
                                           group si by new { p.Id, p.NameEn, si.UnitType } into g
                                           select new TopProductDto
                                           {
                                               ProductId = g.Key.Id,
                                               ProductName = g.Key.NameEn ?? "Unknown",
                                               TotalSales = g.Sum(si => si.LineTotal),
                                               TotalQty = g.Sum(si => si.Qty),
                                               UnitType = g.Key.UnitType ?? "PIECE"
                                           };
                    
                    if (tenantId > 0)
                    {
                        topProductsQuery = from si in _context.SaleItems
                                           join s in _context.Sales on si.SaleId equals s.Id
                                           join p in _context.Products on si.ProductId equals p.Id
                                           where !s.IsDeleted 
                                               && s.TenantId == tenantId
                                               && s.InvoiceDate >= startDate 
                                               && s.InvoiceDate < endDate
                                           group si by new { p.Id, p.NameEn, si.UnitType } into g
                                           select new TopProductDto
                                           {
                                               ProductId = g.Key.Id,
                                               ProductName = g.Key.NameEn ?? "Unknown",
                                               TotalSales = g.Sum(si => si.LineTotal),
                                               TotalQty = g.Sum(si => si.Qty),
                                               UnitType = g.Key.UnitType ?? "PIECE"
                                           };
                    }
                    
                    if (branchId.HasValue)
                    {
                        topProductsQuery = from si in _context.SaleItems
                                           join s in _context.Sales on si.SaleId equals s.Id
                                           join p in _context.Products on si.ProductId equals p.Id
                                           where !s.IsDeleted 
                                               && s.BranchId == branchId.Value
                                               && s.InvoiceDate >= startDate 
                                               && s.InvoiceDate < endDate
                                           group si by new { p.Id, p.NameEn, si.UnitType } into g
                                           select new TopProductDto
                                           {
                                               ProductId = g.Key.Id,
                                               ProductName = g.Key.NameEn ?? "Unknown",
                                               TotalSales = g.Sum(si => si.LineTotal),
                                               TotalQty = g.Sum(si => si.Qty),
                                               UnitType = g.Key.UnitType ?? "PIECE"
                                           };
                        if (tenantId > 0)
                        {
                            topProductsQuery = from si in _context.SaleItems
                                               join s in _context.Sales on si.SaleId equals s.Id
                                               join p in _context.Products on si.ProductId equals p.Id
                                               where !s.IsDeleted 
                                                   && s.TenantId == tenantId
                                                   && s.BranchId == branchId.Value
                                                   && s.InvoiceDate >= startDate 
                                                   && s.InvoiceDate < endDate
                                               group si by new { p.Id, p.NameEn, si.UnitType } into g
                                               select new TopProductDto
                                               {
                                                   ProductId = g.Key.Id,
                                                   ProductName = g.Key.NameEn ?? "Unknown",
                                                   TotalSales = g.Sum(si => si.LineTotal),
                                                   TotalQty = g.Sum(si => si.Qty),
                                                   UnitType = g.Key.UnitType ?? "PIECE"
                                               };
                        }
                    }
                    
                    topProducts = await topProductsQuery
                        .OrderByDescending(p => p.TotalSales)
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating top products: {ex.Message}");
                    topProducts = new List<TopProductDto>();
                }

                var result = new SummaryReportDto
                {
                    SalesToday = salesToday,
                    PurchasesToday = purchasesToday,
                    ExpensesToday = expensesToday,
                    CogsToday = cogsToday,
                    ProfitToday = profitToday,
                    LowStockProducts = lowStockProducts,
                    PendingInvoices = pendingInvoices,
                    PendingBills = pendingBillsCount,
                    PendingBillsAmount = pendingBillsAmount,
                    PaidBills = paidBillsCount,
                    PaidBillsAmount = paidBillsAmount,
                    InvoicesToday = invoicesToday,
                    InvoicesWeekly = invoicesWeekly,
                    InvoicesMonthly = invoicesMonthly,
                    BranchBreakdown = branchBreakdown,
                    DailySalesTrend = dailySalesTrend,
                    TopCustomersToday = topCustomers,
                    TopProductsToday = topProducts
                };
                
                Console.WriteLine($"? SummaryReportDto created: Sales={salesToday}, COGS={cogsToday}, Purchases={purchasesToday}, Expenses={expensesToday}, Profit={profitToday}");
                Console.WriteLine($"? Bills Summary: Pending={pendingBillsCount} (${pendingBillsAmount:C}), Paid={paidBillsCount} (${paidBillsAmount:C})");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in GetSummaryReportAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Return a safe default response
                return new SummaryReportDto
                {
                    SalesToday = 0,
                    PurchasesToday = 0,
                    ExpensesToday = 0,
                    ProfitToday = 0,
                    LowStockProducts = new List<ProductDto>(),
                    PendingInvoices = new List<SaleDto>(),
                    PendingBills = 0,
                    PendingBillsAmount = 0,
                    PaidBills = 0,
                    PaidBillsAmount = 0,
                    InvoicesToday = 0,
                    InvoicesWeekly = 0,
                    InvoicesMonthly = 0
                };
            }
        }

        public async Task<PagedResponse<SaleDto>> GetSalesReportAsync(
            int tenantId,
            DateTime fromDate, 
            DateTime toDate, 
            int? customerId = null,
            string? status = null,
            int page = 1, 
            int pageSize = 10,
            int? branchId = null,
            int? routeId = null,
            int? userIdForStaff = null,
            string? roleForStaff = null)
        {
            // CRITICAL FIX: Use < instead of <= for toDate comparison
            var query = _context.Sales
                .Where(s => !s.IsDeleted && s.InvoiceDate >= fromDate && s.InvoiceDate < toDate);
            if (tenantId > 0)
            {
                query = query.Where(s => s.TenantId == tenantId);
            }
            if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId.Value);
            if (routeId.HasValue) query = query.Where(s => s.RouteId == routeId.Value);
            if (tenantId > 0 && userIdForStaff.HasValue && string.Equals(roleForStaff, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                var restrictedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userIdForStaff.Value, tenantId, roleForStaff ?? "");
                if (restrictedRouteIds != null && restrictedRouteIds.Length > 0)
                    query = query.Where(s => s.RouteId != null && restrictedRouteIds.Contains(s.RouteId.Value));
                else if (restrictedRouteIds != null && restrictedRouteIds.Length == 0)
                    query = query.Where(s => false);
            }
            
            Console.WriteLine($"?? GetSalesReportAsync: fromDate={fromDate:yyyy-MM-dd HH:mm:ss}, toDate={toDate:yyyy-MM-dd HH:mm:ss}, customerId={customerId}, SuperAdmin={tenantId == 0}");
            
            // Apply customer filter
            if (customerId.HasValue)
            {
                query = query.Where(s => s.CustomerId == customerId.Value);
            }
            
            // Apply status filter (Pending, Paid, Partial)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusUpper = status.ToUpper();
                if (statusUpper == "PENDING" || statusUpper == "UNPAID")
                {
                    // Pending: balance > 0.01
                    query = query.Where(s => (s.GrandTotal - s.PaidAmount) > 0.01m);
                }
                else if (statusUpper == "PAID")
                {
                    // Paid: balance <= 0.01
                    query = query.Where(s => (s.GrandTotal - s.PaidAmount) <= 0.01m);
                }
                else if (statusUpper == "PARTIAL")
                {
                    // Partial: paid > 0 but balance > 0.01
                    query = query.Where(s => s.PaidAmount > 0 && (s.GrandTotal - s.PaidAmount) > 0.01m);
                }
            }
            
            var totalCount = await query.CountAsync();
            
            // CRITICAL: Include PaidAmount to calculate actual balance for accurate reporting
            // Calculate balance = GrandTotal - PaidAmount for each sale
            var sales = await (from s in query
                              join c in _context.Customers on s.CustomerId equals c.Id into customerGroup
                              from c in customerGroup.DefaultIfEmpty()
                              orderby s.InvoiceDate descending
                              select new SaleDto
                              {
                                  Id = s.Id,
                                  InvoiceNo = s.InvoiceNo,
                                  InvoiceDate = s.InvoiceDate,
                                  CustomerId = s.CustomerId,
                                  BranchId = s.BranchId,
                                  RouteId = s.RouteId,
                                  CustomerName = c != null ? c.Name : null,
                                  Subtotal = s.Subtotal,
                                  VatTotal = s.VatTotal,
                                  Discount = s.Discount,
                                  GrandTotal = s.GrandTotal,
                                  PaidAmount = s.PaidAmount, // CRITICAL: Include for balance calculation
                                  PaymentStatus = s.PaymentStatus.ToString(),
                                  Notes = s.Notes,
                                  Items = new List<SaleItemDto>() // Items not needed for report view
                              })
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();
            
            Console.WriteLine($"?? Sales Report: {totalCount} total sales, returning {sales.Count} for page {page}");

            return new PagedResponse<SaleDto>
            {
                Items = sales,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        public async Task<List<ProductSalesDto>> GetProductSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, int top = 20)
        {
            try
            {
                // First, get the grouped sales data
                // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                var baseQuery = from si in _context.SaleItems
                                join s in _context.Sales on si.SaleId equals s.Id
                                where !s.IsDeleted && s.InvoiceDate >= fromDate && s.InvoiceDate < toDate
                                select new { si, s };
                
                if (tenantId > 0)
                {
                    baseQuery = baseQuery.Where(x => x.s.TenantId == tenantId);
                }
                
                var groupedData = await (from x in baseQuery
                                        group x.si by x.si.ProductId into g
                                        select new
                                        {
                                            ProductId = g.Key,
                                            TotalQty = g.Sum(si => si.Qty),
                                            TotalAmount = g.Sum(si => si.LineTotal),
                                            TotalSales = g.Count()
                                        })
                                        .OrderByDescending(x => x.TotalAmount)
                                        .Take(top)
                                        .ToListAsync();

                // If no data, return empty list
                if (!groupedData.Any())
                {
                    return new List<ProductSalesDto>();
                }

                // Get product IDs to fetch product details
                // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                var productIds = groupedData.Select(x => x.ProductId).ToList();
                var productsQuery = _context.Products.Where(p => productIds.Contains(p.Id));
                if (tenantId > 0)
                {
                    productsQuery = productsQuery.Where(p => p.TenantId == tenantId);
                }
                var products = await productsQuery
                    .Select(p => new { p.Id, p.NameEn, p.Sku })
                    .ToListAsync();

                // Combine the data
                var productSales = groupedData.Select(g =>
                {
                    var product = products.FirstOrDefault(p => p.Id == g.ProductId);
                    return new ProductSalesDto
                    {
                        ProductId = g.ProductId,
                        ProductName = product != null ? (product.NameEn ?? "Unknown") : "Deleted Product",
                        Sku = product != null ? (product.Sku ?? "N/A") : "N/A",
                        TotalQty = g.TotalQty,
                        TotalAmount = g.TotalAmount,
                        TotalSales = g.TotalSales
                    };
                }).ToList();

                return productSales;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProductSalesReportAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                // Return empty list instead of throwing
                return new List<ProductSalesDto>();
            }
        }

        public async Task<List<CustomerDto>> GetOutstandingCustomersAsync(int tenantId, int days = 30)
        {
            try
            {
                // CRITICAL: Get customers with outstanding balance > 0.01 (PendingBalance = what they owe, #7)
                var customers = await _context.Customers
                    .Where(c => c.TenantId == tenantId && c.PendingBalance > 0.01m)
                    .OrderByDescending(c => c.PendingBalance)
                    .Select(c => new CustomerDto
                    {
                        Id = c.Id,
                        Name = c.Name ?? "Unknown",
                        Phone = c.Phone,
                        Email = c.Email,
                        Trn = c.Trn,
                        Address = c.Address,
                        CreditLimit = c.CreditLimit,
                        Balance = c.PendingBalance,
                        PendingBalance = c.PendingBalance
                    })
                    .ToListAsync();

                Console.WriteLine($"? GetOutstandingCustomersAsync: Found {customers.Count} customers with outstanding balance");
                if (customers.Any())
                {
                    Console.WriteLine($"? Total outstanding: {customers.Sum(c => c.Balance):C}");
                    Console.WriteLine($"? Highest balance: {customers.First().Balance:C} ({customers.First().Name})");
                }
                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error in GetOutstandingCustomersAsync: {ex.Message}");
                Console.WriteLine($"? Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"? Inner exception: {ex.InnerException.Message}");
                }
                // Return empty list instead of throwing
                return new List<CustomerDto>();
            }
        }

        public async Task<List<PaymentDto>> GetChequeReportAsync(int tenantId)
        {
            var cheques = await _context.Payments
                .Where(p => p.TenantId == tenantId && p.Mode == PaymentMode.CHEQUE)
                .Include(p => p.Sale)
                .Include(p => p.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    SaleId = p.SaleId,
                    InvoiceNo = p.Sale != null ? p.Sale.InvoiceNo : null,
                    CustomerId = p.CustomerId,
                    CustomerName = p.Customer != null ? p.Customer.Name : null,
                    Amount = p.Amount,
                    Mode = p.Mode.ToString(),
                    Reference = p.Reference,
                    Status = p.Status.ToString(),
                    PaymentDate = p.PaymentDate,
                    CreatedBy = p.CreatedBy,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return cheques;
        }

        public async Task<AISuggestionsDto> GetAISuggestionsAsync(int tenantId, int periodDays = 30)
        {
            try
            {
                var fromDate = DateTime.UtcNow.Date.AddDays(-periodDays).ToUtcKind();

                // Top sellers - Safe query with null checks
                List<ProductDto> topSellers = new List<ProductDto>();
                try
                {
                    topSellers = await _context.SaleItems
                        .Include(si => si.Sale)
                        .Include(si => si.Product)
                        .Where(si => si.Sale != null && si.Sale.TenantId == tenantId && si.Sale.InvoiceDate >= fromDate && si.Product != null)
                        .GroupBy(si => new { 
                            si.ProductId, 
                            ProductName = si.Product != null ? si.Product.NameEn : "Unknown", 
                            ProductSku = si.Product != null ? si.Product.Sku : "N/A",
                            UnitType = si.Product != null ? si.Product.UnitType : "KG",
                            CostPrice = si.Product != null ? si.Product.CostPrice : 0,
                            SellPrice = si.Product != null ? si.Product.SellPrice : 0,
                            StockQty = si.Product != null ? si.Product.StockQty : 0,
                            ReorderLevel = si.Product != null ? si.Product.ReorderLevel : 0
                        })
                        .Select(g => new ProductDto
                        {
                            Id = g.Key.ProductId,
                            Sku = g.Key.ProductSku,
                            NameEn = g.Key.ProductName,
                            UnitType = g.Key.UnitType,
                            ConversionToBase = 1,
                            CostPrice = g.Key.CostPrice,
                            SellPrice = g.Key.SellPrice,
                            StockQty = g.Key.StockQty,
                            ReorderLevel = g.Key.ReorderLevel
                        })
                        .Take(5)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching top sellers: {ex.Message}");
                }

                // Restock candidates (low stock)
                List<ProductDto> restockCandidates = new List<ProductDto>();
                try
                {
                    restockCandidates = await _context.Products
                        .Where(p => p.TenantId == tenantId && p.StockQty <= p.ReorderLevel)
                        .OrderBy(p => p.StockQty)
                        .Take(5)
                        .Select(p => new ProductDto
                        {
                            Id = p.Id,
                            Sku = p.Sku,
                            NameEn = p.NameEn,
                            NameAr = p.NameAr,
                            UnitType = p.UnitType,
                            ConversionToBase = p.ConversionToBase,
                            CostPrice = p.CostPrice,
                            SellPrice = p.SellPrice,
                            StockQty = p.StockQty,
                            ReorderLevel = p.ReorderLevel,
                            DescriptionEn = p.DescriptionEn,
                            DescriptionAr = p.DescriptionAr
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching restock candidates: {ex.Message}");
                }

                // Low margin products
                List<ProductDto> lowMarginProducts = new List<ProductDto>();
                try
                {
                    // Load data first, then order in memory to avoid SQLite decimal ORDER BY issues
                    var products = await _context.Products
                        .Where(p => p.TenantId == tenantId && p.SellPrice > 0 && (p.SellPrice - p.CostPrice) / p.SellPrice < 0.2m)
                        .Select(p => new ProductDto
                        {
                            Id = p.Id,
                            Sku = p.Sku,
                            NameEn = p.NameEn,
                            NameAr = p.NameAr,
                            UnitType = p.UnitType,
                            ConversionToBase = p.ConversionToBase,
                            CostPrice = p.CostPrice,
                            SellPrice = p.SellPrice,
                            StockQty = p.StockQty,
                            ReorderLevel = p.ReorderLevel,
                            DescriptionEn = p.DescriptionEn,
                            DescriptionAr = p.DescriptionAr
                        })
                        .ToListAsync();
                    
                    lowMarginProducts = products
                        .OrderBy(p => p.SellPrice > 0 ? (p.SellPrice - p.CostPrice) / p.SellPrice : 0)
                        .Take(5)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching low margin products: {ex.Message}");
                }

                // Pending customers
                List<CustomerDto> pendingCustomers = new List<CustomerDto>();
                try
                {
                    pendingCustomers = await _context.Customers
                        .Where(c => c.TenantId == tenantId && c.Balance > 0)
                        .OrderByDescending(c => c.Balance)
                        .Take(5)
                        .Select(c => new CustomerDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Phone = c.Phone,
                            Trn = c.Trn,
                            Address = c.Address,
                            CreditLimit = c.CreditLimit,
                            Balance = c.Balance
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching pending customers: {ex.Message}");
                }

                // Promotion candidates
                List<ProductDto> promotionCandidates = new List<ProductDto>();
                try
                {
                    promotionCandidates = await _context.Products
                        .Where(p => p.TenantId == tenantId && p.SellPrice > 0 && (p.SellPrice - p.CostPrice) / p.SellPrice > 0.3m && p.StockQty <= p.ReorderLevel * 2)
                        .OrderByDescending(p => p.SellPrice > 0 ? (p.SellPrice - p.CostPrice) / p.SellPrice : 0)
                        .Take(5)
                        .Select(p => new ProductDto
                        {
                            Id = p.Id,
                            Sku = p.Sku,
                            NameEn = p.NameEn,
                            NameAr = p.NameAr,
                            UnitType = p.UnitType,
                            ConversionToBase = p.ConversionToBase,
                            CostPrice = p.CostPrice,
                            SellPrice = p.SellPrice,
                            StockQty = p.StockQty,
                            ReorderLevel = p.ReorderLevel,
                            DescriptionEn = p.DescriptionEn,
                            DescriptionAr = p.DescriptionAr
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching promotion candidates: {ex.Message}");
                }

                return new AISuggestionsDto
                {
                    TopSellers = topSellers,
                    RestockCandidates = restockCandidates,
                    LowMarginProducts = lowMarginProducts,
                    PendingCustomers = pendingCustomers,
                    PromotionCandidates = promotionCandidates
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in GetAISuggestionsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Return empty suggestions instead of crashing
                return new AISuggestionsDto
                {
                    TopSellers = new List<ProductDto>(),
                    RestockCandidates = new List<ProductDto>(),
                    LowMarginProducts = new List<ProductDto>(),
                    PendingCustomers = new List<CustomerDto>(),
                    PromotionCandidates = new List<ProductDto>()
                };
            }
        }

        public async Task<List<PendingBillDto>> GetPendingBillsAsync(
            int tenantId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            string? search = null,
            string? status = null)
        {
            // CRITICAL: Single SQL aggregation — filter by balance in database, project to DTO (no load-all in memory)
            // PRODUCTION_MASTER_TODO #33
            var utcNow = DateTime.UtcNow;
            var today = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);

            var query = from s in _context.Sales
                        join c in _context.Customers on s.CustomerId equals c.Id into cGrp
                        from c in cGrp.DefaultIfEmpty()
                        where !s.IsDeleted
                              && (s.PaymentStatus == SalePaymentStatus.Pending || s.PaymentStatus == SalePaymentStatus.Partial)
                              && (s.GrandTotal - s.PaidAmount) > 0.01m
                        select new { s, c };

            if (tenantId > 0)
                query = query.Where(x => x.s.TenantId == tenantId);

            if (fromDate.HasValue && toDate.HasValue)
            {
                var from = new DateTime(fromDate.Value.Year, fromDate.Value.Month, fromDate.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                var toEnd = toDate.Value.AddDays(1).AddTicks(-1).ToUtcKind();
                query = query.Where(x => x.s.InvoiceDate >= from && x.s.InvoiceDate < toEnd);
            }

            if (customerId.HasValue)
                query = query.Where(x => x.s.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                var statusLower = status.ToLower();
                if (statusLower == "pending")
                    query = query.Where(x => x.s.PaymentStatus == SalePaymentStatus.Pending);
                else if (statusLower == "partial")
                    query = query.Where(x => x.s.PaymentStatus == SalePaymentStatus.Partial);
                else if (statusLower == "overdue")
                {
                    var cutoff = today.AddDays(-30);
                    query = query.Where(x => x.s.InvoiceDate < cutoff && x.s.PaymentStatus != SalePaymentStatus.Paid);
                }
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.s.InvoiceNo.Contains(search) || (x.c != null && x.c.Name.Contains(search)));

            // Project to DTO in database (single query, no full-entity load)
            var list = await query
                .OrderByDescending(x => x.s.InvoiceDate)
                .Select(x => new PendingBillDto
                {
                    Id = x.s.Id,
                    InvoiceNo = x.s.InvoiceNo,
                    InvoiceDate = x.s.InvoiceDate,
                    DueDate = x.s.DueDate ?? x.s.InvoiceDate.AddDays(30),
                    CustomerId = x.s.CustomerId,
                    CustomerName = x.c != null ? x.c.Name : null,
                    GrandTotal = x.s.GrandTotal,
                    PaidAmount = x.s.PaidAmount,
                    BalanceAmount = x.s.GrandTotal - x.s.PaidAmount,
                    PaymentStatus = x.s.PaymentStatus.ToString(),
                    DaysOverdue = 0 // set below in memory (EF may not translate DateTime diff for all providers)
                })
                .ToListAsync();

            // Set DaysOverdue and sort (lightweight in-memory; list is already filtered and projected)
            foreach (var dto in list)
            {
                var due = dto.DueDate ?? dto.InvoiceDate.AddDays(30);
                dto.DaysOverdue = due < today ? (today - due).Days : 0;
            }

            return list
                .OrderByDescending(pb => pb.DaysOverdue)
                .ThenByDescending(pb => pb.InvoiceDate)
                .ToList();
        }

        public async Task<List<ExpenseByCategoryDto>> GetExpensesByCategoryAsync(int tenantId, DateTime fromDate, DateTime toDate, int? branchId = null)
        {
            try
            {
                // CRITICAL: Super admin (TenantId = 0) sees ALL owners
                var baseQuery = from e in _context.Expenses
                               join c in _context.ExpenseCategories on e.CategoryId equals c.Id into categoryGroup
                               from category in categoryGroup.DefaultIfEmpty()
                               where e.Date >= fromDate && e.Date <= toDate
                               select new { e, category };
                
                if (tenantId > 0)
                {
                    baseQuery = baseQuery.Where(x => x.e.TenantId == tenantId);
                }
                if (branchId.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.e.BranchId == branchId.Value);
                }
                
                var expenses = await (from x in baseQuery
                                     group x by new { 
                                         x.e.CategoryId,
                                         CategoryName = x.category != null ? x.category.Name : "Uncategorized",
                                         CategoryColor = x.category != null ? x.category.ColorCode : "#6B7280"
                                     } into g
                                     select new ExpenseByCategoryDto
                                     {
                                         CategoryId = g.Key.CategoryId,
                                         CategoryName = g.Key.CategoryName,
                                         CategoryColor = g.Key.CategoryColor,
                                         TotalAmount = g.Sum(x => x.e.Amount),
                                         ExpenseCount = g.Count()
                                     })
                                     .OrderByDescending(x => x.TotalAmount)
                                     .ToListAsync();

                return expenses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetExpensesByCategoryAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                // Return empty list instead of throwing
                return new List<ExpenseByCategoryDto>();
            }
        }

        public async Task<List<SalesVsExpensesDto>> GetSalesVsExpensesAsync(int tenantId, DateTime fromDate, DateTime toDate, string groupBy = "day")
        {
            List<SalesVsExpensesDto> result = new List<SalesVsExpensesDto>();
            // CRITICAL: Super admin (TenantId = 0) sees ALL owners

            if (groupBy == "month")
            {
                // Group by month
                var salesBaseQuery = _context.Sales.Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate < toDate);
                if (tenantId > 0) salesBaseQuery = salesBaseQuery.Where(s => s.TenantId == tenantId);
                var salesData = await salesBaseQuery
                    .GroupBy(s => new { Year = s.InvoiceDate.Year, Month = s.InvoiceDate.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                        Sales = g.Sum(s => s.GrandTotal)
                    })
                    .ToListAsync();

                var purchasesBaseQuery = _context.Purchases.Where(p => p.PurchaseDate >= fromDate && p.PurchaseDate < toDate);
                if (tenantId > 0) purchasesBaseQuery = purchasesBaseQuery.Where(p => p.TenantId == tenantId);
                var purchasesData = await purchasesBaseQuery
                    .GroupBy(p => new { Year = p.PurchaseDate.Year, Month = p.PurchaseDate.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                        Purchases = g.Sum(p => p.TotalAmount)
                    })
                    .ToListAsync();

                var expensesBaseQuery = _context.Expenses.Where(e => e.Date >= fromDate && e.Date < toDate);
                if (tenantId > 0) expensesBaseQuery = expensesBaseQuery.Where(e => e.TenantId == tenantId);
                var expensesData = await expensesBaseQuery
                    .GroupBy(e => new { Year = e.Date.Year, Month = e.Date.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                        Expenses = g.Sum(e => e.Amount)
                    })
                    .ToListAsync();

                var allPeriods = salesData.Select(s => s.Period)
                    .Union(purchasesData.Select(p => p.Period))
                    .Union(expensesData.Select(e => e.Period))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                foreach (var period in allPeriods)
                {
                    var sale = salesData.FirstOrDefault(s => s.Period == period);
                    var purchase = purchasesData.FirstOrDefault(p => p.Period == period);
                    var expense = expensesData.FirstOrDefault(e => e.Period == period);

                    result.Add(new SalesVsExpensesDto
                    {
                        Period = period,
                        Date = sale?.Date ?? purchase?.Date ?? expense?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc), // CRITICAL FIX: Never use .Date
                        Sales = sale?.Sales ?? 0,
                        Purchases = purchase?.Purchases ?? 0,
                        Expenses = expense?.Expenses ?? 0,
                        Profit = (sale?.Sales ?? 0) - (purchase?.Purchases ?? 0) - (expense?.Expenses ?? 0)
                    });
                }
            }
            else
            {
                // Group by day - LOAD DATA FIRST (avoid DateTime.Date in database query for PostgreSQL)
                var allSalesQuery = _context.Sales.Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate < toDate);
                if (tenantId > 0) allSalesQuery = allSalesQuery.Where(s => s.TenantId == tenantId);
                var allSales = await allSalesQuery.ToListAsync();
                // CRITICAL FIX: .GroupBy().Date creates Unspecified - group by Year/Month/Day instead
                var salesData = allSales
                    .GroupBy(s => new { s.InvoiceDate.Year, s.InvoiceDate.Month, s.InvoiceDate.Day })
                    .Select(g => new
                    {
                        Period = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd"),
                        Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                        Sales = g.Sum(s => s.GrandTotal)
                    })
                    .ToList();

                var allPurchasesQuery = _context.Purchases.Where(p => p.PurchaseDate >= fromDate && p.PurchaseDate < toDate);
                if (tenantId > 0) allPurchasesQuery = allPurchasesQuery.Where(p => p.TenantId == tenantId);
                var allPurchases = await allPurchasesQuery.ToListAsync();
                // CRITICAL FIX: Never use .Date property
                var purchasesData = allPurchases
                    .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month, p.PurchaseDate.Day })
                    .Select(g => new
                    {
                        Period = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd"),
                        Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                        Purchases = g.Sum(p => p.TotalAmount)
                    })
                    .ToList();

                var allExpensesQuery = _context.Expenses.Where(e => e.Date >= fromDate && e.Date < toDate);
                if (tenantId > 0) allExpensesQuery = allExpensesQuery.Where(e => e.TenantId == tenantId);
                var allExpenses = await allExpensesQuery.ToListAsync();
                // CRITICAL FIX: Never use .Date property
                var expensesData = allExpenses
                    .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Date.Day })
                    .Select(g => new
                    {
                        Period = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd"),
                        Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                        Expenses = g.Sum(e => e.Amount)
                    })
                    .ToList();

                var allPeriods = salesData.Select(s => s.Period)
                    .Union(purchasesData.Select(p => p.Period))
                    .Union(expensesData.Select(e => e.Period))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                foreach (var period in allPeriods)
                {
                    var sale = salesData.FirstOrDefault(s => s.Period == period);
                    var purchase = purchasesData.FirstOrDefault(p => p.Period == period);
                    var expense = expensesData.FirstOrDefault(e => e.Period == period);

                    result.Add(new SalesVsExpensesDto
                    {
                        Period = period,
                        Date = sale?.Date ?? purchase?.Date ?? expense?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc), // CRITICAL FIX: Never use .Date
                        Sales = sale?.Sales ?? 0,
                        Purchases = purchase?.Purchases ?? 0,
                        Expenses = expense?.Expenses ?? 0,
                        Profit = (sale?.Sales ?? 0) - (purchase?.Purchases ?? 0) - (expense?.Expenses ?? 0)
                    });
                }
            }

            return result;
        }

        // Enhanced Sales Report with Granularity
        public async Task<EnhancedSalesReportDto> GetEnhancedSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, string granularity = "day", int? productId = null, int? customerId = null, string? status = null, int page = 1, int pageSize = 50)
        {
            // CRITICAL: Super admin (TenantId = 0) sees ALL owners
            var query = _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Customer)
                .Where(s => !s.IsDeleted && s.InvoiceDate >= fromDate && s.InvoiceDate < toDate)
                .AsQueryable();
            
            if (tenantId > 0)
            {
                query = query.Where(s => s.TenantId == tenantId);
            }

            if (productId.HasValue)
            {
                query = query.Where(s => s.Items.Any(i => i.ProductId == productId.Value));
            }

            if (customerId.HasValue)
            {
                query = query.Where(s => s.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                var statusEnum = Enum.Parse<SalePaymentStatus>(status, true);
                query = query.Where(s => s.PaymentStatus == statusEnum);
            }

            var totalCount = await query.CountAsync();
            var sales = await query
                .OrderByDescending(s => s.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate summary
            var allSales = await query.ToListAsync();
            var summary = new SummaryInfo
            {
                TotalSales = allSales.Sum(s => s.GrandTotal),
                NetSales = allSales.Sum(s => s.Subtotal),
                VatCollected = allSales.Sum(s => s.VatTotal),
                TotalInvoices = allSales.Count,
                AvgOrderValue = allSales.Any() ? allSales.Average(s => s.GrandTotal) : 0
            };

            // Generate series based on granularity
            var series = new List<SalesSeriesDto>();
            if (granularity == "day")
            {
                // CRITICAL FIX: .GroupBy().Date creates Unspecified - group by Year/Month/Day instead
                var grouped = allSales.GroupBy(s => new { s.InvoiceDate.Year, s.InvoiceDate.Month, s.InvoiceDate.Day });
                series = grouped.Select(g => new SalesSeriesDto
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd"),
                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                    Amount = g.Sum(s => s.GrandTotal),
                    Count = g.Count()
                }).OrderBy(s => s.Date).ToList();
            }
            else if (granularity == "week")
            {
                var grouped = allSales.GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.InvoiceDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday));
                series = grouped.Select(g => new SalesSeriesDto
                {
                    Period = $"Week {g.Key}",
                    Date = g.Min(s => s.InvoiceDate),
                    Amount = g.Sum(s => s.GrandTotal),
                    Count = g.Count()
                }).OrderBy(s => s.Date).ToList();
            }
            else if (granularity == "month")
            {
                var grouped = allSales.GroupBy(s => new { s.InvoiceDate.Year, s.InvoiceDate.Month });
                series = grouped.Select(g => new SalesSeriesDto
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    Amount = g.Sum(s => s.GrandTotal),
                    Count = g.Count()
                }).OrderBy(s => s.Date).ToList();
            }

            // Map sales to report items
            var reportItems = sales.Select(s => new SalesReportItemDto
            {
                InvoiceId = s.Id,
                Date = s.InvoiceDate,
                InvoiceNo = s.InvoiceNo,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer?.Name,
                Items = s.Items.Take(2).Select(i => new ProductSummaryDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.NameEn ?? "Unknown",
                    Qty = i.Qty,
                    Price = i.UnitPrice
                }).ToList(),
                Qty = s.Items.Sum(i => i.Qty),
                Gross = s.Subtotal,
                Vat = s.VatTotal,
                Discount = s.Discount,
                Net = s.GrandTotal,
                PaymentStatus = s.PaymentStatus.ToString()
            }).ToList();

            return new EnhancedSalesReportDto
            {
                Summary = summary,
                Series = series,
                Data = new PagedResponse<SalesReportItemDto>
                {
                    Items = reportItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };
        }

        // Enhanced Product Sales Report with Margin Analysis
        public async Task<List<ProductSalesDto>> GetEnhancedProductSalesReportAsync(int tenantId, DateTime fromDate, DateTime toDate, int? productId = null, string? unitType = null, bool lowStockOnly = false)
        {
            var query = from si in _context.SaleItems
                       join s in _context.Sales on si.SaleId equals s.Id
                       join p in _context.Products on si.ProductId equals p.Id
                       where s.TenantId == tenantId && !s.IsDeleted && s.InvoiceDate >= fromDate && s.InvoiceDate < toDate
                       select new { si, s, p };

            if (productId.HasValue)
            {
                query = query.Where(x => x.p.Id == productId.Value);
            }

            if (!string.IsNullOrEmpty(unitType))
            {
                query = query.Where(x => x.p.UnitType == unitType);
            }

            var grouped = await query
                .GroupBy(x => x.p.Id)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Product = g.First().p,
                    TotalQty = g.Sum(x => x.si.Qty),
                    TotalAmount = g.Sum(x => x.si.LineTotal),
                    TotalSales = g.Count(),
                    CostValue = g.Sum(x => x.p.CostPrice * x.si.Qty)
                })
                .ToListAsync();

            var result = grouped.Select(g => new ProductSalesDto
            {
                ProductId = g.ProductId,
                ProductName = g.Product.NameEn ?? "Unknown",
                Sku = g.Product.Sku ?? "N/A",
                UnitType = g.Product.UnitType.ToString(),
                TotalQty = g.TotalQty,
                TotalAmount = g.TotalAmount,
                CostValue = g.CostValue,
                GrossProfit = g.TotalAmount - g.CostValue,
                MarginPercent = g.TotalAmount > 0 ? ((g.TotalAmount - g.CostValue) / g.TotalAmount) * 100 : 0,
                TotalSales = g.TotalSales,
                StockOnHand = g.Product.StockQty,
                ReorderLevel = g.Product.ReorderLevel,
                IsLowStock = g.Product.StockQty <= g.Product.ReorderLevel
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

            if (lowStockOnly)
            {
                result = result.Where(p => p.IsLowStock).ToList();
            }

            return result;
        }

        // Customer Report with Outstanding Analysis
        public async Task<CustomerReportDto> GetCustomerReportAsync(int tenantId, DateTime fromDate, DateTime toDate, decimal? minOutstanding = null)
        {
            var customers = await _context.Customers
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();
            var customerReports = new List<CustomerReportItemDto>();

            foreach (var customer in customers)
            {
                var sales = await _context.Sales
                    .Where(s => !s.IsDeleted && s.CustomerId == customer.Id && s.InvoiceDate >= fromDate && s.InvoiceDate < toDate)
                    .ToListAsync();

                var payments = await _context.Payments
                    .Where(p => p.CustomerId == customer.Id && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
                    .ToListAsync();

                var totalSales = sales.Sum(s => s.GrandTotal);
                var totalPayments = payments.Sum(p => p.Amount);
                var outstanding = customer.Balance; // Use calculated balance

                // Calculate avg days to pay
                var paidInvoices = sales.Where(s => s.PaymentStatus == SalePaymentStatus.Paid).ToList();
                var avgDaysToPay = 0m;
                if (paidInvoices.Any())
                {
                    var daysList = new List<int>();
                    foreach (var inv in paidInvoices)
                    {
                        var firstPayment = payments.Where(p => p.SaleId == inv.Id).OrderBy(p => p.PaymentDate).FirstOrDefault();
                        if (firstPayment != null)
                        {
                            // CRITICAL FIX: TimeSpan subtraction, not .Date property
                            daysList.Add((firstPayment.PaymentDate - inv.InvoiceDate).Days);
                        }
                    }
                    if (daysList.Any())
                    {
                        avgDaysToPay = (decimal)daysList.Average();
                    }
                }

                var lastPayment = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

                if (minOutstanding == null || outstanding >= minOutstanding.Value)
                {
                    customerReports.Add(new CustomerReportItemDto
                    {
                        CustomerId = customer.Id,
                        CustomerName = customer.Name ?? "Unknown",
                        Trn = customer.Trn,
                        TotalSales = totalSales,
                        TotalPayments = totalPayments,
                        Outstanding = outstanding,
                        AvgDaysToPay = avgDaysToPay,
                        LastPaymentDate = lastPayment?.PaymentDate,
                        LastPaymentMode = lastPayment?.Mode.ToString()
                    });
                }
            }

            var summary = new CustomerReportSummary
            {
                TotalCustomers = customerReports.Count,
                TotalSales = customerReports.Sum(c => c.TotalSales),
                TotalPayments = customerReports.Sum(c => c.TotalPayments),
                TotalOutstanding = customerReports.Sum(c => c.Outstanding),
                AvgDaysToPay = customerReports.Any() ? customerReports.Average(c => c.AvgDaysToPay) : 0
            };

            return new CustomerReportDto
            {
                Customers = customerReports.OrderByDescending(c => c.Outstanding).ToList(),
                Summary = summary
            };
        }

        // Aging Report with Buckets
        public async Task<AgingReportDto> GetAgingReportAsync(int tenantId, DateTime asOfDate, int? customerId = null)
        {
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.PaymentStatus != SalePaymentStatus.Paid)
                .AsQueryable();

            if (customerId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.CustomerId == customerId.Value);
            }

            var sales = await salesQuery.ToListAsync();
            var invoices = new List<AgingInvoiceDto>();

            // PRODUCTION_MASTER_TODO #9: Use remaining balance (GrandTotal - PaidAmount), not total. PaidAmount = cleared only.
            foreach (var sale in sales)
            {
                var paidAmount = sale.PaidAmount; // CLEARED-only, matches customer balance and invoice status
                var balance = sale.GrandTotal - paidAmount;
                if (balance <= 0.01m) continue;

                // CRITICAL FIX: TimeSpan subtraction, not .Date property
                var daysOverdue = (asOfDate - sale.InvoiceDate).Days;
                string bucket;
                if (daysOverdue <= 30) bucket = "0-30";
                else if (daysOverdue <= 60) bucket = "31-60";
                else if (daysOverdue <= 90) bucket = "61-90";
                else bucket = "90+";

                invoices.Add(new AgingInvoiceDto
                {
                    Id = sale.Id,
                    InvoiceNo = sale.InvoiceNo,
                    InvoiceDate = sale.InvoiceDate,
                    CustomerId = sale.CustomerId,
                    CustomerName = sale.Customer?.Name,
                    GrandTotal = sale.GrandTotal,
                    PaidAmount = paidAmount,
                    BalanceAmount = balance,
                    DaysOverdue = daysOverdue,
                    AgingBucket = bucket
                });
            }

            var bucket0_30 = new AgingBucket
            {
                Invoices = invoices.Where(i => i.AgingBucket == "0-30").ToList(),
                Total = invoices.Where(i => i.AgingBucket == "0-30").Sum(i => i.BalanceAmount),
                Count = invoices.Count(i => i.AgingBucket == "0-30")
            };

            var bucket31_60 = new AgingBucket
            {
                Invoices = invoices.Where(i => i.AgingBucket == "31-60").ToList(),
                Total = invoices.Where(i => i.AgingBucket == "31-60").Sum(i => i.BalanceAmount),
                Count = invoices.Count(i => i.AgingBucket == "31-60")
            };

            var bucket61_90 = new AgingBucket
            {
                Invoices = invoices.Where(i => i.AgingBucket == "61-90").ToList(),
                Total = invoices.Where(i => i.AgingBucket == "61-90").Sum(i => i.BalanceAmount),
                Count = invoices.Count(i => i.AgingBucket == "61-90")
            };

            var bucket90Plus = new AgingBucket
            {
                Invoices = invoices.Where(i => i.AgingBucket == "90+").ToList(),
                Total = invoices.Where(i => i.AgingBucket == "90+").Sum(i => i.BalanceAmount),
                Count = invoices.Count(i => i.AgingBucket == "90+")
            };

            return new AgingReportDto
            {
                Bucket0_30 = bucket0_30,
                Bucket31_60 = bucket31_60,
                Bucket61_90 = bucket61_90,
                Bucket90Plus = bucket90Plus,
                TotalOutstanding = invoices.Sum(i => i.BalanceAmount),
                Invoices = invoices.OrderByDescending(i => i.DaysOverdue).ToList()
            };
        }

        // Stock Report with Restock Alerts
        public async Task<StockReportDto> GetStockReportAsync(int tenantId, bool lowOnly = false)
        {
            var productsQuery = _context.Products
                .Where(p => p.TenantId == tenantId)
                .AsQueryable();

            if (lowOnly)
            {
                productsQuery = productsQuery.Where(p => p.StockQty <= p.ReorderLevel);
            }

            var products = await productsQuery.ToListAsync();

            // Get reserved quantities (pending sales)
            var reservedByProduct = await _context.SaleItems
                .Include(si => si.Sale)
                .Where(si => !si.Sale.IsDeleted && si.Sale.PaymentStatus == SalePaymentStatus.Pending)
                .GroupBy(si => si.ProductId)
                .Select(g => new { ProductId = g.Key, Reserved = g.Sum(si => si.Qty) })
                .ToListAsync();

            var stockItems = products.Select(p =>
            {
                var reserved = reservedByProduct.FirstOrDefault(r => r.ProductId == p.Id)?.Reserved ?? 0;
                var available = p.StockQty - reserved;

                // Calculate predicted days to stockout (based on 30-day avg sales)
                // CRITICAL FIX: Never use .Date property, it creates Unspecified
                var utcNow = DateTime.UtcNow;
                var last30Days = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-30);
                var avgDailySales = _context.SaleItems
                    .Include(si => si.Sale)
                    .Where(si => si.ProductId == p.Id && !si.Sale.IsDeleted && si.Sale.InvoiceDate >= last30Days)
                    .Sum(si => (decimal?)si.Qty) ?? 0;
                avgDailySales = avgDailySales / 30;
                var predictedDays = avgDailySales > 0 && available > 0 ? (int)(available / avgDailySales) : (int?)null;

                return new StockItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.NameEn ?? "Unknown",
                    Sku = p.Sku ?? "N/A",
                    UnitType = p.UnitType.ToString(),
                    OnHand = p.StockQty,
                    Reserved = reserved,
                    Available = available,
                    ReorderLevel = p.ReorderLevel,
                    SafetyStock = p.ReorderLevel,
                    LastPurchaseDate = _context.Purchases
                        .Include(pi => pi.Items)
                        .Where(pi => pi.Items.Any(pi => pi.ProductId == p.Id))
                        .OrderByDescending(pi => pi.PurchaseDate)
                        .Select(pi => (DateTime?)pi.PurchaseDate)
                        .FirstOrDefault(),
                    IsLowStock = available <= p.ReorderLevel,
                    PredictedDaysToStockOut = predictedDays
                };
            }).ToList();

            var summary = new StockSummary
            {
                TotalSKUs = products.Count,
                LowStockCount = stockItems.Count(i => i.IsLowStock),
                OutOfStockCount = stockItems.Count(i => i.Available <= 0),
                StockValue = products.Sum(p => p.StockQty * p.CostPrice)
            };

            return new StockReportDto
            {
                Summary = summary,
                Items = stockItems.OrderByDescending(i => i.IsLowStock).ThenBy(i => i.Available).ToList()
            };
        }

        public async Task<SalesLedgerReportDto> GetComprehensiveSalesLedgerAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? branchId = null, int? routeId = null, int? staffId = null, int? userIdForStaff = null, string? roleForStaff = null)
        {
            var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-365)).ToUtcKind();
            var to = (toDate ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1).ToUtcKind();

            var salesQuery = _context.Sales
                .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.InvoiceDate >= from && s.InvoiceDate < to);
            if (branchId.HasValue) salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
            if (routeId.HasValue) salesQuery = salesQuery.Where(s => s.RouteId == routeId.Value);
            if (staffId.HasValue) salesQuery = salesQuery.Where(s => s.CreatedBy == staffId.Value);
            if (tenantId > 0 && userIdForStaff.HasValue && string.Equals(roleForStaff, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                var restrictedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userIdForStaff.Value, tenantId, roleForStaff ?? "");
                if (restrictedRouteIds != null && restrictedRouteIds.Length > 0)
                    salesQuery = salesQuery.Where(s => s.RouteId != null && restrictedRouteIds.Contains(s.RouteId.Value));
                else if (restrictedRouteIds != null && restrictedRouteIds.Length == 0)
                    salesQuery = salesQuery.Where(s => false);
            }
            var sales = await salesQuery
                .OrderBy(s => s.InvoiceDate)
                .ThenBy(s => s.Id)
                .ToListAsync();

            var saleIds = sales.Select(s => s.Id).ToHashSet();

            // Get payments within date range; when branch/route/staff filter is active, only include payments linked to filtered sales
            var paymentsQuery = _context.Payments
                .Where(p => p.TenantId == tenantId && p.PaymentDate >= from && p.PaymentDate <= to);
            if (branchId.HasValue || routeId.HasValue || staffId.HasValue)
                paymentsQuery = paymentsQuery.Where(p => !p.SaleId.HasValue || saleIds.Contains(p.SaleId.Value));
            var payments = await paymentsQuery
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            // Get payment totals per sale for status calculation
            var salePayments = await _context.Payments
                .Where(p => p.TenantId == tenantId && p.SaleId.HasValue)
                .GroupBy(p => p.SaleId!.Value)
                .Select(g => new { SaleId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.SaleId, x => x.TotalPaid);

            // Load all customers in one query for efficiency
            var customerIds = sales.Select(s => s.CustomerId).Concat(payments.Select(p => p.CustomerId))
                .Where(id => id.HasValue)
                .Distinct()
                .ToList();
            var customers = await _context.Customers
                .Where(c => c.TenantId == tenantId && customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            // Build ledger entries
            var ledgerEntries = new List<SalesLedgerEntryDto>();
            
            // Track per-customer balances (not global)
            // Use 0 as key for null customer IDs
            var customerBalances = new Dictionary<int, decimal>();

            // Add sales entries (Debit)
            foreach (var sale in sales)
            {
                var paidAmount = salePayments.GetValueOrDefault(sale.Id, 0m);
                var balance = sale.GrandTotal - paidAmount;
                
                // Determine status
                string status = "Unpaid";
                if (balance <= 0.01m)
                {
                    status = "Paid";
                }
                else if (paidAmount > 0)
                {
                    status = "Partial";
                }

                // Calculate Plan Date (Due Date = Invoice Date + 30 days)
                var planDate = sale.InvoiceDate.AddDays(30);

                // Update customer balance
                var customerKey = sale.CustomerId ?? 0;
                if (!customerBalances.ContainsKey(customerKey))
                {
                    customerBalances[customerKey] = 0m;
                }
                customerBalances[customerKey] += sale.GrandTotal; // Add debit

                // Payment mode: Show "NOT PAID" if unpaid, otherwise show payment mode from related payments
                string paymentModeDisplay = "NOT PAID";
                if (status == "Paid" || status == "Partial")
                {
                    // Get payment mode from first payment for this sale
                    var firstPayment = payments.FirstOrDefault(p => p.SaleId == sale.Id);
                    if (firstPayment != null)
                    {
                        paymentModeDisplay = firstPayment.Mode.ToString().ToUpper();
                    }
                    else if (status == "Paid")
                    {
                        paymentModeDisplay = "PAID";
                    }
                }

                // Calculate real pending (GrandTotal - PaidAmount)
                var realPending = sale.GrandTotal - paidAmount;
                
                ledgerEntries.Add(new SalesLedgerEntryDto
                {
                    Date = sale.InvoiceDate,
                    Type = "Sale",
                    InvoiceNo = sale.InvoiceNo,
                    CustomerId = sale.CustomerId,
                    CustomerName = sale.CustomerId.HasValue && customers.ContainsKey(sale.CustomerId.Value)
                        ? customers[sale.CustomerId.Value]
                        : "Cash Customer",
                    PaymentMode = paymentModeDisplay,
                    GrandTotal = sale.GrandTotal, // CRITICAL: Full invoice amount
                    PaidAmount = paidAmount, // CRITICAL: Amount already paid for this invoice
                    RealPending = realPending > 0 ? realPending : 0,
                    RealGotPayment = paidAmount, // CRITICAL: Show actual paid amount for sales (not 0)
                    Status = status,
                    CustomerBalance = customerBalances[customerKey],
                    PlanDate = planDate,
                    SaleId = sale.Id
                });
            }

            // Add payment entries (Credit)
            foreach (var payment in payments)
            {
                // Get related sale for invoice number and status
                var relatedSale = payment.SaleId.HasValue 
                    ? sales.FirstOrDefault(s => s.Id == payment.SaleId.Value)
                    : null;

                var invoiceNo = relatedSale?.InvoiceNo ?? payment.Reference ?? "-";
                var paidAmount = salePayments.GetValueOrDefault(relatedSale?.Id ?? 0, 0m);
                var saleBalance = relatedSale != null ? relatedSale.GrandTotal - paidAmount : 0m;
                
                // Determine status from related sale
                string status = "Partial";
                if (relatedSale != null)
                {
                    if (saleBalance <= 0.01m)
                    {
                        status = "Paid";
                    }
                    else if (paidAmount > 0)
                    {
                        status = "Partial";
                    }
                    else
                    {
                        status = "Unpaid";
                    }
                }

                // Update customer balance
                var paymentCustomerKey = payment.CustomerId ?? 0;
                if (!customerBalances.ContainsKey(paymentCustomerKey))
                {
                    customerBalances[paymentCustomerKey] = 0m;
                }
                customerBalances[paymentCustomerKey] -= payment.Amount; // Subtract credit

                ledgerEntries.Add(new SalesLedgerEntryDto
                {
                    Date = payment.PaymentDate,
                    Type = "Payment",
                    InvoiceNo = invoiceNo,
                    CustomerId = payment.CustomerId,
                    CustomerName = payment.CustomerId.HasValue && customers.ContainsKey(payment.CustomerId.Value)
                        ? customers[payment.CustomerId.Value]
                        : "Cash Customer",
                    PaymentMode = payment.Mode.ToString().ToUpper(),
                    GrandTotal = payment.Amount, // CRITICAL: Payment amount
                    PaidAmount = 0, // Payments don't have paidAmount (they ARE the payment)
                    RealPending = 0, // Payments don't have pending
                    RealGotPayment = payment.Amount, // Real payment received
                    Status = status,
                    CustomerBalance = customerBalances[paymentCustomerKey],
                    PlanDate = null, // Payments don't have plan dates
                    PaymentId = payment.Id,
                    SaleId = payment.SaleId
                });
            }

            // Sort by date, then by type (Sales before Payments on same date)
            ledgerEntries = ledgerEntries
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Type == "Payment" ? 1 : 0)
                .ToList();

            // Calculate summary totals - CORRECTED CALCULATIONS
            // 1. Total Sales = Sum of GrandTotal from all sales in date range
            var totalSales = sales.Sum(s => s.GrandTotal);
            
            // 2. Total Payments = Sum of payments linked to sales in this period ONLY
            // CRITICAL: Only count payments that are linked to sales in the date range
            // This ensures: Total Payments <= Total Sales (logically correct)
            // We use salePayments dictionary which already has the correct totals per sale
            var saleIdsInPeriod = sales.Select(s => s.Id).ToHashSet();
            var totalPayments = salePayments
                .Where(kvp => saleIdsInPeriod.Contains(kvp.Key))
                .Sum(kvp => kvp.Value);
            
            // Alternative: Sum from payments directly (for verification)
            var totalPaymentsFromPayments = payments
                .Where(p => p.SaleId.HasValue && saleIdsInPeriod.Contains(p.SaleId.Value))
                .Sum(p => p.Amount);
            
            // Use the higher value to ensure accuracy (should be same, but handle edge cases)
            totalPayments = Math.Max(totalPayments, totalPaymentsFromPayments);
            
            // CRITICAL: Ensure payments never exceed sales (logically impossible)
            totalPayments = Math.Min(totalPayments, totalSales);
            
            // 3. Real Pending = Sum of unpaid amounts (GrandTotal - PaidAmount) from sales only
            var totalRealPending = ledgerEntries
                .Where(e => e.Type == "Sale")
                .Sum(e => e.RealPending);
            
            // 4. Total Real Got Payment = Total Payments (same value, different name)
            var totalRealGotPayment = totalPayments;
            
            // 5. Pending Balance = Total Sales - Total Payments (net outstanding)
            // This is the actual amount still owed after all payments
            var pendingBalance = totalSales - totalPayments;

            return new SalesLedgerReportDto
            {
                Entries = ledgerEntries,
                Summary = new SalesLedgerSummary
                {
                    TotalDebit = totalRealPending, // Keep for backward compatibility
                    TotalCredit = totalRealGotPayment, // Keep for backward compatibility
                    OutstandingBalance = pendingBalance, // CORRECTED: Use calculated pending balance
                    TotalSales = totalSales, // CORRECTED: Sum of all invoice amounts
                    TotalPayments = totalPayments // CORRECTED: Sum of all payments
                }
            };
        }

        public async Task<List<StaffPerformanceDto>> GetStaffPerformanceAsync(int tenantId, DateTime fromDate, DateTime toDate, int? routeId = null) // FIX: Add route filter parameter
        {
            var from = fromDate.ToUtcKind();
            var to = toDate.AddDays(1).AddTicks(-1).ToUtcKind();

            var staffUsers = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Role == UserRole.Staff)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            var result = new List<StaffPerformanceDto>();

            foreach (var staff in staffUsers)
            {
                var routeIdsFromRouteStaff = await _context.RouteStaff
                    .Where(rs => rs.UserId == staff.Id)
                    .Select(rs => rs.RouteId)
                    .ToListAsync();
                var routeIdsFromAssigned = await _context.Routes
                    .Where(r => r.TenantId == tenantId && r.AssignedStaffId == staff.Id)
                    .Select(r => r.Id)
                    .ToListAsync();
                var allRouteIds = routeIdsFromRouteStaff.Union(routeIdsFromAssigned).Distinct().ToList();
                var routeNames = await _context.Routes
                    .Where(r => allRouteIds.Contains(r.Id))
                    .Select(r => r.Name)
                    .ToListAsync();
                var assignedRoutes = string.Join(", ", routeNames);
                if (string.IsNullOrEmpty(assignedRoutes)) assignedRoutes = "-";

                // FIX: Filter sales by staff's assigned routes only (route-scoped performance)
                var salesQuery = _context.Sales
                    .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.CreatedBy == staff.Id
                        && s.InvoiceDate >= from && s.InvoiceDate < to);
                
                // FIX: Only count sales for routes assigned to this staff member
                if (allRouteIds != null && allRouteIds.Count > 0)
                {
                    salesQuery = salesQuery.Where(s => s.RouteId != null && allRouteIds.Contains(s.RouteId.Value));
                    
                    // FIX: Additional route filter if provided (for route-specific performance)
                    if (routeId.HasValue)
                    {
                        salesQuery = salesQuery.Where(s => s.RouteId == routeId.Value);
                    }
                }
                else
                {
                    // Staff with no routes assigned should show 0 performance
                    salesQuery = salesQuery.Where(s => false);
                }
                
                var sales = await salesQuery.ToListAsync();

                var invoicesCreated = sales.Count;
                var totalBilled = sales.Sum(s => s.GrandTotal);
                var cashCollected = sales.Sum(s => s.PaidAmount);
                var collectionRate = totalBilled > 0 ? (double)(cashCollected / totalBilled * 100) : 0;

                var avgDaysToPay = 0.0;
                var paidSales = sales.Where(s => s.PaidAmount > 0 && s.LastPaymentDate.HasValue).ToList();
                if (paidSales.Any())
                {
                    avgDaysToPay = paidSales.Average(s => (s.LastPaymentDate!.Value - s.InvoiceDate).TotalDays);
                }

                result.Add(new StaffPerformanceDto
                {
                    UserId = staff.Id,
                    UserName = staff.Name ?? "Unknown",
                    AssignedRoutes = assignedRoutes,
                    InvoicesCreated = invoicesCreated,
                    TotalBilled = totalBilled,
                    CashCollected = cashCollected,
                    CollectionRatePercent = (decimal)Math.Round(collectionRate, 1),
                    AvgDaysToPay = Math.Round(avgDaysToPay, 1)
                });
            }

            return result.OrderByDescending(r => r.TotalBilled).ToList();
        }
    }

    public class ProductSalesDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? CostValue { get; set; }
        public decimal? GrossProfit { get; set; }
        public decimal? MarginPercent { get; set; }
        public int TotalSales { get; set; }
        public decimal StockOnHand { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsLowStock { get; set; }
    }
}

