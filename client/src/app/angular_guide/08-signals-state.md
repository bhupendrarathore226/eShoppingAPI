# Chapter 08 — Signals & State Management

---

## PART A — Angular Signals

Signals are Angular's **built-in fine-grained reactivity system** (stable since Angular 17).  
They replace Zone.js-based change detection for most use cases.

---

### 1. `signal()` — Writable Signal

```typescript
import { signal } from '@angular/core';

// Create
const count = signal(0);

// Read — always call as a function
console.log(count()); // 0

// Write
count.set(5);         // replace value
count.update(n => n + 1);  // update based on current value

// Mutate (for objects/arrays — avoid if possible, prefer immutable updates)
const list = signal<string[]>([]);
list.mutate(arr => arr.push('item'));  // mutates in place (use with caution)
```

### 2. `computed()` — Derived Signal

Automatically recalculates when its dependencies change.

```typescript
import { signal, computed } from '@angular/core';

const firstName = signal('John');
const lastName  = signal('Doe');

// Automatically recomputes when firstName or lastName changes
const fullName  = computed(() => `${firstName()} ${lastName()}`);

console.log(fullName()); // "John Doe"
firstName.set('Jane');
console.log(fullName()); // "Jane Doe"
```

> `computed()` is **lazy** (recalculates only when read) and **memoized** (same dependencies → same value, no recalculation).

### 3. `effect()` — Side Effects

Runs a function whenever its signal dependencies change.

```typescript
import { signal, effect, Component, OnInit } from '@angular/core';

@Component({ ... })
export class ThemeComponent implements OnInit {
  theme = signal<'light' | 'dark'>('light');

  ngOnInit() {
    // Runs immediately, then re-runs when `theme` changes
    effect(() => {
      document.body.className = `theme-${this.theme()}`;
      localStorage.setItem('theme', this.theme());
    });
  }
}
```

> **Cleanup:** return a cleanup function from `effect()` when needed:

```typescript
effect((onCleanup) => {
  const timer = setInterval(() => console.log('tick'), 1000);
  onCleanup(() => clearInterval(timer));
});
```

### 4. `toSignal()` — Observable → Signal

```typescript
import { toSignal }    from '@angular/core/rxjs-interop';
import { HttpClient }  from '@angular/common/http';

@Component({ ... })
export class ProductListComponent {
  private http = inject(HttpClient);

  // No async pipe needed — use directly in template
  products = toSignal(
    this.http.get<Product[]>('/api/products'),
    { initialValue: [] as Product[] }
  );
}
```

```html
@for (p of products(); track p.id) { ... }
```

### 5. `toObservable()` — Signal → Observable

```typescript
import { toObservable } from '@angular/core/rxjs-interop';

@Component({ ... })
export class SearchComponent {
  searchTerm = signal('');

  results$ = toObservable(this.searchTerm).pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(term => this.productService.search(term)),
  );
}
```

### 6. `input()`, `output()`, `model()` Signals

```typescript
import { Component, input, output, model } from '@angular/core';

@Component({ selector: 'app-slider', standalone: true, template: `
  <input type="range" [value]="value()" (input)="value.set(+$event.target.value)" />
`})
export class SliderComponent {
  // Signal input — read only, set by parent
  min   = input(0);
  max   = input(100);

  // Two-way signal (replaces @Input + @Output combo)
  value = model(50);  // parent binds with [(value)]="myVar"

  // Signal output — emits events to parent
  changed = output<number>();
}
```

---

## PART B — Zoneless Angular (Angular 18+)

Remove Zone.js entirely for maximum performance:

```typescript
// app.config.ts
import { provideExperimentalZonelessChangeDetection } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideExperimentalZonelessChangeDetection(),  // no zone.js needed
    provideRouter(routes),
  ],
};
```

```json
// Remove from polyfills.ts
// import 'zone.js';  ← delete this line
```

With zoneless Angular, **only signals and async pipe** trigger change detection.

---

## PART C — State Management Patterns

### Pattern 1 — Service with Signals (simple apps)

```typescript
// store/product.state.ts
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient }                   from '@angular/common/http';
import { inject }                       from '@angular/core';

export interface ProductState {
  products: Product[];
  loading:  boolean;
  error:    string | null;
  selected: Product | null;
}

@Injectable({ providedIn: 'root' })
export class ProductStateService {
  private http = inject(HttpClient);

  // State
  private state = signal<ProductState>({
    products: [],
    loading: false,
    error: null,
    selected: null,
  });

  // Selectors
  products = computed(() => this.state().products);
  loading  = computed(() => this.state().loading);
  error    = computed(() => this.state().error);
  selected = computed(() => this.state().selected);

  // Actions
  async loadProducts() {
    this.state.update(s => ({ ...s, loading: true, error: null }));
    try {
      const products = await this.http.get<Product[]>('/api/products').toPromise();
      this.state.update(s => ({ ...s, products: products!, loading: false }));
    } catch (err: any) {
      this.state.update(s => ({ ...s, loading: false, error: err.message }));
    }
  }

  selectProduct(product: Product) {
    this.state.update(s => ({ ...s, selected: product }));
  }

  clearSelection() {
    this.state.update(s => ({ ...s, selected: null }));
  }
}
```

