---
name: vertical-slice-architect
description: Enforces strict vertical slice architecture, highly granular file-based Single Responsibility Principle (SRP), and domain-driven folder structures before writing any code.
---

# Modular Feature Architecture

Guides the structural design of applications by enforcing Screaming Architecture and strict file-level Single Responsibility. 

## 🧠 Core Philosophy

Your folder structure should instantly communicate **what the application does**, not what framework it uses. Avoid monolithic files; always default to a highly granular, file-based separation of concerns.

## 🗺️ 1. Mandatory Pre-Planning (Tree First)

**Never start writing implementation code immediately.** You must map out the structure first.

- Always generate and present a clear, hierarchical folder tree representing the intended modules and components.
- Finalize the structure before fleshing out the actual code.

## 🍰 2. Vertical Slicing (Feature-Driven)

Group files strictly by **business domain or feature**, rather than by technical responsibility.

- **Do this:** `modules/user/`, `modules/payments/`, `components/admin/`
- **Avoid this:** `controllers/`, `services/`, `views/`

## 🧩 3. Hyper-Granular Single Responsibility

Do not compact related logic into a single file. Dedicate **one file to one specific action or component**.

- **For APIs/Routes:** Instead of a massive `user.controller.ts`, break it down into `user.create.ts`, `user.getAll.ts`, `user.update.ts`.
- **For UIs/Components:** Instead of a massive `AdminPanel.tsx`, break it down into `AdminHome.tsx`, `AdminSettingsForm.tsx`, and shared bits in `common/ModalView.tsx`.

## 🏷️ 4. Explicit Naming Conventions

Use consistent, predictable naming formats (like dot-notation for backend, PascalCase for frontend components) to clearly describe the domain and the exact action.

**Backend Example:**
```text
src/
|_ modules/
   |_ user/
      |_ user.service.ts
      |_ user.getAll.ts
      |_ user.create.ts
      |_ user.schema.ts

```

**Frontend Example:**

```text
src/
|_ components/
   |_ admin/
      |_ AdminHome.tsx
      |_ AdminSidebar.tsx
   |_ common/
      |_ ModalView.tsx
      |_ ButtonAction.tsx

```
