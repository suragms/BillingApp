/*
 * Branch + Route services for enterprise branch/route architecture.
 */
using Microsoft.EntityFrameworkCore;
using HexaBill.Api.Data;
using HexaBill.Api.Models;

namespace HexaBill.Api.Modules.Branches
{
    public interface IBranchService
    {
        Task<List<BranchDto>> GetBranchesAsync(int tenantId);
        Task<BranchDto?> GetBranchByIdAsync(int id, int tenantId);
        Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, int tenantId);
        Task<BranchDto?> UpdateBranchAsync(int id, CreateBranchRequest request, int tenantId);
        Task<bool> DeleteBranchAsync(int id, int tenantId);
        Task<BranchSummaryDto?> GetBranchSummaryAsync(int branchId, int tenantId, DateTime? fromDate, DateTime? toDate);
    }

    public class BranchService : IBranchService
    {
        private readonly AppDbContext _context;

        public BranchService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BranchDto>> GetBranchesAsync(int tenantId)
        {
            return await _context.Branches
                .AsNoTracking()
                .Where(b => tenantId <= 0 || b.TenantId == tenantId)
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    TenantId = b.TenantId,
                    Name = b.Name,
                    Address = b.Address,
                    CreatedAt = b.CreatedAt,
                    RouteCount = b.Routes.Count,
                    AssignedStaffIds = b.BranchStaff.Select(bs => bs.UserId).ToList()
                })
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<BranchDto?> GetBranchByIdAsync(int id, int tenantId)
        {
            var b = await _context.Branches
                .AsNoTracking()
                .Where(x => x.Id == id && (tenantId <= 0 || x.TenantId == tenantId))
                .Select(x => new BranchDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    Name = x.Name,
                    Address = x.Address,
                    CreatedAt = x.CreatedAt,
                    RouteCount = x.Routes.Count,
                    AssignedStaffIds = x.BranchStaff.Select(bs => bs.UserId).ToList()
                })
                .FirstOrDefaultAsync();
            return b;
        }

        public async Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, int tenantId)
        {
            var branch = new Branch
            {
                TenantId = tenantId,
                Name = request.Name.Trim(),
                Address = request.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // Assign staff to branch if provided
            if (request.AssignedStaffIds != null && request.AssignedStaffIds.Any())
            {
                foreach (var staffId in request.AssignedStaffIds)
                {
                    _context.BranchStaff.Add(new BranchStaff { BranchId = branch.Id, UserId = staffId, AssignedAt = DateTime.UtcNow });
                }
                await _context.SaveChangesAsync();
            }

            return new BranchDto
            {
                Id = branch.Id,
                TenantId = branch.TenantId,
                Name = branch.Name,
                Address = branch.Address,
                CreatedAt = branch.CreatedAt,
                RouteCount = 0,
                AssignedStaffIds = request.AssignedStaffIds ?? new List<int>()
            };
        }

        public async Task<BranchDto?> UpdateBranchAsync(int id, CreateBranchRequest request, int tenantId)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);
            if (branch == null) return null;
            branch.Name = request.Name.Trim();
            branch.Address = request.Address?.Trim();
            branch.UpdatedAt = DateTime.UtcNow;

            if (request.AssignedStaffIds != null)
            {
                // Remove existing
                var existing = await _context.BranchStaff.Where(bs => bs.BranchId == id).ToListAsync();
                _context.BranchStaff.RemoveRange(existing);

                // Add new
                foreach (var staffId in request.AssignedStaffIds)
                {
                    _context.BranchStaff.Add(new BranchStaff { BranchId = branch.Id, UserId = staffId, AssignedAt = DateTime.UtcNow });
                }
            }

            await _context.SaveChangesAsync();
            return await GetBranchByIdAsync(id, tenantId);
        }

        public async Task<bool> DeleteBranchAsync(int id, int tenantId)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);
            if (branch == null) return false;
            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BranchSummaryDto?> GetBranchSummaryAsync(int branchId, int tenantId, DateTime? fromDate, DateTime? toDate)
        {
            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == branchId && (tenantId <= 0 || b.TenantId == tenantId));
            if (branch == null) return null;

            var from = fromDate ?? DateTime.UtcNow.Date.AddYears(-1);
            var to = (toDate ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

            var routeIds = await _context.Routes.Where(r => r.BranchId == branchId && (tenantId <= 0 || r.TenantId == tenantId)).Select(r => r.Id).ToListAsync();
            var salesByRoute = await _context.Sales
                .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId!.Value) && (tenantId <= 0 || s.TenantId == tenantId) && !s.IsDeleted && s.InvoiceDate >= from && s.InvoiceDate <= to)
                .GroupBy(s => s.RouteId)
                .Select(g => new { RouteId = g.Key!.Value, Total = g.Sum(s => s.GrandTotal) })
                .ToListAsync();

            var saleIdsInBranch = await _context.Sales
                .Where(s => s.RouteId != null && routeIds.Contains(s.RouteId!.Value) && (tenantId <= 0 || s.TenantId == tenantId) && !s.IsDeleted && s.InvoiceDate >= from && s.InvoiceDate <= to)
                .Select(s => s.Id)
                .ToListAsync();

            var cogsByRouteList = new List<(int RouteId, decimal Cogs)>();
            if (saleIdsInBranch.Count > 0)
            {
                try
                {
                    // Use explicit Select to only get CostPrice (avoid loading entire Product entity with missing columns)
                    var cogsQuery = await (from si in _context.SaleItems
                        join s in _context.Sales on si.SaleId equals s.Id
                        where saleIdsInBranch.Contains(si.SaleId) && s.RouteId != null
                        select new { si.SaleId, si.Qty, si.ProductId, s.RouteId })
                        .ToListAsync();
                    
                    // Get CostPrice separately to avoid loading entire Product entity
                    var productIds = cogsQuery.Select(x => x.ProductId).Distinct().ToList();
                    var productCosts = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.CostPrice })
                        .ToDictionaryAsync(p => p.Id, p => p.CostPrice);
                    
                    var cogsByRoute = cogsQuery
                        .Where(x => x.RouteId.HasValue && productCosts.ContainsKey(x.ProductId))
                        .GroupBy(x => x.RouteId!.Value)
                        .Select(g => new { RouteId = g.Key, Cogs = g.Sum(x => x.Qty * productCosts[x.ProductId]) })
                        .ToList();
                    
                    cogsByRouteList = cogsByRoute.Select(x => (x.RouteId, x.Cogs)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error calculating branch COGS: {ex.Message}");
                    // Return empty list - COGS will be 0 for all routes
                    cogsByRouteList = new List<(int RouteId, decimal Cogs)>();
                }
            }
            var expensesByRoute = await _context.RouteExpenses
                .Where(e => routeIds.Contains(e.RouteId) && (tenantId <= 0 || e.TenantId == tenantId) && e.ExpenseDate >= from && e.ExpenseDate <= to)
                .GroupBy(e => e.RouteId)
                .Select(g => new { RouteId = g.Key, Total = g.Sum(e => e.Amount) })
                .ToListAsync();

            // Branch-level expenses (Rent, Utilities, etc.) - separate from route expenses
            var branchExpensesTotal = await _context.Expenses
                .Where(e => e.BranchId == branchId && (tenantId <= 0 || e.TenantId == tenantId) && e.Date >= from && e.Date <= to)
                .SumAsync(e => e.Amount);

            var routeList = await _context.Routes
                .AsNoTracking()
                .Where(r => r.BranchId == branchId && (tenantId <= 0 || r.TenantId == tenantId))
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            var routes = routeList.Select(r =>
            {
                var sales = salesByRoute.FirstOrDefault(x => x.RouteId == r.Id)?.Total ?? 0;
                var expenses = expensesByRoute.FirstOrDefault(x => x.RouteId == r.Id)?.Total ?? 0;
                var cogs = cogsByRouteList.FirstOrDefault(x => x.RouteId == r.Id).Cogs;
                return new RouteSummaryDto
                {
                    RouteId = r.Id,
                    RouteName = r.Name,
                    BranchName = branch.Name,
                    TotalSales = sales,
                    TotalExpenses = expenses,
                    CostOfGoodsSold = cogs,
                    Profit = sales - cogs - expenses
                };
            }).ToList();

            var routeExpensesTotal = routes.Sum(r => r.TotalExpenses);
            var totalExpenses = routeExpensesTotal + branchExpensesTotal;
            var totalSales = routes.Sum(r => r.TotalSales);
            var totalCogs = routes.Sum(r => r.CostOfGoodsSold);

            // Performance metrics
            var invoiceCount = saleIdsInBranch.Count;
            var averageInvoiceSize = invoiceCount > 0 ? totalSales / invoiceCount : 0m;

            // Calculate total payments for this branch's customers in the period
            var branchCustomerIds = await _context.Customers
                .Where(c => c.BranchId == branchId && (tenantId <= 0 || c.TenantId == tenantId))
                .Select(c => c.Id)
                .ToListAsync();
            
            var totalPayments = 0m;
            if (branchCustomerIds.Any())
            {
                totalPayments = await _context.Payments
                    .Where(p => p.CustomerId != null && branchCustomerIds.Contains(p.CustomerId.Value) && 
                                (tenantId <= 0 || p.TenantId == tenantId) && 
                                p.PaymentDate >= from && p.PaymentDate <= to)
                    .SumAsync(p => p.Amount);
            }

            // Collections ratio: Payments / Total Sales (for credit customers)
            var collectionsRatio = totalSales > 0 ? (totalPayments / totalSales * 100) : (decimal?)null;

            // Calculate growth percent (compare with previous period of same duration)
            decimal? growthPercent = null;
            var periodDays = (int)(to - from).TotalDays;
            if (periodDays > 0)
            {
                var prevTo = from.AddDays(-1);
                var prevFrom = prevTo.AddDays(-periodDays);
                var prevSummary = await GetBranchSummaryAsync(branch.Id, tenantId, prevFrom, prevTo.AddDays(1));
                if (prevSummary != null && prevSummary.TotalSales > 0)
                {
                    growthPercent = ((totalSales - prevSummary.TotalSales) / prevSummary.TotalSales * 100);
                }
                else if (prevSummary != null && totalSales > 0)
                {
                    growthPercent = 100; // 100% growth if no previous sales
                }
            }

            return new BranchSummaryDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                TotalSales = totalSales,
                TotalExpenses = totalExpenses,
                CostOfGoodsSold = totalCogs,
                Profit = totalSales - totalCogs - totalExpenses,
                Routes = routes,
                GrowthPercent = growthPercent,
                CollectionsRatio = collectionsRatio,
                AverageInvoiceSize = averageInvoiceSize,
                InvoiceCount = invoiceCount,
                TotalPayments = totalPayments
            };
        }
    }
}
