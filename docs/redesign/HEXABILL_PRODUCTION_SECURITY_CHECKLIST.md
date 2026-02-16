# 🔒 HEXABILL PRODUCTION SECURITY & PERFORMANCE CHECKLIST
## Critical Issues, Data Leakage Prevention, Performance Optimization, Testing Framework

**Last Updated:** February 16, 2026  
**Target:** Production deployment on Vercel + Render + PostgreSQL  
**Risk Level:** HIGH (Multi-tenant SaaS with financial data)

---

## 🚨 CRITICAL SECURITY RISKS IDENTIFIED

### **1. DATA ISOLATION RISKS (HIGHEST PRIORITY)**

#### **Multi-Tenant Data Leakage**

**Problem:** In multi-tenant systems, one company's data can accidentally be shown to another company.

**Where This Can Happen:**
```csharp
// ❌ DANGEROUS - No tenant isolation
var invoices = await _context.Invoices.ToListAsync();

// ✅ SAFE - Always filter by CompanyId
var invoices = await _context.Invoices
    .Where(i => i.CompanyId == currentUser.CompanyId)
    .ToListAsync();
```

**Checklist:**

- [ ] **AUDIT EVERY DATABASE QUERY**
  - Every `_context.{Table}` must include `.Where(x => x.CompanyId == currentUser.CompanyId)`
  - Check ALL controllers: Products, Invoices, Customers, Purchases, etc.
  - Search codebase for: `_context.` and verify CompanyId filter

- [ ] **Row-Level Security (RLS) in PostgreSQL**
```sql
-- Enable RLS on all tables
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE purchases ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE expenses ENABLE ROW LEVEL SECURITY;

-- Create policy (example for invoices)
CREATE POLICY tenant_isolation_policy ON invoices
    USING (company_id = current_setting('app.current_company_id')::int);

-- Set company context per request
SET app.current_company_id = '{companyId}';
```

- [ ] **Test Data Isolation**
```bash
# Create test script:
# 1. Create Company A with 10 invoices
# 2. Create Company B with 10 invoices
# 3. Login as Company A user
# 4. Try to access Company B invoice by ID
# Expected: 404 Not Found or 403 Forbidden
# Actual: Should NEVER return Company B data
```

**Test Cases:**
```
✅ User from Company A cannot see Company B customers
✅ User from Company A cannot access Company B invoices via direct URL
✅ User from Company A cannot modify Company B products
✅ Reports only show data for current company
✅ Dashboard KPIs only calculate for current company
✅ Search results filtered by company
✅ CSV exports only include current company data
```

---

### **2. AUTHENTICATION & AUTHORIZATION ISSUES**

#### **Check Current Implementation**

**Location:** `/backend/HexaBill.Api/Modules/Auth/`

**Critical Checks:**

- [ ] **JWT Token Security**
```csharp
// Check Program.cs or Startup.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // ✅ Must be true
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"])
            ),
            ClockSkew = TimeSpan.Zero // ✅ Prevent token replay
        };
    });
```

**Test:**
```bash
# 1. Login and get token
# 2. Wait for token to expire
# 3. Try to use expired token
# Expected: 401 Unauthorized
```

- [ ] **Password Security**
```csharp
// Must use proper hashing (bcrypt, argon2, PBKDF2)
// ❌ NEVER store plain text passwords
// ❌ NEVER use MD5 or SHA1

// ✅ Example with ASP.NET Identity
var hashedPassword = _passwordHasher.HashPassword(user, password);
```

**Requirements:**
- Minimum 8 characters
- At least 1 uppercase
- At least 1 number
- At least 1 special character
- Hash with bcrypt (cost factor 10+)

- [ ] **Session Management**
```csharp
// Check if session versioning exists
public class User
{
    public int SessionVersion { get; set; } // Increment on password change
}

// Validate session version in JWT claims
if (tokenSessionVersion != user.SessionVersion)
{
    return Unauthorized("Session expired");
}
```

- [ ] **Role-Based Access Control (RBAC)**
```csharp
// Every controller action must have [Authorize] attribute
[Authorize(Roles = "Owner,Admin")]
public async Task<IActionResult> DeleteCustomer(int id)
{
    // Owner can delete, Staff cannot
}

[Authorize(Roles = "Owner,Admin,Staff")]
public async Task<IActionResult> CreateInvoice()
{
    // All roles can create invoices
}
```

