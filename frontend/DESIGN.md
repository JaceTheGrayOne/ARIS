# ARIS Frontend Design System

**Style Direction**: Technical Precision
**Aesthetic**: Industrial aerospace workstation meets cyberpunk command center

---

## Design Principles

1. **Modular Identity** - Each tool has a distinct color signature for instant recognition
2. **Information Density** - Compact, efficient layouts that maximize workspace utility
3. **Surgical Clarity** - Clear visual hierarchy through weight, spacing, and targeted accent usage
4. **Substrate Depth** - Layered surfaces with subtle glass-panel effects and technical grid texture
5. **Restrained Luminance** - Dark foundation with surgical accent placement

---

## Color Palette

### Base Substrate
```css
--bg-void:      #0a0a0b      /* Deep background, near-black */
--bg-base:      #141418      /* Primary canvas with grid texture */
--bg-panel:     #1a1a22      /* Elevated glass panel surface */
--bg-panel-2:   #21212b      /* Secondary elevated surface */
--bg-inset:     #0f0f13      /* Recessed areas (console, inputs) */
```

### Structural Elements
```css
--border-subtle:  #2a2a35    /* Faint dividers */
--border-default: #35354a    /* Standard borders */
--border-strong:  #45455f    /* Emphasized borders */
```

### Typography
```css
--text-primary:   #e8e8f0    /* Primary text */
--text-secondary: #a8a8b8    /* Secondary text */
--text-muted:     #6a6a7a    /* Tertiary/disabled text */
```

### Module Accent Signatures

Each tool module has a distinct accent color that propagates through:
- Module icon
- Page title underline (2px border)
- Primary action buttons
- Input focus rings
- Active navigation indicator
- Card outline on hover

| Module | Accent Color | Hex | Usage |
|--------|--------------|-----|-------|
| **Retoc** | Amber/Gold | `#f59e0b` | Warm, industrial - for IoStore/Pak conversion |
| **UAsset** | Cyan | `#06b6d4` | Technical, data-focused - for asset inspection |
| **UWP Dumper** | Emerald | `#10b981` | System-level, secure - for package dumping |
| **DLL Injector** | Magenta | `#ec4899` | Sharp, invasive - for process injection |

### System Feedback
```css
--status-success:  #10b981   /* Emerald - operation succeeded */
--status-error:    #ef4444   /* Red - operation failed */
--status-warning:  #f59e0b   /* Amber - warnings/cautions */
--status-info:     #3b82f6   /* Blue - informational */
```

---

## Typography

### Font Families

```css
--font-display: 'Rajdhani', sans-serif;           /* Headers, titles */
--font-body: 'Work Sans', system-ui, sans-serif;  /* UI text, labels */
--font-mono: 'JetBrains Mono', monospace;         /* Code, paths, IDs */
```

**Rajdhani** (600/700 weights)
- Geometric, technical, distinctive
- Used for: Page titles, section headers, app branding

**Work Sans** (400/500/600 weights)
- Clean, readable, professional
- Used for: Body text, form labels, buttons, descriptions

**JetBrains Mono** (400/500/600 weights)
- Monospace for technical content
- Used for: File paths, operation IDs, console output, code blocks

### Type Scale (Desktop-Optimized)

```css
--text-xs:    0.6875rem  (11px)  /* Meta info, timestamps */
--text-sm:    0.8125rem  (13px)  /* Secondary labels, help text */
--text-base:  0.875rem   (14px)  /* Body text, form fields */
--text-lg:    1rem       (16px)  /* Emphasized body */
--text-xl:    1.25rem    (20px)  /* Section headers */
--text-2xl:   1.75rem    (28px)  /* Page titles */
--text-3xl:   2.25rem    (36px)  /* Dashboard headings */
```

---

## Spacing System

```css
--space-1:  0.25rem   (4px)   /* Tight spacing */
--space-2:  0.5rem    (8px)   /* Small gaps */
--space-3:  0.75rem   (12px)  /* Medium-small */
--space-4:  1rem      (16px)  /* Standard spacing */
--space-6:  1.5rem    (24px)  /* Section gaps */
--space-8:  2rem      (32px)  /* Large gaps */
--space-12: 3rem      (48px)  /* XL spacing */
```

---

## Component Inventory

### Layout Components

**AppShell**
- Full viewport frame with fixed sidebar + top header
- Includes grid texture background and subtle vignette overlay
- Usage: Root layout wrapper in `MainLayout.tsx`

