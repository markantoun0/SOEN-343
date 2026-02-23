# SOEN-343

Full-stack starter project using:

- ASP.NET Core Web API (backend)
- Angular (frontend)

---

## Requirements

Make sure you have installed:

- .NET SDK (recommended: .NET 8)
- Node.js (LTS)
- npm (comes with Node)

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/markantoun0/SOEN-343.git
cd SOEN-343
```

### 2. Run the Backend (ASP.NET Core)

From the project root:

```bash
dotnet restore
dotnet run
```

The backend will start on:
- http://localhost:5065

Swagger UI:
- http://localhost:5065/swagger

Test endpoint:
- http://localhost:5065/api/ping

Leave this terminal running.

---

### 3. Run the Frontend (Angular)

Open a second terminal:

```bash
cd frontend/summs-ui
npm install
npm start -- --proxy-config src/proxy.conf.json
```

Angular will start on:
- http://localhost:4200

---

### 4. Verify Frontend ↔ Backend Connection

With both servers running, open:

```
http://localhost:4200/api/ping
```

If you see JSON with `"pong"`, the frontend is successfully connected to the backend.

You can also open:

```
http://localhost:4200
```

and click **Ping backend** in the UI.

---

## About the Proxy

During development:
- Angular runs on `http://localhost:4200`
- Backend runs on `http://localhost:5065`

Angular uses a development proxy (`frontend/summs-ui/src/proxy.conf.json`) so requests like `/api/ping` are automatically forwarded to the backend. This avoids CORS issues and keeps API calls simple.

---

## Notes

- The backend must be running before frontend API calls will work.
- If the backend port changes, update `src/proxy.conf.json`.
- If `npm start` fails with proxy errors, ensure `proxy.conf.json` contains valid JSON and starts with `{`.
