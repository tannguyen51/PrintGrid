# PrintGrid Login Page Redesign — Implementation Plan

## Goal

Replace the current plain centered-card login page with a polished, split-panel layout matching the visual proportions and feel of the provided reference image:

- **Left panel (≈40%)**: dark background, 3D interactive printer showcase built with React Three Fiber, brand wordmark "PrintGrid", tagline
- **Right panel (≈60%)**: light card, centered login form, Inter font, existing blue/orange brand accents

---

## Reference Analysis (login-reference.png)

| Aspect | Reference | PrintGrid Translation |
|---|---|---|
| Layout | Two-column split, ~40/60 | Flex row, left dark + right light |
| Left panel bg | Dark gradient / near-black | `#0b1120` → `#162032` radial gradient |
| Right panel bg | Off-white / light gray | Existing `#f6f7f9` background |
| Brand area | Top-left logo + tagline | "PrintGrid" wordmark (Inter, white) + tagline |
| Login card | White rounded card, generous padding, elevated shadow | MUI `Card` with `borderRadius: 16`, shadow elevation 6 |
| Inputs | Two fields, filled variant, rounded | MUI `TextField variant="filled"`, `borderRadius: 12` |
| Button | Full-width solid orange, rounded-lg | MUI `Button variant="contained"`, `secondary` color `#f97316` |
| Typography | Sans-serif (Inter), weight 600 for headings | Inter via `@fontsource`, MUI typography tokens |
| 3D showcase | Interactive 3D printer model with soft lighting | R3F `<Canvas>` + stylized printer geometry + `<Environment>` |
| Responsive | Stacks to single column on narrow screens | `@media (max-width: 960px)` → column stack |

---

## Existing Codebase Context

**Stack already in place:**
- React 19 + TypeScript
- Vite 8 (dev server + build)
- MUI 7 (components, icons, theming)
- Emotion (styled)
- React Three Fiber + drei + three (installed, not yet used)
- React Hook Form + Zod (for future form validation)
- React Router v7 (SPA routing)
- TanStack React Query
- Axios

**Key files to modify or create:**

| File | Action |
|---|---|
| `frontend/src/shared/theme/theme.ts` | Extend with new login-specific tokens |
| `frontend/src/features/auth/LoginPage.tsx` | Complete rewrite (split panel + form) |
| `frontend/src/features/auth/components/LoginForm.tsx` | **New** — extracted form component |
| `frontend/src/features/auth/components/LoginShowcase.tsx` | **New** — 3D scene + brand area |
| `frontend/src/features/auth/components/PrinterModel.tsx` | **New** — stylized 3D printer mesh |
| `frontend/src/features/auth/components/LoginParticles.tsx` | **New** — floating particle/ring effects |
| `frontend/src/index.css` | Add Inter font import and base reset tweaks |
| `frontend/index.html` | Add Google Fonts `<link>` for Inter |

No changes needed to: `App.tsx`, `AppRoutes.tsx`, `AuthContext.tsx`, `ProtectedRoute.tsx`.

---

## Step-by-Step Implementation

### Step 1 — Extend design tokens (`theme.ts`)

Add:
- `palette.common.dark = '#0b1120'` for the showcase panel
- `palette.primary.light = '#3b82f6'` (lighter blue for input focus rings on dark)
- A custom `custom` key to the theme:
  ```ts
  custom: {
    showcaseBg: 'linear-gradient(135deg, #0b1120 0%, #162032 50%, #1a2a44 100%)',
    loginCardShadow: '0 8px 32px rgba(0,0,0,0.12)',
  }
  ```
- `borderRadius: 16` for the global shape

Use MUI module augmentation to type `custom`:
```ts
declare module '@mui/material/styles' {
  interface ThemeOptions { custom?: { showcaseBg?: string; loginCardShadow?: string } }
  interface Theme { custom?: { showcaseBg: string; loginCardShadow: string } }
}
```

### Step 2 — Add Inter font

In `frontend/index.html`, inside `<head>`, add:
```html
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
```

No CSS `@import` needed — the `<link>` in `index.html` is faster.

### Step 3 — Rewrite `LoginPage.tsx`

New structure:
```
<Box display="flex" height="100vh">
  <LoginShowcase />           ← left 40%, dark, 3D + brand
  <LoginForm />               ← right 60%, light, form card
</Box>
```

On narrow screens (`< 960px`): stack vertically, show a compact version of the showcase with no 3D canvas (or reduce canvas size).

### Step 4 — `LoginForm.tsx`

- MUI `Card` with `borderRadius: 16`, `boxShadow: theme.custom.loginCardShadow`, `width: '100%'`, `maxWidth: 440`
- Headed with small "PrintGrid" wordmark (logo mark + text)
- "Đăng nhập" heading
- Subtext: "Đăng nhập vào tài khoản của bạn"
- Two `TextField variant="filled"`:
  - "Email" with `<EmailRounded />` inputAdornment
  - "Mật khẩu" with `<LockRounded />` inputAdornment + show/hide toggle (`<IconButton>`)
- "Ghi nhớ đăng nhập" `<Checkbox>` + `<FormControlLabel>`
- Full-width "Đăng nhập" `<Button>`, solid, `secondary` color, `borderRadius: 12`
- `<Divider>` + social login buttons row (Google / Facebook icons as placeholder cards — **disabled, not wired**)
- Footer: "Bạn chưa có tài khoản? Liên hệ quản trị viên"
- Uses existing `useAuth().login()` and `useNavigate()` — no logic changes

