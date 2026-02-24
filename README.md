# SOEN-343 – SUMMS

Full-stack project using:
- **ASP.NET Core 8** Web API (backend, layered architecture)
- **Angular 21** (frontend)
- **Google Maps + Places API** (mobility locations for Montréal & Laval)
- **BIXI GBFS API** (live bike-share stations)

---

## Features

- Interactive map for Montréal & Laval
- Live BIXI bike-share station locations (via BIXI GBFS API)
- Google Places API for parking spots
- Simulated parking spot availability (1–28 spots per location)
- Responsive, full-screen map UI

---

## Architecture

```
SOEN-343/
├── Domain/Models/          ← Pure domain models (no dependencies)
├── Services/Interfaces/    ← Service contracts
├── Services/               ← Business logic (Google Places integration)
├── Controllers/            ← HTTP layer (REST endpoints)
├── Program.cs              ← DI wiring, CORS, .env loading
└── frontend/summs-ui/
    └── src/app/
        └── map/            ← Map component + MobilityService
```

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Node.js LTS + npm
- A Google Cloud project with **Maps JavaScript API** and **Places API** enabled

---

## Setup

### 1. Clone & configure secrets

```bash
git clone https://github.com/markantoun0/SOEN-343.git
cd SOEN-343
cp .env.example .env
```

Edit `.env` and add your real API keys:

```dotenv
GOOGLE_PLACES_API_KEY=your_key_here
GOOGLE_MAPS_JS_API_KEY=your_key_here
ALLOWED_ORIGINS=http://localhost:4200
```

### 2. Install frontend dependencies

```bash
cd frontend/summs-ui
npm install
cd ../..
```

---

## Running the project

### Backend (.NET)
1. Open a terminal in the project root (`SOEN-343`).
2. Run the backend:
   ```bash
   dotnet run
   ```

### Frontend (Angular)
1. Open a second terminal.
2. Navigate to the frontend directory:
   ```bash
   cd frontend/summs-ui
   ```
3. Install dependencies (only needed once):
   ```bash
   npm install
   ```
4. Start the Angular dev server:
   ```bash
   npm start
   ```

### Access the app
- Open [http://localhost:4200](http://localhost:4200) in your browser.

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/ping` | Health check |
| `GET` | `/api/mobility/nearby?lat=&lng=&radius=` | Bike + parking near coordinates |
| `GET` | `/api/mobility/montreal-laval` | Combined results for Montréal & Laval |
| `GET` | `/api/config/maps-key` | Serves Maps JS key to frontend |

---

## Implementation Notes

- The map and BIXI integration were implemented using the official BIXI GBFS API (no API key required).
- Google Places API is used for parking locations and future directions features.
- Parking spots display a simulated number of available spaces (1–28) for demo purposes.
- JetBrains Rider IDE is recommended for running and debugging the backend.

---

## Notes

- `.env` is git-ignored — never commit it.
- `.env.example` is committed so others know what variables to set.
- The Angular dev proxy (`src/proxy.conf.json`) forwards `/api/*` to the backend, so no CORS issues during development.
- The Maps JS API key is never hardcoded in the frontend source; it is fetched at runtime from `/api/config/maps-key`.

---

For any issues, please check the README or contact the maintainer.
