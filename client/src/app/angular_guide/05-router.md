# Chapter 05 — Angular Router

---

## 1. Setup

```typescript
// app.config.ts
import { ApplicationConfig }             from '@angular/core';
import { provideRouter, withPreloading, PreloadAllModules, withComponentInputBinding } from '@angular/router';
import { routes }                        from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(
      routes,
      withPreloading(PreloadAllModules),    // preload lazy modules in background
      withComponentInputBinding(),          // bind route params directly to @Input
    ),
  ],
};
```

---

## 2. Defining Routes

```typescript
// app.routes.ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  // Redirect
  { path: '', redirectTo: 'home', pathMatch: 'full' },

  // Eager-loaded
  { path: 'home', component: HomeComponent },

  // Lazy-loaded (standalone component)
  {
    path: 'store',
    loadComponent: () =>
      import('./store/store.component').then(m => m.StoreComponent),
  },

  // Lazy-loaded (feature routes file)
  {
    path: 'checkout',
    loadChildren: () =>
      import('./checkout/checkout.routes').then(m => m.checkoutRoutes),
  },

  // Route with parameter
  { path: 'products/:id', component: ProductDetailsComponent },

  // Nested routes
  {
    path: 'admin',
    component: AdminLayoutComponent,
    children: [
      { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'users',     component: UserListComponent },
    ],
  },

  // Wildcard — must be last
  { path: '**', component: NotFoundComponent },
];
```

---

## 3. RouterOutlet — Where to Render

```html
<!-- app.component.html -->
<app-navbar />
<main>
  <router-outlet />   <!-- routed components render here -->
</main>
<app-footer />
```

For nested routes, add a second `<router-outlet>` inside the parent layout:

```html
<!-- admin-layout.component.html -->
<aside><app-admin-sidebar /></aside>
<section>
  <router-outlet />
</section>
```

---

## 4. RouterLink — Navigation in Templates

```html
<!-- Basic -->
<a routerLink="/store">Shop</a>

<!-- With parameter -->
<a [routerLink]="['/products', product.id]">View</a>

<!-- With query params & fragment -->
<a [routerLink]="['/store']"
   [queryParams]="{ category: 'electronics', page: 1 }"
   fragment="results">
  Electronics
</a>

<!-- Active class -->
<a routerLink="/home" routerLinkActive="active-link">Home</a>

<!-- Exact match only -->
<a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
  Home
</a>
```

---

## 5. Programmatic Navigation

```typescript
import { Component, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Component({ selector: 'app-login', standalone: true, template: `...` })
export class LoginComponent {
  private router = inject(Router);
  private route  = inject(ActivatedRoute);

  login() {
    // ... auth logic ...

    // Navigate to absolute path
    this.router.navigate(['/dashboard']);

    // Navigate relative to current route
    this.router.navigate(['../home'], { relativeTo: this.route });

    // With query params & extras
    this.router.navigate(['/store'], {
      queryParams: { sort: 'price' },
      queryParamsHandling: 'merge',   // keep existing params
    });

    // Replace history entry (no back button)
    this.router.navigate(['/home'], { replaceUrl: true });
  }
}
```

---

## 6. Reading Route Parameters

### Method A — `ActivatedRoute` observables

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { switchMap } from 'rxjs';

@Component({ selector: 'app-product-details', standalone: true, template: `...` })
export class ProductDetailsComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private productService = inject(ProductService);
  product$               = this.route.paramMap.pipe(
    switchMap(params => this.productService.getById(+params.get('id')!))
  );
}
```

### Method B — `withComponentInputBinding()` (Angular 16+ — cleanest)

```typescript
// app.config.ts — provideRouter(routes, withComponentInputBinding())

// product-details.component.ts
import { Component, Input, OnChanges } from '@angular/core';

@Component({ selector: 'app-product-details', standalone: true, template: `...` })
export class ProductDetailsComponent implements OnChanges {
  @Input() id!: string;              // ← route param :id mapped here automatically
  @Input() category?: string;        // ← query param ?category= mapped here
  @Input() fragment?: string;        // ← fragment #xxx mapped here

  ngOnChanges() {
    console.log('Route id:', this.id);
  }
}
```

---

## 7. Route Guards

Guards protect routes from unauthorized access.

### Functional Guard (Angular 15+ — recommended)

```typescript
// auth.guard.ts
import { inject }   from '@angular/core';
import { Router }   from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  // Redirect to login and save the attempted URL
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: router.url }
  });
};
```

```typescript
// app.routes.ts
{
  path: 'account',
  canActivate: [authGuard],
  loadComponent: () => import('./account/account.component').then(m => m.AccountComponent),
},
```

### canActivateChild — protect all children

```typescript
{
  path: 'admin',
  canActivateChild: [authGuard],
  children: [...]
}
```

### canDeactivate — warn before leaving

```typescript
// unsaved-changes.guard.ts
export interface CanComponentDeactivate {
  canDeactivate: () => boolean | Observable<boolean>;
}

export const unsavedChangesGuard = (component: CanComponentDeactivate) => {
  return component.canDeactivate?.() ?? true;
};
```

```typescript
// edit-product.component.ts
export class EditProductComponent implements CanComponentDeactivate {
  isDirty = false;

  canDeactivate(): boolean {
    if (this.isDirty) {
      return confirm('You have unsaved changes. Leave anyway?');
    }
    return true;
  }
}
```

### canMatch — conditionally match a route (A/B testing, feature flags)

```typescript
{
  path: 'dashboard',
  canMatch: [() => inject(FeatureFlagService).isEnabled('new-dashboard')],
  component: NewDashboardComponent,
},
{
  path: 'dashboard',
  component: OldDashboardComponent,
},
```

---

## 8. Route Resolvers — Pre-fetch Data

Resolvers load data **before** a route activates, avoiding empty states.

```typescript
// product.resolver.ts
import { inject }          from '@angular/core';
import { ResolveFn }       from '@angular/router';
import { ProductService }  from '../services/product.service';
import { Product }         from '../models/product.model';