**Sidebar** (240px fixed width)
- Left navigation with grouped module sections
- Module icons with accent colors
- Active state: accent-colored left border + highlighted background

**Header**
- Top bar showing workspace info and backend status
- Status indicator with colored dot (green/amber/red/gray)
- Version badge on right

### Surface Components

**Panel**
- Primary elevated surface with glass effect
- Border: `var(--border-default)`
- Background: `var(--bg-panel)` with backdrop blur
- Shadow: Subtle inset glow + drop shadow

**PanelHeader**
- Title + optional subtitle + optional actions
- When `accent={true}`: 2px bottom border using module accent color
- Typography: `font-display` for title

**PanelBody**
- Content area with configurable padding (none/sm/md/lg)
- Default: `p-6` (md padding)

**Card**
- Smaller contained surface for grouping content
- Optional `hover` prop for interactive cards
- Optional `accent` prop for accent-colored border

**ModuleCard** (Dashboard)
- Icon + title + description + optional actions
- Icon container: 2px border with accent color + light background tint
- Title uses accent color on hover
- Used on Dashboard for tool module cards

### Form Components

**Field**
- Wrapper for form inputs with label + help text + error display
- Required indicator: red asterisk
- Error messages: red with warning icon
- Help text: monospace, muted color

**Input**
- Text input with dark inset background
- Monospace font for file paths and technical input
- Focus: 2px ring using module accent color
- Error state: red border + red focus ring

**Select**
- Dropdown with custom chevron icon
- Same styling as Input
- Dropdown icon: right-aligned, non-interactive

**Textarea**
- Multi-line input with minimum height 80px
- Resizable vertically
- Same styling as Input

**Button**
- Variants: `primary`, `secondary`, `ghost`, `accent`, `danger`
- Sizes: `sm`, `md`, `lg`
- `accent` variant: transparent bg with accent border, fills on hover
- `primary` variant: uses module accent as background
- Disabled: 50% opacity, no pointer events

### Data Display

**Badge**
- Small inline label with variants matching system feedback colors
- Variants: `default`, `success`, `error`, `warning`, `info`
- Usage: Tags, labels, metadata

**StatusPill**
- Larger status indicator for operation results
- Statuses: `success`, `error`, `warning`, `pending`
- Usage: Operation history, result summaries

**ConsoleBlock**
- Monospace log output with dark inset background
- Optional line numbers with border separator
- Max height with scroll overflow
- Usage: Tool output, error logs

**CodeBlock**
- Syntax display with optional language label
- Language badge in header
- Inset surface styling

### Feedback Components

**Alert**
- Inline message with icon + title + description
- Variants: `info`, `success`, `warning`, `error`
- Icon automatically selected based on variant
- Semi-transparent colored background with matching border

---

## Module Accent Application

Each tool page includes a module-specific CSS class on the root container:

```tsx
<div className="space-y-6 module-retoc">   {/* Retoc page */}
<div className="space-y-6 module-uasset">  {/* UAsset page */}
<div className="space-y-6 module-uwp">     {/* UWP Dumper page */}
<div className="space-y-6 module-injector"> {/* DLL Injector page */}
```

This sets CSS custom properties that components can reference:

```css
.module-retoc {
  --module-accent: #f59e0b;  /* Amber */
  --module-glow: rgba(245, 158, 11, 0.4);
}
```

Components use `var(--module-accent)` for:
- Button backgrounds (variant="accent")
- Input focus rings
- Panel header accent borders
- Page title underlines
- Icon colors
- Active state highlights

---

## Layout Strategy

### Grid Foundation

**Desktop-First**
- Sidebar: Fixed 240px width
- Main content: Flex 1 with max-width constraint (7xl = 1280px)
- No mobile breakpoints (Windows desktop-only app)

**Two-Column Tool Layout**
- Left: Settings panel (forms, configuration)
- Right: Results panel + history
- Gap: 24px
- Grid: `grid-cols-1 lg:grid-cols-2`
- Breakpoint: 1024px (stacks below, acceptable for desktop)

### Page Structure

All tool pages follow this pattern:

