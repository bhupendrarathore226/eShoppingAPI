# Chapter 01 — Getting Started with Angular 20

---

## 1. Installing the Tools

```bash
# Node.js 22+ required — check version
node -v     # should be >= 22.0.0
npm -v      # should be >= 10.0.0

# Install Angular CLI globally
npm install -g @angular/cli@20

# Verify
ng version
```

---

## 2. Creating Your First Project

```bash
ng new my-shop \
  --standalone \        # no NgModule boilerplate
  --routing \           # generates app routing
  --style=scss \        # SCSS for styles
  --strict              # strict TypeScript
```

**What each flag means:**

| Flag | Purpose |
|------|---------|
| `--standalone` | Every component is standalone (Angular 20 default) |
| `--routing` | Creates `app.routes.ts` and wires up the router |
| `--style=scss` | Use SCSS instead of plain CSS |
| `--strict` | Enables strict TypeScript & template type-checking |

---

## 3. Project Structure Explained

```
my-shop/
├── angular.json          ← Workspace / build config
├── package.json          ← npm dependencies
├── tsconfig.json         ← TypeScript root config
├── tsconfig.app.json     ← App-specific TS config
├── tsconfig.spec.json    ← Test-specific TS config
└── src/
    ├── index.html        ← Single HTML shell
    ├── main.ts           ← Bootstrap entry point
    ├── styles.scss       ← Global styles
    └── app/
        ├── app.component.ts        ← Root component
        ├── app.component.html
        ├── app.component.scss
        ├── app.component.spec.ts   ← Unit tests
        └── app.routes.ts           ← Route definitions
```

### `main.ts` — Bootstrap (Standalone style)

```typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig }            from './app/app.config';
import { AppComponent }         from './app/app.component';

bootstrapApplication(AppComponent, appConfig)
  .catch(err => console.error(err));
```

### `app.config.ts`

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter }     from '@angular/router';
import { routes }            from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
  ],
};
```

### `app.routes.ts`

```typescript
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  // add more routes here
];
```

---

## 4. Angular CLI Cheat Sheet

```bash
# Serve with live reload
ng serve

# Serve on a custom port
ng serve --port 4300

# Generate a standalone component
ng generate component features/product-list

# Shorthand
ng g c features/product-list

# Generate a service
ng g s core/services/product

# Generate a guard
ng g guard core/guards/auth

# Run tests
ng test

# Build for production
ng build --configuration production

# Analyze bundle
ng build --stats-json
npx webpack-bundle-analyzer dist/my-shop/stats.json
```

---

## 5. Understanding `angular.json`

Key sections you will touch most:

```json
{
  "projects": {
    "my-shop": {
      "architect": {
        "build": {
          "options": {
            "outputPath": "dist/my-shop",
            "index": "src/index.html",
            "main": "src/main.ts",
            "styles": ["src/styles.scss"],
            "assets": ["src/favicon.ico", "src/assets"]
          },
          "configurations": {
            "production": {
              "optimization": true,
              "sourceMap": false,
              "budgets": [
                { "type": "initial", "maximumWarning": "500kB", "maximumError": "1MB" }
              ]
            }
          }
        }
      }
    }
  }
}
```

---

## 6. TypeScript Basics You Need for Angular

### Decorators

```typescript
// A decorator is just a function that modifies a class
function Log(target: any) {
  console.log('Class created:', target.name);
}

@Log
class MyService {}
```

### Interfaces & Types

```typescript
// Prefer interfaces for object shapes
interface Product {
  id: number;
  name: string;
  price: number;
  inStock?: boolean;   // optional
}

// Type alias for union types
type Status = 'active' | 'inactive' | 'pending';
```

### Generics

```typescript
// A wrapper that can hold any type
interface ApiResponse<T> {
  data: T;
  total: number;
  page: number;
}

// Usage
const response: ApiResponse<Product[]> = {
  data: [...],
  total: 100,
  page: 1,
};
```

### Optional chaining & Nullish coalescing

```typescript
const city = user?.address?.city ?? 'Unknown';
```

---

## 7. Hello World — Step by Step

### Step 1: Edit `app.component.ts`

```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-root',          // HTML tag: <app-root>
  standalone: true,              // no NgModule needed
  template: `
    <h1>Hello, {{ name }}!</h1>
    <button (click)="changeName()">Change Name</button>
  `,
  styles: [`h1 { color: steelblue; }`]
})
export class AppComponent {
  name = 'Angular 20';

  changeName() {
    this.name = 'Developer!';
  }
}
```

### Step 2: `ng serve` and open the browser

You see: **Hello, Angular 20!**  
Click the button and it changes to **Hello, Developer!**

This is Angular in its simplest form:
- **Class property** `name` = the data (model)
- **`{{ name }}`** = interpolation binding (view reads the model)
- **`(click)`** = event binding (user interaction updates the model)

---

## 8. Environment Configuration

```
src/environments/
  environment.ts          ← development
  environment.prod.ts     ← production
```

```typescript
// environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
};

// environment.prod.ts
export const environment = {
  production: true,
  apiUrl: 'https://api.myshop.com',
};
```

**Use it anywhere:**

```typescript
import { environment } from '../../environments/environment';

const url = `${environment.apiUrl}/products`;
```

Angular CLI automatically swaps the file based on the build configuration.

---

## 9. VS Code Extensions for Angular

| Extension | Purpose |
|-----------|---------|
| Angular Language Service | Template IntelliSense |
| ESLint | Linting |
| Prettier | Code formatting |
| Angular Snippets | Code snippets |
| Material Icon Theme | Better file icons |

---

## Summary

- Angular 20 uses **standalone components** by default (no NgModule).
- The entry point is `main.ts` → `bootstrapApplication()`.
- `angular.json` controls build, serve, and test targets.
- Angular CLI (`ng`) is your best friend for generating code.

**Next:** [02 — Components →](./02-components.md)
