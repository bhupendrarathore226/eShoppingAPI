# Chapter 10 — Performance & Best Practices

---

## 1. OnPush Change Detection

The **single most impactful** performance change you can make.

```typescript
import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h3>{{ product().name }}</h3>`,
})
export class ProductCardComponent {
  product = input.required<Product>();
  // Angular only checks this component when:
  // 1. A signal changes
  // 2. An @Input reference changes
  // 3. An Observable emits via async pipe
  // 4. markForCheck() is called
}
```

**Rule:** Apply `OnPush` to **every** component except the root.

---

## 2. trackBy / track in @for

Without `track`, Angular re-creates all DOM nodes on any array change.  
With `track`, only changed items update.

```html
<!-- Modern (Angular 17+) — use track expression directly -->
@for (product of products(); track product.id) {
  <app-product-card [product]="product" />
}

<!-- Legacy *ngFor -->
<app-product-card
  *ngFor="let p of products; trackBy: trackById"
  [product]="p"
/>
```

```typescript
trackById(_index: number, item: { id: number }) {
  return item.id;
}
```

---

## 3. Lazy Loading Routes

Split the bundle — load features only when needed:

```typescript
// app.routes.ts
{
  path: 'store',
  loadChildren: () => import('./store/store.routes').then(m => m.storeRoutes),
},
{
  path: 'admin',
  loadComponent: () => import('./admin/admin.component').then(m => m.AdminComponent),
},
```

**Preloading strategy** — load idle routes in background after initial navigation:

```typescript
provideRouter(routes, withPreloading(PreloadAllModules))
```

**Custom preloading** — only preload routes marked with `data.preload`:

```typescript
import { PreloadingStrategy, Route } from '@angular/router';
import { Observable, of } from 'rxjs';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SelectivePreloadingStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<any>): Observable<any> {
    return route.data?.['preload'] ? load() : of(null);
  }
}

// app.routes.ts
{ path: 'store', data: { preload: true }, loadChildren: ... }
```

---

## 4. Deferrable Views `@defer`

Defer loading of heavy components until needed:

```html
<!-- Load only when scrolled into view -->
@defer (on viewport) {
  <app-product-reviews [productId]="product().id" />
} @placeholder {
  <div class="placeholder" style="height: 300px"></div>
} @loading (minimum 200ms) {
  <app-skeleton-loader />
}

<!-- Load only when user interacts -->
@defer (on interaction) {
  <app-chat-widget />
} @placeholder {
  <button>Open Chat</button>
}

<!-- Load after 3 seconds -->
@defer (on timer(3s)) {
  <app-newsletter-popup />
}
```

---

## 5. Pure Pipes vs Impure Pipes

```typescript
// Pure pipe (default) — only runs when input reference changes
@Pipe({ name: 'filter', standalone: true, pure: true })
export class FilterPipe implements PipeTransform {
  transform(items: Product[], search: string): Product[] {
    return items.filter(p => p.name.includes(search));
  }
}

// Impure pipe — runs every change detection cycle (avoid for performance)
@Pipe({ name: 'myImpure', standalone: true, pure: false })
export class ImpurePipe implements PipeTransform { ... }
```

> Prefer `pure: true` (default). If you need reactive filtering, use `computed()` signals instead.

---

## 6. Signals — Zoneless Angular

The future of Angular performance:

```typescript
// app.config.ts
import { provideExperimentalZonelessChangeDetection } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [provideExperimentalZonelessChangeDetection()],
};
```

With signals + zoneless:
- No Zone.js overhead
- Only components that depend on changed signals re-render
- Much faster initial load

---

## 7. Virtual Scrolling (CDK)

For very long lists (1000+ items), render only what's visible:

```bash
npm install @angular/cdk
```

```typescript
import { ScrollingModule } from '@angular/cdk/scrolling';

@Component({
  standalone: true,
  imports: [ScrollingModule],
  template: `
    <cdk-virtual-scroll-viewport itemSize="72" style="height: 500px">
      <app-product-card
        *cdkVirtualFor="let p of products; trackBy: trackById"
        [product]="p"
      />
    </cdk-virtual-scroll-viewport>
  `,
})
export class ProductListComponent { }
```

---

## 8. Server-Side Rendering (SSR) with Angular Universal

```bash
# Add SSR to existing project
ng add @angular/ssr
```

```typescript
// app.config.server.ts (auto-generated)
import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering }                    from '@angular/platform-server';
import { appConfig }                                 from './app.config';

