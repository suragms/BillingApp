# 🤖 HEXABILL AUTOMATED CODE REVIEW PROMPT
## For Cursor Pro - Security, Performance, Data Leakage Detection

Copy this prompt into Cursor Pro to automatically review your codebase for security issues, data leakage risks, performance problems, and code quality issues.

---

## 🔍 MASTER CODE REVIEW PROMPT

```
You are an expert security auditor and performance engineer specializing in multi-tenant SaaS applications with financial data.

Context:
I'm building Hexabill, a multi-tenant billing SaaS in C# (ASP.NET Core) backend and React frontend. The app handles sensitive financial data for multiple companies. Data isolation is CRITICAL.

Your Mission:
Perform a comprehensive security and performance audit of the provided code. Identify EVERY potential issue, no matter how small.

---

CRITICAL SECURITY RULES TO CHECK:

1. DATA ISOLATION (HIGHEST PRIORITY)
   ✅ EVERY database query MUST filter by CompanyId
   ✅ NEVER allow cross-tenant data access
   ✅ Check: Are there any queries without CompanyId filter?

   Example of what to find:
   ❌ BAD: var invoices = await _context.Invoices.ToListAsync();
   ✅ GOOD: var invoices = await _context.Invoices.Where(i => i.CompanyId == currentUser.CompanyId).ToListAsync();

2. SQL INJECTION PREVENTION
   ✅ ALL queries must be parameterized
   ✅ NO string concatenation in SQL
   ✅ Check: Any FromSqlRaw() with string interpolation?

   Example:
   ❌ BAD: FromSqlRaw($"SELECT * FROM Products WHERE Name = '{name}'")
   ✅ GOOD: Where(p => p.Name.Contains(name))

3. AUTHENTICATION & AUTHORIZATION
   ✅ EVERY controller action needs [Authorize] attribute
   ✅ Sensitive actions need role checks
   ✅ Check: Any public endpoints that should be protected?

4. INPUT VALIDATION
   ✅ ALL user inputs must be validated
   ✅ No trust in client-side validation
   ✅ Check: Missing validation on DTOs?

5. XSS PREVENTION
   ✅ ALL user content must be sanitized before display
   ✅ Check: Any raw HTML rendering?

6. RATE LIMITING
   ✅ Login endpoints must be rate limited (5 per 5 min)
   ✅ API endpoints must be rate limited (100 per min)
   ✅ Check: Is rate limiting configured?

7. SENSITIVE DATA EXPOSURE
   ✅ NO passwords, keys, or secrets in code
   ✅ NO logging of sensitive data
   ✅ Check: Any hardcoded credentials?

8. BROKEN ACCESS CONTROL
   ✅ Users can only access their own company data
   ✅ Staff cannot access owner-only features
   ✅ Check: Any authorization bypass possible?

---

PERFORMANCE RULES TO CHECK:

1. DATABASE QUERIES
   ✅ Use indexes on frequently queried columns
   ✅ Use pagination for large result sets
   ✅ Avoid N+1 query problems
   ✅ Check: Any .ToList() followed by LINQ filtering?

   Example:
   ❌ BAD: var active = (await _context.Products.ToListAsync()).Where(p => p.IsActive);
   ✅ GOOD: var active = await _context.Products.Where(p => p.IsActive).ToListAsync();

2. CACHING
   ✅ Cache frequently accessed data
   ✅ Cache user settings, categories, etc.
   ✅ Check: Should this endpoint be cached?

3. ASYNC/AWAIT
   ✅ ALL I/O operations must be async
   ✅ NO blocking calls (.Result, .Wait())
   ✅ Check: Any synchronous database calls?

4. RESOURCE CLEANUP
   ✅ Dispose database connections
   ✅ Use 'using' statements
   ✅ Check: Any undisposed resources?

---

CODE QUALITY RULES TO CHECK:

1. ERROR HANDLING
   ✅ ALL operations wrapped in try-catch
   ✅ Meaningful error messages
   ✅ Log errors with context
   ✅ Check: Any unhandled exceptions?

2. NULL SAFETY
   ✅ Check for null before accessing
   ✅ Use null-conditional operators
   ✅ Check: Any potential NullReferenceException?

3. MAGIC NUMBERS
   ✅ NO hardcoded values
   ✅ Use constants or configuration
   ✅ Check: Any magic numbers?

4. CODE DUPLICATION
   ✅ Extract common logic to methods
   ✅ Use inheritance or composition
   ✅ Check: Any duplicated code?

---

YOUR ANALYSIS MUST INCLUDE:

For EACH file reviewed, provide:

## Security Issues
[List every security vulnerability found]

Severity: Critical | High | Medium | Low
Location: File.cs, Line X
Issue: [Description]
Risk: [What could happen]
Fix: [Specific code changes needed]

## Performance Issues
[List every performance problem found]

Severity: Critical | High | Medium | Low
Location: File.cs, Line X
Issue: [Description]
Impact: [How it affects users]
Fix: [Specific optimization needed]

## Code Quality Issues
[List every code smell found]

Severity: High | Medium | Low
Location: File.cs, Line X
Issue: [Description]
Improvement: [Specific refactoring needed]

## Data Leakage Risks
[List ANY query that doesn't filter by CompanyId]

CRITICAL: This is the #1 priority
Location: File.cs, Line X
Query: [Show the problematic query]
Risk: Company A could see Company B's data
Fix: Add .Where(x => x.CompanyId == currentUser.CompanyId)

## Missing Indexes
[List columns that need indexes]

Table: [Table name]
Column(s): [Column(s) to index]
Reason: [Why it needs an index]
SQL: CREATE INDEX idx_name ON table(column);

## Test Coverage Gaps
[List critical paths without tests]

Feature: [What's not tested]
Risk: [What could break]
Test Needed: [Describe test case]

---

OUTPUT FORMAT:

Provide a detailed report in this structure:

# Code Review Report: [File/Module Name]

## 🚨 Critical Issues (Fix Immediately)
[List all critical security and data leakage issues]

## ⚠️ High Priority Issues (Fix Before Production)
[List all high severity issues]

## 📝 Medium Priority Issues (Fix Soon)
[List all medium severity issues]

## 💡 Low Priority Issues (Nice to Have)
[List all minor improvements]

## ✅ Good Practices Found
[List things that are done well]

## 📊 Summary
- Total Issues: X
- Critical: X
- High: X
- Medium: X
- Low: X
- Estimated Fix Time: X hours

## 🎯 Top 3 Priorities
1. [Most critical issue]
2. [Second most critical]
3. [Third most critical]

---

IMPORTANT NOTES:

- Be EXTREMELY thorough - miss nothing
- Assume the worst case scenario for every issue
- Prioritize data isolation above everything else
- Provide specific, actionable fixes with code examples
- If you're unsure if something is an issue, flag it anyway
- Be brutally honest - this is production code with real user data

BEGIN CODE REVIEW NOW.
```

