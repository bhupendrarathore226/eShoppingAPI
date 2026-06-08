# Chapter 03 — Templates, Data Binding & Directives

---

## 1. Interpolation `{{ }}`

Renders a TypeScript expression as text.

```html
<h1>{{ title }}</h1>
<p>Price: {{ product.price | currency }}</p>
<p>Sum: {{ 5 + 3 }}</p>
<p>Status: {{ isActive ? 'Active' : 'Inactive' }}</p>
```

> **Rule:** Expressions inside `{{ }}` must not have side effects (no assignments, no `new`, no `;`).

---

## 2. Property Binding `[ ]`

Sets a DOM property or component `@Input` from a class value.

```html
<!-- DOM property -->
<img [src]="product.imageUrl" [alt]="product.name" />
<button [disabled]="isLoading">Save</button>
<input [value]="username" />

<!-- Component input -->
<app-product-card [product]="selectedProduct" />

<!-- Class & style binding -->
<div [class.active]="isActive">...</div>
<div [style.color]="isError ? 'red' : 'black'">...</div>
<div [ngClass]="{ active: isActive, error: hasError }">...</div>
<div [ngStyle]="{ fontSize: fontSize + 'px', color: themeColor }">...</div>
```

**Difference: attribute vs property**

```html
<!-- Attribute (HTML) — sets the initial value only -->
<input value="hello" />

<!-- Property (DOM) — live binding -->
<input [value]="username" />
```

---

## 3. Event Binding `( )`

Listen to DOM events or component output events.

```html
<!-- DOM events -->
<button (click)="onSave()">Save</button>
<input (input)="onInput($event)" />
<input (keyup.enter)="onEnter()" />
<form (submit)="onSubmit($event)">...</form>
<div (mouseover)="showTooltip()" (mouseout)="hideTooltip()">...</div>

<!-- Prevent default -->
<a href="#" (click)="navigate($event); $event.preventDefault()">Link</a>
```

```typescript
export class DemoComponent {
  onInput(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    console.log(value);
  }
}
```

---

## 4. Two-Way Binding `[( )]` — "Banana in a box"

Combines property + event binding. The most common use is with forms.

```html
<!-- Requires FormsModule imported -->
<input [(ngModel)]="username" />
<p>Hello, {{ username }}</p>
```

```typescript
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-demo',
  standalone: true,
  imports: [FormsModule],
  template: `
    <input [(ngModel)]="username" placeholder="Type your name" />
    <p>Hello, {{ username }}</p>
  `,
})
export class DemoComponent {
  username = '';
}
```

**Custom two-way binding** (no FormsModule needed):

```typescript
// counter.component.ts
import { Component, model } from '@angular/core';

@Component({
  selector: 'app-counter',
  standalone: true,
  template: `
    <button (click)="decrement()">-</button>
    <span>{{ count() }}</span>
    <button (click)="increment()">+</button>
  `,
})
export class CounterComponent {
  count = model(0);   // Angular 17+ model() signal

  increment() { this.count.update(v => v + 1); }
  decrement() { this.count.update(v => v - 1); }
}
```

```html
<!-- parent — two-way bound to parentValue signal -->
<app-counter [(count)]="parentValue" />
```

---

## 5. Built-in Control Flow (Angular 17+)

Angular 20 uses the new `@if`, `@for`, `@switch` syntax (replacing `*ngIf`, `*ngFor`).

### @if

```html
@if (isLoggedIn) {
  <p>Welcome back!</p>
} @else if (isLoading) {
  <app-spinner />
} @else {
  <a routerLink="/login">Please log in</a>
}
```

### @for

```html
@for (product of products; track product.id) {
  <app-product-card [product]="product" />
} @empty {
  <p>No products found.</p>
}
```

> `track` is **required** in `@for` — it tells Angular how to identify each item for efficient DOM updates. Use a unique field like `id`.

### @switch

```html
@switch (orderStatus) {
  @case ('pending') {
    <span class="badge pending">Pending</span>
  }
  @case ('shipped') {
    <span class="badge shipped">Shipped</span>
  }
  @case ('delivered') {
    <span class="badge delivered">Delivered</span>
  }
  @default {
    <span class="badge">Unknown</span>
  }
}
```

---

## 6. Legacy Structural Directives (still supported)

