# Material Design Quick Start - Invoices Module

## Getting Started with Material Components

This guide shows how to apply Material Design to the Invoices module.

---

## Common Material Components

### 1. Form Fields (Input/Textarea)

```typescript
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  imports: [MatFormFieldModule, MatInputModule, ReactiveFormsModule],
  template: `
    <mat-form-field appearance="fill">
      <mat-label>Invoice Number</mat-label>
      <input matInput formControl="invoiceNumber" type="text">
    </mat-form-field>
    
    <mat-form-field appearance="fill">
      <mat-label>Customer Name</mat-label>
      <input matInput formControl="customerName" required>
    </mat-form-field>
    
    <mat-form-field appearance="fill">
      <mat-label>Description</mat-label>
      <textarea matInput rows="4" formControl="description"></textarea>
    </mat-form-field>
  `
})
export class InvoiceFormComponent {}
```

### 2. Data Tables

```typescript
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';

@Component({
  imports: [MatTableModule, MatSortModule, MatPaginatorModule],
  template: `
    <table mat-table [dataSource]="invoices" class="invoices-table">
      <!-- Invoice Number Column -->
      <ng-container matColumnDef="invoiceNumber">
        <th mat-header-cell *matHeaderCellDef>Invoice #</th>
        <td mat-cell *matCellDef="let invoice">{{ invoice.number }}</td>
      </ng-container>

      <!-- Customer Column -->
      <ng-container matColumnDef="customer">
        <th mat-header-cell *matHeaderCellDef>Customer</th>
        <td mat-cell *matCellDef="let invoice">{{ invoice.customerName }}</td>
      </ng-container>

      <!-- Amount Column -->
      <ng-container matColumnDef="amount">
        <th mat-header-cell *matHeaderCellDef>Amount</th>
        <td mat-cell *matCellDef="let invoice">
          {{ invoice.amount | currency }}
        </td>
      </ng-container>

      <!-- Status Column -->
      <ng-container matColumnDef="status">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let invoice">
          <span [class]="'status-' + invoice.status.toLowerCase()">
            {{ invoice.status }}
          </span>
        </td>
      </ng-container>

      <!-- Actions Column -->
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let invoice">
          <button mat-icon-button (click)="editInvoice(invoice)">
            <mat-icon>edit</mat-icon>
          </button>
          <button mat-icon-button (click)="deleteInvoice(invoice)">
            <mat-icon>delete</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>

    <mat-paginator [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons>
    </mat-paginator>
  `
})
export class InvoicesListComponent {
  displayedColumns: string[] = ['invoiceNumber', 'customer', 'amount', 'status', 'actions'];
}
```

### 3. Dialogs (Modals)

```typescript
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

// Dialog component
@Component({
  selector: 'app-invoice-dialog',
  imports: [MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Create Invoice</h2>
    
    <mat-dialog-content>
      <mat-form-field>
        <mat-label>Customer Name</mat-label>
        <input matInput [(ngModel)]="data.customerName">
      </mat-form-field>
      
      <mat-form-field>
        <mat-label>Amount</mat-label>
        <input matInput type="number" [(ngModel)]="data.amount">
      </mat-form-field>
    </mat-dialog-content>
    
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">Cancel</button>
      <button mat-raised-button color="primary" [mat-dialog-close]="data">
        Create
      </button>
    </mat-dialog-actions>
  `
})
export class InvoiceDialogComponent {
  constructor(public dialogRef: MatDialogRef<InvoiceDialogComponent>,
              @Inject(MAT_DIALOG_DATA) public data: any) {}
}

