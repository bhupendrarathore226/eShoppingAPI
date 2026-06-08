# Chapter 11 — Testing

---

## 1. Testing Stack

| Layer | Tool |
|-------|------|
| Unit tests | Jasmine + Karma (default) or Jest |
| Component tests | Angular `TestBed` |
| Component harnesses | Angular CDK Harnesses |
| E2E tests | Playwright or Cypress |

```bash
# Run unit tests
ng test

# Run with coverage
ng test --code-coverage

# E2E (Playwright)
npm install -D @playwright/test
npx playwright test
```

---

## 2. Unit Testing a Service

```typescript
// product.service.spec.ts
import { TestBed }       from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ProductService],
    });
    service  = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());  // assert no pending requests

  it('should fetch all products', () => {
    const mockProducts = [{ id: 1, name: 'Laptop', price: 999 }];

    service.getAll().subscribe(products => {
      expect(products.length).toBe(1);
      expect(products[0].name).toBe('Laptop');
    });

    const req = httpMock.expectOne('/api/products');
    expect(req.request.method).toBe('GET');
    req.flush(mockProducts);   // supply mock response
  });

  it('should handle errors', () => {
    service.getAll().subscribe({
      error: err => expect(err.status).toBe(500),
    });

    httpMock.expectOne('/api/products').flush(
      { message: 'Server error' },
      { status: 500, statusText: 'Internal Server Error' }
    );
  });
});
```

---

## 3. Unit Testing a Component

```typescript
// product-card.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By }                        from '@angular/platform-browser';
import { ProductCardComponent }      from './product-card.component';

describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;

  const mockProduct = {
    id: 1,
    name: 'Laptop Pro',
    price: 999,
    imageUrl: '/img/laptop.jpg',
    inStock: true,
    description: 'A great laptop',
    rating: 4.5,
    reviewCount: 120,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCardComponent],  // standalone component — import directly
    }).compileComponents();

    fixture   = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;

    // Set required signal input
    fixture.componentRef.setInput('product', mockProduct);
    fixture.detectChanges();
  });

  it('should display product name', () => {
    const el = fixture.debugElement.query(By.css('.name'));
    expect(el.nativeElement.textContent).toContain('Laptop Pro');
  });

  it('should emit cartAdded when button clicked', () => {
    const spy = spyOn(component.cartAdded, 'emit');

    const button = fixture.debugElement.query(By.css('button'));
    button.triggerEventHandler('click');

    expect(spy).toHaveBeenCalledWith(mockProduct);
  });

  it('should disable button when out of stock', () => {
    fixture.componentRef.setInput('product', { ...mockProduct, inStock: false });
    fixture.detectChanges();

    const button = fixture.debugElement.query(By.css('button'));
    expect(button.nativeElement.disabled).toBeTrue();
  });
});
```

---

## 4. Testing with Fake Services

```typescript
// auth.guard.spec.ts
import { TestBed }    from '@angular/core/testing';
import { Router }     from '@angular/router';
import { authGuard }  from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', ['isLoggedIn']);
    router      = jasmine.createSpyObj('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router,      useValue: router },
      ],
    });
  });

  it('should return true when logged in', () => {
    authService.isLoggedIn.and.returnValue(true);
    const result = TestBed.runInInjectionContext(() => authGuard());
    expect(result).toBeTrue();
  });

  it('should redirect when not logged in', () => {
    authService.isLoggedIn.and.returnValue(false);
    router.createUrlTree.and.returnValue('/login' as any);

    const result = TestBed.runInInjectionContext(() => authGuard());
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], jasmine.any(Object));
  });
});
```

---

## 5. Testing Signals

```typescript
// cart.service.spec.ts
import { TestBed }    from '@angular/core/testing';
import { CartService } from './cart.service';

describe('CartService', () => {
  let service: CartService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CartService);
  });

  it('should start empty', () => {
    expect(service.items()).toEqual([]);
    expect(service.totalItems()).toBe(0);
  });

  it('should add an item', () => {
    service.addItem({ id: 1, name: 'Laptop', price: 999 });

    expect(service.items().length).toBe(1);
    expect(service.totalItems()).toBe(1);
    expect(service.totalPrice()).toBe(999);
  });

  it('should increment quantity for duplicate item', () => {
    service.addItem({ id: 1, name: 'Laptop', price: 999 });
    service.addItem({ id: 1, name: 'Laptop', price: 999 });

    expect(service.items().length).toBe(1);
    expect(service.items()[0].quantity).toBe(2);
    expect(service.totalItems()).toBe(2);
  });

  it('should remove an item', () => {
    service.addItem({ id: 1, name: 'Laptop', price: 999 });
    service.removeItem(1);
    expect(service.items()).toEqual([]);
  });
});
```

---

## 6. TestBed with RouterTestingHarness (Angular 15+)