```html
<!-- *ngIf -->
<div *ngIf="isLoggedIn; else loginBlock">Welcome!</div>
<ng-template #loginBlock><a routerLink="/login">Login</a></ng-template>

<!-- *ngFor -->
<ul>
  <li *ngFor="let item of items; let i = index; trackBy: trackById">
    {{ i + 1 }}. {{ item.name }}
  </li>
</ul>

<!-- *ngSwitch -->
<div [ngSwitch]="role">
  <p *ngSwitchCase="'admin'">Admin Panel</p>
  <p *ngSwitchCase="'user'">User Dashboard</p>
  <p *ngSwitchDefault>Guest View</p>
</div>
```

```typescript
trackById(index: number, item: { id: number }) {
  return item.id;
}
```

---

## 7. Attribute Directives

Attribute directives **change the appearance or behaviour** of an existing element.

### `NgClass`

```html
<div [ngClass]="cardClasses">Card</div>
```

```typescript
get cardClasses() {
  return {
    'card': true,
    'card--featured': this.product.featured,
    'card--sold-out': !this.product.inStock,
  };
}
```

### `NgStyle`

```html
<div [ngStyle]="{ 'font-size.px': fontSize, color: textColor }">Text</div>
```

### Building a Custom Attribute Directive

```typescript
// highlight.directive.ts
import { Directive, ElementRef, HostListener, Input } from '@angular/core';

@Directive({
  selector: '[appHighlight]',
  standalone: true,
})
export class HighlightDirective {
  @Input() appHighlight = 'yellow';   // color passed as attribute value

  constructor(private el: ElementRef) {}

  @HostListener('mouseenter')
  onMouseEnter() {
    this.el.nativeElement.style.backgroundColor = this.appHighlight;
  }

  @HostListener('mouseleave')
  onMouseLeave() {
    this.el.nativeElement.style.backgroundColor = '';
  }
}
```

```html
<!-- usage -->
<p appHighlight="lightblue">Hover over me!</p>
<p appHighlight>Uses default yellow</p>
```

---

## 8. Structural Directive — Build Your Own

```typescript
// unless.directive.ts  (opposite of *ngIf)
import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';

@Directive({
  selector: '[appUnless]',
  standalone: true,
})
export class UnlessDirective {
  private rendered = false;

  constructor(
    private tpl: TemplateRef<any>,
    private vcr: ViewContainerRef,
  ) {}

  @Input() set appUnless(condition: boolean) {
    if (!condition && !this.rendered) {
      this.vcr.createEmbeddedView(this.tpl);
      this.rendered = true;
    } else if (condition && this.rendered) {
      this.vcr.clear();
      this.rendered = false;
    }
  }
}
```

```html
<p *appUnless="isLoggedIn">Please log in to continue.</p>
```

---

## 9. Pipes

Pipes **transform displayed values** without changing the underlying data.

### Built-in Pipes

```html
<!-- DatePipe -->
{{ today | date }}                     <!-- May 5, 2026 -->
{{ today | date:'dd/MM/yyyy' }}        <!-- 05/05/2026 -->
{{ today | date:'shortTime' }}         <!-- 10:30 AM -->

<!-- CurrencyPipe -->
{{ price | currency }}                 <!-- $999.00 -->
{{ price | currency:'EUR':'symbol' }}  <!-- €999.00 -->

<!-- DecimalPipe -->
{{ 3.14159 | number:'1.2-2' }}        <!-- 3.14 -->

<!-- PercentPipe -->
{{ 0.75 | percent }}                   <!-- 75% -->

<!-- UpperCase / LowerCase / TitleCase -->
{{ 'hello world' | titlecase }}        <!-- Hello World -->

<!-- SlicePipe -->
{{ items | slice:0:5 }}               <!-- first 5 items -->

<!-- JsonPipe (debug) -->
<pre>{{ someObject | json }}</pre>

<!-- AsyncPipe — subscribes automatically -->
{{ products$ | async | json }}
```

### Chaining Pipes

```html
{{ product.name | uppercase | slice:0:10 }}
```

### Custom Pipe

```typescript
// truncate.pipe.ts
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'truncate',
  standalone: true,
})
export class TruncatePipe implements PipeTransform {
  transform(value: string, limit = 50, ellipsis = '…'): string {
    if (!value) return '';
    return value.length > limit ? value.substring(0, limit) + ellipsis : value;
  }
}
```

```html
<!-- import TruncatePipe in the component's imports array -->
{{ product.description | truncate:80 }}
```

---

## 10. Template Reference Variables

