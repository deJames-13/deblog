# Technical Architecture: Tailwind File-First VSA

This reference documents the architectural principles, CSS cascade mechanics, layer prioritization, and component state rules governing the Tailwind File-First Vertical Slice Architecture pattern.

---

## 1. CSS Cascade & The `@layer components` Mandate

When using Tailwind's `@apply` directive to abstract utility classes into reusable stylesheets, **all classes must be defined inside `@layer components`**:

```css
/* ✅ CORRECT: Placed within components layer */
@layer components {
  .feature-button {
    @apply inline-flex items-center justify-center px-4 py-2 font-medium rounded-lg;
  }
}

/* ❌ INCORRECT: Naked rule outside @layer */
.feature-button {
  @apply inline-flex items-center justify-center px-4 py-2 font-medium rounded-lg;
}
```

### Why This Matters: Utility Precedence
Tailwind outputs three primary layers:
1. `base`: Reset and default element styles.
2. `components`: Class-based component styles that can be overridden by utilities.
3. `utilities`: Single-purpose utility classes (e.g., `pt-8`, `hidden`, `text-red-500`).

Wrapping styles in `@layer components` guarantees that if a component consumer passes an ad-hoc utility class (e.g. `<button className="feature-button px-6">`), the utility class (`px-6`) will win the cascade over the component's default (`px-4`) because the `utilities` layer is evaluated after the `components` layer.

---

## 2. Vertical Slice Architecture (VSA) Slicing Rules

```text
styles/
├── auth/                      # Feature Slice
│   ├── main.css               # Slice aggregator
│   ├── login-form.css
│   └── oauth-providers.css
├── dashboard/                 # Feature Slice
│   ├── main.css               # Slice aggregator
│   ├── header.css
│   └── widgets.css
├── button.css                 # Global shared primitive
├── card.css                   # Global shared primitive
├── main.css                   # Global shared aggregator
└── styles.css                 # Root stylesheet entrypoint
```

### Rule 1: Slices Follow Business Domains
Slice directories reflect application domains (`auth`, `billing`, `dashboard`, `settings`), not technical layers (`forms`, `tables`, `containers`).

### Rule 2: The Aggregator Pattern (`main.css`)
Each directory has a single `main.css` aggregator:
- `styles/<feature>/main.css` imports all feature-specific `.css` files.
- `styles/main.css` imports all global primitive `.css` files.
- `styles/styles.css` imports the base directives and each aggregator.

### Rule 3: Zero Cross-Slice Imports
A feature slice (e.g., `auth`) must never import directly from another feature slice (e.g., `dashboard`). If a style or layout pattern is required in both:
1. Promote the shared element to root `styles/` as a shared primitive.
2. Import it via `styles/main.css`.

---

## 3. Variant, State, and Token Management

### State Coverage (5-State Standard)
Ensure every interactive component stylesheet accounts for all essential UX states:
- **Initial / Default:** Base layout and tokens.
- **Hover / Active:** Smooth micro-interactions (`transition-colors duration-150`).
- **Focus / Keyboard:** Clear accessibility focus rings (`focus-visible:ring-2 focus-visible:ring-offset-2`).
- **Loading / Skeleton:** Pulse animation and muted color blocks (`animate-pulse bg-slate-800`).
- **Disabled / Error:** Explicit disabled cursors and descriptive visual cues (`disabled:opacity-50 disabled:cursor-not-allowed`).

### Example: Semantic Button Slice
```css
@layer components {
  .btn-primary {
    @apply inline-flex items-center justify-center gap-2 px-4 py-2 rounded-lg font-medium text-sm;
    @apply bg-indigo-600 text-white shadow-sm transition-all duration-150;
    @apply hover:bg-indigo-500 active:bg-indigo-700;
    @apply focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-900;
    @apply disabled:opacity-50 disabled:pointer-events-none;
  }
}
```

---

## 4. Tailwind Version Compatibility

### Tailwind CSS v3
In v3 projects, setup uses `@tailwind` directives in `styles.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@import "./main.css";
@import "./dashboard/main.css";
```

### Tailwind CSS v4
In v4 projects, Tailwind is CSS-first. Use standard CSS imports and `@theme`:
```css
@import "tailwindcss";

@import "./main.css";
@import "./dashboard/main.css";
```

---

## 5. Anti-Patterns & Common Pitfalls

| Anti-Pattern | Why It Fails | Correct Solution |
| :--- | :--- | :--- |
| **Inline class hoarding** | Obscures component structure, increases merge conflicts. | Extract to VSA slice stylesheet with `@apply`. |
| **Missing `@layer components`** | Causes utility overrides to fail due to CSS order. | Always wrap classes in `@layer components`. |
| **Deep selector nesting** | Creates high CSS specificity, breaks reusability. | Keep selectors shallow (flat `.component-part`). |
| **Trivial single-utility classes** | Creates unnecessary CSS abstractions (e.g. `.text-bold { @apply font-bold; }`). | Only abstract cohesive component tokens or combinations. |
| **Direct cross-slice imports** | Creates spaghetti dependencies between domains. | Promote shared elements to global `styles/`. |
