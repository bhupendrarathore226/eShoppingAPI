# Chapter 12 — Advanced Patterns

---

## 1. Dynamic Components

Render a component at runtime without knowing it at compile time.

```typescript
// dynamic-host.component.ts
import { Component, ViewChild, ViewContainerRef, Type, inject } from '@angular/core';

@Component({
  selector: 'app-dynamic-host',
  standalone: true,
  template: `<ng-container #host></ng-container>`,
})
export class DynamicHostComponent {
  @ViewChild('host', { read: ViewContainerRef }) host!: ViewContainerRef;

  loadComponent<T>(component: Type<T>, inputs?: Partial<T>) {
    this.host.clear();
    const ref = this.host.createComponent(component);

    // Set inputs
    if (inputs) {
      Object.entries(inputs).forEach(([key, value]) => {
        ref.setInput(key, value);
      });
    }

    return ref;
  }
}
```

```typescript
// Usage — render a widget based on user config
@Component({ ... })
export class DashboardComponent {
  @ViewChild(DynamicHostComponent) host!: DynamicHostComponent;

  async loadWidget(type: string) {
    const widgetMap: Record<string, () => Promise<Type<any>>> = {
      'chart':  () => import('./widgets/chart.component').then(m => m.ChartComponent),
      'table':  () => import('./widgets/table.component').then(m => m.TableComponent),
      'stats':  () => import('./widgets/stats.component').then(m => m.StatsComponent),
    };

    const component = await widgetMap[type]();
    this.host.loadComponent(component, { data: this.widgetData });
  }
}
```

---

## 2. Component Portal (CDK)

Render a component in a different part of the DOM (useful for modals, tooltips, toasts).

```bash
npm install @angular/cdk
```

```typescript
// overlay.service.ts
import { Injectable, inject } from '@angular/core';
import { Overlay, OverlayRef }  from '@angular/cdk/overlay';
import { ComponentPortal }      from '@angular/cdk/portal';
import { ToastComponent }       from '../components/toast.component';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private overlay = inject(Overlay);

  show(message: string) {
    const overlayRef: OverlayRef = this.overlay.create({
      positionStrategy: this.overlay.position()
        .global()
        .bottom('20px')
        .centerHorizontally(),
    });

    const portal = new ComponentPortal(ToastComponent);
    const ref    = overlayRef.attach(portal);
    ref.setInput('message', message);

    // Auto-dismiss after 3 seconds
    setTimeout(() => overlayRef.dispose(), 3000);
  }
}
```

---

## 3. Reactive Data Patterns

### Resource API (Angular 19+)

```typescript
import { resource, signal } from '@angular/core';

@Component({ ... })
export class ProductComponent {
  private http = inject(HttpClient);

  productId = signal(1);

  // Automatically re-fetches when productId changes
  product = resource({
    request: () => this.productId(),   // dependency
    loader: ({ request: id }) =>
      this.http.get<Product>(`/api/products/${id}`).toPromise(),
  });

  // product.value() — the data
  // product.isLoading() — loading state
  // product.error() — any error
  // product.reload() — manually trigger refetch
}
```

```html
@if (product.isLoading()) {
  <app-spinner />
} @else if (product.error()) {
  <p>Error: {{ product.error() }}</p>
} @else {
  <h2>{{ product.value()?.name }}</h2>
}
```

---

## 4. Custom Decorators

```typescript
// decorators/log.decorator.ts
export function Log(target: any, methodName: string, descriptor: PropertyDescriptor) {
  const original = descriptor.value;
  descriptor.value = function (...args: any[]) {
    console.log(`[${target.constructor.name}] ${methodName}(`, args, ')');
    const result = original.apply(this, args);
    console.log(`[${target.constructor.name}] ${methodName} returned:`, result);
    return result;
  };
  return descriptor;
}

