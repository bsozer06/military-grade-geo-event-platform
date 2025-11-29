# GeoEvents UI

Real-time geospatial event visualization using Angular 21 + Cesium + milsymbol. This frontend connects to the GeoEvents backend via SignalR to display live unit positions, zone violations, and proximity alerts on a 3D globe.

---

## Quick Start

### Prerequisites
- Node.js v22+ and npm 11+
- Backend API and Simulator running (optional for standalone mock mode)

### Step-by-Step Setup

1. **Navigate to the frontend directory:**
   ```bash
   cd frontend/geo-events-ui
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Run the development server:**
   ```bash
   ng serve
   ```
   Or if you have the `start` script configured:
   ```bash
   npm start
   ```

4. **Open in browser:**
   Navigate to `http://localhost:4200/`

5. **View events:**
   - **Mock Mode (no backend required):** Click "Start Mock" in the header to generate random events
   - **Live Mode (with backend):** Configure SignalR URL (see below) and start the backend + simulator

---

## Running the Full Stack

### Option A: Mock Mode (Frontend Only)
1. Start frontend: `ng serve`
2. Open `http://localhost:4200/`
3. Click **"Start Mock"** button in the header
4. Events appear in the right panel and icons on the map
5. Use **"Fit to Data"** and **"Follow Latest"** controls to adjust camera

### Option B: Live Mode (with Backend)

1. **Start Postgres + RabbitMQ (from repo root):**
   ```bash
   docker-compose up -d
   ```

2. **Start Backend API:**
   ```bash
   cd backend/src/GeoEvents.Api
   dotnet run
   ```
   Backend runs on `http://localhost:5045` (configured in launchSettings.json)

3. **Start Simulator (in a new terminal):**
   ```bash
   cd simulator/GeoEvents.Simulator
   dotnet run -- --units 10 --rate 5 --duration 120
   ```
   Simulator publishes events to RabbitMQ every second

4. **Configure SignalR URL in frontend:**
   The frontend needs to know where to connect for real-time events. Add this to `frontend/geo-events-ui/src/index.html` before `</head>`:
   ```html
   <script>
     window.__GEO_EVENTS_SIGNALR_URL__ = 'http://localhost:5045/hub/events';
   </script>
   ```
   
   **Note:** This step is already done in the committed `index.html`. The port (5045) is configured in `backend/src/GeoEvents.Api/Properties/launchSettings.json`.

5. **Start Frontend:**
   ```bash
   cd frontend/geo-events-ui
   ng serve
   ```

6. **Open browser:** `http://localhost:4200/`
   - Connection status appears in header (Connected/Disconnected)
   - Real-time events stream from backend
   - Map updates with unit positions using military symbols

---

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

## Real-time Event Stream Configuration

The app attempts to connect to a SignalR hub at `http://localhost:5000/hub/events` on the browser. Override this by defining a global before Angular bootstraps (e.g. in `index.html`):

```html
<script>window.__GEO_EVENTS_SIGNALR_URL__ = 'https://your-host/hub/events';</script>
```

## Cesium Setup

Cesium assets are copied to `assets/cesium`. If you change output paths adjust `angular.json` assets section accordingly. Import of widgets CSS is done in `src/styles.scss`.

## Military Symbols (milsymbol)

`SymbolService` provides a simple wrapper with caching. Example usage:

```ts
const imgDataUrl = symbolService.buildSymbol('SFGPUCI----K', { size: 48, uniqueDesignation: 'UNIT-01' });
```

## Features

- **3D Globe Visualization:** Powered by Cesium, view events on an interactive 3D globe
- **Military Symbols:** Unit positions displayed using NATO APP-6 symbols via milsymbol
- **Real-time Updates:** SignalR connection streams events from backend (optional)
- **Mock Event Generator:** Built-in mock producer for standalone testing
- **Event Filtering:** Toggle visibility of UNIT_POSITION, ZONE_VIOLATION, PROXIMITY_ALERT events
- **Camera Controls:**
  - **Fit to Data:** Auto-zoom to show all units
  - **Follow Latest:** Track the most recent unit update
- **Event Timeline:** Scrollable list of last 200 events with metadata

---

## Project Structure

```
src/
├── app/
│   ├── event-stream/         # Event list component with filters
│   ├── header/               # Top bar with connection status and controls
│   ├── map/                  # Cesium map component
│   ├── models/               # TypeScript event models and type guards
│   └── services/
│       ├── event-stream.service.ts    # SignalR hub connection
│       ├── event-store.service.ts     # Event buffer and filtering
│       ├── mock-producer.service.ts   # Mock event generator
│       └── symbol.service.ts          # milsymbol wrapper with caching
├── styles.scss               # Global styles + Cesium widgets CSS
└── index.html                # Entry point with Cesium base URL config
```

---

## Configuration

### SignalR Connection
By default, the app runs in **standalone mode** (no backend required). To enable live backend connection, define the SignalR URL before Angular bootstraps:

**In `src/index.html`:**
```html
<script>
  window.__GEO_EVENTS_SIGNALR_URL__ = 'http://localhost:5000/hub/events';
</script>
```

If this variable is not set, the SignalR service will not attempt to connect and no console errors will appear.

### Cesium Assets
Cesium static assets are automatically copied to `assets/cesium/` during build. The base URL is configured in `index.html`:
```html
<script>
  window.CESIUM_BASE_URL = '/assets/cesium';
</script>
```

### Event Types
The application handles three event types:
- `UNIT_POSITION`: Moving unit coordinates with optional speed/heading
- `ZONE_VIOLATION`: Unit entered a restricted zone
- `PROXIMITY_ALERT`: Two units within a threshold distance

---

## Troubleshooting

### Cesium Assets Not Loading (404 errors)
Ensure `window.CESIUM_BASE_URL` is set in `index.html` and assets are copied via `angular.json` build configuration.

### SignalR Connection Errors
If you see connection errors in console:
1. Verify backend is running on the configured URL
2. Check CORS is enabled in backend for `http://localhost:4200`
3. Or simply run in mock mode (no backend required)

### Map Not Showing Events
1. Check the right panel shows events (filters enabled?)
2. Click **"Fit to Data"** button to zoom to units
3. Ensure events are of type `UNIT_POSITION` with valid lat/lon

### Milsymbol Icons Not Rendering
Check browser console for import errors. The `SymbolService` handles both ESM and CJS imports automatically.

---

## Commands Reference

| Command | Description |
|---------|-------------|
| `npm install` | Install all dependencies |
| `ng serve` | Start dev server on port 4200 |
| `ng build` | Production build to `dist/` |
| `ng build --configuration development` | Development build |
| `ng test` | Run unit tests (Vitest) |
| `ng lint` | Lint code (if ESLint configured) |

---

## Advanced Usage

### Custom Event Scenarios
Edit mock producer service or create a custom scenario JSON file and load events programmatically.

### Icon Customization
Modify SIDC codes in `map.component.ts` (e.g., `'SFGPUCI----K'`) to change unit symbol types. See milsymbol documentation for full symbol codes.

### Camera Presets
Adjust `Camera.DEFAULT_VIEW_RECTANGLE` and `DEFAULT_VIEW_FACTOR` in `map.component.ts` to change initial view region and zoom level.