**Test RBAC:**
```
✅ Staff cannot access Settings page
✅ Staff cannot delete customers
✅ Staff cannot view financial reports (if restricted)
✅ Staff cannot add/remove other staff
✅ Owner can do everything
✅ Admin can do most things (not change subscription)
```

---

### **3. SQL INJECTION PREVENTION**

**Problem:** User input directly in SQL queries allows attackers to access/modify data.

**Check Every Query:**

```csharp
// ❌ DANGEROUS - SQL Injection vulnerable
var query = $"SELECT * FROM Customers WHERE Name = '{searchTerm}'";
var customers = await _context.Customers.FromSqlRaw(query).ToListAsync();

// ✅ SAFE - Parameterized query
var customers = await _context.Customers
    .Where(c => c.Name.Contains(searchTerm) && c.CompanyId == companyId)
    .ToListAsync();

// ✅ SAFE - If raw SQL needed
var customers = await _context.Customers
    .FromSqlRaw("SELECT * FROM Customers WHERE Name = {0} AND CompanyId = {1}", searchTerm, companyId)
    .ToListAsync();
```

**Audit Locations:**
- Search functionality (Products, Customers, Invoices)
- Report filters (date ranges, customer filters)
- CSV import validation
- Any `FromSqlRaw()` or `ExecuteSqlRaw()` calls

**Test:**
```bash
# Try malicious inputs:
Search: "'; DROP TABLE Customers; --"
Search: "' OR '1'='1"
Customer Name: "<script>alert('XSS')</script>"

# Expected: Query fails safely or input sanitized
# Never: Data exposed or tables dropped
```

---

### **4. API RATE LIMITING**

**Problem:** Attackers can overload your server or brute force passwords without rate limiting.

**Implementation Needed:**

```csharp
// Install: AspNetCoreRateLimit
// In Program.cs
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
app.UseIpRateLimiting();
```

