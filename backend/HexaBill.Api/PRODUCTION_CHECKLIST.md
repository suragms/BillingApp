# Production Deployment Checklist

## Pre-Deployment Validation

### 1. Database Migrations
- [ ] All migrations tested on staging PostgreSQL database
- [ ] No SQLite-specific syntax in migrations
- [ ] All migrations are idempotent (can be re-run safely)
- [ ] Migration rollback tested
- [ ] Database schema matches Entity Framework model

### 2. Tenant Isolation
- [ ] Create 2 test tenants
- [ ] Verify Tenant A cannot see Tenant B's data
- [ ] Verify Tenant A cannot modify Tenant B's data
- [ ] Verify Tenant A cannot delete Tenant B's data
- [ ] Test SuperAdmin can see all tenants (expected behavior)
- [ ] Verify file uploads are tenant-isolated

### 3. Performance Testing
- [ ] Test with 10,000+ records per tenant
- [ ] Identify queries taking >500ms
- [ ] Verify pagination works on all list endpoints
- [ ] Test API rate limiting (100 req/min)
- [ ] Memory usage test (check for leaks)
- [ ] Cold start test (Render starter plan)

### 4. Error Handling
- [ ] All controllers have try/catch blocks
- [ ] All errors return structured ApiResponse
- [ ] No raw exceptions exposed to frontend
- [ ] Error logging works (check ErrorLogs table)
- [ ] Health check endpoint works (/api/health)

### 5. Financial Operations
- [ ] All Sales creation wrapped in transactions
- [ ] All Payments wrapped in transactions
- [ ] Stock adjustments wrapped in transactions
- [ ] Balance calculations verified
- [ ] Payment integrity checks pass

### 6. Security
- [ ] Swagger disabled in production
- [ ] JWT tokens expire correctly
- [ ] Password hashing uses BCrypt
- [ ] SQL injection prevention verified
- [ ] CORS configured correctly
- [ ] Rate limiting enabled

### 7. Backup & Restore
- [ ] Backup creation tested
- [ ] Backup restore tested
- [ ] Verify backups are tenant-scoped (not cross-tenant)
- [ ] Test restore on staging database

### 8. Environment Variables
- [ ] Database connection string set
- [ ] JWT secret key set
- [ ] SMTP configured (if email enabled)
- [ ] R2 storage configured (if using cloud storage)
- [ ] ASPNETCORE_ENVIRONMENT=Production

### 9. Monitoring
- [ ] Health check endpoint accessible
- [ ] Request logging enabled
- [ ] Error logging enabled
- [ ] Slow query logging enabled (>500ms)
- [ ] Correlation IDs in logs

### 10. Load Testing
- [ ] Test with 10 concurrent users
- [ ] Test with 50 concurrent users
- [ ] Test with 100 concurrent users
- [ ] Verify no memory leaks
- [ ] Verify no connection pool exhaustion
- [ ] Verify response times acceptable

## Post-Deployment Validation

### 1. Smoke Tests
- [ ] Login works
- [ ] Dashboard loads
- [ ] Create product works
- [ ] Create customer works
- [ ] Create sale works
- [ ] Payment works
- [ ] Reports load

### 2. Data Integrity
- [ ] Create sale → verify stock decremented
- [ ] Create payment → verify balance updated
- [ ] Delete customer → verify related data handled
- [ ] Update product → verify changes saved

### 3. Error Scenarios
- [ ] Invalid login → proper error message
- [ ] Missing data → proper 404 response
- [ ] Invalid input → proper 400 response
- [ ] Unauthorized access → proper 403 response

### 4. Performance Monitoring
- [ ] Check slow query logs
- [ ] Check error logs
- [ ] Monitor memory usage
- [ ] Monitor response times
- [ ] Check database connection pool

## Critical Production Rules

1. **Never deploy without testing migrations on staging**
2. **Never skip tenant isolation tests**
3. **Never disable error logging**
4. **Never remove transaction wrapping from financial operations**
5. **Never expose raw exceptions to frontend**
6. **Always verify backups before production deployment**
7. **Always test restore procedure**
8. **Always monitor error logs after deployment**
9. **Always verify health check endpoint**
10. **Always test with realistic data volumes**

## Emergency Rollback Procedure

1. Stop deployment
2. Restore database from backup
3. Revert code to previous version
4. Verify system is operational
5. Investigate root cause
6. Fix issue
7. Re-test before re-deployment
