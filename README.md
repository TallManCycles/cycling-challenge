# Cycling Challenge Platform

A web application that enables two friends to create monthly cycling challenges against each other, integrating with Garmin devices to automatically track progress through the Garmin Activity API.

## Features

- 🚴‍♂️ **Monthly Challenges**: Create distance, climbing, or speed challenges
- 📱 **Garmin Integration**: Automatic activity sync from Garmin Connect
- 📊 **Real-time Progress**: Live progress tracking and winner determination  
- 🔐 **OAuth Security**: Secure Garmin OAuth 2.0 PKCE authentication
- 📈 **Progress Visualization**: Interactive progress bars and statistics
- ☁️ **Azure Hosting**: Deployed on Azure free tier (Static Web Apps + Functions)

## Technology Stack

### Backend
- **C# Azure Functions** (isolated model v4)
- **Entity Framework Core** with SQLite
- **Azure Blob Storage** for database storage
- **Garmin Activity API** integration

### Frontend  
- **Vue.js 3** with TypeScript
- **Vue Router** for navigation
- **Axios** for API communication
- **Responsive CSS** with modern design

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Vue.js SPA    │◄──►│  Azure Functions │◄──►│  Garmin API     │
│ (Static Web App)│    │   (REST API)     │    │   (Webhooks)    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │ SQLite Database  │
                       │ (Azure Blob)     │
                       └──────────────────┘
```

## Challenge Types

1. **Distance Challenge**: Total kilometers cycled in the month
2. **Climbing Challenge**: Total elevation gain (meters) in the month  
3. **Speed Challenge**: Average speed across all cycling activities

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+
- Azure Functions Core Tools
- Garmin Developer Account

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Install dependencies:
   ```bash
   dotnet restore
   ```

3. Configure environment variables in `local.settings.json`:
   ```json
   {
     "Values": {
       "GARMIN_CLIENT_ID": "your-garmin-client-id",
       "GARMIN_CLIENT_SECRET": "your-garmin-client-secret",
       "GARMIN_REDIRECT_URI": "http://localhost:5173/auth/callback"
     }
   }
   ```

4. Run the Functions locally:
   ```bash
   func start
   ```

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

4. Open http://localhost:5173 in your browser

## API Endpoints

### Authentication
- `GET /api/auth/garmin/start` - Start Garmin OAuth flow
- `POST /api/auth/garmin/callback` - Handle OAuth callback
- `DELETE /api/user/registration` - Delete user account (Garmin compliance)

### Challenges
- `GET /api/challenges` - List user challenges
- `POST /api/challenges` - Create new challenge
- `POST /api/challenges/{id}/accept` - Accept challenge invitation
- `GET /api/challenges/{id}/progress` - Get challenge progress

### Webhooks (Garmin Compliance)
- `POST /api/webhooks/activity` - Activity notifications
- `POST /api/webhooks/deregistration` - User deregistration  
- `POST /api/webhooks/permissions` - Permission changes

## Garmin Integration

### OAuth 2.0 PKCE Flow
1. Generate code verifier and challenge
2. Redirect to Garmin authorization
3. Exchange authorization code for tokens
4. Store and refresh tokens automatically

### Webhook Processing
- Responds to activity pings within 30 seconds (Garmin requirement)
- Processes only cycling activity types
- Updates challenge progress automatically
- Handles user deregistration and permission changes

## Database Schema

```sql
-- Users table
Users: Id, Name, Email, GarminUserId, GarminAccessToken, GarminRefreshToken, TokenExpiry, CreatedAt

-- Challenges table  
Challenges: Id, Name, Type, Status, TargetValue, StartDate, EndDate, CreatorId, OpponentId, CreatedAt

-- Activities table
Activities: Id, GarminActivityId, ActivityType, Distance, ElevationGain, AverageSpeed, ActivityDate, UserId, ChallengeId, UploadedAt
```

## Deployment

### Azure Setup
1. Create Azure Function App (Consumption plan)
2. Create Azure Static Web App
3. Create Azure Storage Account
4. Configure Application Insights

### GitHub Actions
The repository includes automated deployment via GitHub Actions:
- Frontend deploys to Azure Static Web Apps
- Backend deploys to Azure Functions
- Triggered on push to main branch

### Environment Variables
Set these in Azure Function App configuration:
```
GARMIN_CLIENT_ID=your-production-client-id
GARMIN_CLIENT_SECRET=your-production-client-secret  
GARMIN_REDIRECT_URI=https://yourapp.azurestaticapps.net/auth/callback
AZURE_STORAGE_CONNECTION_STRING=your-storage-connection
DATABASE_CONTAINER=cycling-challenges
DATABASE_BLOB_NAME=challenges.db
```

## Development

### Project Structure
```
cycling-challenge/
├── backend/                 # C# Azure Functions
│   ├── Functions/          # HTTP trigger functions
│   ├── Models/             # Entity Framework models
│   ├── Data/               # Database context
│   └── Services/           # Business logic
├── frontend/               # Vue.js application
│   ├── src/
│   │   ├── components/     # Vue components
│   │   ├── views/          # Page components
│   │   ├── services/       # API services
│   │   └── types/          # TypeScript definitions
└── .github/workflows/      # GitHub Actions
```

### Development Commands

Backend:
```bash
# Run locally
func start

# Run tests
dotnet test

# Build for production
dotnet build --configuration Release
```

Frontend:
```bash
# Development server
npm run dev

# Type checking  
npm run type-check

# Build for production
npm run build

# Lint and format
npm run lint
npm run format
```

## Garmin Compliance Requirements

- ✅ OAuth 2.0 PKCE authentication
- ✅ Webhook response within 30 seconds
- ✅ Deregistration endpoint implementation
- ✅ Permission change handling
- ✅ Activity ping processing
- ✅ Support for 2+ users (as required for production key)

## License

This project is for educational/demonstration purposes. Ensure compliance with Garmin's terms of service when using their APIs.