**Configuration (appsettings.json):**
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*/auth/login",
        "Period": "5m",
        "Limit": 5
      },
      {
        "Endpoint": "*/api/*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

**Specific Limits:**
- Login: 5 attempts per 5 minutes per IP
- Signup: 3 per hour per IP
- General API: 100 requests per minute per user
- File Upload: 10 per hour per user
- Export: 20 per hour per user

**Test:**
```bash
# Use script to send 100 login requests in 1 minute
# Expected: First 5 succeed, rest get 429 Too Many Requests
```

---

### **5. INPUT VALIDATION & SANITIZATION**

**Every User Input Must Be Validated:**

```csharp
// Install: FluentValidation

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name too long")
            .Matches(@"^[a-zA-Z0-9\s\-\.]+$").WithMessage("Invalid characters");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^[0-9]{10}$").WithMessage("Must be 10 digit number");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.GstNumber)
            .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .When(x => !string.IsNullOrEmpty(x.GstNumber))
            .WithMessage("Invalid GST format");
    }
}
```

**Validation Needed:**
- Customer name, phone, email, GST
- Product name, SKU, price, stock
- Invoice amounts (no negative)
- Date ranges (start < end)
- File uploads (type, size)
- Search queries (SQL injection prevention)

**XSS Prevention:**
```csharp
// Install: HtmlSanitizer

public string SanitizeInput(string input)
{
    var sanitizer = new HtmlSanitizer();
    return sanitizer.Sanitize(input);
}

// Use before saving to database
customer.Name = SanitizeInput(dto.Name);
customer.Address = SanitizeInput(dto.Address);
```

---

### **6. FILE UPLOAD SECURITY**

**Current Risk:** CSV/Excel imports and image uploads can be weaponized.

**Implementation:**

```csharp
public async Task<IActionResult> UploadProductImage(IFormFile file)
{
    // 1. Validate file exists
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded");

    // 2. Validate file size (max 5MB)
    if (file.Length > 5 * 1024 * 1024)
        return BadRequest("File too large. Max 5MB.");

    // 3. Validate file type
    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
    if (!allowedTypes.Contains(file.ContentType))
        return BadRequest("Invalid file type. Only JPG, PNG, WebP allowed.");

    // 4. Validate file extension
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
        return BadRequest("Invalid file extension");

    // 5. Generate safe filename (don't use user's filename)
    var safeFileName = $"{Guid.NewGuid()}{extension}";

    // 6. Scan for malware (optional but recommended)
    // Use ClamAV or similar

    // 7. Store in isolated directory
    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, 
        "uploads", currentUser.CompanyId.ToString(), safeFileName);
    
    // 8. Save file
    using (var stream = new FileStream(uploadPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Ok(new { fileName = safeFileName });
}
```

**CSV Import Security:**
```csharp
public async Task<IActionResult> ImportProducts(IFormFile file)
{
    // 1. Validate file
    if (file.ContentType != "text/csv" && 
        file.ContentType != "application/vnd.ms-excel")
        return BadRequest("Only CSV files allowed");

    // 2. Limit file size (max 10MB)
    if (file.Length > 10 * 1024 * 1024)
        return BadRequest("File too large");

    // 3. Parse with try-catch
    try
    {
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        // 4. Validate headers
        csv.Read();
        csv.ReadHeader();
        var expectedHeaders = new[] { "Name", "SKU", "Price", "Stock" };
        if (!ValidateHeaders(csv.HeaderRecord, expectedHeaders))
            return BadRequest("Invalid CSV format");

        // 5. Limit rows (max 5000)
        var records = csv.GetRecords<ProductImportDto>().Take(5000).ToList();

        // 6. Validate each row
        var validationResults = new List<string>();
        foreach (var record in records)
        {
            if (string.IsNullOrEmpty(record.Name))
                validationResults.Add($"Row {csv.Parser.Row}: Name required");
            if (record.Price < 0)
                validationResults.Add($"Row {csv.Parser.Row}: Price cannot be negative");
            // ... more validations
        }

        if (validationResults.Any())
            return BadRequest(new { errors = validationResults });

        // 7. Import in transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var record in records)
            {
                var product = new Product
                {
                    Name = SanitizeInput(record.Name),
                    SKU = SanitizeInput(record.SKU),
                    Price = record.Price,
                    Stock = record.Stock,
                    CompanyId = currentUser.CompanyId // ✅ Always set CompanyId
                };
                _context.Products.Add(product);
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(new { imported = records.Count });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Import failed");
        return StatusCode(500, "Import failed");
    }
}
```

---

### **7. ENVIRONMENT VARIABLES & SECRETS**

**Problem:** Hardcoded secrets in code expose credentials.

**Check `.env` and `appsettings.json`:**

```bash
# ❌ NEVER commit these files to git
.env
appsettings.json
appsettings.Production.json

# ✅ Add to .gitignore
echo ".env" >> .gitignore
echo "appsettings.json" >> .gitignore
echo "appsettings.Production.json" >> .gitignore
```

**Use Environment Variables:**

```bash
# Vercel (Frontend)
DATABASE_URL=postgresql://...
JWT_SECRET=...
STRIPE_KEY=...

# Render (Backend)
ConnectionStrings__DefaultConnection=postgresql://...
Jwt__Key=...
Jwt__Issuer=...
Jwt__Audience=...
```

**In Code:**
```csharp
// ❌ DANGEROUS
var jwtKey = "my-secret-key-12345";

// ✅ SAFE
var jwtKey = Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("JWT key not configured");
```

**Secrets Checklist:**
- [ ] Database connection string (not in git)
- [ ] JWT secret key (not in git)
- [ ] Stripe API keys (not in git)
- [ ] Email service keys (not in git)
- [ ] SMS service keys (not in git)
- [ ] AWS S3 credentials (not in git)
- [ ] Google OAuth credentials (not in git)

---

## ⚡ PERFORMANCE OPTIMIZATION

### **1. DATABASE QUERY OPTIMIZATION**

**Problem:** Slow queries cause 500 errors and timeouts.

**Add Indexes:**

```sql
-- Critical indexes for performance
CREATE INDEX idx_invoices_company_date ON invoices(company_id, created_at DESC);
CREATE INDEX idx_customers_company_name ON customers(company_id, name);
CREATE INDEX idx_products_company_sku ON products(company_id, sku);
CREATE INDEX idx_sales_company_date ON sales(company_id, sale_date DESC);
CREATE INDEX idx_payments_company_date ON payments(company_id, payment_date DESC);
CREATE INDEX idx_expenses_company_date ON expenses(company_id, expense_date DESC);

-- Composite indexes for common queries
CREATE INDEX idx_invoices_status_date ON invoices(company_id, status, created_at DESC);
CREATE INDEX idx_products_category_name ON products(company_id, category, name);
```

**Test Query Performance:**
```sql
-- Run EXPLAIN ANALYZE on slow queries
EXPLAIN ANALYZE
SELECT * FROM invoices 
WHERE company_id = 1 
  AND status = 'pending' 
  AND created_at > '2026-01-01'
ORDER BY created_at DESC;

-- Look for:
-- ✅ Index Scan (good)
-- ❌ Seq Scan (bad - needs index)
```

**Use Pagination:**
```csharp
// ❌ BAD - Loads all records
var invoices = await _context.Invoices
    .Where(i => i.CompanyId == companyId)
    .ToListAsync();

// ✅ GOOD - Pagination
var invoices = await _context.Invoices
    .Where(i => i.CompanyId == companyId)
    .OrderByDescending(i => i.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**Connection Pooling:**
```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=...;Maximum Pool Size=20;Connection Lifetime=0"
  }
}
```

---

### **2. CACHING STRATEGY**

**Implement Redis or In-Memory Cache:**

```csharp
// Install: Microsoft.Extensions.Caching.Memory

