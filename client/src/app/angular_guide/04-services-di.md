# Chapter 04 — Services & Dependency Injection

---

## 1. What is a Service?

A **service** is a TypeScript class that:
- Encapsulates **business logic** (data fetching, calculations, state)
- Is **shared** across multiple components
- Is **injectable** — Angular creates and manages one instance for you

> **Rule of thumb:** If logic doesn't belong to a specific component's view, put it in a service.

---

## 2. Creating a Service

```bash
ng generate service core/services/product
# Creates: src/app/core/services/product.service.ts
```

```typescript
// product.service.ts
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',   // Singleton — one instance for the whole app
})
export class ProductService {
  private products = [
    { id: 1, name: 'Laptop', price: 999 },
    { id: 2, name: 'Mouse',  price: 29  },
  ];

  getAll() {
    return this.products;
  }

  getById(id: number) {
    return this.products.find(p => p.id === id);
  }
}
```

---

## 3. Injecting a Service — Three Ways

### (A) Constructor injection (classic)

```typescript
import { Component, OnInit } from '@angular/core';
import { ProductService } from '../services/product.service';

@Component({ selector: 'app-list', standalone: true, template: `...` })
export class ProductListComponent implements OnInit {
  constructor(private productService: ProductService) {}

  ngOnInit() {
    const products = this.productService.getAll();
  }
}
```

### (B) `inject()` function (Angular 14+ — recommended for Angular 20)

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { ProductService } from '../services/product.service';

@Component({ selector: 'app-list', standalone: true, template: `...` })
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);  // no constructor needed

  ngOnInit() {
    const products = this.productService.getAll();
  }
}
```

> **Prefer `inject()`** in Angular 20. It works in class fields, functions, and injection contexts.

### (C) inject() in a standalone function (e.g., functional guards, resolvers)

```typescript
export const authGuard = () => {
  const authService = inject(AuthService);  // works in injection context
  return authService.isLoggedIn() || inject(Router).navigate(['/login']);
};
```

---

## 4. Provider Scopes

Where you provide a service determines its **lifetime and sharing**.

```
App (root injector)
 └── Feature Module injector
       └── Component injector
```

### `providedIn: 'root'` — App-wide singleton

```typescript
@Injectable({ providedIn: 'root' })
export class CartService { }
```

One instance shared everywhere. Destroyed when the app closes.

### `providedIn: 'platform'` — Shared across micro-frontends

```typescript
@Injectable({ providedIn: 'platform' })
export class ThemeService { }
```

### Component-level provider — New instance per component

```typescript
@Component({
  selector: 'app-wizard',
  standalone: true,
  providers: [WizardStateService],   // fresh instance for this component tree
  template: `...`,
})
export class WizardComponent { }
```

Each `<app-wizard>` gets its own `WizardStateService`.

### Route-level provider

```typescript
// app.routes.ts
{
  path: 'checkout',
  component: CheckoutComponent,
  providers: [CheckoutService],  // new instance scoped to this route
}
```

---

## 5. `InjectionToken` — Inject Non-Class Values

Use `InjectionToken` when you need to inject a value, configuration object, or interface.

```typescript
// tokens.ts
import { InjectionToken } from '@angular/core';

export interface AppConfig {
  apiUrl: string;
  maxRetries: number;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
```

```typescript
// app.config.ts
import { ApplicationConfig } from '@angular/core';
import { APP_CONFIG } from './tokens';

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_CONFIG,
      useValue: { apiUrl: 'https://api.myshop.com', maxRetries: 3 },
    },
  ],
};
```

```typescript
// product.service.ts
import { Injectable, inject } from '@angular/core';
import { APP_CONFIG, AppConfig } from '../tokens';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private config = inject(APP_CONFIG);

  getProducts() {
    return fetch(`${this.config.apiUrl}/products`);
  }
}
```

---

## 6. useClass, useExisting, useFactory

```typescript
providers: [
  // 1. useClass — provide a different implementation
  { provide: LoggerService, useClass: ConsoleLoggerService },

  // 2. useExisting — alias one token to another (same instance)
  { provide: AbstractCache, useExisting: MemoryCacheService },

  // 3. useFactory — build instance with custom logic
  {
    provide: AuthService,
    useFactory: (http: HttpClient, config: AppConfig) => {
      return config.production
        ? new OAuthService(http)
        : new MockAuthService();
    },
    deps: [HttpClient, APP_CONFIG],
  },

  // 4. useValue — plain value or object
  { provide: API_URL, useValue: 'https://api.myshop.com' },
]
```

---

## 7. Real-World Example — Cart Service with Signals

```typescript
// cart.service.ts
import { Injectable, computed, signal } from '@angular/core';