---

## 📂 HOW TO USE THIS PROMPT

### **Method 1: Review Entire Module**

1. Open Cursor Pro
2. Select all files in a module (e.g., `/Modules/Billing/`)
3. Paste the prompt above
4. Add: "Review all selected files for security and performance issues"
5. Review the report

### **Method 2: Review Single File**

1. Open the file you want to review
2. Open Cursor chat
3. Paste the prompt above
4. Add: "Review this file: [filename]"
5. Fix issues found

### **Method 3: Review Specific Function**

1. Highlight the function code
2. Open Cursor chat
3. Paste the prompt above
4. Add: "Review this function for security issues"
5. Apply fixes

---

## 🎯 PRIORITY REVIEW ORDER

Review in this order for maximum security:

### **1. Authentication & Authorization**
```
Files to review:
- /Modules/Auth/AuthController.cs
- /Modules/Auth/AuthService.cs
- /Shared/Authorization/*.cs
- /Shared/Middleware/AuthenticationMiddleware.cs
```

**Focus:** 
- JWT validation
- Password hashing
- Session management
- Role-based access control

### **2. Data Access Layer**
```
Files to review:
- /Data/AppDbContext.cs
- /Modules/*/Controllers/*.cs (all controllers)
- /Modules/*/Services/*.cs (all services)
```

**Focus:**
- CompanyId filtering on EVERY query
- SQL injection prevention
- Pagination
- Indexes

### **3. Input Validation**
```
Files to review:
- /Models/DTOs.cs
- /Shared/Validation/*.cs
- All controllers accepting POST/PUT requests
```

**Focus:**
- Input validation rules
- XSS prevention
- File upload validation
- Max length enforcement

### **4. Business Logic**
```
Files to review:
- /Modules/Billing/InvoiceService.cs
- /Modules/Reports/ReportService.cs
- /Modules/Inventory/ProductService.cs
- /Modules/Purchases/PurchaseService.cs
```

**Focus:**
- Calculation accuracy
- Business rule enforcement
- Error handling
- Transaction management

### **5. API Endpoints**
```
Files to review:
- All *Controller.cs files
```

**Focus:**
- Authorization attributes
- Rate limiting
- Input validation
- Error responses

---

## 🔍 SPECIFIC PROMPTS FOR COMMON CHECKS

### **Check Data Isolation in Controllers:**
```
Review all database queries in this controller.
For EACH query, verify:
1. Does it filter by CompanyId?
2. Could a user from Company A access Company B's data?
3. Is the CompanyId taken from the authenticated user (not request)?

List every query that is missing CompanyId filter.
Provide the exact line number and the fix.
```