public class ProductService
{
    private readonly IMemoryCache _cache;
    
    public async Task<List<Product>> GetProductsAsync(int companyId)
    {
        var cacheKey = $"products_{companyId}";
        
        if (!_cache.TryGetValue(cacheKey, out List<Product> products))
        {
            products = await _context.Products
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            
            _cache.Set(cacheKey, products, cacheOptions);
        }
        
        return products;
    }
    
    public async Task InvalidateCache(int companyId)
    {
        _cache.Remove($"products_{companyId}");
    }
}
```

**What to Cache:**
- Product lists (5 minutes)
- Customer lists (10 minutes)
- Dashboard stats (2 minutes)
- Settings (30 minutes)
- Category lists (1 hour)

**What NOT to Cache:**
- Invoice lists (real-time data)
- Payment records (real-time)
- Stock levels (real-time)

---

### **3. FRONTEND OPTIMIZATION**

**Code Splitting:**
```jsx
// Lazy load heavy components
const Dashboard = lazy(() => import('./pages/Dashboard'));
const Reports = lazy(() => import('./pages/Reports'));
const POS = lazy(() => import('./pages/PosPage'));

// In Router
<Suspense fallback={<LoadingSpinner />}>
  <Route path="/dashboard" element={<Dashboard />} />
</Suspense>
```

**Image Optimization:**
```jsx
// Use Next.js Image component (if using Next.js)
import Image from 'next/image';

<Image 
  src="/product.jpg"
  width={200}
  height={200}
  alt="Product"
  loading="lazy"
/>

// Or optimize manually
// 1. Convert to WebP format
// 2. Generate thumbnails (100x100, 300x300)
// 3. Lazy load off-screen images
```

**API Call Optimization:**
```jsx
// Debounce search
import { debounce } from 'lodash';

const debouncedSearch = useCallback(
  debounce((query) => {
    fetchSearchResults(query);
  }, 300),
  []
);

// Cancel pending requests
const controller = new AbortController();
fetch('/api/products', { signal: controller.signal });
// On component unmount:
controller.abort();
```

---

### **4. RENDER.COM OPTIMIZATION**

**Current Plan:** Starter ($7/month)
- 512 MB RAM
- 0.5 CPU
- Limited database connections

**Optimizations:**

**1. Upgrade Plan if Needed:**
```
Starter: $7/mo - 512MB RAM (current)
Standard: $25/mo - 2GB RAM (recommended for production)
Pro: $85/mo - 4GB RAM (if >100 active companies)
```

**2. Database Connection Pooling:**
```csharp
// Limit connections (Render Starter has max 20)
"Maximum Pool Size=10;Minimum Pool Size=2"
```

**3. Background Jobs:**
```csharp
// Use Hangfire for background tasks
services.AddHangfire(config =>
    config.UsePostgreSqlStorage(Configuration.GetConnectionString("DefaultConnection")));

