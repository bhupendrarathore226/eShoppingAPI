# Chapter 02 — Components

A **component** is the fundamental building block of every Angular application.  
Every UI element — a button, a product card, a full page — is a component.

---

## 1. Anatomy of a Component

```typescript
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-product-card',    // 1. HTML tag
  standalone: true,                // 2. No NgModule
  templateUrl: './product-card.component.html',  // 3. Template
  styleUrls: ['./product-card.component.scss'],  // 4. Styles
})
export class ProductCardComponent implements OnInit {
  // 5. Properties (state / data)
  title = 'Laptop';
  price = 999;

  // 6. Lifecycle hook
  ngOnInit(): void {
    console.log('ProductCard initialised');
  }
}
```

| Part | Purpose |
|------|---------|
| `selector` | The HTML tag used to render this component: `<app-product-card>` |
| `standalone: true` | Component manages its own imports — no NgModule required |
| `templateUrl` | External HTML file (or use inline `template`) |
| `styleUrls` | External SCSS/CSS files (or use inline `styles`) |

---

## 2. Inline vs External Template/Styles

### Inline (good for tiny components)

```typescript
@Component({
  selector: 'app-badge',
  standalone: true,
  template: `<span class="badge">{{ label }}</span>`,
  styles: [`.badge { background: gold; padding: 4px 8px; border-radius: 4px; }`]
})
export class BadgeComponent {
  label = 'NEW';
}
```

### External files (preferred for most components)

```
badge/
  badge.component.ts
  badge.component.html
  badge.component.scss
  badge.component.spec.ts
```

---

## 3. Component Inputs — Passing Data In

Use `@Input()` to accept data from a parent component.

```typescript
// child: product-card.component.ts
import { Component, Input } from '@angular/core';

interface Product {
  id: number;
  name: string;
  price: number;
  imageUrl: string;
}

@Component({
  selector: 'app-product-card',
  standalone: true,
  template: `
    <div class="card">
      <img [src]="product.imageUrl" [alt]="product.name" />
      <h3>{{ product.name }}</h3>
      <p>{{ product.price | currency }}</p>
    </div>
  `,
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;
  // Angular 20: required input — compiler error if parent forgets it
}
```

```html
<!-- parent template -->
<app-product-card [product]="selectedProduct" />
```

### Input with transform (Angular 16+)

```typescript
import { Input, booleanAttribute, numberAttribute } from '@angular/core';

@Component({ selector: 'app-toggle', standalone: true, template: `...` })
export class ToggleComponent {
  @Input({ transform: booleanAttribute }) disabled = false;
  @Input({ transform: numberAttribute }) tabIndex = 0;
}
```

```html
<!-- No need to bind: disabled="true", just use the attribute -->
<app-toggle disabled tabIndex="3" />
```

---

## 4. Component Outputs — Emitting Events

Use `@Output()` with `EventEmitter` to send data to the parent.

```typescript
// child
import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-product-card',
  standalone: true,
  template: `
    <div class="card">
      <h3>{{ product.name }}</h3>
      <button (click)="addToCart()">Add to Cart</button>
    </div>
  `,
})
export class ProductCardComponent {
  @Input({ required: true }) product!: any;
  @Output() cartAdded = new EventEmitter<number>(); // emits the product id

  addToCart() {
    this.cartAdded.emit(this.product.id);
  }
}
```

```html
<!-- parent -->
<app-product-card
  [product]="p"
  (cartAdded)="onCartAdded($event)"
/>
```

```typescript
// parent class
onCartAdded(productId: number) {
  console.log('Added product', productId);
}
```

---

## 5. Modern Signal-based Inputs & Outputs (Angular 17+)

Angular 20 recommends the new signal-based API:

```typescript
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-product-card',
  standalone: true,
  template: `
    <h3>{{ product().name }}</h3>   <!-- call as function -->
    <button (click)="addToCart()">Add</button>
  `,
})
export class ProductCardComponent {
  // Signal input — reactive, no @Input decorator
  product = input.required<Product>();

  // Signal output — replaces EventEmitter
  cartAdded = output<number>();

  addToCart() {
    this.cartAdded.emit(this.product().id);
  }
}
```

> **Signal inputs** are read-only signals — use `product()` to get the value.  
> Changes propagate without Zone.js.

---

## 6. Lifecycle Hooks

Angular calls lifecycle hooks in this order:

```
constructor()
  ↓
ngOnChanges()    ← called when @Input values change
  ↓
ngOnInit()       ← called once after first ngOnChanges
  ↓
ngDoCheck()      ← every change detection cycle
  ↓
ngAfterContentInit()   ← after projected content initialised
  ↓
ngAfterContentChecked()
  ↓
ngAfterViewInit()      ← after the component's view initialised
  ↓
ngAfterViewChecked()
  ↓
ngOnDestroy()    ← just before component is removed from DOM
```

### Practical examples

