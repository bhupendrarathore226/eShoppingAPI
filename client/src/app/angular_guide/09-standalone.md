# Chapter 09 — Standalone Components & Modern APIs

---

## 1. What is "Standalone"?

Before Angular 14, every component had to be declared inside an **NgModule**.  
Angular 20 defaults to **standalone**: components manage their own imports directly.

| NgModule style (old) | Standalone style (modern) |
|---------------------|--------------------------|
| Declare in `declarations: []` | No declaration needed |
| Import modules like `CommonModule` | Import specific directives/pipes |
| `BrowserModule`, `FormsModule` in AppModule | Import per-component |
| Boilerplate `app.module.ts` | `bootstrapApplication()` |

---

## 2. Standalone Component

```typescript
import { Component }      from '@angular/core';
import { NgClass, NgIf }  from '@angular/common';   // named imports
import { RouterLink }     from '@angular/router';
import { FormsModule }    from '@angular/forms';
import { ProductCardComponent } from './product-card.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    NgClass,
    NgIf,                     // or use @if in template (no import needed)
    RouterLink,
    FormsModule,
    ProductCardComponent,     // import other standalone components
  ],
  templateUrl: './product-list.component.html',
})
export class ProductListComponent { }
```

> **In Angular 20:** `@if`, `@for`, `@switch` are **built-in** — no `NgIf`/`NgFor` imports required.

---

## 3. `bootstrapApplication` — No AppModule

```typescript
// main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent }         from './app/app.component';
import { appConfig }            from './app/app.config';

bootstrapApplication(AppComponent, appConfig)
  .catch(console.error);
```

```typescript
// app.config.ts
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter }       from '@angular/router';
import { provideHttpClient }   from '@angular/common/http';
import { provideAnimations }   from '@angular/platform-browser/animations';
import { routes }              from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    provideAnimations(),

    // Legacy modules that haven't been migrated yet:
    importProvidersFrom(SomeOldModule),
  ],
};
```

---

## 4. Migrating from NgModule to Standalone

### Before (NgModule)

```typescript
// old product.module.ts
@NgModule({
  declarations: [ProductListComponent, ProductCardComponent],
  imports: [CommonModule, RouterModule, FormsModule],
  exports: [ProductListComponent],
})
export class ProductModule { }
```

### After (Standalone)

```typescript
// product-list.component.ts
@Component({
  standalone: true,
  imports: [RouterLink, FormsModule, ProductCardComponent],
  ...
})
export class ProductListComponent { }

// product-card.component.ts
@Component({
  standalone: true,
  imports: [],
  ...
})
export class ProductCardComponent { }
```

**Migration command (automatic):**

```bash
ng generate @angular/core:standalone
# Follow prompts to migrate components, pipes, directives, and finally the AppModule
```

---

## 5. Standalone Directives & Pipes

Same pattern — add `standalone: true`:

```typescript
// truncate.pipe.ts
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'truncate', standalone: true })
export class TruncatePipe implements PipeTransform {
  transform(value: string, limit = 50): string {
    return value.length > limit ? value.slice(0, limit) + '…' : value;
  }
}
```

```typescript
// highlight.directive.ts
import { Directive } from '@angular/core';

@Directive({ selector: '[appHighlight]', standalone: true })
export class HighlightDirective { }
```

Import them directly in any component's `imports: []`.

---

## 6. Feature-level `ApplicationConfig` (environment providers)

Provide services/routes at the route level:

```typescript
// checkout/checkout.routes.ts
import { Routes }              from '@angular/router';
import { provideState }        from '@ngrx/store';
import { checkoutReducer }     from './checkout.reducer';
import { CheckoutComponent }   from './checkout.component';

export const checkoutRoutes: Routes = [
  {
    path: '',
    component: CheckoutComponent,
    providers: [
      provideState('checkout', checkoutReducer),  // scoped NgRx state
      CheckoutService,                             // scoped service
    ],
  },
];
```

---

## 7. `provideRouter` Feature Flags

```typescript
import {
  provideRouter,
  withPreloading, PreloadAllModules,
  withComponentInputBinding,
  withViewTransitions,
  withRouterConfig,
  withDebugTracing,        // logs all router events (dev only)
  withHashLocation,
  withEnabledBlockingInitialNavigation,
} from '@angular/router';

provideRouter(
  routes,
  withPreloading(PreloadAllModules),
  withComponentInputBinding(),      // route params → @Input
  withViewTransitions(),            // CSS View Transitions API
  withRouterConfig({ paramsInheritanceStrategy: 'always' }),
)
```

---

## 8. View Transitions API (Angular 17+)

```typescript
// app.config.ts
provideRouter(routes, withViewTransitions())
```

```scss
/* styles.scss */
::view-transition-old(root) {
  animation: 200ms ease-out fade-out;
}
::view-transition-new(root) {
  animation: 200ms ease-in fade-in;
}
```

Angular automatically wraps route navigations in the browser's View Transitions API — smooth page transitions with no extra code!

---

## 9. Environment Injectors — Lazy Injection Context

```typescript
import { EnvironmentInjector, inject, runInInjectionContext } from '@angular/core';

@Component({ ... })
export class MyComponent {
  private envInjector = inject(EnvironmentInjector);

  doLazyWork() {
    runInInjectionContext(this.envInjector, () => {
      // inject() works here even though we're not in a constructor
      const service = inject(SomeService);
      service.doWork();
    });
  }
}
```

---

## 10. Lazy-loaded Standalone Component with its own Providers

```typescript
// product-details.component.ts
import { Component }         from '@angular/core';
import { ProductService }    from './product.service';

@Component({
  selector: 'app-product-details',
  standalone: true,
  providers: [ProductService],   // fresh instance scoped to this component
  template: `...`,
})
export class ProductDetailsComponent { }
```

```typescript
// app.routes.ts
{
  path: 'products/:id',
  loadComponent: () =>
    import('./products/product-details.component')
      .then(m => m.ProductDetailsComponent),
}
```

---

## 11. Complete Standalone App Structure

```
src/
  main.ts                    ← bootstrapApplication(AppComponent, appConfig)
  app/
    app.component.ts         ← Root standalone component
    app.config.ts            ← ApplicationConfig (providers)
    app.routes.ts            ← Top-level routes
    core/
      guards/
        auth.guard.ts        ← Functional guards
      interceptors/
        auth.interceptor.ts  ← Functional interceptors
      services/
        auth.service.ts
        cart.service.ts
    shared/
      components/
        button/
          button.component.ts     ← standalone: true
        spinner/
          spinner.component.ts    ← standalone: true
      pipes/
        truncate.pipe.ts          ← standalone: true
      directives/
        highlight.directive.ts    ← standalone: true
    features/
      store/
        store.component.ts        ← standalone: true
        store.routes.ts           ← feature routes
        product-card/
          product-card.component.ts
      checkout/
        checkout.component.ts
        checkout.routes.ts
      account/
        account.component.ts
        account.routes.ts
```

---

## Summary

| Old NgModule API | Modern Standalone API |
|------------------|-----------------------|
| `@NgModule({ declarations })` | `@Component({ standalone: true })` |
| `imports: [CommonModule]` | `imports: [NgClass, AsyncPipe, ...]` |
| `AppModule` | `app.config.ts` + `bootstrapApplication()` |
| Module-level providers | Route-level `providers:[]` |
| `RouterModule.forChild()` | `loadChildren: () => import('./routes')` |
| Class-based guards | Functional guards with `inject()` |
| Class-based interceptors | `HttpInterceptorFn` |

**Next:** [10 — Performance →](./10-performance.md)