const serverConfig: ApplicationConfig = {
  providers: [provideServerRendering()],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
```

```bash
# Build and serve SSR
ng build
node dist/my-app/server/server.mjs
```

**Benefits:**
- Faster First Contentful Paint (FCP)
- Better SEO — search crawlers see full HTML
- Social media link previews work correctly

---

## 9. Incremental Hydration (Angular 19+)

Hydrate only parts of the SSR-rendered page on demand:

```html
<!-- Only hydrate when user scrolls to this section -->
@defer (hydrate on viewport) {
  <app-product-reviews [productId]="id" />
}

<!-- Hydrate when user interacts -->
@defer (hydrate on interaction) {
  <app-comment-section />
}

<!-- Never hydrate — stays static HTML -->
@defer (hydrate never) {
  <app-static-footer />
}
```

---

## 10. Image Optimization with `NgOptimizedImage`

```typescript
import { NgOptimizedImage } from '@angular/common';

@Component({
  standalone: true,
  imports: [NgOptimizedImage],
  template: `
    <!-- Automatic lazy loading, intrinsic sizing, priority hints -->
    <img ngSrc="products/laptop.jpg" width="400" height="300" alt="Laptop" />

    <!-- Above-the-fold image — load eagerly with priority -->
    <img ngSrc="hero-banner.jpg" width="1920" height="600" priority alt="Banner" />
  `,
})
export class ProductCardComponent { }
```

**What `NgOptimizedImage` does automatically:**
- Adds `loading="lazy"` (except `priority`)
- Prevents layout shift (requires `width`/`height`)
- Adds `fetchpriority="high"` for priority images
- Warns about missing `alt`, wrong sizes

---

## 11. Bundle Size Optimization

```bash
# Analyze bundle
ng build --stats-json
npx webpack-bundle-analyzer dist/my-app/stats.json
```

**Common improvements:**

```typescript
// BAD — imports entire library
import * as _ from 'lodash';

// GOOD — import only what you need
import { debounce } from 'lodash-es';
```

```typescript
// BAD — import entire Angular Material module
import { MatModule } from '@angular/material';  // doesn't exist but people do this pattern

// GOOD — tree-shakeable named imports
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule }  from '@angular/material/input';
```

```json
// angular.json — production build budgets (warn/error if exceeded)
"budgets": [
  { "type": "initial",    "maximumWarning": "500kB", "maximumError": "1MB" },
  { "type": "anyScript",  "maximumWarning": "150kB", "maximumError": "300kB" },
  { "type": "any",        "maximumWarning": "300kB", "maximumError": "600kB" }
]
```

---

## 12. Security Best Practices

```typescript
// 1. Never use innerHTML with user content — use DomSanitizer
import { DomSanitizer } from '@angular/platform-browser';

@Component({ ... })
export class HtmlComponent {
  private sanitizer = inject(DomSanitizer);

  // WRONG
  unsafeHtml = '<script>alert("XSS")</script>';

  // CORRECT — Angular auto-sanitizes [innerHTML] in templates
  // but use bypassSecurity ONLY for trusted content
  safeHtml = this.sanitizer.bypassSecurityTrustHtml('<b>Bold text</b>');
}

// In template — Angular strips dangerous tags automatically
<div [innerHTML]="trustedContent"></div>
```

```typescript
// 2. Use HttpClient — it automatically prevents XSRF/CSRF
// Angular sets the X-XSRF-TOKEN header automatically

// 3. Never expose secrets in environment.ts — they go into the bundle
// Use server-side proxying for API keys

// 4. Content Security Policy — configure in server headers
// not in Angular

// 5. Use strict TypeScript — catches many bugs at compile time
// tsconfig.json: "strict": true
```

---

## 13. Performance Checklist

| ✅ | Optimization |
|----|-------------|
| ☐ | `ChangeDetectionStrategy.OnPush` on all components |
| ☐ | `track` expression in all `@for` loops |
| ☐ | Lazy-load all feature routes |
| ☐ | `@defer` for below-the-fold content |
| ☐ | `NgOptimizedImage` for all `<img>` tags |
| ☐ | Pure pipes or `computed()` signals for derived data |
| ☐ | Virtual scrolling for lists > 100 items |
| ☐ | Bundle budget configured in `angular.json` |
| ☐ | SSR enabled for public-facing pages |
| ☐ | Avoid `any` type — use strict TypeScript |
| ☐ | `takeUntilDestroyed()` for all subscriptions |
| ☐ | `withFetch()` in `provideHttpClient()` |

---

## Summary

| Technique | Impact |
|-----------|--------|
| OnPush | High — fewer renders |
| `track` in @for | High — efficient DOM diffing |
| Lazy loading | High — smaller initial bundle |
| `@defer` | High — defer heavy components |
| Signals + Zoneless | Very High — no Zone.js overhead |
| SSR | High — faster FCP, better SEO |
| Virtual scrolling | High — for long lists |
| NgOptimizedImage | Medium — LCP improvement |

**Next:** [11 — Testing →](./11-testing.md)
