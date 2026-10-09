import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  template: `
    <section class="page-header">
      <p class="eyebrow">Overview</p>
      <h1>Dashboard</h1>
    </section>

    <div class="card-grid">
      <article class="card accent">
        <span class="label">Modules</span>
        <strong>1 active</strong>
        <small>Invoices</small>
      </article>
      <article class="card">
        <span class="label">Status</span>
        <strong>Ready</strong>
        <small>Core shell online</small>
      </article>
      <article class="card">
        <span class="label">Next phase</span>
        <strong>Invoice domain</strong>
        <small>Backend and persistence</small>
      </article>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .page-header {
        margin-bottom: 2rem;
      }
      .eyebrow {
        margin: 0 0 0.5rem;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        font-size: 0.72rem;
        color: #64748b;
      }
      h1 {
        margin: 0;
        font-size: clamp(2rem, 3vw, 2.8rem);
      }
      .card-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1.25rem;
      }
      .card {
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
        background: rgba(15, 23, 42, 0.02);
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 1rem;
        padding: 1.25rem;
      }
      .card.accent {
        background: linear-gradient(135deg, #e0f2fe, #f8fafc);
      }
      .label {
        color: #475569;
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
      }
      strong {
        font-size: 1.45rem;
      }
      small {
        color: #64748b;
      }
    `
  ]
})
export class DashboardPageComponent {}
