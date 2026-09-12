# Example: Refactoring with Tailwind File-First VSA

This walkthrough demonstrates refactoring a bloated, utility-heavy component into a clean, maintainable Vertical Slice Architecture (VSA) stylesheet structure.

---

## 🛑 Before: Utility Class Bloat in JSX

In standard Tailwind codebases, UI components frequently accumulate dozens of utility classes, obscuring component logic and making visual maintenance painful:

```tsx
// src/components/dashboard/MetricsCard.tsx (Bloated)
export function MetricsCard({ title, value, change, isPositive, isLoading }: MetricsCardProps) {
  if (isLoading) {
    return (
      <div className="flex flex-col p-6 bg-slate-900/60 border border-slate-800/80 rounded-2xl shadow-xl shadow-slate-950/40 backdrop-blur-sm animate-pulse">
        <div className="h-4 w-24 bg-slate-800 rounded-md mb-4" />
        <div className="h-8 w-36 bg-slate-800 rounded-lg mb-2" />
        <div className="h-4 w-16 bg-slate-800 rounded-md" />
      </div>
    );
  }

  return (
    <div className="group relative flex flex-col p-6 bg-slate-900/60 border border-slate-800/80 hover:border-indigo-500/50 rounded-2xl shadow-xl shadow-slate-950/40 hover:shadow-indigo-500/10 backdrop-blur-sm transition-all duration-300 ease-out cursor-pointer">
      <div className="flex items-center justify-between mb-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-slate-400 group-hover:text-slate-200 transition-colors">
          {title}
        </span>
        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
          isPositive ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20" : "bg-rose-500/10 text-rose-400 border border-rose-500/20"
        }`}>
          {change}
        </span>
      </div>
      <div className="text-3xl font-bold tracking-tight text-white mb-1">
        {value}
      </div>
    </div>
  );
}
```

---

## 🏗️ Step-by-Step VSA Slicing

### 1. Directory Structure
Identify that `MetricsCard` belongs to the `dashboard` feature slice:

```text
src/
└── styles/
    ├── dashboard/
    │   ├── main.css              # Dashboard aggregator
    │   └── metrics-card.css      # Component stylesheet slice
    ├── button.css                # Global element
    ├── card.css                  # Global element
    ├── main.css                  # Global elements aggregator
    └── styles.css                # Root stylesheet
```

### 2. Stylesheet Implementation (`styles/dashboard/metrics-card.css`)

```css
@layer components {
  /* Base Card Container */
  .metrics-card {
    @apply relative flex flex-col p-6 rounded-2xl transition-all duration-300 ease-out cursor-pointer;
    @apply bg-slate-900/60 border border-slate-800/80 shadow-xl shadow-slate-950/40 backdrop-blur-sm;
    @apply hover:border-indigo-500/50 hover:shadow-indigo-500/10;
  }

  /* Skeleton / Loading State */
  .metrics-card-skeleton {
    @apply flex flex-col p-6 rounded-2xl animate-pulse;
    @apply bg-slate-900/60 border border-slate-800/80 shadow-xl shadow-slate-950/40 backdrop-blur-sm;
  }

  .metrics-card-skeleton-bar {
    @apply bg-slate-800 rounded-md;
  }

  /* Card Header & Labels */
  .metrics-card-header {
    @apply flex items-center justify-between mb-3;
  }

  .metrics-card-label {
    @apply text-xs font-semibold uppercase tracking-wider text-slate-400 transition-colors;
  }

  .metrics-card:hover .metrics-card-label {
    @apply text-slate-200;
  }

  /* Value Typography */
  .metrics-card-value {
    @apply text-3xl font-bold tracking-tight text-white mb-1;
  }

  /* Trend Badges */
  .metrics-card-badge {
    @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border;
  }

  .metrics-card-badge-positive {
    @apply bg-emerald-500/10 text-emerald-400 border-emerald-500/20;
  }

  .metrics-card-badge-negative {
    @apply bg-rose-500/10 text-rose-400 border-rose-500/20;
  }
}
```

### 3. Aggregators & Root Setup

**`src/styles/dashboard/main.css`**:
```css
@import "./metrics-card.css";
```

**`src/styles/styles.css`**:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Global shared styles */
@import "./main.css";

/* Feature slices */
@import "./dashboard/main.css";
```

---

## ✨ After: Clean, Semantic Component

With styles encapsulated in the VSA slice, the JSX is readable, maintainable, and declarative:

```tsx
// src/components/dashboard/MetricsCard.tsx (Refactored)
export function MetricsCard({ title, value, change, isPositive, isLoading }: MetricsCardProps) {
  if (isLoading) {
    return (
      <div className="metrics-card-skeleton" aria-busy="true" aria-label="Loading metrics">
        <div className="metrics-card-skeleton-bar h-4 w-24 mb-4" />
        <div className="metrics-card-skeleton-bar h-8 w-36 mb-2" />
        <div className="metrics-card-skeleton-bar h-4 w-16" />
      </div>
    );
  }

  const badgeClass = isPositive
    ? "metrics-card-badge metrics-card-badge-positive"
    : "metrics-card-badge metrics-card-badge-negative";

  return (
    <div className="metrics-card" role="region" aria-label={title}>
      <div className="metrics-card-header">
        <span className="metrics-card-label">{title}</span>
        <span className={badgeClass}>{change}</span>
      </div>
      <div className="metrics-card-value">{value}</div>
    </div>
  );
}
```

---

## 🎯 Verification Checklist

- [x] JSX is free of long, unreadable utility chains.
- [x] All styles are wrapped inside `@layer components` to preserve utility override priority.
- [x] Stylesheet is co-located in its domain slice (`styles/dashboard/metrics-card.css`).
- [x] Aggregator `styles/dashboard/main.css` imports the slice file.
- [x] Loading, hover, and conditional states are cleanly represented.
