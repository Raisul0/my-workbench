# Material Design Guide for My Workbench

## Overview

This guide outlines the Material Design implementation for My Workbench using Angular Material. The design follows a modern aesthetic with a color theme of **Black, White, and Dark Navy Blue**.

## Color Palette

### Primary Colors
- **Dark Navy Blue (Primary):** `#004589` - Main brand color for buttons, links, and primary actions
- **Navy Dark:** `#00254d` - Darker shade for hover states and accents
- **Navy Light:** `#1a6bc9` - Lighter shade for secondary elements

### Neutral Colors
- **Black:** `#000000` - Text and dark elements
- **White:** `#ffffff` - Background and contrast
- **Dark Gray:** `#1a1a1a` - Secondary text
- **Light Gray:** `#f5f5f5` - Background panels and cards
- **Border Gray:** `#e0e0e0` - Dividers and borders

### Semantic Colors
- **Success:** `#4caf50` - Positive actions and status
- **Warning:** `#ff9800` - Cautionary elements
- **Error:** `#f44336` - Errors and destructive actions
- **Info:** `#2196f3` - Informational content

## CSS Variables

All colors and spacing are defined as CSS custom properties in `:root`. Access them throughout components:

```scss
// Colors
--primary-navy
--primary-navy-dark
--primary-navy-light
--color-black
--color-white
--color-dark-gray
--color-light-gray
--color-border-gray
--color-success
--color-warning
--color-error
--color-info

// Spacing
--spacing-xs: 4px
--spacing-sm: 8px
--spacing-md: 16px
--spacing-lg: 24px
--spacing-xl: 32px

// Border Radius
--border-radius-sm: 4px
--border-radius-md: 8px
--border-radius-lg: 12px

// Typography
--font-family-primary
--font-size-xs: 12px
--font-size-sm: 14px
--font-size-base: 16px
--font-size-lg: 18px
--font-size-xl: 20px
--font-size-2xl: 24px
--font-size-3xl: 32px

// Shadows
--shadow-sm
--shadow-md
--shadow-lg
--shadow-xl
```

## Component Patterns

### Layout Components

#### Toolbar (Top Navigation)
```html
<mat-toolbar color="primary" class="app-toolbar">
  <button mat-icon-button (click)="sidenav.toggle()">
    <mat-icon>menu</mat-icon>
  </button>
  <span class="toolbar-brand">My Workbench</span>
  <span class="toolbar-spacer"></span>
  <button mat-icon-button>
    <mat-icon>account_circle</mat-icon>
  </button>
</mat-toolbar>
```

#### Sidenav (Navigation)
```html
<mat-sidenav #sidenav mode="side" opened="true" fixedInViewport>
  <mat-nav-list>
    <mat-list-item
      *ngFor="let item of navItems"
      [routerLink]="item.path"
      routerLinkActive="active"
    >
      <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
      <span matListItemTitle>{{ item.label }}</span>
    </mat-list-item>
  </mat-nav-list>
</mat-sidenav>
```

### Card Components

Use `mat-card` for grouping content:

```html
<mat-card class="stat-card">
  <mat-card-header>
    <mat-icon class="card-icon">description</mat-icon>
  </mat-card-header>
  <mat-card-content>
    <p class="card-label">Label</p>
    <p class="card-value">Value</p>
  </mat-card-content>
</mat-card>
```

#### Card Styling
```scss
.stat-card {
  border-radius: var(--border-radius-md);
  box-shadow: var(--shadow-sm);
  transition: all 0.3s ease-in-out;
  border: 1px solid var(--color-border-gray);

  &:hover {
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
  }
}
```

### Button Components

#### Primary Button (Actions)
```html
<button mat-raised-button color="primary">
  <mat-icon>add</mat-icon>
  Create New
</button>
```

#### Secondary Button (Alternative)
```html
<button mat-stroked-button>
  <mat-icon>cancel</mat-icon>
  Cancel
</button>
```

#### Icon Button
```html
<button mat-icon-button>
  <mat-icon>more_vert</mat-icon>
</button>
```

### Data Tables

```html
<table mat-table [dataSource]="data" class="data-table">
  <!-- Column definitions -->
  <ng-container matColumnDef="name">
    <th mat-header-cell *matHeaderCellDef>Name</th>
    <td mat-cell *matCellDef="let element">{{element.name}}</td>
  </ng-container>

  <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
  <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
</table>
```

#### Table Styling
```scss
.data-table {
  background-color: var(--color-white);
  width: 100%;

  .mat-header-row {
    background-color: var(--color-light-gray);
    border-bottom: 2px solid var(--primary-navy);
  }

  .mat-row {
    border-bottom: 1px solid var(--color-border-gray);

    &:hover {
      background-color: var(--color-light-gray);
    }
  }
}
```

### Forms

#### Form Field
```html
<mat-form-field appearance="fill">
  <mat-label>Email Address</mat-label>
  <input matInput type="email" formControl="email">
</mat-form-field>
```

