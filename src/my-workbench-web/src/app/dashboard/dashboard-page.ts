import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatGridListModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <section class="page-header">
      <p class="page-eyebrow">Overview</p>
      <h1 class="page-title">Dashboard</h1>
    </section>

    <div class="card-grid">
      <!-- Active Modules Card -->
      <mat-card class="stat-card accent">
        <mat-card-header>
          <mat-icon class="card-icon">extension</mat-icon>
        </mat-card-header>
        <mat-card-content>
          <p class="card-label">Modules</p>
          <p class="card-value">1 active</p>
          <p class="card-subtitle">Invoices</p>
        </mat-card-content>
      </mat-card>

      <!-- Status Card -->
      <mat-card class="stat-card">
        <mat-card-header>
          <mat-icon class="card-icon success">check_circle</mat-icon>
        </mat-card-header>
        <mat-card-content>
          <p class="card-label">Status</p>
          <p class="card-value">Ready</p>
          <p class="card-subtitle">Core shell online</p>
        </mat-card-content>
      </mat-card>

      <!-- Next Phase Card -->
      <mat-card class="stat-card">
        <mat-card-header>
          <mat-icon class="card-icon">timeline</mat-icon>
        </mat-card-header>
        <mat-card-content>
          <p class="card-label">Next phase</p>
          <p class="card-value">Invoice domain</p>
          <p class="card-subtitle">Backend and persistence</p>
        </mat-card-content>
      </mat-card>
    </div>

    <!-- Info Section -->
    <section class="info-section">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Getting Started</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Welcome to My Workbench. Navigate to the Invoices module to get started with invoice management.</p>
          <div class="action-buttons">
            <button mat-raised-button color="primary">
              <mat-icon>description</mat-icon>
              Go to Invoices
            </button>
            <button mat-stroked-button>
              <mat-icon>help</mat-icon>
              Learn More
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .page-header {
        margin-bottom: var(--spacing-xl);
      }

      .page-eyebrow {
        margin: 0 0 var(--spacing-sm);
        text-transform: uppercase;
        letter-spacing: 0.12em;
        font-size: var(--font-size-xs);
        color: #999;
        font-weight: 600;
      }

      .page-title {
        margin: 0;
        font-size: clamp(2rem, 3vw, 2.8rem);
        font-weight: 700;
        color: var(--color-black);
        letter-spacing: -1px;
      }

      .card-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
        gap: var(--spacing-lg);
        margin-bottom: var(--spacing-xl);
      }

      .stat-card {
        border-radius: var(--border-radius-md);
        box-shadow: var(--shadow-sm);
        transition: all 0.3s ease-in-out;
        border: 1px solid var(--color-border-gray);

        &:hover {
          box-shadow: var(--shadow-md);
          transform: translateY(-2px);
        }

        mat-card-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: var(--spacing-md);
          border-bottom: 1px solid var(--color-border-gray);
          padding-bottom: var(--spacing-md);

          .card-icon {
            font-size: 32px;
            width: 32px;
            height: 32px;
            color: var(--primary-navy);

            &.success {
              color: var(--color-success);
            }
          }
        }

        mat-card-content {
          padding: 0;

          .card-label {
            margin: 0 0 var(--spacing-sm);
            font-size: var(--font-size-xs);
            text-transform: uppercase;
            letter-spacing: 0.08em;
            color: #999;
            font-weight: 600;
          }

          .card-value {
            margin: 0 0 var(--spacing-xs);
            font-size: var(--font-size-2xl);
            font-weight: 700;
            color: var(--color-black);
          }

          .card-subtitle {
            margin: 0;
            font-size: var(--font-size-sm);
            color: #999;
          }
        }

        &.accent {
          background: linear-gradient(135deg, rgba(26, 107, 201, 0.08), rgba(0, 69, 137, 0.05));
          border-color: rgba(26, 107, 201, 0.2);
        }
      }

      .info-section {
        mat-card {
          border-radius: var(--border-radius-md);
          border: 1px solid var(--color-border-gray);

          mat-card-header {
            border-bottom: 1px solid var(--color-border-gray);
            padding-bottom: var(--spacing-md);
            margin-bottom: var(--spacing-md);

            mat-card-title {
              margin: 0;
              font-size: var(--font-size-lg);
              font-weight: 600;
              color: var(--color-black);
            }
          }

          mat-card-content {
            p {
              color: #666;
              line-height: 1.6;

              &:last-of-type {
                margin-bottom: var(--spacing-md);
              }
            }

            .action-buttons {
              display: flex;
              gap: var(--spacing-md);
              margin-top: var(--spacing-lg);

              button {
                gap: var(--spacing-sm);
                font-weight: 500;
              }
            }
          }
        }
      }

      @media (max-width: 768px) {
        .page-title {
          font-size: 1.8rem;
        }

        .card-grid {
          grid-template-columns: 1fr;
          gap: var(--spacing-md);
        }
      }
    `
  ]
})
export class DashboardPageComponent {}
