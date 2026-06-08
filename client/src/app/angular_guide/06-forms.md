# Chapter 06 — Forms

Angular provides two approaches to forms:

| | Template-Driven | Reactive |
|--|----------------|---------|
| Logic lives in | HTML template | TypeScript class |
| API | `NgModel`, `NgForm` | `FormGroup`, `FormControl`, `FormBuilder` |
| Best for | Simple forms | Complex, dynamic, testable forms |
| Validation | HTML attributes | Validator functions |
| Type safety | Limited | Full (Angular 14+) |

---

## PART A — Template-Driven Forms

### 1. Setup

```typescript
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],  // required
  templateUrl: './login.component.html',
})
export class LoginComponent { }
```

### 2. Basic Login Form

```html
<!-- login.component.html -->
<form #loginForm="ngForm" (ngSubmit)="onSubmit(loginForm)">

  <div class="field">
    <label for="email">Email</label>
    <input
      id="email"
      name="email"
      type="email"
      [(ngModel)]="credentials.email"
      required
      email
      #emailField="ngModel"
    />
    @if (emailField.invalid && emailField.touched) {
      @if (emailField.errors?.['required']) {
        <span class="error">Email is required</span>
      }
      @if (emailField.errors?.['email']) {
        <span class="error">Must be a valid email</span>
      }
    }
  </div>

  <div class="field">
    <label for="password">Password</label>
    <input
      id="password"
      name="password"
      type="password"
      [(ngModel)]="credentials.password"
      required
      minlength="8"
      #passwordField="ngModel"
    />
    @if (passwordField.invalid && passwordField.touched) {
      <span class="error">Password must be at least 8 characters</span>
    }
  </div>

  <button type="submit" [disabled]="loginForm.invalid || isLoading">
    {{ isLoading ? 'Logging in…' : 'Login' }}
  </button>
</form>
```

```typescript
export class LoginComponent {
  credentials = { email: '', password: '' };
  isLoading   = false;

  onSubmit(form: NgForm) {
    if (form.invalid) return;
    this.isLoading = true;
    console.log('Submitting:', this.credentials);
    // call service...
  }
}
```

### 3. Form States

Angular adds CSS classes to inputs automatically:

| Class | Meaning |
|-------|---------|
| `ng-pristine` | Never changed |
| `ng-dirty` | Has been modified |
| `ng-untouched` | Never focused |
| `ng-touched` | Has been focused and left |
| `ng-valid` | Passes all validators |
| `ng-invalid` | Fails at least one validator |

```scss
input.ng-invalid.ng-touched {
  border-color: red;
}
```

---

## PART B — Reactive Forms

### 1. Setup

```typescript
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule],
  ...
})
```

### 2. FormControl — Single Field

```typescript
import { Component } from '@angular/core';
import { FormControl, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <input [formControl]="searchCtrl" placeholder="Search…" />
    @if (searchCtrl.errors?.['minlength']) {
      <span>Minimum 3 characters</span>
    }
  `,
})
export class SearchComponent {
  searchCtrl = new FormControl('', [Validators.minLength(3)]);
}
```

### 3. FormGroup — Multiple Fields

```typescript
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(8), this.strongPassword]],
    confirm:   ['', Validators.required],
    agree:     [false, Validators.requiredTrue],
  }, { validators: this.passwordMatch });

  // Custom validator function
  strongPassword(control: AbstractControl) {
    const v = control.value as string;
    return /[A-Z]/.test(v) && /[0-9]/.test(v) ? null : { weakPassword: true };
  }

  // Cross-field validator
  passwordMatch(group: AbstractControl) {
    const pw  = group.get('password')?.value;
    const cfm = group.get('confirm')?.value;
    return pw === cfm ? null : { mismatch: true };
  }

  // Typed helpers
  get f() { return this.form.controls; }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    console.log(this.form.getRawValue());
  }
}
```

```html
<!-- register.component.html -->
<form [formGroup]="form" (ngSubmit)="onSubmit()">

  <input formControlName="firstName" placeholder="First name" />
  @if (f.firstName.invalid && f.firstName.touched) {
    <span>First name is required (min 2 chars)</span>
  }

  <input formControlName="email" type="email" placeholder="Email" />
  @if (f.email.errors?.['required'] && f.email.touched) {
    <span>Email required</span>
  }
  @if (f.email.errors?.['email'] && f.email.touched) {
    <span>Invalid email format</span>
  }

  <input formControlName="password" type="password" placeholder="Password" />
  @if (f.password.errors?.['weakPassword'] && f.password.touched) {
    <span>Password needs uppercase + number</span>
  }

  <input formControlName="confirm" type="password" placeholder="Confirm password" />
  @if (form.errors?.['mismatch'] && f.confirm.touched) {
    <span>Passwords do not match</span>
  }

  <label>
    <input type="checkbox" formControlName="agree" />
    I agree to the terms
  </label>

  <button type="submit" [disabled]="form.invalid">Register</button>
</form>
```

---

## 4. Built-in Validators

```typescript
import { Validators } from '@angular/forms';

Validators.required
Validators.requiredTrue          // for checkboxes
Validators.email
Validators.minLength(n)
Validators.maxLength(n)
Validators.min(n)                // numeric
Validators.max(n)
Validators.pattern(/regex/)
Validators.nullValidator         // always valid (no-op)
```

---

## 5. Custom Validators

### Sync Validator

```typescript
// validators/no-spaces.validator.ts
import { AbstractControl, ValidationErrors } from '@angular/forms';