```html
<!-- Grab a DOM element reference -->
<input #nameInput type="text" />
<button (click)="greet(nameInput.value)">Greet</button>

<!-- Grab a component/directive reference -->
<app-product-card #card [product]="p" />
<button (click)="card.addToCart()">Quick Add</button>

<!-- Grab NgForm -->
<form #loginForm="ngForm" (ngSubmit)="submit(loginForm)">
  <input name="email" ngModel required />
  <button type="submit" [disabled]="loginForm.invalid">Login</button>
</form>
```

---

## 11. `ng-container` & `ng-template`

### `ng-container` — logical grouping, renders no DOM element

```html
<!-- Apply multiple structural directives without adding a wrapping div -->
<ng-container *ngIf="isAdmin">
  <button>Edit</button>
  <button>Delete</button>
</ng-container>

<!-- New syntax -->
@if (isAdmin) {
  <button>Edit</button>
  <button>Delete</button>
}
```

### `ng-template` — define reusable template fragments

```html
<ng-template #loading>
  <app-spinner />
  <p>Loading…</p>
</ng-template>

@if (products.length; else loading) {
  <app-product-card *ngFor="let p of products" [product]="p" />
}
```

---

## 12. Deferrable Views `@defer` (Angular 17+)

Lazy-load heavy parts of the template:

```html
@defer (on viewport) {
  <!-- Loaded only when this area scrolls into view -->
  <app-heavy-chart [data]="chartData" />
} @placeholder {
  <div class="placeholder">Chart loading…</div>
} @loading (minimum 300ms) {
  <app-spinner />
} @error {
  <p>Failed to load chart.</p>
}
```

**Triggers:**

| Trigger | Description |
|---------|-------------|
| `on idle` | When browser is idle (default) |
| `on viewport` | When element enters the viewport |
| `on interaction` | On first click or focus |
| `on hover` | On first hover |
| `on timer(2s)` | After a delay |
| `when condition` | When a boolean expression is true |

---

## 13. Full Example — Product List Template

```typescript
// product-list.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, NgClass, SlicePipe } from '@angular/common';
import { ProductCardComponent } from './product-card.component';
import { TruncatePipe } from '../pipes/truncate.pipe';
import { Product } from '../models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CurrencyPipe, NgClass, SlicePipe, ProductCardComponent, TruncatePipe],
  template: `
    <section class="product-list">
      <header>
        <h2>Products ({{ products().length }})</h2>
        <input
          #searchInput
          type="text"
          placeholder="Search…"
          (input)="search(searchInput.value)"
        />
      </header>

      @if (isLoading()) {
        <app-spinner />
      } @else if (filtered().length === 0) {
        <p class="empty">No products match "{{ searchTerm() }}".</p>
      } @else {
        <div class="grid">
          @for (product of filtered(); track product.id) {
            <app-product-card
              [product]="product"
              (cartAdded)="onCartAdded($event)"
            />
          }
        </div>
      }
    </section>
  `,
})
export class ProductListComponent implements OnInit {
  products = signal<Product[]>([]);
  searchTerm = signal('');
  isLoading = signal(true);

  filtered = computed(() =>
    this.products().filter(p =>
      p.name.toLowerCase().includes(this.searchTerm().toLowerCase())
    )
  );

  ngOnInit() {
    // Simulated fetch
    setTimeout(() => {
      this.products.set([
        { id: 1, name: 'Laptop Pro', price: 1499, /* ... */ } as Product,
        { id: 2, name: 'Wireless Mouse', price: 39, /* ... */ } as Product,
      ]);
      this.isLoading.set(false);
    }, 1000);
  }

  search(term: string) {
    this.searchTerm.set(term);
  }

  onCartAdded(product: Product) {
    console.log('Cart:', product.name);
  }
}
```

---

## Summary

| Syntax | Purpose |
|--------|---------|
| `{{ expr }}` | Interpolation — display data |
| `[prop]="expr"` | Property binding — set DOM property |
| `(event)="handler()"` | Event binding — react to events |
| `[(ngModel)]` | Two-way binding |
| `@if / @for / @switch` | Control flow (modern) |
| `*ngIf / *ngFor` | Control flow (legacy, still supported) |
| `\| pipeName` | Transform display values |
| `#ref` | Template reference variable |
| `@defer` | Lazy-load template blocks |

**Next:** [04 — Services & Dependency Injection →](./04-services-di.md)
