# Deploy to Render.com

This guide explains how to deploy the SIE Converter to Render.com's free tier.

## Prerequisites

- GitHub account with the sie-converter repo
- Render.com account (free)

## Deployment Steps

### 1. Connect GitHub Repo to Render

1. Go to https://dashboard.render.com
2. Click "New +" → "Blueprint"
3. Connect your GitHub account and select `anek05/sie-converter`
4. Render will automatically detect the `render.yaml` and create both services

### 2. Manual Setup (Alternative)

If Blueprint doesn't work, create services manually:

#### Backend API Service

1. Click "New +" → "Web Service"
2. Connect your GitHub repo
3. Configure:
   - **Name**: `sie-converter-api`
   - **Runtime**: Docker
   - **Root Directory**: `src/backend`
   - **Plan**: Free
4. Add Environment Variables:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ASPNETCORE_URLS` = `http://+:8080`
   - `EPPlus__LicenseContext` = `NonCommercial`
   - `CORS_ORIGINS` = `https://sie-converter.onrender.com`
5. Click "Create Web Service"

#### Frontend Static Site

1. Click "New +" → "Static Site"
2. Connect your GitHub repo
3. Configure:
   - **Name**: `sie-converter`
   - **Root Directory**: `src/frontend/public`
   - **Build Command**: `echo "No build needed"`
   - **Publish Directory**: `.`
   - **Plan**: Free
4. Click "Create Static Site"

### 3. Update Frontend URL (One-time)

After the backend is deployed, update the frontend API URL if needed:

1. Go to your static site settings on Render
2. Add Environment Variable:
   - `API_URL` = `https://sie-converter-api.onrender.com` (your actual backend URL)
3. Redeploy the frontend

Or update `src/frontend/public/index.html` line 15-17:
```javascript
const API_URL = window.location.hostname === 'localhost' 
    ? 'http://localhost:5101' 
    : 'https://YOUR-BACKEND-URL.onrender.com';
```

### 4. Custom Domain (Optional)

1. In Render dashboard, go to your static site
2. Click "Settings" → "Custom Domain"
3. Add your domain and follow DNS instructions

## URLs After Deployment

- **Frontend**: https://sie-converter.onrender.com
- **Backend API**: https://sie-converter-api.onrender.com
- **API Health Check**: https://sie-converter-api.onrender.com/api/conversion/options

## Troubleshooting

### CORS Errors
If you see CORS errors in browser console:
1. Check that `CORS_ORIGINS` env var includes your frontend URL
2. Redeploy the backend after updating env vars

### API Not Responding
1. Check backend logs in Render dashboard
2. Verify health check endpoint: `/api/conversion/options`
3. Ensure Docker builds successfully

### Frontend Can't Connect to API
1. Verify the API_URL in frontend matches the backend service URL
2. Check browser console for exact error
3. Test API directly: `curl https://sie-converter-api.onrender.com/api/conversion/options`

## Free Tier Limits

- **Web Service**: 512 MB RAM, sleeps after 15 min inactivity (wakes on request)
- **Static Site**: Unlimited bandwidth, CDN included
- **Bandwidth**: 100 GB/month combined

## Updates

Push to GitHub main branch → Render auto-deploys both services

```bash
git add .
git commit -m "Your changes"
git push origin master
```