### Pattern 2 — NgRx SignalStore (Angular 20 recommended)

> Install: `npm install @ngrx/signals`

```typescript
// store/product.store.ts
import { signalStore, withState, withComputed, withMethods } from '@ngrx/signals';
import { withEntities, setAllEntities, selectEntity }        from '@ngrx/signals/entities';
import { rxMethod }                                           from '@ngrx/signals/rxjs-interop';
import { computed, inject }                                   from '@angular/core';
import { switchMap, tap }                                     from 'rxjs';
import { ProductService }                                     from '../services/product.service';

// Entity type
interface Product { id: number; name: string; price: number; }

// State shape
interface ProductState {
  loading: boolean;
  error:   string | null;
  filter:  string;
}

export const ProductStore = signalStore(
  { providedIn: 'root' },

  withEntities<Product>(),                      // id-indexed entity map + helpers
  withState<ProductState>({
    loading: false,
    error: null,
    filter: '',
  }),

  withComputed(({ entities, filter }) => ({
    filteredProducts: computed(() =>
      entities().filter(p =>
        p.name.toLowerCase().includes(filter().toLowerCase())
      )
    ),
    totalCount: computed(() => entities().length),
  })),

  withMethods((store) => {
    const service = inject(ProductService);
    return {
      loadProducts: rxMethod<void>(
        switchMap(() => service.getAll().pipe(
          tap({
            next:  (products) => patchState(store, setAllEntities(products)),
            error: (err)      => patchState(store, { error: err.message }),
          })
        ))
      ),
      setFilter(filter: string) {
        patchState(store, { filter });
      },
    };
  }),
);
```

```typescript
// product-list.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { ProductStore }               from '../store/product.store';

@Component({
  selector: 'app-product-list',
  standalone: true,
  template: `
    <input (input)="store.setFilter($event.target.value)" placeholder="Filter..." />
    <p>Total: {{ store.totalCount() }}</p>
    @for (p of store.filteredProducts(); track p.id) {
      <app-product-card [product]="p" />
    }
  `,
})
export class ProductListComponent implements OnInit {
  store = inject(ProductStore);

  ngOnInit() {
    this.store.loadProducts();
  }
}
```

---

### Pattern 3 — Classic NgRx Store (large apps)

> Install: `npm install @ngrx/store @ngrx/effects @ngrx/entity`

```typescript
// products/products.actions.ts
import { createAction, props } from '@ngrx/store';

export const loadProducts        = createAction('[Products] Load');
export const loadProductsSuccess = createAction('[Products] Load Success', props<{ products: Product[] }>());
export const loadProductsFailure = createAction('[Products] Load Failure', props<{ error: string }>());
```

```typescript
// products/products.reducer.ts
import { createReducer, on } from '@ngrx/store';

interface ProductsState { products: Product[]; loading: boolean; error: string | null; }
const initial: ProductsState = { products: [], loading: false, error: null };

export const productsReducer = createReducer(
  initial,
  on(loadProducts,        state => ({ ...state, loading: true })),
  on(loadProductsSuccess, (state, { products }) => ({ ...state, products, loading: false })),
  on(loadProductsFailure, (state, { error })    => ({ ...state, error, loading: false })),
);
```

```typescript
// products/products.effects.ts
import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { switchMap, map, catchError, of } from 'rxjs';

@Injectable()
export class ProductsEffects {
  private actions$ = inject(Actions);
  private service  = inject(ProductService);

  loadProducts$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadProducts),
      switchMap(() =>
        this.service.getAll().pipe(
          map(products => loadProductsSuccess({ products })),
          catchError(err => of(loadProductsFailure({ error: err.message }))),
        )
      )
    )
  );
}
```

---

## Summary

| Tool | Best for |
|------|---------|
| `signal()` + `computed()` + `effect()` | Component-level state, UI state |
| Service with signals | Shared state in small/medium apps |
| NgRx SignalStore | Feature-level state in medium/large apps |
| Classic NgRx | Enterprise apps needing Redux DevTools, time-travel |
| `toSignal()` / `toObservable()` | RxJS ↔ Signals bridge |

**Next:** [09 — Standalone APIs →](./09-standalone.md)