export function noSpaces(control: AbstractControl): ValidationErrors | null {
  const hasSpaces = (control.value as string)?.includes(' ');
  return hasSpaces ? { noSpaces: true } : null;
}
```

```typescript
username: ['', [Validators.required, noSpaces]],
```

### Validator with Parameters

```typescript
export function forbiddenWords(words: string[]) {
  return (control: AbstractControl): ValidationErrors | null => {
    const found = words.find(w =>
      control.value?.toLowerCase().includes(w.toLowerCase())
    );
    return found ? { forbiddenWord: { word: found } } : null;
  };
}

// usage
username: ['', forbiddenWords(['admin', 'root', 'system'])],
```

### Async Validator — Check Uniqueness via API

```typescript
// validators/unique-email.validator.ts
import { inject }            from '@angular/core';
import { AbstractControl }   from '@angular/forms';
import { UserService }       from '../services/user.service';
import { debounceTime, distinctUntilChanged, switchMap, map, first } from 'rxjs';

export function uniqueEmailValidator(userService: UserService) {
  return (control: AbstractControl) =>
    control.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(email => userService.checkEmailAvailable(email)),
      map(available => (available ? null : { emailTaken: true })),
      first(),
    );
}
```

```typescript
email: ['', [Validators.required, Validators.email],
        [uniqueEmailValidator(this.userService)]],
```

---

## 6. FormArray — Dynamic Lists

```typescript
import { FormArray, FormGroup, FormControl, FormBuilder, Validators } from '@angular/forms';

@Component({ ... })
export class OrderFormComponent {
  private fb = inject(FormBuilder);

  form = this.fb.group({
    customerName: ['', Validators.required],
    items: this.fb.array([this.newItem()]),
  });

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  newItem(): FormGroup {
    return this.fb.group({
      productId: [null, Validators.required],
      quantity:  [1,    [Validators.required, Validators.min(1)]],
    });
  }

  addItem()          { this.items.push(this.newItem()); }
  removeItem(i: number) { this.items.removeAt(i); }
}
```

```html
<div formArrayName="items">
  @for (item of items.controls; track $index; let i = $index) {
    <div [formGroupName]="i" class="item-row">
      <select formControlName="productId">
        @for (p of products; track p.id) {
          <option [value]="p.id">{{ p.name }}</option>
        }
      </select>
      <input type="number" formControlName="quantity" />
      <button type="button" (click)="removeItem(i)">Remove</button>
    </div>
  }
  <button type="button" (click)="addItem()">+ Add Item</button>
</div>
```

---

## 7. Typed Reactive Forms (Angular 14+)

```typescript
import { FormControl, FormGroup } from '@angular/forms';

// Fully typed — TypeScript knows every field's type
const form = new FormGroup({
  email:    new FormControl<string>('', { nonNullable: true }),
  age:      new FormControl<number | null>(null),
  agree:    new FormControl<boolean>(false, { nonNullable: true }),
});

// TypeScript infers: { email: string; age: number | null; agree: boolean }
const value = form.getRawValue();
```

### Using `FormBuilder` with types

```typescript
const fb = inject(FormBuilder);

const form = fb.nonNullable.group({
  username: ['', Validators.required],   // FormControl<string>
  age:      [0,  Validators.min(0)],     // FormControl<number>
});
// All fields are non-nullable with nonNullable.group()
```

---

## 8. Reacting to Value Changes

```typescript
// Listen to entire form
this.form.valueChanges.subscribe(value => {
  console.log('Form changed:', value);
});

// Listen to single control
this.form.get('email')!.valueChanges
  .pipe(debounceTime(300), distinctUntilChanged())
  .subscribe(email => this.checkEmail(email));

// With takeUntilDestroyed (Angular 16+)
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

this.form.valueChanges.pipe(
  takeUntilDestroyed()   // auto-unsubscribes when component destroyed
).subscribe(console.log);
```

---

## 9. Patching vs Setting Values

```typescript
// setValue — must provide ALL fields
this.form.setValue({
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  password: '',
  confirm: '',
  agree: false,
});

// patchValue — partial update, only named fields change
this.form.patchValue({
  firstName: 'John',
  email: 'john@example.com',
});

// Reset to initial values
this.form.reset();

// Reset to specific values
this.form.reset({ email: 'default@example.com' });
```

---

## 10. Complete Registration Form Example

```typescript
// register.component.ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private fb      = inject(FormBuilder);
  private auth    = inject(AuthService);
  private router  = inject(Router);

  isLoading = signal(false);
  serverError = signal('');

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(8),
                     Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).*$/)]],
    confirm:   ['', Validators.required],
  }, { validators: (g: AbstractControl) => {
    const pw  = g.get('password')?.value;
    const cfm = g.get('confirm')?.value;
    return pw === cfm ? null : { mismatch: true };
  }});

  get f() { return this.form.controls; }

  async onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isLoading.set(true);
    this.serverError.set('');

    try {
      const { confirm, ...payload } = this.form.getRawValue();
      await this.auth.register(payload);
      this.router.navigate(['/account']);
    } catch (err: any) {
      this.serverError.set(err.message ?? 'Registration failed');
    } finally {
      this.isLoading.set(false);
    }
  }
}
```

---

## Summary

| Concept | Template-Driven | Reactive |
|---------|----------------|---------|
| Bind control | `ngModel` | `formControlName` |
| Group controls | `ngForm` | `FormGroup` |
| Dynamic lists | not ideal | `FormArray` |
| Custom validators | directive | function |
| Async validators | directive | async function array |
| Value access | template variable | `form.getRawValue()` |

**Next:** [07 — HTTP Client →](./07-http-client.md)
