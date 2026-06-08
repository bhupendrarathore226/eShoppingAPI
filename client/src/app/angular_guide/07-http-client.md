# Chapter 07 — HTTP Client

---

## 1. Setup

```typescript
// app.config.ts
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withFetch(),                            // use fetch API instead of XHR (Angular 18+)
      withInterceptors([authInterceptor, errorInterceptor]),
    ),
  ],
};
```

---

## 2. Basic HTTP Calls

```typescript
import { Injectable, inject }  from '@angular/core';
import { HttpClient }          from '@angular/common/http';
import { Observable }          from 'rxjs';
import { environment }         from '../../environments/environment';
import { Product }             from '../models/product.model';

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

  create(dto: Omit<Product, 'id'>): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, dto);
  }

  update(id: number, dto: Partial<Product>): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, dto);
  }

  patch(id: number, dto: Partial<Product>): Observable<Product> {
    return this.http.patch<Product>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

---

## 3. Consuming HTTP in a Component

### With `async` pipe (preferred — no manual subscribe/unsubscribe)

```typescript
@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [AsyncPipe, ProductCardComponent],
  template: `
    @if (products$ | async; as products) {
      @for (p of products; track p.id) {
        <app-product-card [product]="p" />
      }
    } @else {
      <app-spinner />
    }
  `,
})
export class ProductListComponent {
  private service = inject(ProductService);
  products$ = this.service.getAll();
}
```

### With Signals + `toSignal` (Angular 16+)

```typescript
import { toSignal } from '@angular/core/rxjs-interop';

@Component({ ... })
export class ProductListComponent {
  private service = inject(ProductService);

  products = toSignal(this.service.getAll(), { initialValue: [] });
  // products() is now a regular signal — no async pipe needed in template
}
```

```html
@for (p of products(); track p.id) {
  <app-product-card [product]="p" />
}
```

---

## 4. Query Parameters & Headers

```typescript
import { HttpParams, HttpHeaders } from '@angular/common/http';

getFiltered(filters: { category?: string; minPrice?: number; page?: number }) {
  let params = new HttpParams();

  if (filters.category) params = params.set('category', filters.category);
  if (filters.minPrice) params = params.set('minPrice', filters.minPrice);
  if (filters.page)     params = params.set('page',     filters.page);

  const headers = new HttpHeaders({ 'X-Custom-Header': 'MyApp' });

  return this.http.get<Product[]>(this.apiUrl, { params, headers });
}
```

---

## 5. Full HTTP Response (status, headers)

```typescript
import { HttpResponse } from '@angular/common/http';

getWithMeta(): Observable<HttpResponse<Product[]>> {
  return this.http.get<Product[]>(this.apiUrl, { observe: 'response' });
}

// Usage
this.service.getWithMeta().subscribe(response => {
  console.log('Status:', response.status);
  console.log('Total:', response.headers.get('X-Total-Count'));
  console.log('Body:', response.body);
});
```

---

## 6. Upload Progress

```typescript
import { HttpEventType, HttpEvent } from '@angular/common/http';

uploadFile(file: File): Observable<number> {
  const formData = new FormData();
  formData.append('file', file);

  return this.http.post('/api/upload', formData, {
    reportProgress: true,
    observe: 'events',
  }).pipe(
    map((event: HttpEvent<any>) => {
      if (event.type === HttpEventType.UploadProgress && event.total) {
        return Math.round(100 * event.loaded / event.total);
      }
      return 0;
    }),
  );
}
```

---

## 7. RxJS Operators for HTTP

### `catchError` — handle errors

```typescript
import { catchError, throwError } from 'rxjs';

getAll(): Observable<Product[]> {
  return this.http.get<Product[]>(this.apiUrl).pipe(
    catchError(err => {
      console.error('HTTP error:', err);
      return throwError(() => new Error(err.error?.message ?? 'Server error'));
    }),
  );
}
```

### `retry` — automatic retries

```typescript
import { retry, catchError } from 'rxjs';

getAll() {
  return this.http.get<Product[]>(this.apiUrl).pipe(
    retry({ count: 3, delay: 1000 }),   // retry 3 times with 1s delay
    catchError(err => throwError(() => err)),
  );
}
```

### `switchMap` — cancel previous request on new emission

```typescript
// Search with debounce — cancels inflight request on each new keystroke
searchCtrl.valueChanges.pipe(
  debounceTime(300),
  distinctUntilChanged(),
  switchMap(term => this.productService.search(term)),
).subscribe(results => this.results.set(results));
```

### `forkJoin` — parallel requests

```typescript
import { forkJoin } from 'rxjs';

