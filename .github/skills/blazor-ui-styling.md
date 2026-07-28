# SKILL: Blazor UI & Styling (MyBusApp)

## When to Use
Use this skill when creating or modifying Razor components, pages, layouts, or CSS files.

## Context — MyBusApp
- **CSS Framework:** Bootstrap 5 (from `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`)
- **Icons:** Bootstrap Icons (`bi bi-*`)
- **Custom styles:** Scoped CSS files (`Component.razor.css`) per component
- **Global CSS:** `wwwroot/css/app.css` (for root variables only)
- **Layout:** `Layout/MainLayout.razor` with `Layout/NavMenu.razor`

## Core Philosophy
Deliver clean, modern, fully responsive bus information UIs using Bootstrap 5 utility classes and native HTML elements. Prioritize accessibility, mobile-first design, and real-time data visualization.

## Rules

### 1. Layout & Responsive Design
- Mobile-first approach using Bootstrap grid: `container`, `row`, `col-12`, `col-md-*`, `col-lg-*`
- Use Bootstrap utility classes for layout:
  - Flexbox: `d-flex`, `flex-column`, `align-items-center`, `justify-content-between`, `gap-2`, `gap-3`
  - Margins/Padding: `m-0`, `mb-3`, `py-4`, `p-2`

### 2. Component Styling
- **Buttons:** `btn btn-primary`, `btn btn-outline-secondary`, `btn-sm`
- **Provider badges:** Color-code by provider (Metropolitana=green, Carris=red, Transitland=blue)
- **Cards:** `card`, `card-header`, `card-body`, `card-footer` for bus info panels
- **Tables:** `table table-hover align-middle` for stop lists and schedules
- **Forms:** `form-control`, `form-label`, `form-select` for search inputs
- **Validation:** `text-danger small` or `<ValidationMessage />`

### 3. Loading & Status States
- **Loading:** Always show Bootstrap spinner (`spinner-border spinner-border-sm`) during async API calls
- **Empty state:** Show "No arrivals found" or "Select a line first" with `text-muted`
- **Error state:** Show dismissible alert: `alert alert-warning alert-dismissible`
- **Disabled states:** Apply `disabled` attribute to buttons/inputs during API calls

### 4. CSS Isolation
- Write custom CSS only in scoped `.razor.css` files — never inline `style="..."`
- Global styles in `wwwroot/css/app.css` only for CSS variables and theme overrides

### 5. Accessibility
- Use `role` attributes: `role="alert"`, `role="status"`, `role="list"`, `role="listitem"`
- Use semantic HTML (`<header>`, `<main>`, `<nav>`, `<section>`)
- Ensure proper `aria-label` on interactive elements
- Color contrast must meet WCAG AA standards (especially provider colors)

## What NEVER to do
- Never use inline `style="..."` attributes — use CSS classes instead.
- Never add third-party UI frameworks (MudBlazor, Radzen, Syncfusion, AntDesign).
- Never mix Tailwind CSS or other utility frameworks.
- Never add custom JS for layout or interactivity — use Blazor C#.
- Never add external font imports without explicit approval.