Form validation via `react-hook-form` + Zod:
```ts
const schema = z.object({
  email: z.string().min(1, 'Email là bắt buộc').email('Email không hợp lệ'),
  password: z.string().min(1, 'Mật khẩu là bắt buộc'),
})
```

`useForm({ resolver: zodResolver(schema) })` — keeps typed errors, shows on blur/submit.

### Step 5 — `LoginShowcase.tsx`

- Full-height flex container, dark bg
- `position: relative` with `overflow: hidden`
- **3D Canvas** (bottom-center, overlapping into the card area slightly):
  ```tsx
  <Canvas camera={{ position: [0, 1, 5], fov: 45 }}>
    <ambientLight intensity={0.5} />
    <pointLight position={[5, 5, 5]} intensity={1} />
    <directionalLight position={[-5, 3, -2]} intensity={0.4} />
    <PrinterModel />
    <Float speed={1.5} rotationIntensity={0.2} floatIntensity={0.3}>
      {/* decorative torus / rings around the printer */}
    </Float>
    <OrbitControls enableZoom={false} enablePan={false} autoRotate autoRotateSpeed={0.8} />
  </Canvas>
  ```
- Brand area (top-left):
  - "PrintGrid" in white, `h4`, weight 700, `letterSpacing: 2px`
  - Tagline: "Hệ thống quản lý in 3D thông minh" in `rgba(255,255,255,0.6)`
- Bottom-left stats: three small stat boxes:
  - "Đơn hàng" / "Đang sản xuất" / "Hoàn thành" with animated counters (CSS animation only, no data source needed — these are decorative)

### Step 6 — `PrinterModel.tsx`

Stylized geometric printer using three.js primitives (no external `.glb` model needed):
- **Base plate**: `<RoundedBox>` flat slab, dark gray (`#1e293b`)
- **Frame**: four `<RoundedBox>` vertical pillars + top crossbar, metallic (`#334155`)
- **Print head**: small `<RoundedBox>` that oscillates on Y via `useFrame` (animated)
- **Print bed**: flat plane, slight emissive orange (`#f97316`) glow
- **Filament spool**: `<Cylinder>` on top, colored accent (`#3b82f6`)
- Materials: `MeshStandardMaterial` with `metalness: 0.6`, `roughness: 0.3`
- Optional: thin `<Line>` trails in brand blue/orange for visual interest

All geometry uses drei primitives (`RoundedBox`, `Cylinder`, etc.) — no external assets.

### Step 7 — `LoginParticles.tsx` (decorative)

Floating decorative elements around the 3D scene:
- drei `<Float>` components with ring/torus geometries, wireframe, low opacity
- Subtle animated dots using drei `<Instances>` or CSS `<div>` particles
- Keep performance light: cap at ~15–20 decorative elements

### Step 8 — Responsive adjustments

```css
@media (max-width: 960px) {
  /* LoginPage: flex-direction: column */
  /* Left panel: height: 220px (compact) or hide 3D */
  /* Right panel: flex: 1 */
  /* Card: mx: 2, width: 100% */
}

@media (max-width: 600px) {
  /* Login card: borderRadius: 12, px: 3 */
  /* Hide decorative stat boxes */
  /* 3D canvas: height: 160px */
}
```

MUI `useMediaQuery('(min-width:960px)')` to conditionally render the 3D canvas on mobile (show a static gradient instead).

---

## Files Summary

| # | File | Action | Lines (est.) |
|---|---|---|---|
| 1 | `frontend/index.html` | Edit — add Inter font `<link>` | +4 |
| 2 | `frontend/src/shared/theme/theme.ts` | Edit — extend tokens + types | +20 |
| 3 | `frontend/src/features/auth/LoginPage.tsx` | Rewrite — split panel layout | ~30 |
| 4 | `frontend/src/features/auth/components/LoginForm.tsx` | **Create** — login form | ~120 |
| 5 | `frontend/src/features/auth/components/LoginShowcase.tsx` | **Create** — 3D scene + brand | ~80 |
| 6 | `frontend/src/features/auth/components/PrinterModel.tsx` | **Create** — 3D printer geometry | ~100 |
| 7 | `frontend/src/features/auth/components/LoginParticles.tsx` | **Create** — decorative elements | ~50 |
| 8 | `frontend/src/index.css` | Edit — add `@font-face` fallback + reset | +5 |

**No changes to:** `App.tsx`, `AppRoutes.tsx`, `AuthContext.tsx`, `ProtectedRoute.tsx`, `apiClient.ts`, any other feature pages.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| R3F canvas is heavy on low-end devices | Add `useMediaQuery` to skip canvas on small screens; static gradient fallback |
| `react-hook-form` + Zod adds new dep surface | Both are already in `package.json` and used elsewhere — no new deps |
| Theme `custom` type augmentation may confuse team | Document it with a comment block in `theme.ts`; it's a single optional field |
| Google Fonts `<link>` may violate CSP if added later | Keep it as `<link>` (no inline styles); document CSP nonce requirement |
| Inter font missing on first paint | `font-display: swap` is default in Google Fonts; no CLS for form |

---

## Ready to implement

Once you approve this plan, I will implement steps 1–8 in order, run `npm run lint` and `npm run build` to validate, then report results.