// Usage
@Injectable({ providedIn: 'root' })
export class ProductService {
  @Log
  getById(id: number) { ... }
}
```

---

## 5. Abstract Base Service

```typescript
// base.service.ts
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export abstract class BaseService<T> {
  protected http      = inject(HttpClient);
  protected apiUrl    = `${environment.apiUrl}/${this.endpoint}`;

  constructor(protected endpoint: string) {}

  getAll():                Observable<T[]>  { return this.http.get<T[]>(this.apiUrl); }
  getById(id: number):     Observable<T>    { return this.http.get<T>(`${this.apiUrl}/${id}`); }
  create(dto: Partial<T>): Observable<T>    { return this.http.post<T>(this.apiUrl, dto); }
  update(id: number, dto: Partial<T>): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}/${id}`, dto);
  }
  delete(id: number):      Observable<void> { return this.http.delete<void>(`${this.apiUrl}/${id}`); }
}
```

```typescript
// product.service.ts
@Injectable({ providedIn: 'root' })
export class ProductService extends BaseService<Product> {
  constructor() { super('products'); }

  // Add product-specific methods
  search(term: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/search`, {
      params: new HttpParams().set('q', term),
    });
  }
}
```

---

## 6. Reactive Store Pattern (without NgRx)

```typescript
// Generic reactive store
import { signal, computed, Signal } from '@angular/core';

export class Store<T extends object> {
  private _state: ReturnType<typeof signal<T>>;

  constructor(initialState: T) {
    this._state = signal<T>(initialState);
  }

  select<K extends keyof T>(key: K): Signal<T[K]> {
    return computed(() => this._state()[key]);
  }

  selectDerived<R>(selector: (state: T) => R): Signal<R> {
    return computed(() => selector(this._state()));
  }

  patch(partial: Partial<T>) {
    this._state.update(s => ({ ...s, ...partial }));
  }

  reset(state: T) {
    this._state.set(state);
  }

  snapshot(): T {
    return this._state();
  }
}
```

```typescript
// Usage
interface AppState {
  user: User | null;
  cart: CartItem[];
  theme: 'light' | 'dark';
}

@Injectable({ providedIn: 'root' })
export class AppStore extends Store<AppState> {
  constructor() {
    super({ user: null, cart: [], theme: 'light' });
  }

  user     = this.select('user');
  cart     = this.select('cart');
  theme    = this.select('theme');
  cartSize = this.selectDerived(s => s.cart.length);

  setUser(user: User)  { this.patch({ user }); }
  toggleTheme()        { this.patch({ theme: this.theme() === 'light' ? 'dark' : 'light' }); }
}
```

---

## 7. Animations

```typescript
// app.config.ts
import { provideAnimations } from '@angular/platform-browser/animations';
providers: [provideAnimations()]
```

```typescript
// product-card.component.ts
import { trigger, state, style, animate, transition } from '@angular/animations';

@Component({
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-10px)' })),
      ]),
    ]),
    trigger('cardState', [
      state('normal',   style({ transform: 'scale(1)' })),
      state('hovered',  style({ transform: 'scale(1.03)', boxShadow: '0 8px 24px rgba(0,0,0,0.2)' })),
      transition('normal <=> hovered', animate('200ms ease')),
    ]),
  ],
  template: `
    <article @fadeIn [@cardState]="cardState"
      (mouseenter)="cardState='hovered'"
      (mouseleave)="cardState='normal'">
      ...
    </article>
  `,
})
export class ProductCardComponent {
  cardState = 'normal';
}
```

---

## 8. Internationalization (i18n)

```html
<!-- Mark text for translation -->
<h1 i18n="@@homeTitle">Welcome to MyShop</h1>
<p i18n>{{ product.name }} is in stock.</p>
```

```bash
# Extract messages
ng extract-i18n --output-path src/locale

# This creates: src/locale/messages.xlf
```

```xml
<!-- src/locale/messages.fr.xlf -->
<trans-unit id="homeTitle">
  <source>Welcome to MyShop</source>
  <target>Bienvenue chez MyShop</target>