#### Form Layout
```scss
.form-group {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);

  mat-form-field {
    width: 100%;
  }
}
```

### Dialogs

```typescript
import { MatDialog } from '@angular/material/dialog';

constructor(public dialog: MatDialog) {}

openDialog(): void {
  this.dialog.open(DialogComponent, {
    width: '500px',
    disableClose: false,
  });
}
```

```html
<h2 mat-dialog-title>Dialog Title</h2>
<mat-dialog-content>
  <!-- Content here -->
</mat-dialog-content>
<mat-dialog-actions align="end">
  <button mat-button [mat-dialog-close]="false">Cancel</button>
  <button mat-raised-button color="primary" [mat-dialog-close]="true">
    Confirm
  </button>
</mat-dialog-actions>
```

### Snackbar Notifications

```typescript
import { MatSnackBar } from '@angular/material/snack-bar';

constructor(private snackBar: MatSnackBar) {}

showNotification(message: string, action = 'Close'): void {
  this.snackBar.open(message, action, {
    duration: 4000,
    panelClass: ['success-snackbar'],
  });
}
```

## Typography

### Heading Styles

```html
<!-- Large title -->
<h1 class="page-title">Page Title</h1>

<!-- Section heading -->
<h2 class="section-heading">Section Heading</h2>

<!-- Card title -->
<h3 class="card-title">Card Title</h3>
```

### Text Hierarchy

```html
<!-- Primary text -->
<p class="text-primary">Important text</p>

<!-- Secondary text -->
<p class="text-secondary">Secondary information</p>

<!-- Muted text -->
<p class="text-muted">Less important details</p>
```

## Spacing & Layout

### Flexbox Utilities

```html
<!-- Center content -->
<div class="flex-center">
  <button>Centered Button</button>
</div>

<!-- Space between -->
<div class="flex-between">
  <span>Left</span>
  <span>Right</span>
</div>

<!-- Column layout -->
<div class="flex-column gap-md">
  <div>Item 1</div>
  <div>Item 2</div>
</div>
```

### Grid Layout

```scss
.grid-layout {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--spacing-lg);
}
```

## Icons

Use Material Icons from the `mat-icon` library:

```html
<mat-icon>home</mat-icon>
<mat-icon>description</mat-icon>
<mat-icon>settings</mat-icon>
<mat-icon>delete</mat-icon>
<mat-icon>edit</mat-icon>
```

Common icons:
- Navigation: `menu`, `home`, `arrow_back`, `arrow_forward`
- Content: `add`, `edit`, `delete`, `save`, `close`
- Status: `check_circle`, `error`, `warning`, `info`
- Actions: `search`, `download`, `upload`, `print`

## Shadows & Elevation

Use predefined shadow variables for consistent depth:

```scss
// Light shadow
box-shadow: var(--shadow-sm);

// Medium shadow (default for cards)
box-shadow: var(--shadow-md);

// Dark shadow (for modals, elevated surfaces)
box-shadow: var(--shadow-lg);

// Extra dark shadow (for floating elements)
box-shadow: var(--shadow-xl);
```

## Responsive Design

Breakpoints:
- Mobile: < 600px
- Tablet: 600px - 768px
- Desktop: > 768px

```scss
@media (max-width: 768px) {
  .container {
    grid-template-columns: 1fr;
    padding: var(--spacing-md);
  }
}

@media (max-width: 600px) {
  .toolbar-brand .brand-text {
    display: none; // Hide text on mobile
  }
}
```

## Transitions & Animations

Use Angular Material's built-in animations:

```typescript
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  imports: [BrowserAnimationsModule],
})
export class AppModule { }
```

Common transitions:
```scss
// Smooth hover effect
transition: all 0.2s ease-in-out;

// Transform on hover
&:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}
```

## Component Checklist

When creating new components:

- [ ] Import Material modules needed (MatCardModule, MatButtonModule, etc.)
- [ ] Use `mat-card` for content containers
- [ ] Use `mat-button` or `mat-raised-button` for actions
- [ ] Apply primary color to main actions: `color="primary"`
- [ ] Use `mat-icon` for visual elements
- [ ] Follow spacing guidelines (use CSS variables)
- [ ] Add hover states with `box-shadow` and `transform`
- [ ] Use border-radius: `var(--border-radius-md)`
- [ ] Test on mobile (< 768px)
- [ ] Ensure proper color contrast (WCAG AA)
- [ ] Use semantic HTML and ARIA labels

## Customization

To customize the theme globally, edit `/src/theme.scss`:

```scss
// Change primary color palette
$primary-palette: (
  40: #004589, // Your color
  // ... other shades
);
```

## Resources

- [Angular Material Documentation](https://material.angular.io/)
- [Material Design Guidelines](https://m3.material.io/)
- [Material Icons](https://fonts.google.com/icons)