// Schedule jobs
RecurringJob.AddOrUpdate(
    "daily-backup",
    () => _backupService.RunBackupAsync(),
    Cron.Daily);
```

**4. Health Checks:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

---

## 🧪 TESTING FRAMEWORK

### **1. UNIT TESTS**

**Test Critical Business Logic:**

```csharp
// Install: xUnit, Moq

public class InvoiceServiceTests
{
    [Fact]
    public async Task CalculateTotalAmount_WithGST_ReturnsCorrectTotal()
    {
        // Arrange
        var items = new List<InvoiceItem>
        {
            new() { Quantity = 2, Price = 100, GstRate = 18 }
        };
        var service = new InvoiceService();

        // Act
        var total = service.CalculateTotalAmount(items);

        // Assert
        Assert.Equal(236, total); // (2 * 100) * 1.18 = 236
    }

    [Fact]
    public async Task CreateInvoice_WithInvalidCustomer_ThrowsException()
    {
        // Test that data isolation works
    }
}
```

**Test Coverage Target:** 70% minimum

**Critical Tests:**
- Invoice calculations (subtotal, tax, total)
- Payment calculations (balance, change)
- Stock updates (add, subtract, adjust)
- Profit/loss calculations
- Data isolation (CompanyId filtering)
- Authentication (token validation)
- Authorization (role checks)

---

### **2. INTEGRATION TESTS**

**Test API Endpoints:**

```csharp
public class InvoiceControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoiceControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInvoices_ReturnsOnlyCompanyInvoices()
    {
        // Arrange
        var token = await GetAuthTokenAsync("company1@test.com");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/invoices");

        // Assert
        response.EnsureSuccessStatusCode();
        var invoices = await response.Content.ReadFromJsonAsync<List<Invoice>>();
        Assert.All(invoices, i => Assert.Equal(1, i.CompanyId));
    }
}
```

---

### **3. SECURITY TESTS**

**Automated Security Scanning:**

```bash
# Install OWASP ZAP
# Run security scan
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://your-app.vercel.app

# Check for:
# - SQL Injection
# - XSS vulnerabilities
# - CSRF vulnerabilities
# - Authentication bypass
# - Session management issues
```

**Manual Security Tests:**

```bash
# 1. Test data isolation
# Login as Company A
# Try to access Company B invoice:
curl -H "Authorization: Bearer {company_a_token}" \
  https://api.hexabill.com/api/invoices/{company_b_invoice_id}
# Expected: 403 Forbidden or 404 Not Found

# 2. Test SQL injection
curl -X POST https://api.hexabill.com/api/products/search \
  -H "Content-Type: application/json" \
  -d '{"query": "'; DROP TABLE Products; --"}'
# Expected: Safe handling, no table dropped

# 3. Test rate limiting
for i in {1..10}; do
  curl -X POST https://api.hexabill.com/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email": "test@test.com", "password": "wrong"}'
done
# Expected: After 5 attempts, get 429 Too Many Requests

# 4. Test authentication
curl https://api.hexabill.com/api/invoices
# Expected: 401 Unauthorized

# 5. Test expired token
# Use token from yesterday
curl -H "Authorization: Bearer {expired_token}" \
  https://api.hexabill.com/api/invoices
# Expected: 401 Unauthorized
```

---

### **4. LOAD TESTING**

**Test with k6 or Apache JMeter:**

```javascript
// k6 test script
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 10 }, // Ramp up to 10 users
    { duration: '5m', target: 10 }, // Stay at 10 users
    { duration: '2m', target: 50 }, // Ramp up to 50 users
    { duration: '5m', target: 50 }, // Stay at 50 users
    { duration: '2m', target: 0 },  // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests under 500ms
    http_req_failed: ['rate<0.01'],   // Less than 1% errors
  },
};

