/*
Purpose: Tenant context middleware for multi-tenant SaaS isolation
Author: AI Assistant
Date: 2026
*/
using HexaBill.Api.Data;
using HexaBill.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace HexaBill.Api.Shared.Middleware
{
    /// <summary>
    /// Middleware that validates tenant and sets context for every request
    /// MUST be registered AFTER authentication, BEFORE authorization
    /// </summary>
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantContextMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Skip for public endpoints
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            if (path.StartsWith("/api/auth") ||
                path.StartsWith("/health") ||
                path == "/" ||
                path.StartsWith("/swagger"))
            {
                await _next(context);
                return;
            }

            // Skip if not authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            try
            {
                // Extract tenant_id from JWT claim - check multiple claim type formats (JWT serialization may vary)
                var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value
                    ?? context.User.FindFirst("owner_id")?.Value
                    ?? context.User.Claims.FirstOrDefault(c => c.Type.EndsWith("tenant_id", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? context.User.Claims.FirstOrDefault(c => c.Type.EndsWith("owner_id", StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    _logger.LogWarning("No tenant_id or owner_id claim found in token for user {UserId}",
                        context.User.FindFirst("id")?.Value);
                    
                    // SystemAdmin may not have tenant_id - allow to proceed
                    context.Items["TenantId"] = 0; // 0 = SystemAdmin
                    await _next(context);
                    return;
                }

                if (!int.TryParse(tenantIdClaim, out int tenantId))
                {
                    _logger.LogError("Invalid tenant_id format in token: {TenantIdClaim}", tenantIdClaim);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid tenant context");
                    return;
                }

                // SystemAdmin has TenantId = 0 (or null in database)
                if (tenantId == 0)
                {
                    context.Items["TenantId"] = 0;
                    await _next(context);
                    return;
                }

                // Validate tenant exists and is active
                var tenant = await dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                {
                    // Development: use first active tenant as fallback so app keeps working
                    if (_env.IsDevelopment())
                    {
                        var fallbackTenant = await dbContext.Tenants.AsNoTracking()
                            .Where(t => t.Status == TenantStatus.Active)
                            .OrderBy(t => t.Id)
                            .FirstOrDefaultAsync();
                        if (fallbackTenant != null)
                        {
                            _logger.LogWarning("Development: Tenant {TenantId} not found - using fallback tenant {FallbackId} ({Name})", tenantId, fallbackTenant.Id, fallbackTenant.Name);
                            tenant = fallbackTenant;
                            tenantId = fallbackTenant.Id;
                        }
                    }
                    if (tenant == null)
                    {
                        _logger.LogWarning("Tenant {TenantId} not found in database", tenantId);
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Tenant not found");
                        return;
                    }
                }

                // Check tenant status - in Development, auto-activate suspended/expired tenants
                var wouldBlock = tenant.Status == TenantStatus.Suspended ||
                    tenant.Status == TenantStatus.Expired ||
                    (tenant.Status == TenantStatus.Trial && tenant.TrialEndDate.HasValue && tenant.TrialEndDate.Value < DateTime.UtcNow);

                if (wouldBlock && _env.IsDevelopment())
                {
                    _logger.LogInformation("Development: Auto-activating tenant {TenantId} ({Name}) - was {Status}", tenant.Id, tenant.Name, tenant.Status);
                    var dbTenant = await dbContext.Tenants.FindAsync(tenantId);
                    if (dbTenant != null)
                    {
                        dbTenant.Status = TenantStatus.Active;
                        dbTenant.TrialEndDate = null;
                        await dbContext.SaveChangesAsync();
                    }
                    // CRITICAL: Must not fall through to 403 - continue to set tenant context
                }
                else if (wouldBlock)
                {
                    // Production: block suspended/expired tenants
                    var reason = tenant.Status == TenantStatus.Suspended ? "Tenant account is suspended"
                        : tenant.Status == TenantStatus.Expired ? "Tenant trial has expired"
                        : "Tenant trial has expired";
                    _logger.LogWarning("Tenant {TenantId} blocked: {Status}", tenantId, tenant.Status);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync(reason);
                    return;
                }

                // Set tenant context
                context.Items["TenantId"] = tenantId;

                // Set PostgreSQL session variable for RLS only when using PostgreSQL (SQLite does not support SET)
                var connString = (dbContext.Database.GetConnectionString() ?? "").Trim();
                var isSqlite = connString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                              connString.Contains(".db", StringComparison.OrdinalIgnoreCase);
                var isPostgres = connString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
                                connString.StartsWith("Server=", StringComparison.OrdinalIgnoreCase);
                if (!isSqlite && isPostgres)
                {
                    try
                    {
                        // PostgreSQL SET does not accept parameterized $1; use set_config which does
                        await dbContext.Database.ExecuteSqlRawAsync(
                            "SELECT set_config('app.tenant_id', {0}, true)",
                            tenantId.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set PostgreSQL session variable app.tenant_id");
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TenantContextMiddleware");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
            }
        }
    }

    public static class TenantContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantContextMiddleware>();
        }
    }
}