// Fires all requests simultaneously, emits when ALL complete
const init$ = forkJoin({
  products:   this.productService.getAll(),
  categories: this.categoryService.getAll(),
  user:       this.userService.getCurrent(),
});

init$.subscribe(({ products, categories, user }) => {
  this.products.set(products);
  this.categories.set(categories);
  this.user.set(user);
});
```

### `tap` — side-effects without modifying data

```typescript
return this.http.get<Product[]>(this.apiUrl).pipe(
  tap(products => console.log('Fetched:', products.length)),
  map(products => products.filter(p => p.inStock)),
);
```

---

## 8. HTTP Interceptors (functional, Angular 15+)

Interceptors run for every HTTP request/response in the app.

### Auth Interceptor — attach JWT token

```typescript
// auth.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject }            from '@angular/core';
import { AuthService }       from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();

  if (!token) return next(req);

  const cloned = req.clone({
    headers: req.headers.set('Authorization', `Bearer ${token}`),
  });

  return next(cloned);
};
```

### Loading Interceptor — show/hide spinner

```typescript
// loading.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject }            from '@angular/core';
import { LoadingService }    from '../services/loading.service';
import { finalize }          from 'rxjs';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loader = inject(LoadingService);
  loader.show();
  return next(req).pipe(finalize(() => loader.hide()));
};
```

### Error Interceptor — global error handling

```typescript
// error.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject }           from '@angular/core';
import { Router }           from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      switch (err.status) {
        case 401:
          router.navigate(['/login']);
          break;
        case 403:
          router.navigate(['/forbidden']);
          break;
        case 404:
          router.navigate(['/not-found']);
          break;
        case 500:
          router.navigate(['/server-error'], { state: { error: err } });
          break;
      }
      return throwError(() => err);
    }),
  );
};
```

### Caching Interceptor

```typescript
// cache.interceptor.ts
import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject }  from '@angular/core';
import { of, tap } from 'rxjs';

// Simple in-memory cache
const cache = new Map<string, HttpResponse<any>>();

export const cacheInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') return next(req);

  const cached = cache.get(req.url);
  if (cached) return of(cached);

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        cache.set(req.url, event);
      }
    }),
  );
};
```

### Registering interceptors

```typescript
// app.config.ts
provideHttpClient(
  withInterceptors([
    authInterceptor,
    loadingInterceptor,
    errorInterceptor,
    cacheInterceptor,
  ]),
)
```

> Interceptors run in **registration order** on the way **out** and in **reverse order** on the way **back**.

---

## 9. Loading Service

```typescript
// loading.service.ts
import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = signal(0);

  isLoading = computed(() => this._count() > 0);

  show() { this._count.update(n => n + 1); }
  hide() { this._count.update(n => Math.max(0, n - 1)); }
}
```

```html
<!-- app.component.html -->
@if (loadingService.isLoading()) {
  <div class="loading-bar"></div>
}
```

---

## 10. Typed API Response Wrapper

```typescript
// models/api-response.model.ts
export interface PagedResult<T> {
  data: T[];
  pagination: {
    pageIndex: number;
    pageSize: number;
    count: number;
    totalPages: number;
  };
}
```

```typescript
// product.service.ts
getPagedProducts(params: {
  pageIndex: number;
  pageSize: number;
  search?: string;
  sort?: string;
}): Observable<PagedResult<Product>> {
  let httpParams = new HttpParams()
    .set('pageIndex', params.pageIndex)
    .set('pageSize',  params.pageSize);

  if (params.search) httpParams = httpParams.set('search', params.search);
  if (params.sort)   httpParams = httpParams.set('sort',   params.sort);

  return this.http.get<PagedResult<Product>>(this.apiUrl, { params: httpParams });
}
```

---

## Summary

| Task | API |
|------|-----|
| Setup | `provideHttpClient(withFetch(), withInterceptors([...]))` |
| GET / POST / PUT / DELETE | `http.get<T>()`, `http.post<T>()` … |
| Query params | `new HttpParams().set(key, value)` |
| Custom headers | `new HttpHeaders({ key: value })` |
| Interceptors | `HttpInterceptorFn` — functional |
| Error handling | `catchError` operator |
| Parallel requests | `forkJoin` |
| Cancel on new | `switchMap` |
| Signals integration | `toSignal(observable$)` |

**Next:** [08 — Signals & State Management →](./08-signals-state.md)