export default function() {
  let response = http.get('https://api.hexabill.com/api/products', {
    headers: { 'Authorization': `Bearer ${__ENV.TOKEN}` },
  });
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(1);
}
```

**Run Test:**
```bash
k6 run load-test.js
```

**Acceptance Criteria:**
- 95% of requests under 500ms
- 99% of requests under 1000ms
- Error rate < 1%
- Can handle 50 concurrent users
- Dashboard loads in < 2s on 3G
- POS page responds in < 200ms

---

## 📋 PRODUCTION DEPLOYMENT CHECKLIST

### **Before Deploying:**

#### **Backend (Render)**

- [ ] Environment variables set correctly
  - [ ] Database URL (PostgreSQL)
  - [ ] JWT Secret (strong, random)
  - [ ] Stripe API keys (if using)
  - [ ] Email service keys
  - [ ] File upload storage credentials

- [ ] Database migrations applied
  ```bash
  dotnet ef database update --project HexaBill.Api
  ```

- [ ] Database indexes created (see SQL above)

- [ ] Row-Level Security (RLS) enabled

- [ ] Health check endpoint working
  ```bash
  curl https://your-api.render.com/health
  ```

- [ ] CORS configured for frontend domain
  ```csharp
  services.AddCors(options =>
  {
      options.AddPolicy("AllowFrontend",
          builder => builder
              .WithOrigins("https://hexabill.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
  });
  ```

- [ ] Rate limiting enabled

- [ ] Logging configured (Serilog to file + console)

- [ ] Error tracking (Sentry or similar)

- [ ] Background jobs scheduled (daily backup, etc.)

#### **Frontend (Vercel)**

- [ ] Environment variables set
  - [ ] VITE_API_URL (backend URL)
  - [ ] VITE_STRIPE_PUBLIC_KEY (if using)

- [ ] Build succeeds without errors
  ```bash
  npm run build
  ```

- [ ] Bundle size optimized (< 500KB initial)

- [ ] Code splitting enabled

- [ ] Images optimized (WebP format)

- [ ] Service worker for offline support (optional)

- [ ] Analytics configured (Google Analytics, etc.)

- [ ] Error tracking (Sentry or similar)

#### **Database (PostgreSQL)**

- [ ] Backups enabled (daily minimum)

- [ ] Connection pooling configured

- [ ] Monitoring enabled

- [ ] Slow query log enabled

- [ ] Indexes created

- [ ] RLS policies created

- [ ] Vacuum and analyze scheduled

#### **Security**

- [ ] SSL/TLS certificates valid

- [ ] HTTPS enforced (no HTTP)

- [ ] Security headers configured
  ```csharp
  app.Use(async (context, next) =>
  {
      context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
      context.Response.Headers.Add("X-Frame-Options", "DENY");
      context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
      context.Response.Headers.Add("Referrer-Policy", "no-referrer");
      context.Response.Headers.Add("Content-Security-Policy", 
          "default-src 'self'; script-src 'self' 'unsafe-inline';");
      await next();
  });
  ```

- [ ] Rate limiting active

- [ ] Input validation everywhere

- [ ] SQL injection protection verified

- [ ] XSS protection verified

- [ ] CSRF protection enabled

- [ ] Authentication working

- [ ] Authorization working

- [ ] Data isolation tested

---

## 🔧 MONITORING & ALERTS

### **Set Up Monitoring:**

**1. Application Performance Monitoring (APM)**
- Use: New Relic, DataDog, or Application Insights
- Monitor: Response times, error rates, throughput

**2. Database Monitoring**
- Use: pganalyze, DataDog, or native PostgreSQL monitoring
- Monitor: Query performance, connection count, disk usage

**3. Error Tracking**
- Use: Sentry
- Get alerts for: 500 errors, unhandled exceptions, critical errors

**4. Uptime Monitoring**
- Use: UptimeRobot, Pingdom, or StatusCake
- Check every 5 minutes: API health endpoint, frontend homepage

**5. Log Aggregation**
- Use: Loggly, Papertrail, or ELK stack
- Centralize logs from: Backend, frontend, database

### **Set Up Alerts:**

```yaml
Alerts:
  Critical (Immediate):
    - API downtime > 1 minute
    - Error rate > 5%
    - Database connection failures
    - Disk space > 90%
    - Memory usage > 90%

  Warning (15 minutes):
    - Response time > 1 second (p95)
    - Error rate > 1%
    - Failed background jobs
    - Low disk space (> 80%)

  Info (Daily):
    - Daily backup completion
    - New user signups
    - Revenue summary
```

---

## 🚀 POST-DEPLOYMENT TESTING

**Immediately After Deploy:**

1. **Smoke Tests** (5 minutes)
   - [ ] Homepage loads
   - [ ] Login works
   - [ ] Dashboard loads
   - [ ] Create invoice works
   - [ ] POS works
   - [ ] Reports generate

2. **Critical Path Testing** (15 minutes)
   - [ ] New user signup flow
   - [ ] Company onboarding
   - [ ] Create first invoice
   - [ ] Record payment
   - [ ] View dashboard stats
   - [ ] Export report

3. **Data Integrity Tests** (10 minutes)
   - [ ] Login as Company A, verify only Company A data visible
   - [ ] Login as Company B, verify only Company B data visible
   - [ ] Create invoice in Company A, verify Company B can't see it
   - [ ] Check dashboard calculations match database

4. **Performance Tests** (5 minutes)
   - [ ] Dashboard loads in < 2 seconds
   - [ ] Product list loads in < 1 second
   - [ ] Reports generate in < 5 seconds
   - [ ] Search responds in < 500ms

5. **Mobile Testing** (10 minutes)
   - [ ] Test on actual phone (not just browser DevTools)
   - [ ] Login works
   - [ ] Bottom navigation works
   - [ ] POS is usable
   - [ ] Forms are usable

---

## 📝 ONGOING MAINTENANCE

**Daily:**
- [ ] Check error logs for new issues
- [ ] Verify daily backup completed
- [ ] Check API response times

**Weekly:**
- [ ] Review slow query log
- [ ] Check disk space
- [ ] Review failed background jobs
- [ ] Update dependencies (security patches)

**Monthly:**
- [ ] Run full security audit
- [ ] Load testing
- [ ] Database optimization (vacuum, analyze)
- [ ] Review and update indexes
- [ ] Check for unused dependencies

---

## ✅ FINAL VERIFICATION

**Before Going Live:**

```bash
# Run all these commands and verify results

# 1. Backend health
curl https://api.hexabill.com/health

# 2. Frontend loads
curl -I https://hexabill.vercel.app

# 3. Database accessible
psql -h your-db-host -U your-user -d hexabill -c "SELECT COUNT(*) FROM companies;"

# 4. Rate limiting works
for i in {1..10}; do curl https://api.hexabill.com/api/auth/login; done

# 5. Authentication required
curl https://api.hexabill.com/api/invoices
# Should get 401

# 6. Data isolation works
# (Manual test - login as two different companies)

# 7. All tests pass
dotnet test
npm test

# 8. No vulnerabilities
npm audit
dotnet list package --vulnerable

# 9. SSL valid
curl -vI https://api.hexabill.com 2>&1 | grep -i "SSL certificate verify ok"

# 10. CORS working
curl -H "Origin: https://hexabill.vercel.app" \
  -H "Access-Control-Request-Method: POST" \
  -X OPTIONS https://api.hexabill.com/api/invoices
```

---

## 🎯 SUCCESS CRITERIA

Your Hexabill is production-ready when:

✅ All security tests pass  
✅ Data isolation verified (100% confident)  
✅ No SQL injection vulnerabilities  
✅ Rate limiting active  
✅ Authentication/Authorization working  
✅ All tests passing (unit + integration)  
✅ Load tests pass (50 concurrent users)  
✅ Response times < 500ms (p95)  
✅ Error rate < 1%  
✅ Database indexed and optimized  
✅ Backups running daily  
✅ Monitoring and alerts configured  
✅ SSL/HTTPS enforced  
✅ Environment variables secure  
✅ Mobile testing complete  

---

**Remember:** Security is not a one-time thing. It's an ongoing process. Review this checklist monthly and after every major release.

**Critical Security Mindset:**
- Assume every input is malicious
- Never trust user data
- Always filter by CompanyId
- Always validate and sanitize
- Always use parameterized queries
- Always test data isolation
- Always have backups

**Your customers trust you with their financial data. Don't let them down.** 🔒
