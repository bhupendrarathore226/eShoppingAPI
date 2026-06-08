# Angular 20 — Zero to Pro Developer Guide

> A complete, practical, example-driven guide covering every core and advanced Angular 20 concept.

---

## Table of Contents

| # | Chapter | Topics Covered |
|---|---------|----------------|
| 01 | [Getting Started](./01-getting-started.md) | Installation, CLI, project structure, first app |
| 02 | [Components](./02-components.md) | Class, decorator, lifecycle hooks, view encapsulation, inputs/outputs |
| 03 | [Templates, Binding & Directives](./03-templates-binding-directives.md) | Interpolation, property/event/two-way binding, built-in directives, pipes |
| 04 | [Services & Dependency Injection](./04-services-di.md) | Providers, hierarchical DI, InjectionToken, inject() |
| 05 | [Angular Router](./05-router.md) | Routes, lazy loading, guards, resolvers, query params |
| 06 | [Forms](./06-forms.md) | Template-driven forms, Reactive forms, validators, async validators |
| 07 | [HTTP Client](./07-http-client.md) | HttpClient, interceptors, error handling, caching |
| 08 | [Signals & State](./08-signals-state.md) | signal(), computed(), effect(), NgRx SignalStore |
| 09 | [Standalone APIs](./09-standalone.md) | Standalone components, bootstrapApplication, importProvidersFrom |
| 10 | [Performance](./10-performance.md) | OnPush, trackBy, deferrable views, SSR, hydration |
| 11 | [Testing](./11-testing.md) | Unit tests, TestBed, component harnesses, E2E with Playwright |
| 12 | [Advanced Patterns](./12-advanced.md) | Dynamic components, portals, custom decorators, micro-frontends |

---

## Prerequisites

| Skill | Level needed |
|-------|-------------|
| HTML / CSS | Basic |
| JavaScript (ES2022+) | Intermediate |
| TypeScript | Basic (guide teaches as needed) |

---

## What is Angular 20?

Angular 20 is a **platform and framework** for building single-page client applications in HTML and TypeScript.  
Key pillars of modern Angular (v16 → v20):

- **Signals** — fine-grained reactivity without Zone.js
- **Standalone** — no NgModule boilerplate required
- **Deferrable Views** — built-in lazy-loading inside templates
- **SSR + Hydration** — first-class server-side rendering
- **Typed Forms** — fully type-safe reactive forms
- **esbuild / Vite** — blazing fast builds

---

## Quick-start (3 commands)

```bash
npm install -g @angular/cli@20
ng new my-app --standalone --style=scss
cd my-app && ng serve
```

Open **http://localhost:4200** — your first Angular 20 app is running!

---

> Start with [01-getting-started.md](./01-getting-started.md) and work through each chapter in order.
