---
name: tailwind-file-first-vsa
description: >-
  Prioritizes file-based Tailwind CSS styling using Vertical Slice Architecture (VSA) to reduce component bloat and maintain a scalable global design system. Use when structuring Tailwind styles, refactoring utility-bloated JSX/HTML components, or organizing stylesheets into feature domain slices.
version: 1.0.0
tags: ["tailwind", "css", "vsa", "architecture", "styling"]
---

# Tailwind File-First VSA Architect

Enforces a strict **"File-Based First"** architecture for Tailwind CSS development. Instead of polluting JSX/HTML components with repetitive inline utility strings, styles are composed into dedicated, domain-sliced `.css` files using Tailwind's `@apply` directive.

## 🎯 When to Activate This Skill

- When creating or styling UI components with Tailwind CSS.
- When refactoring JSX/HTML files burdened with inline utility class bloat.
- When structuring or auditing project stylesheets using Vertical Slice Architecture (VSA).
- When establishing centralized design tokens, global themes, or domain-specific style aggregators.

## 🧠 Core Objectives

1. **Unified Global Design:** Consolidate design tokens and utility combinations into dedicated stylesheets for consistency.
2. **Maintainable Design:** Decouple CSS style rules from React/Vue/Svelte component logic.
3. **Flexible Theming:** Centralized styling enables rapid token overrides, dark-mode variations, and theme swapping.
4. **Zero Component Bloat:** Keep JSX/HTML lean, semantic, and readable by abstracting utility chains into meaningful class names.

## 📐 Vertical Slice Architecture (VSA) for Styles

Styles mirror domain boundaries using screaming architecture:

```text
styles/
├── dashboard/                 # Feature Slice Context
│   ├── main.css               # Feature Aggregator (imports slice styles)
│   ├── header.css             # Domain-specific header styles
│   └── sidebar.css            # Domain-specific sidebar styles
├── button.css                 # Global shared element
├── card.css                   # Global shared element
├── input.css                  # Global shared element
├── main.css                   # Shared elements aggregator
└── styles.css                 # Root stylesheet (Tailwind directives & slice imports)
```

- **Global Shared Elements:** Primitive, universally reused components (`button.css`, `card.css`) live directly under `styles/`.
- **Feature Slices:** Co-located context styles belong in a dedicated domain folder (e.g., `styles/dashboard/`, `styles/auth/`).
- **Aggregators (`main.css`):** Every slice folder contains a `main.css` that imports its child stylesheets.

## 🚀 Execution Workflow for Agents

### Step 1: Categorize Component Scope
Determine if the element is a **Global Shared Element** (used across features) or belongs to a **Feature Slice** (specific page or domain context).

### Step 2: Scaffold the Stylesheet Slice
Create the target `.css` file in `styles/` or the corresponding `styles/<slice>/` directory. You can automate this with the helper script:
```bash
./scripts/run.sh <slice-name> <element-name>
# Example: ./scripts/run.sh dashboard sidebar
```

### Step 3: Compose Utilities with `@apply`
Write semantic CSS classes wrapping Tailwind utilities within `@layer components`:
```css
@layer components {
  .dashboard-sidebar {
    @apply flex flex-col w-64 min-h-screen bg-slate-900 border-r border-slate-800 p-4;
  }
  .dashboard-sidebar-item {
    @apply flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-slate-300 hover:text-white hover:bg-slate-800 transition-colors;
  }
}
```

### Step 4: Wire Aggregator & Root Imports
Ensure the `.css` file is imported into its slice `main.css`, and that the slice aggregator is imported into the root `styles.css`.

### Step 5: Apply Semantic Classes to Components
Replace inline utility strings in JSX/HTML with the semantic class names.

## 📁 References & Examples

- [Concrete Refactoring Walkthrough](./examples/basic-example.md) - Before-and-after component refactor and file layout.
- [Technical Architecture & @apply Guide](./references/technical-details.md) - Layer precedence, variant handling, and Tailwind v3/v4 compatibility.
- [Scaffold Helper Script](./scripts/run.sh) - Bash automation to generate VSA slices and wire aggregator imports.
