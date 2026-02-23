# SOEN-343
Full-stack starter project using **ASP.NET Core Web API** (backend) + **Angular** (frontend).

---

## For Backend

### Instructions
1. Open a new terminal.
2. Navigate to the repo root (the folder that contains `SUMMS.sln`).
3. Run <code>dotnet restore</code> to install required dependencies.
4. Run <code>dotnet run</code> to boot-up the backend.

### Verify it’s running
- Swagger UI: <code>http://localhost:5065/swagger</code>
- Ping endpoint (REST): <code>http://localhost:5065/api/ping</code>
- Sample endpoint: <code>http://localhost:5065/weatherforecast</code>

> If your backend starts on a different port, check <code>Properties/launchSettings.json</code> for the <code>applicationUrl</code>.

### Technologies Used
- <b>ASP.NET Core Web API</b> – Used to build the backend REST API (Minimal API setup).
- <b>Swagger (OpenAPI)</b> – Used to explore and test the API endpoints in the browser.
- <b>.NET SDK</b> – Build + run tooling for the backend.

---

## For Frontend

### Instructions
1. Open a new terminal.
2. Run <code>cd frontend/summs-ui</code>.
3. Run <code>npm install</code> to install required dependencies.
4. Start the Angular dev server using the proxy config:
   - Run <code>npm start -- --proxy-config src/proxy.conf.json</code>

Then open:
- <code>http://localhost:4200</code>

### Verify frontend ↔ backend connection
With the backend running, open:
- <code>http://localhost:4200/api/ping</code>

If it returns JSON like <code>{"message":"pong", ...}</code>, the frontend is connected to the backend through the proxy.

### Technologies Used
- <b>Angular</b> – Used to build the frontend SPA.
- <b>TypeScript</b> – Improves code safety and maintainability.
- <b>Angular HttpClient</b> – Used to call the backend REST API.

---

## How the Frontend Connects to the Backend (Proxy)

During development you have:
- Frontend dev server: <code>http://localhost:4200</code>
- Backend API server: <code>http://localhost:5065</code>

Calling <code>http://localhost:5065</code> directly from a page served on <code>http://localhost:4200</code> is cross-origin and often needs CORS.

To avoid CORS headaches and keep requests clean (e.g. <code>/api/ping</code>), Angular uses a <b>dev proxy</b>:
- File: <code>frontend/summs-ui/src/proxy.conf.json</code>
- It forwards <code>/api/*</code> → <code>http://localhost:5065/api/*</code>

This lets the Angular code call:
```ts
this.http.get('/api/ping')