export interface CartItem {
  productId: number;
  name: string;
  price: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  // Private writable signal
  private _items = signal<CartItem[]>([]);

  // Public read-only view
  readonly items = this._items.asReadonly();

  // Derived/computed values
  readonly totalItems = computed(() =>
    this._items().reduce((sum, item) => sum + item.quantity, 0)
  );

  readonly totalPrice = computed(() =>
    this._items().reduce((sum, item) => sum + item.price * item.quantity, 0)
  );

  addItem(product: { id: number; name: string; price: number }) {
    this._items.update(items => {
      const existing = items.find(i => i.productId === product.id);
      if (existing) {
        return items.map(i =>
          i.productId === product.id
            ? { ...i, quantity: i.quantity + 1 }
            : i
        );
      }
      return [...items, { productId: product.id, name: product.name, price: product.price, quantity: 1 }];
    });
  }

  removeItem(productId: number) {
    this._items.update(items => items.filter(i => i.productId !== productId));
  }

  updateQuantity(productId: number, quantity: number) {
    if (quantity <= 0) {
      this.removeItem(productId);
      return;
    }
    this._items.update(items =>
      items.map(i => i.productId === productId ? { ...i, quantity } : i)
    );
  }

  clear() {
    this._items.set([]);
  }
}
```

### Using CartService in a component

```typescript
// header.component.ts
import { Component, inject } from '@angular/core';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-header',
  standalone: true,
  template: `
    <nav>
      <a routerLink="/store">Shop</a>
      <a routerLink="/cart">
        Cart
        @if (cart.totalItems() > 0) {
          <span class="badge">{{ cart.totalItems() }}</span>
        }
      </a>
    </nav>
  `,
})
export class HeaderComponent {
  cart = inject(CartService);  // signals auto-update the template
}
```

---

## 8. Service with HTTP (preview — full details in Chapter 07)

```typescript
// product.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient }         from '@angular/common/http';
import { Observable }         from 'rxjs';
import { environment }        from '../../environments/environment';

export interface Product {
  id: number;
  name: string;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http   = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/products`;

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  create(product: Omit<Product, 'id'>): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  update(id: number, product: Partial<Product>): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

---

## 9. Hierarchical Injector Tree

```
Root Injector (bootstrapApplication)
 │
 ├── RouteLevelInjector (route providers:[])
 │     └── ComponentInjector (providers:[])
 │
 └── AnotherComponent
```

When Angular needs to resolve a dependency it walks **up** the tree.  
The first injector that has the provider wins.

```typescript
// Skip self — look in parent injectors only
constructor(@SkipSelf() private logger: LoggerService) {}

// Optional — null if not found, don't throw
constructor(@Optional() private config: ConfigService) {}

// Only look in self — don't go up
constructor(@Self() private service: LocalService) {}

// Inject the HOST component (used in directives)
constructor(@Host() private form: NgForm) {}
```

---

## 10. Multi Providers

Register multiple values for the same token (e.g., multiple validators, interceptors):

```typescript
// Registers multiple interceptors
providers: [
  { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor,  multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
]
```

```typescript
// Custom multi-provider pattern
export const VALIDATORS = new InjectionToken<Validator[]>('VALIDATORS');

providers: [
  { provide: VALIDATORS, useClass: EmailValidator,  multi: true },
  { provide: VALIDATORS, useClass: PhoneValidator,  multi: true },
]

// Consume
const validators = inject(VALIDATORS); // Validator[]
```

---

## Summary

| Concept | Code |
|---------|------|
| Create service | `@Injectable({ providedIn: 'root' })` |
| Inject service | `inject(MyService)` |
| App-wide singleton | `providedIn: 'root'` |
| Component-scoped | `@Component({ providers: [MyService] })` |
| Inject value/config | `InjectionToken<T>` |
| Multiple providers | `{ provide: TOKEN, useClass: X, multi: true }` |
| Decorate DI | `@Optional()`, `@Self()`, `@SkipSelf()` |

**Next:** [05 — Angular Router →](./05-router.md)
