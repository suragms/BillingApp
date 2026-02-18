# Cloudflare R2 Storage Setup Guide

## Overview
HexaBill now supports Cloudflare R2 for persistent file storage, preventing data loss on Render's ephemeral disk. R2 is S3-compatible object storage with a free tier of 10GB.

## Benefits
- **Persistent Storage**: Files survive server restarts and deploys
- **Free Tier**: 10GB free storage, then $0.015/GB/month
- **Fast CDN**: Files served via Cloudflare's global network
- **S3-Compatible**: Uses AWS SDK, easy to migrate

## Setup Steps

### 1. Create Cloudflare R2 Bucket
1. Log in to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Go to **R2** → **Create bucket**
3. Name your bucket (e.g., `hexabill-uploads`)
4. Choose a location (closest to your users)

### 2. Create API Token
1. Go to **R2** → **Manage R2 API Tokens**
2. Click **Create API Token**
3. Set permissions:
   - **Object Read & Write** (or **Admin Read & Write**)
   - Select your bucket
4. Copy the **Access Key ID** and **Secret Access Key**

### 3. Get R2 Endpoint URL
Your R2 endpoint follows this format:
```
https://<account-id>.r2.cloudflarestorage.com
```

To find your account ID:
- Go to **R2** → **Overview**
- Your account ID is shown in the URL or dashboard

### 4. Set Up Custom Domain (Optional but Recommended)
1. Go to **R2** → Your bucket → **Settings**
2. Under **Public Access**, click **Connect Domain**
3. Add a custom domain (e.g., `files.yourdomain.com`)
4. Update DNS records as instructed
5. This gives you a public URL like: `https://files.yourdomain.com/`

### 5. Configure Environment Variables
Add these to your Render environment variables (or `appsettings.json` for local dev):

```bash
R2_ENDPOINT=https://<account-id>.r2.cloudflarestorage.com
R2_ACCESS_KEY=your-access-key-id
R2_SECRET_KEY=your-secret-access-key
R2_BUCKET_NAME=hexabill-uploads
R2_PUBLIC_URL=https://files.yourdomain.com  # Optional: if using custom domain
```

### 6. Verify Setup
After deploying with R2 configured:
1. Check application logs for: `✅ Cloudflare R2 storage enabled for file uploads`
2. Upload a logo or product image
3. Verify the file is accessible via the public URL

## Fallback Behavior
- If R2 is **not configured**: System uses local disk storage (files lost on restart/deploy)
- If R2 is **configured**: All uploads go to R2, files persist permanently

## Migration from Local Storage
Existing files on local disk will remain there. New uploads will go to R2. To migrate existing files:
1. Export files from local disk
2. Upload them again through the UI (they'll be stored in R2)

## Cost Estimate
- **Free Tier**: First 10GB free
- **After 10GB**: $0.015/GB/month
- **Example**: 50GB storage = ~$0.60/month

## Troubleshooting

### "R2 storage is not configured" error
- Check that all 3 required env vars are set: `R2_ENDPOINT`, `R2_ACCESS_KEY`, `R2_SECRET_KEY`
- Verify credentials are correct (no extra spaces)

### Files not accessible publicly
- Ensure bucket has **Public Access** enabled
- Check custom domain DNS records if using custom domain
- Verify `R2_PUBLIC_URL` matches your custom domain

### Upload fails
- Check file size limits (logos: 5MB, invoices: 10MB, profiles: 2MB)
- Verify bucket name matches `R2_BUCKET_NAME`
- Check Cloudflare dashboard for API token permissions

## Security Notes
- API tokens have access only to the specified bucket
- Files are stored with public read access (required for serving images/logos)
- Consider using Cloudflare Access for additional security if needed