export const productResolver: ResolveFn<Product> = (route) => {
  const id = +route.paramMap.get('id')!;
  return inject(ProductService).getById(id);
};
```

```typescript
// app.routes.ts
{
  path: 'products/:id',
  component: ProductDetailsComponent,
  resolve: { product: productResolver },
}
```

```typescript
// product-details.component.ts
import { Component, inject } from '@angular/core';
import { ActivatedRoute }    from '@angular/router';

@Component({ selector: 'app-product-details', standalone: true, template: `
  <h2>{{ product.name }}</h2>
  <p>{{ product.price | currency }}</p>
`})
export class ProductDetailsComponent {
  private route = inject(ActivatedRoute);
  product = this.route.snapshot.data['product'] as Product;
}
```

---

## 9. Lazy Loading with Feature Routes

```typescript
// checkout/checkout.routes.ts
import { Routes } from '@angular/router';

export const checkoutRoutes: Routes = [
  {
    path: '',
    component: CheckoutComponent,
    children: [
      { path: 'address',  component: AddressStepComponent },
      { path: 'payment',  component: PaymentStepComponent },
      { path: 'review',   component: ReviewStepComponent },
    ],
  },
];
```

```typescript
// app.routes.ts
{
  path: 'checkout',
  canActivate: [authGuard],
  loadChildren: () =>
    import('./checkout/checkout.routes').then(m => m.checkoutRoutes),
},
```

---

## 10. Router Events — Track Navigation

```typescript
import { Component, inject, OnInit } from '@angular/core';
import { Router, NavigationStart, NavigationEnd, NavigationError } from '@angular/router';
import { filter } from 'rxjs';

@Component({ selector: 'app-root', standalone: true, template: `...` })
export class AppComponent implements OnInit {
  private router = inject(Router);
  isLoading = false;

  ngOnInit() {
    this.router.events.pipe(
      filter(e => e instanceof NavigationStart || e instanceof NavigationEnd || e instanceof NavigationError)
    ).subscribe(event => {
      if (event instanceof NavigationStart) this.isLoading = true;
      if (event instanceof NavigationEnd)   this.isLoading = false;
      if (event instanceof NavigationError) {
        this.isLoading = false;
        console.error('Navigation error:', event.error);
      }
    });
  }
}
```

---

## 11. Title Strategy — Update Page Title

```typescript
// app.config.ts
import { TitleStrategy } from '@angular/router';
import { Injectable }    from '@angular/core';
import { Title }         from '@angular/platform-browser';

@Injectable({ providedIn: 'root' })
export class AppTitleStrategy extends TitleStrategy {
  constructor(private title: Title) { super(); }

  override updateTitle(snapshot: RouterStateSnapshot) {
    const pageTitle = this.buildTitle(snapshot);
    this.title.setTitle(pageTitle ? `${pageTitle} | MyShop` : 'MyShop');
  }
}

// Register in app.config.ts providers:
{ provide: TitleStrategy, useClass: AppTitleStrategy }
```

```typescript
// app.routes.ts
{ path: 'store', title: 'Store', component: StoreComponent }
```

---

## 12. Named Router Outlets (advanced)

```html
<!-- template -->
<router-outlet />                        <!-- primary outlet -->
<router-outlet name="sidebar" />         <!-- named outlet -->
```

```typescript
// route definition
{ path: 'help', component: HelpComponent, outlet: 'sidebar' }
```

```typescript
// navigate to named outlet
this.router.navigate([{ outlets: { primary: ['store'], sidebar: ['help'] } }]);
```

---

## 13. Complete Route Configuration Example

```typescript
// app.routes.ts
import { Routes } from '@angular/router';
import { authGuard }         from './core/guards/auth.guard';
import { productResolver }   from './store/resolvers/product.resolver';
import { HomeComponent }     from './home/home.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },

  { path: 'home', component: HomeComponent, title: 'Home' },

  {
    path: 'store',
    title: 'Store',
    loadChildren: () =>
      import('./store/store.routes').then(m => m.storeRoutes),
  },

  {
    path: 'products/:id',
    title: 'Product Details',
    loadComponent: () =>
      import('./store/product-details/product-details.component')
        .then(m => m.ProductDetailsComponent),
    resolve: { product: productResolver },
  },

  {
    path: 'checkout',
    title: 'Checkout',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./checkout/checkout.routes').then(m => m.checkoutRoutes),
  },

  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./account/account.routes').then(m => m.accountRoutes),
  },

  { path: 'login',    loadComponent: () => import('./account/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./account/register/register.component').then(m => m.RegisterComponent) },

  { path: '**', loadComponent: () => import('./core/not-found/not-found.component').then(m => m.NotFoundComponent) },
];
```

---

## Summary

| Feature | API |
|---------|-----|
| Define routes | `Routes` array in `app.routes.ts` |
| Bootstrap router | `provideRouter(routes, ...)` |
| Render outlet | `<router-outlet>` |
| Navigate in template | `routerLink`, `routerLinkActive` |
| Navigate in code | `Router.navigate()` |
| Read params | `ActivatedRoute.paramMap` or `@Input` with `withComponentInputBinding()` |
| Protect routes | `canActivate`, `canDeactivateChild`, `canMatch` |
| Pre-fetch data | `resolve: { key: resolverFn }` |
| Lazy load | `loadComponent`, `loadChildren` |

**Next:** [06 — Forms →](./06-forms.md)
