# GeoEventsUi

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.0.1.

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

## Development Quick Start

```bash
cd frontend/geo-events-ui
npm install
npm start # alias for ng serve (if script added) else ng serve
```

Then publish a `UNIT_POSITION` event from the backend simulator to see real-time updates on the map.
