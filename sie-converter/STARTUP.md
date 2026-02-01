# SIE to Excel Converter - Startup Guide

## Running the Application

### Backend (API Server)
1. Navigate to the backend directory:
   ```bash
   cd src/backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the API server:
   ```bash
   dotnet run
   ```
   
   The API will be available at `https://localhost:5001` and `http://localhost:5000`

### Frontend (Static Files)
1. Navigate to the frontend directory:
   ```bash
   cd src/frontend
   ```

2. Install dependencies (if not already installed):
   ```bash
   npm install
   ```

3. Serve the frontend:
   ```bash
   npx http-server ./public -p 3000
   ```
   
   The frontend will be available at `http://localhost:3000`

## Configuration

The application uses the following settings in `appsettings.json`:

```json
{
  "DonationSettings": {
    "IsDonationEnabled": true,
    "TargetWeeklyAmount": 10000,
    "SwishNumber": "1234567890",
    "PayPalEmail": "donations@example.com",
    "FreeConversionsPerDay": 5,
    "MaxFileSizeMB": 10
  }
}
```

## API Endpoints

- `POST /api/conversion/sie-to-excel` - Convert SIE file to Excel
- `GET /api/donation/settings` - Get donation settings
- `GET /api/donation/goal-progress` - Get donation goal progress

## Production Deployment

For production deployment, consider:
1. Setting up a reverse proxy (nginx/Apache)
2. Configuring SSL certificates
3. Setting up a proper database for tracking usage
4. Implementing payment processing