```typescript
import {
  Component, OnInit, OnDestroy, OnChanges,
  SimpleChanges, AfterViewInit, ViewChild, ElementRef
} from '@angular/core';

@Component({ selector: 'app-demo', standalone: true, template: `<canvas #chart></canvas>` })
export class DemoComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('chart') canvasRef!: ElementRef<HTMLCanvasElement>;

  ngOnChanges(changes: SimpleChanges): void {
    // changes['inputName'].currentValue / previousValue / firstChange
    if (changes['product']) {
      console.log('Product changed:', changes['product'].currentValue);
    }
  }

  ngOnInit(): void {
    // Good place to: fetch initial data, subscribe to route params
    console.log('Component initialised');
  }

  ngAfterViewInit(): void {
    // DOM is ready — safe to use ViewChild references
    const ctx = this.canvasRef.nativeElement.getContext('2d');
  }

  ngOnDestroy(): void {
    // Unsubscribe from observables, clear intervals, destroy third-party instances
    this.subscription?.unsubscribe();
  }

  private subscription: any;
}
```

---

## 7. ViewChild & ViewChildren

Access child component instances or DOM elements from the class.

```typescript
import { Component, ViewChild, ViewChildren, QueryList, AfterViewInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ProductCardComponent } from './product-card.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [ProductCardComponent],
  template: `
    <app-product-card #firstCard [product]="products[0]" />
    <app-product-card *ngFor="let p of products" [product]="p" />
  `,
})
export class ProductListComponent implements AfterViewInit {
  @ViewChild('firstCard') firstCard!: ProductCardComponent;
  @ViewChildren(ProductCardComponent) allCards!: QueryList<ProductCardComponent>;

  ngAfterViewInit() {
    console.log('First card:', this.firstCard);
    console.log('All cards count:', this.allCards.length);
  }
}
```

---

## 8. Content Projection (ng-content)

Build wrapper/layout components that accept arbitrary HTML from the parent.

```typescript
// card-shell.component.ts
@Component({
  selector: 'app-card-shell',
  standalone: true,
  template: `
    <div class="shell">
      <div class="header">
        <ng-content select="[slot=header]" />
      </div>
      <div class="body">
        <ng-content />          <!-- default slot -->
      </div>
      <div class="footer">
        <ng-content select="[slot=footer]" />
      </div>
    </div>
  `,
})
export class CardShellComponent {}
```

```html
<!-- usage -->
<app-card-shell>
  <h2 slot="header">Product Title</h2>
  <p>Main body content goes here.</p>
  <button slot="footer">Buy Now</button>
</app-card-shell>
```

---

## 9. View Encapsulation

Controls how component styles are scoped.

```typescript
import { Component, ViewEncapsulation } from '@angular/core';

@Component({
  selector: 'app-demo',
  standalone: true,
  template: `<p class="text">Hello</p>`,
  styles: [`.text { color: red; }`],
  encapsulation: ViewEncapsulation.Emulated,  // DEFAULT — scoped via attributes
  // encapsulation: ViewEncapsulation.None,   // Global styles (no scoping)
  // encapsulation: ViewEncapsulation.ShadowDom, // Native Shadow DOM
})
export class DemoComponent {}
```

| Mode | Behaviour |
|------|-----------|
| `Emulated` (default) | Angular adds unique attributes to scope styles — safest |
| `None` | Styles are global — can cause leakage |
| `ShadowDom` | Uses browser's native Shadow DOM — true isolation |

---

## 10. Change Detection

Angular checks for changes and updates the DOM.

### Default strategy

Every browser event (click, HTTP response, timer) triggers change detection on the **entire component tree**.

### `OnPush` — for performance

```typescript
import { Component, ChangeDetectionStrategy, Input } from '@angular/core';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h3>{{ product.name }}</h3>`,
})
export class ProductCardComponent {
  @Input() product!: any;
  // Angular only re-renders this when:
  // 1. An @Input reference changes
  // 2. An Observable emits via async pipe
  // 3. You manually call markForCheck()
}
```

---

## 11. Real-World Example — Product Card

```typescript
// models/product.model.ts
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  rating: number;
  reviewCount: number;
  inStock: boolean;
}
```

```typescript
// product-card.component.ts
import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CurrencyPipe, NgClass } from '@angular/common';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CurrencyPipe, NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="card" [ngClass]="{ 'out-of-stock': !product().inStock }">
      <img [src]="product().imageUrl" [alt]="product().name" loading="lazy" />

      <div class="info">
        <h3 class="name">{{ product().name }}</h3>
        <p class="description">{{ product().description }}</p>

        <div class="meta">
          <span class="price">{{ product().price | currency }}</span>
          <span class="rating">⭐ {{ product().rating }} ({{ product().reviewCount }})</span>
        </div>

        <button
          class="btn-add"
          [disabled]="!product().inStock"
          (click)="addToCart()">
          {{ product().inStock ? 'Add to Cart' : 'Out of Stock' }}
        </button>
      </div>
    </article>
  `,
})
export class ProductCardComponent {
  product = input.required<Product>();
  cartAdded = output<Product>();

  addToCart() {
    if (this.product().inStock) {
      this.cartAdded.emit(this.product());
    }
  }
}
```

---

## Summary

| Concept | Key API |
|---------|---------|
| Define component | `@Component({ selector, standalone, template })` |
| Accept data from parent | `input()` / `@Input()` |
| Emit events to parent | `output()` / `@Output() EventEmitter` |
| Lifecycle | `ngOnInit`, `ngOnDestroy`, `ngAfterViewInit` … |
| Access DOM / child | `@ViewChild`, `@ViewChildren` |
| Project content | `<ng-content>` |
| Performance | `ChangeDetectionStrategy.OnPush` |

**Next:** [03 — Templates, Binding & Directives →](./03-templates-binding-directives.md)