```typescript
// product-details.component.spec.ts
import { TestBed }            from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideRouter }      from '@angular/router';
import { ProductDetailsComponent } from './product-details.component';

describe('ProductDetailsComponent routing', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'products/:id', component: ProductDetailsComponent }
        ]),
      ],
    }).compileComponents();
  });

  it('should read id from route params', async () => {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/products/42',
      ProductDetailsComponent
    );

    expect(component.id).toBe('42');
  });
});
```

---

## 7. Component Harnesses (CDK)

Harnesses provide a high-level API to interact with components in tests — no brittle DOM queries.

```typescript
// Example with Angular Material MatButton harness
import { HarnessLoader }        from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness }     from '@angular/material/button/testing';

describe('with harnesses', () => {
  let loader: HarnessLoader;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MyComponent] }).compileComponents();
    const fixture = TestBed.createComponent(MyComponent);
    loader = TestbedHarnessEnvironment.loader(fixture);
    fixture.detectChanges();
  });

  it('should click save button', async () => {
    const saveBtn = await loader.getHarness(MatButtonHarness.with({ text: 'Save' }));
    await saveBtn.click();
    // assertions...
  });
});
```

---

## 8. Writing a Custom Harness

```typescript
// product-card.harness.ts
import { ComponentHarness } from '@angular/cdk/testing';

export class ProductCardHarness extends ComponentHarness {
  static hostSelector = 'app-product-card';

  private nameEl   = this.locatorFor('.name');
  private priceEl  = this.locatorFor('.price');
  private addBtn   = this.locatorFor('button.btn-add');

  async getName():  Promise<string> { return (await this.nameEl()).text(); }
  async getPrice(): Promise<string> { return (await this.priceEl()).text(); }
  async clickAdd(): Promise<void>   { return (await this.addBtn()).click(); }
  async isAddDisabled(): Promise<boolean> {
    return (await this.addBtn()).getAttribute('disabled').then(v => v !== null);
  }
}
```

```typescript
// In tests
const card = await loader.getHarness(ProductCardHarness);
expect(await card.getName()).toBe('Laptop Pro');
await card.clickAdd();
```

---

## 9. E2E Testing with Playwright

```bash
npm install -D @playwright/test
npx playwright install
```

```typescript
// e2e/store.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Store page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/store');
  });

  test('should display products', async ({ page }) => {
    await expect(page.locator('app-product-card')).toHaveCount(12);
  });

  test('should filter products by search', async ({ page }) => {
    await page.fill('[placeholder="Search…"]', 'laptop');
    await expect(page.locator('app-product-card')).toHaveCountGreaterThan(0);
    await expect(page.locator('.name').first()).toContainText(/laptop/i);
  });

  test('should add product to cart', async ({ page }) => {
    await page.locator('app-product-card').first().locator('button').click();
    await expect(page.locator('.cart-badge')).toHaveText('1');
  });

  test('should navigate to product details', async ({ page }) => {
    await page.locator('app-product-card').first().click();
    await expect(page).toHaveURL(/\/products\/\d+/);
  });
});
```

```json
// playwright.config.ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://localhost:4200',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: 'ng serve',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
  },
});
```

---

## 10. Code Coverage

```bash
ng test --code-coverage
# Opens: coverage/index.html
```

```json
// angular.json — enforce minimum coverage
"test": {
  "options": {
    "codeCoverage": true,
    "codeCoverageExclude": ["**/index.ts", "**/*.spec.ts"],
    "karmaConfig": "karma.conf.js"
  }
}
```

```javascript
// karma.conf.js — fail below threshold
coverageReporter: {
  thresholds: {
    statements: 80,
    branches:   70,
    functions:  80,
    lines:      80,
  }
}
```

---

## 11. Jest (alternative to Karma/Jasmine)

```bash
npm install -D jest @types/jest jest-preset-angular

# Remove karma: ng test will use jest
```

```javascript
// jest.config.js
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterFramework: ['<rootDir>/setup-jest.ts'],
};
```

```typescript
// setup-jest.ts
import 'jest-preset-angular/setup-jest';
```

Jest tests use the same `TestBed` API — just replace Jasmine matchers:

```typescript
expect(result).toBe(5);          // same
expect(spy).toHaveBeenCalled();  // same
expect(result).toEqual({});       // same
```

---

## Summary

| What to test | API |
|-------------|-----|
| Service logic | Plain TypeScript + `TestBed.inject()` |
| HTTP calls | `HttpClientTestingModule` + `HttpTestingController` |
| Component rendering | `TestBed.createComponent()` + `fixture.detectChanges()` |
| Component inputs | `fixture.componentRef.setInput()` |
| User interactions | `element.triggerEventHandler('click')` |
| Signal outputs | `spyOn(component.output, 'emit')` |
| Routing | `RouterTestingHarness` |
| UI interactions | Component Harnesses (CDK) |
| Browser-level E2E | Playwright |

**Next:** [12 — Advanced Patterns →](./12-advanced.md)
