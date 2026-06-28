# Railway Deployment Guide

This document outlines the deployment configuration for hosting the application on Railway. For local development instructions, please refer to the [Local Development Guide](local-development.md).

## 1. Image Storage & Persistent Volume
The application uses persistent local storage for routine image uploads, ensuring images are not lost when the Railway container restarts or redeploys.

*   **Volume Mount Path**: `/data`
*   **Image Storage Root**: `/data/uploads`
*   **Public URL Path**: `/uploads`

### Configuration Requirements
When deploying to Railway, configure the following Environment Variables in the service settings:

```env
ImageStorage__Provider=LocalVolume
ImageStorage__RootPath=/data/uploads
ImageStorage__PublicBasePath=/uploads
ImageStorage__MaxFileSizeBytes=52428800
```

*Note: Application-level file validation strictly enforces this 50MB limit, while the internal HTTP multipart request limits are configured slightly higher (60MB) in Kestrel/FormOptions to accommodate form-data overhead.*

Ensure a Railway Volume is created and mounted to the `/data` path for this specific service.

## 2. Secrets Management
Do not commit secrets to `appsettings.json`. Set the following environment variables securely in the Railway dashboard:

*   `ConnectionStrings__DefaultConnection` (Neon PostgreSQL URL)
*   `ResendSettings__ApiKey` (Email provider)
*   `AdminBootstrap__Username`
*   `AdminBootstrap__Password`
*   `CloudinarySettings__CloudName` (If migrating or retaining old assets)
*   `CloudinarySettings__ApiKey`
*   `CloudinarySettings__ApiSecret`

## 3. Post-Deployment Verification
After deployment, verify the following:
1.  **Admin Login**: Ensure the bootstrap admin credentials or previous credentials work.
2.  **Upload Test**: Upload a new post or branch image via the Admin portal. Verify the image appears correctly on the public site (validating `/uploads` static file routing).
3.  **Redeploy Test**: Trigger a manual redeploy in Railway and confirm the newly uploaded image still loads correctly (validating the `/data` volume persistence).

> **Backup Warning**: The Railway Volume is a single point of failure for newly uploaded assets. You should periodically back up the volume contents or employ a secondary backup strategy. Existing legacy Cloudinary images remain safely hosted on Cloudinary.