</trans-unit>
```

```bash
# Build for French
ng build --localize
```

---

## 9. Micro-Frontend Architecture

Angular supports Module Federation via `@angular-architects/module-federation`:

```bash
npm install @angular-architects/module-federation
ng add @angular-architects/module-federation --project shell --port 4200 --type host
ng add @angular-architects/module-federation --project mfe-store --port 4201 --type remote
```

```javascript
// mfe-store/webpack.config.js
module.exports = withModuleFederationPlugin({
  name: 'mfeStore',
  exposes: {
    './StoreModule': './src/app/store/store.routes.ts',
  },
});
```

```javascript
// shell/webpack.config.js
module.exports = withModuleFederationPlugin({
  remotes: {
    mfeStore: 'mfeStore@http://localhost:4201/remoteEntry.js',
  },
});
```

```typescript
// shell/app.routes.ts
{
  path: 'store',
  loadChildren: () => loadRemoteModule({
    type: 'module',
    remoteEntry: 'http://localhost:4201/remoteEntry.js',
    exposedModule: './StoreModule',
  }).then(m => m.storeRoutes),
}
```

---

## 10. Nx Monorepo (enterprise-scale)

```bash
npx create-nx-workspace@latest myorg --preset=angular
cd myorg

# Generate apps and libraries
nx generate @nx/angular:app my-app
nx generate @nx/angular:lib shared-ui
nx generate @nx/angular:lib feature-store

# Run
nx serve my-app
nx test my-app
nx affected --target=test   # only test changed code
```

---

## 11. Design Patterns Summary

| Pattern | When to use |
|---------|------------|
| Smart/Dumb components | Split data-fetching (smart) from presentation (dumb) |
| Facade service | Hide NgRx complexity from components |
| Repository service | Abstract data access layer |
| Strategy pattern | Swap algorithms at runtime (e.g., different payment providers) |
| Observer (EventEmitter) | Child → Parent communication |
| Singleton service | Shared state across entire app |
| Factory function | Create instances with dependencies (useFactory) |
| Decorator | Cross-cutting concerns (logging, caching) |

---

## 12. Angular 20 New Features Recap

| Feature | Status |
|---------|--------|
| Signal-based components | Stable |
| `input()`, `output()`, `model()` | Stable |
| `@if`, `@for`, `@switch` | Stable |
| `@defer` / Deferrable Views | Stable |
| Zoneless change detection | Stable (experimental flag) |
| `resource()` API | Developer Preview |
| Hydration (Full App) | Stable |
| Incremental Hydration | Developer Preview |
| View Transitions | Stable |
| `withComponentInputBinding()` | Stable |
| Typed Reactive Forms | Stable |
| `inject()` function | Stable |
| Functional guards/interceptors | Stable |
| `takeUntilDestroyed()` | Stable |
| `toSignal()` / `toObservable()` | Stable |

---

## 13. Angular Style Guide Quick Reference

```typescript
// ✅ DO
// File naming: feature.type.ts
product-card.component.ts
product.service.ts
auth.guard.ts

// Class naming: FeatureType
ProductCardComponent
ProductService
AuthGuard

// Use standalone: true for all new components
// Use inject() over constructor injection
// Use OnPush change detection
// Use signals for reactive state
// Use @for with track
// Use functional guards
// Use async/await over .subscribe() where possible

// ❌ DON'T
// Don't use any type
// Don't subscribe without unsubscribing (use async pipe or takeUntilDestroyed)
// Don't manipulate DOM directly (use Angular APIs)
// Don't put business logic in components (use services)
// Don't use NgModule for new code
// Don't use *ngIf/*ngFor for new code (use @if/@for)
```

---

## Congratulations! 🎉

You have completed the **Angular 20 Zero to Pro Guide**.

### What you learned:
1. Project setup and CLI
2. Components, inputs, outputs, lifecycle hooks
3. Templates, data binding, and directives
4. Services and dependency injection
5. Angular Router with guards, resolvers, and lazy loading
6. Template-driven and reactive forms
7. HTTP Client with interceptors
8. Signals and state management
9. Standalone APIs
10. Performance optimization
11. Testing (unit, component, E2E)
12. Advanced patterns

### Next steps:
- Build the full **eShoppingAPI** client using these patterns
- Explore [Angular Material](https://material.angular.io) for UI components
- Learn [NgRx SignalStore](https://ngrx.io/guide/signals) for state management
- Read the [official Angular docs](https://angular.dev)
