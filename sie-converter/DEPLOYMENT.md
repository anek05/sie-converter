# Deployment Guide

## Quick Start

### Option 1: Docker Compose (Recommended)

```bash
# Clone/navigate to project
cd sie-converter

# Start services
docker-compose up -d

# Access the application
open http://localhost:8080
```

### Option 2: Local Development

```bash
# Terminal 1: Start API
cd src/backend
dotnet run

# Terminal 2: Serve frontend
cd src/frontend/public
npx http-server -p 8080
```

### Option 3: Production Build

```bash
cd src/backend
dotnet publish -c Release -o ./publish
cd publish
dotnet sie-converter-api.dll
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Production |
| `ASPNETCORE_URLS` | URLs to listen on | http://+:8080 |
| `EPPlus__LicenseContext` | EPPlus license type | NonCommercial |

### Security Considerations

1. **File Size Limits**: Default is 50MB, configured in:
   - `Program.cs` (Kestrel limits)
   - `nginx.conf` (client_max_body_size)
   - `ConversionController.cs` (RequestSizeLimit)

2. **CORS**: Configured in `Program.cs` to allow frontend access

3. **Security Headers**: Added via middleware and nginx:
   - X-Content-Type-Options: nosniff
   - X-Frame-Options: DENY
   - X-XSS-Protection: 1; mode=block
   - Content-Security-Policy

4. **Temp File Security**:
   - Files stored in system temp directory
   - Cryptographically secure random filenames
   - Automatic cleanup on disposal
   - Secure delete option overwrites files before deletion

## Docker Deployment

### Build and Run

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Production Docker

```bash
# Build production image
docker build -t sie-converter:latest src/backend

# Run with environment variables
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e EPPlus__LicenseContext=NonCommercial \
  --name sie-converter \
  sie-converter:latest
```

## Health Checks

The API includes a health check endpoint:

```bash
curl http://localhost:5101/api/conversion/options
```

## Testing

Run the PowerShell test script:

```powershell
cd src/backend
.\test-converter.ps1
```

This will:
1. Build the project
2. Start the API
3. Validate the example SIE file
4. Test conversion to Excel
5. Clean up

## Troubleshooting

### Port Conflicts

If port 5101 or 8080 is in use:

```yaml
# docker-compose.yml
ports:
  - "5102:8080"  # Change 5102 to any available port
```

### File Upload Issues

Check nginx.conf for `client_max_body_size` if large files fail.

### Encoding Issues

SIE files use CP437/PC8 encoding by default. The parser handles this automatically.

## Updates

To update the application:

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```