```tsx
<div className="space-y-6 module-{name}">
  {/* Page Header with Icon + Title + Accent Border */}
  <div className="border-b-2 border-[var(--module-accent)] pb-4">
    <Icon className="text-[var(--module-accent)]" />
    <h1 className="font-display">Title</h1>
    <p className="text-secondary">Description</p>
  </div>

  {/* Two-Column Layout */}
  <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
    {/* Left: Settings */}
    <Panel>
      <PanelHeader title="Settings" accent />
      <PanelBody>...</PanelBody>
    </Panel>

    {/* Right: Results + History */}
    <div className="space-y-4">
      <Alert>...</Alert>
      <ResultPanel>...</ResultPanel>
      <Panel>History</Panel>
    </div>
  </div>
</div>
```

---

## Visual Effects

### Background Texture
```css
.app-background {
  background-image:
    linear-gradient(rgba(255,255,255,0.01) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255,255,255,0.01) 1px, transparent 1px);
  background-size: 24px 24px;
}
```

### Vignette Overlay
```css
.app-vignette::before {
  content: '';
  position: fixed;
  background: radial-gradient(
    ellipse at center,
    transparent 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
  pointer-events: none;
}
```

### Glass Panel Effect
```css
.glass-panel {
  background: var(--bg-panel);
  border: 1px solid var(--border-default);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.03);
  backdrop-filter: blur(8px);
}
```

### Focus States
- 2px solid outline using module accent color
- Optional soft glow shadow for emphasis
- Transition: 150ms ease

### Hover Transitions
- All interactive elements: `transition-all duration-200`
- Button hover: opacity 90% (primary) or background change (secondary/ghost)
- Card hover: accent border + elevated shadow

---

## Icon System

**Library**: lucide-react

**Navigation Icons**:
- Dashboard: `LayoutDashboard`
- Retoc: `Package`
- UAsset: `FileCode`
- UWP Dumper: `Shield`
- DLL Injector: `Syringe`
- Health: `Activity`
- Settings: `Settings`
- Logs: `FileText`

**Utility Icons**:
- Loading: `Loader2` (with animate-spin)
- Folder: `FolderOpen`
- Status: `CheckCircle`, `AlertCircle`, `AlertTriangle`, `Info`

**Icon Sizing**:
- Sidebar nav: 18px
- Page headers: 32px
- Module cards: 24px
- Buttons: 18px

---

## Scrollbar Styling

```css
::-webkit-scrollbar {
  width: 12px;
  background: var(--bg-inset);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb {
  background: var(--border-strong);
  border-radius: 3px;
  border: 2px solid var(--bg-inset);
}

::-webkit-scrollbar-thumb:hover {
  background: #55557a;
}
```

---

## Usage Examples

### Module Page Header

```tsx
<div className="border-b-2 border-[var(--module-accent)] pb-4">
  <div className="flex items-center gap-3 mb-2">
    <Package size={32} className="text-[var(--module-accent)]" strokeWidth={2} />
    <h1 className="font-display text-3xl font-bold text-[var(--text-primary)]">
      IoStore / Retoc
    </h1>
  </div>
  <p className="text-[var(--text-secondary)] ml-11">
    Pack and unpack Unreal Engine IoStore containers
  </p>
</div>
```

### Form Field with Input

```tsx
<Field
  label="Modified UAsset Directory"
  required
  error={errors.directory}
  help="Path to your modified UAsset files"
>
  <Input
    type="text"
    value={directory}
    onChange={(e) => setDirectory(e.target.value)}
    placeholder="G:\Grounded\Modding\ModFolder"
    error={!!errors.directory}
  />
</Field>
```

### Module-Accented Button

```tsx
<Button variant="accent" size="lg" fullWidth>
  Build Mod
</Button>
```

### Panel with Accent Header

```tsx
<Panel>
  <PanelHeader
    title="Operation Settings"
    subtitle="Configure your mod build"
    accent
  />
  <PanelBody>
    {/* Form content */}
  </PanelBody>
</Panel>
```

---

## Maintenance Notes

- **Font Loading**: Google Fonts via CSS `@import` in `tokens.css`
- **Custom Properties**: Defined in `frontend/src/styles/tokens.css`
- **Tailwind Config**: Extended in `frontend/tailwind.config.js` with design tokens
- **Module Classes**: Applied at page root, cascades accent variables to children
- **Component Library**: All UI primitives in `frontend/src/components/ui/`

---

**Last Updated**: 2025-12-23
**Design System Version**: 1.0
**Target Platform**: Windows Desktop (WebView2)