// Usage in component
@Component({
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <button mat-raised-button color="primary" (click)="openDialog()">
      <mat-icon>add</mat-icon>
      New Invoice
    </button>
  `
})
export class InvoiceCreatorComponent {
  constructor(public dialog: MatDialog) {}

  openDialog(): void {
    this.dialog.open(InvoiceDialogComponent, {
      width: '500px',
      disableClose: false
    }).afterClosed().subscribe(result => {
      if (result) {
        // Handle invoice creation
      }
    });
  }
}
```

### 4. Buttons

```html
<!-- Primary Action (raised with color) -->
<button mat-raised-button color="primary">
  <mat-icon>save</mat-icon>
  Save Invoice
</button>

<!-- Secondary Action (outlined) -->
<button mat-stroked-button>
  <mat-icon>cancel</mat-icon>
  Cancel
</button>

<!-- Tertiary Action (text only) -->
<button mat-button>
  Learn More
</button>

<!-- Icon Button (smaller, compact) -->
<button mat-icon-button>
  <mat-icon>more_vert</mat-icon>
</button>

<!-- Disabled State -->
<button mat-raised-button color="primary" [disabled]="isLoading">
  Submit
</button>
```

### 5. Chips (Tags/Labels)

```typescript
import { MatChipsModule } from '@angular/material/chips';

@Component({
  imports: [MatChipsModule],
  template: `
    <mat-chip-set>
      <mat-chip [highlighted]="invoice.status === 'DRAFT'">
        Draft
      </mat-chip>
      <mat-chip [highlighted]="invoice.status === 'SENT'">
        Sent
      </mat-chip>
      <mat-chip [highlighted]="invoice.status === 'PAID'">
        Paid
      </mat-chip>
    </mat-chip-set>
  `
})
export class StatusChipsComponent {
  @Input() invoice: any;
}
```

### 6. Tabs

```typescript
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  imports: [MatTabsModule],
  template: `
    <mat-tab-group>
      <mat-tab label="Active Invoices">
        <ng-template mat-tab-label>
          <mat-icon>check</mat-icon>
          Active
        </ng-template>
        <app-invoices-list [invoices]="activeInvoices"></app-invoices-list>
      </mat-tab>

      <mat-tab label="Drafts">
        <ng-template mat-tab-label>
          <mat-icon>description</mat-icon>
          Drafts
        </ng-template>
        <app-invoices-list [invoices]="draftInvoices"></app-invoices-list>
      </mat-tab>

      <mat-tab label="Paid">
        <ng-template mat-tab-label>
          <mat-icon>paid</mat-icon>
          Paid
        </ng-template>
        <app-invoices-list [invoices]="paidInvoices"></app-invoices-list>
      </mat-tab>
    </mat-tab-group>
  `
})
export class InvoicesTabsComponent {}
```

### 7. Progress Bar

```typescript
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  imports: [MatProgressBarModule],
  template: `
    <!-- Determinate (with value) -->
    <mat-progress-bar mode="determinate" [value]="uploadProgress"></mat-progress-bar>

    <!-- Indeterminate (loading) -->
    <mat-progress-bar mode="indeterminate" *ngIf="isLoading"></mat-progress-bar>
  `
})
export class UploadComponent {
  uploadProgress = 35;
  isLoading = false;
}
```

### 8. Snackbar (Notifications)

```typescript
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  imports: [MatSnackBarModule],
})
export class InvoiceService {
  constructor(private snackBar: MatSnackBar) {}

  saveInvoice(invoice: any): void {
    // Save logic
    this.snackBar.open('Invoice saved successfully', 'Close', {
      duration: 4000,
      panelClass: ['success-snackbar']
    });
  }

  showError(message: string): void {
    this.snackBar.open(message, 'Retry', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
}
```

---

## Styling with CSS Variables

```scss
.invoice-card {
  background-color: var(--color-white);
  border: 1px solid var(--color-border-gray);
  border-radius: var(--border-radius-md);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
  
  &:hover {
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
  }
}

.invoice-amount {
  color: var(--primary-navy);
  font-weight: 600;
  font-size: var(--font-size-lg);
}

.invoice-status {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--border-radius-sm);
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  
  &.status-draft {
    background-color: rgba(255, 152, 0, 0.1);
    color: #ff9800;
  }
  
  &.status-sent {
    background-color: rgba(33, 150, 243, 0.1);
    color: #2196f3;
  }
  
  &.status-paid {
    background-color: rgba(76, 175, 80, 0.1);
    color: #4caf50;
  }
}
```

---

## Complete Form Example

```typescript
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    ReactiveFormsModule
  ],
  template: `
    <mat-card class="invoice-form-card">
      <mat-card-header>
        <mat-card-title>Create New Invoice</mat-card-title>
      </mat-card-header>

      <mat-card-content>
        <form [formGroup]="form">
          <div class="form-row">
            <mat-form-field appearance="fill">
              <mat-label>Invoice Number</mat-label>
              <input matInput formControlName="invoiceNumber">
            </mat-form-field>

            <mat-form-field appearance="fill">
              <mat-label>Date</mat-label>
              <input matInput [matDatepicker]="picker" formControlName="issueDate">
              <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-datepicker #picker></mat-datepicker>
            </mat-form-field>
          </div>

          <div class="form-row">
            <mat-form-field appearance="fill">
              <mat-label>Customer</mat-label>
              <input matInput formControlName="customerName">
            </mat-form-field>

            <mat-form-field appearance="fill">
              <mat-label>Currency</mat-label>
              <mat-select formControlName="currency">
                <mat-option value="USD">USD</mat-option>
                <mat-option value="EUR">EUR</mat-option>
                <mat-option value="GBP">GBP</mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          <div class="form-row">
            <mat-form-field appearance="fill" class="full-width">
              <mat-label>Notes</mat-label>
              <textarea matInput rows="4" formControlName="notes"></textarea>
            </mat-form-field>
          </div>
        </form>
      </mat-card-content>

      <mat-card-actions align="end">
        <button mat-button (click)="cancel()">Cancel</button>
        <button mat-raised-button color="primary" (click)="save()">
          <mat-icon>save</mat-icon>
          Save Invoice
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .invoice-form-card {
      max-width: 600px;
      margin: 0 auto;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--spacing-md);
      margin-bottom: var(--spacing-md);
    }

    .form-row.full-width {
      grid-template-columns: 1fr;
    }

    .full-width {
      width: 100%;
    }

    mat-card-header {
      border-bottom: 1px solid var(--color-border-gray);
      margin-bottom: var(--spacing-lg);
      padding-bottom: var(--spacing-md);
    }

    mat-card-actions {
      gap: var(--spacing-md);
    }
  `]
})
export class InvoiceFormComponent {
  form = this.fb.group({
    invoiceNumber: [''],
    issueDate: [new Date()],
    customerName: [''],
    currency: ['USD'],
    notes: ['']
  });

  constructor(private fb: FormBuilder) {}

  save(): void {
    console.log(this.form.value);
  }

  cancel(): void {
    this.form.reset();
  }
}
```

---

## Layout Grid Example

```scss
.invoices-container {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
}

@media (max-width: 768px) {
  .invoices-container {
    grid-template-columns: 1fr;
    gap: var(--spacing-md);
  }
}
```

---

## Tips & Best Practices

1. **Always import Material modules** in your component
2. **Use CSS variables** for consistency
3. **Follow Material Design guidelines** from https://m3.material.io/
4. **Test on mobile** - responsive is important
5. **Use semantic colors** - primary for main actions, warn for destructive
6. **Keep forms simple** - clear labels and placeholders
7. **Use icons** - they improve UX
8. **Add loading states** - let users know when something is happening
9. **Provide feedback** - use snackbars for notifications
10. **Test accessibility** - keyboard navigation and screen readers

---

## Material Icons for Invoices

```html
<!-- Common invoice-related icons -->
<mat-icon>description</mat-icon>    <!-- Invoice document -->
<mat-icon>attach_money</mat-icon>   <!-- Money/payment -->
<mat-icon>receipt</mat-icon>        <!-- Receipt -->
<mat-icon>assignment</mat-icon>     <!-- Assignment/form -->
<mat-icon>paid</mat-icon>           <!-- Paid status -->
<mat-icon>pending</mat-icon>        <!-- Pending status -->
<mat-icon>download</mat-icon>       <!-- Download PDF -->
<mat-icon>print</mat-icon>          <!-- Print invoice -->
<mat-icon>email</mat-icon>          <!-- Send email -->
<mat-icon>edit</mat-icon>           <!-- Edit invoice -->
<mat-icon>delete</mat-icon>         <!-- Delete invoice -->
```

---

This quick-start guide should help you apply Material Design to the Invoices module efficiently!