### **Check for SQL Injection:**
```
Review all database queries in this file.
Find any queries that:
1. Use string concatenation
2. Use string interpolation in FromSqlRaw()
3. Don't use parameterized queries

List every potential SQL injection vulnerability.
Show the vulnerable code and the safe version.
```

### **Check Authentication:**
```
Review this controller.
For each action method:
1. Is there an [Authorize] attribute?
2. If it's a sensitive action, is there a role check?
3. Can unauthorized users access this endpoint?

List every unprotected endpoint.
Provide the correct authorization attribute.
```

### **Check Performance:**
```
Review this service for performance issues.
Look for:
1. N+1 query problems
2. Missing pagination
3. Synchronous database calls
4. Missing indexes
5. Unnecessary .ToList() calls
6. Missing caching

List every performance issue with severity.
Estimate the impact on response time.
```

### **Check Error Handling:**
```
Review this code for error handling.
Check:
1. Are all operations wrapped in try-catch?
2. Are errors logged with context?
3. Are user-friendly messages returned?
4. Are exceptions properly handled?

List every missing error handler.
Show the code with proper error handling added.
```

---

## 📊 AUTOMATED TESTING PROMPTS

### **Generate Unit Tests:**
```
Generate comprehensive unit tests for this service.
Cover:
1. Happy path scenarios
2. Edge cases
3. Error scenarios
4. Data isolation (CompanyId filtering)
5. Calculation accuracy

Use xUnit and Moq.
Include setup, test cases, and assertions.
Target 80% code coverage.
```

### **Generate Integration Tests:**
```
Generate integration tests for this controller.
Test:
1. Authentication required
2. Authorization (role checks)
3. Data isolation (Company A can't see Company B)
4. Input validation
5. Response format

Use WebApplicationFactory.
Include setup, HTTP calls, and assertions.
```

### **Generate Security Tests:**
```
Generate security test cases for this endpoint.
Test for:
1. SQL injection attempts
2. XSS attempts
3. Authentication bypass
4. Authorization bypass
5. Rate limiting
6. Data leakage between tenants

Provide test code and expected results.
```

---

## 🛠️ FIX AUTOMATION PROMPTS

### **Auto-Fix Data Isolation:**
```
Add CompanyId filtering to every query in this file.
Rules:
1. Get CompanyId from HttpContext.User.Claims
2. Add .Where(x => x.CompanyId == companyId) to every query
3. Don't break existing functionality
4. Add comments explaining the isolation

Show me the updated code.
```

### **Auto-Add Authorization:**
```
Add [Authorize] attributes to every controller action.
Rules:
1. Public endpoints: No attribute
2. Regular endpoints: [Authorize]
3. Admin only: [Authorize(Roles = "Owner,Admin")]
4. Owner only: [Authorize(Roles = "Owner")]

Show me the updated controller.
```

### **Auto-Add Input Validation:**
```
Add FluentValidation validators for all DTOs in this file.
Rules:
1. Required fields: NotEmpty()
2. String fields: MaxLength(X)
3. Email fields: EmailAddress()
4. Phone fields: Matches(regex)
5. Numeric fields: GreaterThanOrEqualTo(0)

Generate complete validator classes.
```

### **Auto-Add Error Handling:**
```
Wrap all operations in try-catch blocks.
Rules:
1. Log errors with context
2. Return appropriate HTTP status codes
3. Return user-friendly messages
4. Don't expose internal errors

Show me the updated code.
```

---

## 📝 REVIEW CHECKLIST

After running the automated review, manually verify:

- [ ] All queries filter by CompanyId
- [ ] All endpoints have [Authorize] attribute
- [ ] All inputs are validated
- [ ] All sensitive operations are logged
- [ ] All errors are handled gracefully
- [ ] All database calls are async
- [ ] All resources are disposed
- [ ] No hardcoded secrets
- [ ] No SQL injection vulnerabilities
- [ ] No XSS vulnerabilities
- [ ] Rate limiting is configured
- [ ] CORS is configured correctly
- [ ] HTTPS is enforced
- [ ] Security headers are set

---

## 🎯 SUCCESS METRICS

Your codebase is secure when:

✅ Zero queries without CompanyId filter  
✅ Zero endpoints without [Authorize]  
✅ Zero SQL injection vulnerabilities  
✅ Zero XSS vulnerabilities  
✅ 80%+ test coverage  
✅ All critical paths tested  
✅ All security tests passing  
✅ All inputs validated  
✅ All errors handled  
✅ No hardcoded secrets  

---

**Use these prompts religiously. Your users' financial data depends on it.** 🔒
