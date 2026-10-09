import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvoicesApiService, InvoiceSummary, InvoiceTemplateSummary, TemplateFieldBinding } from '../services/invoices-api';

@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, FormsModule, ReactiveFormsModule],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Module</p>
        <h1>Invoices</h1>
      </div>
      <button class="primary-button" type="button" (click)="toggleCreateForm()">
        {{ showCreateForm ? 'Close' : 'Create draft' }}
      </button>
    </section>

    @if (showCreateForm) {
      <form class="panel form-panel" [formGroup]="draftForm" (ngSubmit)="submitDraft()">
        <div class="panel-header">
          <h2>New draft invoice</h2>
        </div>

        <div class="form-grid">
          <label>
            Customer name
            <input formControlName="customerName" placeholder="Acme Ltd" />
          </label>

          <label>
            Currency
            <select formControlName="currencyCode">
              <option value="USD">USD</option>
              <option value="MYR">MYR</option>
              <option value="SGD">SGD</option>
            </select>
          </label>

          <label>
            Issue date
            <input type="date" formControlName="issueDate" />
          </label>

          <label>
            Customer email
            <input formControlName="customerEmail" type="email" placeholder="billing@acme.com" />
          </label>

          <label class="wide-field">
            Customer address
            <textarea formControlName="customerAddress" rows="2" placeholder="123 Market Street"></textarea>
          </label>
        </div>

        <div class="line-items-header">
          <h3>Line items</h3>
          <button type="button" class="secondary-button" (click)="addLineItem()">Add line</button>
        </div>

        <div class="line-items" formArrayName="lineItems">
          @for (lineItem of lineItems.controls; track $index; let index = $index) {
            <div class="line-item" [formGroupName]="index">
              <input formControlName="description" placeholder="Description" />
              <input formControlName="quantity" type="number" min="1" step="0.01" placeholder="Qty" />
              <input formControlName="unitPrice" type="number" min="0" step="0.01" placeholder="Unit price" />
              <input formControlName="discountPercent" type="number" min="0" step="0.01" placeholder="Discount %" />
              <input formControlName="taxRate" type="number" min="0" step="0.01" placeholder="Tax %" />
              <button type="button" class="danger-button" (click)="removeLineItem(index)">Remove</button>
            </div>
          }
        </div>

        @if (draftErrorMessage) {
          <p class="error">{{ draftErrorMessage }}</p>
        }

        @if (draftSuccessMessage) {
          <p class="success">{{ draftSuccessMessage }}</p>
        }

        <div class="form-actions">
          <button type="button" class="secondary-button" (click)="toggleCreateForm()">Cancel</button>
          <button type="submit" class="primary-button" [disabled]="draftForm.invalid || submittingDraft">
            {{ submittingDraft ? 'Saving…' : 'Save draft' }}
          </button>
        </div>
      </form>
    }

    @if (selectedInvoice) {
      <div class="panel selected-panel">
        <div class="toolbar">
          <h2>Selected invoice</h2>
          <span class="badge">{{ selectedInvoice.status }}</span>
        </div>

        <div class="selected-invoice-card">
          <div>
            <p class="invoice-number">{{ selectedInvoice.invoiceNumber }}</p>
            <p class="customer-name">{{ selectedInvoice.customerName || 'Unassigned customer' }}</p>
          </div>

          <div class="meta-block">
            <span>{{ selectedInvoice.issueDate ? (selectedInvoice.issueDate | date: 'mediumDate') : 'No issue date' }}</span>
            <span>{{ selectedInvoice.currencyCode || 'USD' }}</span>
          </div>

          <div class="amount-block">
            <span class="amount">{{ selectedInvoice.grandTotal | currency: selectedInvoice.currencyCode || 'USD' }}</span>
            <small>{{ selectedInvoice.status }}</small>
          </div>
        </div>

        <div class="action-row">
          <button type="button" class="secondary-button" (click)="previewInvoice(selectedInvoice.id)" [disabled]="previewingInvoice">
            {{ previewingInvoice ? 'Opening…' : 'Preview PDF' }}
          </button>
          <button type="button" class="primary-button" (click)="generateInvoice(selectedInvoice.id)" [disabled]="generatingInvoice">
            {{ generatingInvoice ? 'Generating…' : 'Generate invoice' }}
          </button>
        </div>

        @if (actionMessage) {
          <p class="success">{{ actionMessage }}</p>
        }
      </div>
    }

    <div class="panel settings-panel">
      <div class="toolbar">
        <h2>Invoice settings</h2>
        <span class="badge">Profile</span>
      </div>

      @if (settings) {
        <div class="settings-grid">
          <div>
            <p class="muted">Issuer</p>
            <p class="invoice-number">{{ settings.issuerName }}</p>
          </div>
          <div>
            <p class="muted">Default currency</p>
            <p class="invoice-number">{{ settings.defaultCurrencyCode }}</p>
          </div>
          <div>
            <p class="muted">Next invoice number</p>
            <p class="invoice-number">{{ settings.invoicePrefix }}{{ settings.nextSequence }}</p>
          </div>
          <div>
            <p class="muted">Default tax</p>
            <p class="invoice-number">{{ settings.defaultTaxRate }}%</p>
          </div>
        </div>
      } @else {
        <p class="muted">Loading invoice profile…</p>
      }
    </div>

    <div class="panel template-panel">
      <div class="toolbar">
        <h2>Template library</h2>
        <span class="badge">{{ templates.length }} templates</span>
      </div>

      <form class="template-upload" (ngSubmit)="uploadTemplate()">
        <label>
          Template name
          <input [(ngModel)]="templateName" name="templateName" placeholder="Client invoice template" />
        </label>

        <label>
          Description
          <input [(ngModel)]="templateDescription" name="templateDescription" placeholder="Quarterly standard billing template" />
        </label>

        <label>
          Source file
          <input type="file" accept=".pdf,image/png,image/jpeg" (change)="onTemplateFileChange($event)" />
        </label>

        <div class="form-actions template-actions">
          <button type="submit" class="primary-button" [disabled]="uploadingTemplate || !selectedTemplateFile">
            {{ uploadingTemplate ? 'Uploading…' : 'Upload template' }}
          </button>
        </div>
      </form>

      @if (templateUploadError) {
        <p class="error">{{ templateUploadError }}</p>
      }

      @if (templateUploadSuccess) {
        <p class="success">{{ templateUploadSuccess }}</p>
      }

      @if (templates.length === 0) {
        <p class="muted">No templates uploaded yet.</p>
      } @else {
        <div class="template-list" role="list">
          @for (template of templates; track template.id) {
            <button type="button" class="template-item" role="listitem" [class.selected-template-item]="selectedTemplate?.id === template.id" (click)="selectTemplate(template)">
              <div>
                <p class="invoice-number">{{ template.name }}</p>
                <p class="customer-name">{{ template.description || 'No description' }}</p>
              </div>

              <div class="meta-block">
                <span class="status-chip">{{ template.activeVersion?.sourceType || 'unknown' }}</span>
                <span>v{{ template.activeVersion?.versionNumber ?? 1 }}</span>
              </div>
            </button>
          }
        </div>
      }
    </div>

    @if (selectedTemplate) {
      <div class="panel editor-panel">
        <div class="toolbar">
          <h2>Template editor</h2>
          <span class="badge">{{ selectedTemplate.name }}</span>
        </div>

        <div class="editor-layout">
          <div class="field-palette">
            <h3>Field palette</h3>
            @for (fieldType of fieldTypes; track fieldType) {
              <button type="button" class="secondary-button" (click)="addField(fieldType)">{{ fieldType }}</button>
            }
          </div>

          <div class="page-canvas" aria-label="Template preview canvas">
            @if (templatePreviewUrl) {
              <iframe class="template-preview-frame" [src]="templatePreviewUrl" title="Selected template preview"></iframe>
            } @else {
              <div class="page-surface">
                @for (field of editorFields; track field.name; let index = $index) {
                  <div class="field-binding" [class.selected-field-binding]="selectedEditorIndex === index" [style.left.%]="field.x ?? 10" [style.top.%]="field.y ?? 10" [style.width.%]="field.width ?? 18" [style.height.%]="field.height ?? 10" (pointerdown)="startFieldDrag($event, index, 'move')" (click)="selectEditorField(index)">
                    <span>{{ field.name }}</span>
                    <div class="resize-handle" (pointerdown)="startFieldDrag($event, index, 'resize')" aria-label="Resize field"></div>
                  </div>
                }
              </div>
            }
          </div>

          <div class="field-inspector">
            <h3>Inspector</h3>
            @if (selectedEditorIndex >= 0 && editorFields[selectedEditorIndex]) {
              <label>
                Name
                <input [(ngModel)]="editorFields[selectedEditorIndex].name" name="editorFieldName" />
              </label>
              <label>
                Type
                <select [(ngModel)]="editorFields[selectedEditorIndex].type" name="editorFieldType">
                  @for (fieldType of fieldTypes; track fieldType) {
                    <option [value]="fieldType">{{ fieldType }}</option>
                  }
                </select>
              </label>
              <div class="inspector-grid">
                <label>
                  X
                  <input type="number" min="0" max="100" step="1" [(ngModel)]="editorFields[selectedEditorIndex].x" name="editorFieldX" />
                </label>
                <label>
                  Y
                  <input type="number" min="0" max="100" step="1" [(ngModel)]="editorFields[selectedEditorIndex].y" name="editorFieldY" />
                </label>
                <label>
                  W
                  <input type="number" min="5" max="100" step="1" [(ngModel)]="editorFields[selectedEditorIndex].width" name="editorFieldWidth" />
                </label>
                <label>
                  H
                  <input type="number" min="5" max="100" step="1" [(ngModel)]="editorFields[selectedEditorIndex].height" name="editorFieldHeight" />
                </label>
              </div>
            } @else {
              <p class="muted">Select a field to edit its placement and metadata.</p>
            }
          </div>
        </div>

        <div class="form-actions">
          <button type="button" class="secondary-button" (click)="clearEditorSelection()">Clear selection</button>
          <button type="button" class="secondary-button" (click)="previewTemplate()">Preview template</button>
          <button type="button" class="primary-button" (click)="saveTemplateLayout()" [disabled]="savingTemplateLayout">
            {{ savingTemplateLayout ? 'Saving…' : 'Save layout' }}
          </button>
        </div>

        @if (templateLayoutMessage) {
          <p [class.success]="templateLayoutSaved" [class.error]="!templateLayoutSaved">{{ templateLayoutMessage }}</p>
        }
      </div>
    }

    <div class="panel">
      <div class="toolbar">
        <h2>Recent invoices</h2>
        <span class="badge">{{ invoices.length }} total</span>
      </div>

      @if (loading) {
        <p class="muted">Loading invoices…</p>
      } @else if (errorMessage) {
        <p class="error">{{ errorMessage }}</p>
      } @else if (invoices.length === 0) {
        <p class="muted">No invoices yet. Create your first draft to get started.</p>
      } @else {
        <div class="invoice-list" role="list">
          @for (invoice of invoices; track invoice.id) {
            <article class="invoice-card" role="listitem" [class.selected-card]="selectedInvoice?.id === invoice.id" (click)="selectInvoice(invoice)">
              <div>
                <p class="invoice-number">{{ invoice.invoiceNumber }}</p>
                <p class="customer-name">{{ invoice.customerName || 'Unassigned customer' }}</p>
              </div>

              <div class="meta-block">
                <span class="status-chip">{{ invoice.status }}</span>
                <span>{{ invoice.issueDate | date: 'mediumDate' }}</span>
              </div>

              <div class="amount-block">
                <span class="amount">{{ invoice.grandTotal | currency: invoice.currencyCode || 'USD' }}</span>
                <small>{{ invoice.currencyCode || 'USD' }}</small>
              </div>
            </article>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .page-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1.5rem;
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
      .primary-button,
      .secondary-button,
      .danger-button {
        border: none;
        border-radius: 999px;
        padding: 0.7rem 1rem;
        font-weight: 600;
        cursor: pointer;
      }
      .primary-button {
        background: #0f172a;
        color: white;
      }
      .secondary-button {
        background: #e2e8f0;
        color: #0f172a;
      }
      .danger-button {
        background: #fee2e2;
        color: #991b1b;
      }
      .panel {
        background: #fff;
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 1rem;
        padding: 1.5rem;
        box-shadow: 0 10px 25px rgba(15, 23, 42, 0.03);
      }
      .form-panel {
        margin-bottom: 1.5rem;
      }
      .panel-header {
        margin-bottom: 1rem;
      }
      .form-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 1rem;
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
        font-weight: 600;
        color: #334155;
      }
      .wide-field {
        grid-column: 1 / -1;
      }
      input,
      select,
      textarea {
        border: 1px solid #cbd5e1;
        border-radius: 0.65rem;
        background: white;
        padding: 0.7rem 0.8rem;
        font: inherit;
      }
      .line-items-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 1.2rem 0 0.75rem;
      }
      .line-items {
        display: grid;
        gap: 0.8rem;
      }
      .line-item {
        display: grid;
        grid-template-columns: 2fr 0.7fr 1fr 0.8fr 0.8fr auto;
        gap: 0.6rem;
      }
      .toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .toolbar h2 {
        margin: 0;
      }
      .badge {
        display: inline-flex;
        align-items: center;
        border-radius: 999px;
        background: #e2e8f0;
        color: #0f172a;
        font-size: 0.75rem;
        padding: 0.4rem 0.65rem;
      }
      .invoice-list {
        display: grid;
        gap: 1rem;
      }
      .settings-panel {
        margin-bottom: 1.5rem;
      }
      .settings-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 1rem;
      }
      .template-panel {
        margin-bottom: 1.5rem;
      }
      .template-upload {
        display: grid;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .template-list {
        display: grid;
        gap: 0.75rem;
      }
      .template-item {
        display: grid;
        grid-template-columns: minmax(0, 2fr) minmax(120px, 0.5fr);
        gap: 1rem;
        align-items: center;
        padding: 1rem 1.1rem;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 0.9rem;
        width: 100%;
        text-align: left;
        cursor: pointer;
      }
      .selected-template-item {
        border-color: #0f172a;
        box-shadow: inset 0 0 0 1px rgba(15, 23, 42, 0.08);
      }
      .editor-panel {
        margin-bottom: 1.5rem;
      }
      .editor-layout {
        display: grid;
        grid-template-columns: 180px minmax(0, 1.5fr) minmax(220px, 0.9fr);
        gap: 1rem;
        align-items: start;
      }
      .field-palette,
      .field-inspector {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }
      .field-palette h3,
      .field-inspector h3 {
        margin: 0 0 0.25rem;
      }
      .page-canvas {
        display: flex;
        justify-content: center;
      }
      .template-preview-frame {
        width: min(100%, 720px);
        height: 560px;
        border: 1px solid #cbd5e1;
        border-radius: 0.8rem;
        background: white;
      }
      .page-surface {
        position: relative;
        width: min(100%, 720px);
        aspect-ratio: 1 / 1.41;
        background: repeating-linear-gradient(
          0deg,
          #ffffff,
          #ffffff 18px,
          #f8fafc 18px,
          #f8fafc 19px
        );
        border: 1px solid #cbd5e1;
        border-radius: 0.8rem;
        overflow: hidden;
        box-shadow: inset 0 0 0 1px rgba(15, 23, 42, 0.04);
      }
      .field-binding {
        position: absolute;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.2rem 0.35rem;
        background: rgba(59, 130, 246, 0.12);
        border: 1px solid rgba(59, 130, 246, 0.6);
        border-radius: 0.45rem;
        color: #0f172a;
        font-size: 0.7rem;
        font-weight: 700;
        cursor: move;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        user-select: none;
      }
      .selected-field-binding {
        box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.35);
      }
      .resize-handle {
        position: absolute;
        right: 0;
        bottom: 0;
        width: 14px;
        height: 14px;
        background: rgba(15, 23, 42, 0.8);
        border-radius: 2px 0 0 0;
        cursor: nwse-resize;
      }
      .inspector-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
      }
      .selected-panel {
        margin-bottom: 1.5rem;
      }
      .selected-invoice-card {
        display: grid;
        grid-template-columns: minmax(0, 1.5fr) minmax(150px, 0.7fr) minmax(120px, 0.5fr);
        gap: 1rem;
        align-items: center;
        padding: 1rem 1.1rem;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 0.9rem;
        margin-bottom: 1rem;
      }
      .action-row {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 0.5rem;
      }
      .invoice-card {
        display: grid;
        grid-template-columns: minmax(0, 1.5fr) minmax(150px, 0.7fr) minmax(120px, 0.5fr);
        gap: 1rem;
        align-items: center;
        padding: 1rem 1.1rem;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 0.9rem;
        cursor: pointer;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }
      .invoice-card:hover {
        border-color: #94a3b8;
        box-shadow: 0 8px 18px rgba(15, 23, 42, 0.04);
      }
      .selected-card {
        border-color: #0f172a;
        box-shadow: inset 0 0 0 1px rgba(15, 23, 42, 0.1);
      }
      .invoice-number {
        margin: 0;
        font-weight: 700;
        color: #0f172a;
      }
      .customer-name {
        margin: 0.3rem 0 0;
        color: #475569;
      }
      .meta-block,
      .amount-block {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        align-items: flex-start;
      }
      .status-chip {
        display: inline-flex;
        align-items: center;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: #dcfce7;
        color: #166534;
        font-size: 0.72rem;
        font-weight: 700;
        text-transform: uppercase;
      }
      .amount {
        font-size: 1.2rem;
        font-weight: 700;
      }
      .muted {
        color: #475569;
      }
      .error {
        color: #b91c1c;
      }
      .success {
        color: #166534;
      }
      .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1rem;
      }
      @media (max-width: 900px) {
        .form-grid {
          grid-template-columns: 1fr;
        }
        .line-item {
          grid-template-columns: 1fr;
        }
      }
      @media (max-width: 720px) {
        .invoice-card {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class InvoicesPageComponent implements OnInit, OnDestroy {
  invoices: InvoiceSummary[] = [];
  loading = true;
  errorMessage = '';
  draftErrorMessage = '';
  draftSuccessMessage = '';
  showCreateForm = false;
  submittingDraft = false;
  selectedInvoice: InvoiceSummary | null = null;
  actionMessage = '';
  previewingInvoice = false;
  generatingInvoice = false;
  settings: any = null;
  templates: InvoiceTemplateSummary[] = [];
  templateName = '';
  templateDescription = '';
  selectedTemplateFile: File | null = null;
  uploadingTemplate = false;
  templateUploadError = '';
  templateUploadSuccess = '';
  selectedTemplate: InvoiceTemplateSummary | null = null;
  selectedEditorIndex = -1;
  editorFields: TemplateFieldBinding[] = [];
  fieldTypes = ['Text', 'Number', 'Date', 'Currency', 'Address', 'Table'];
  templateLayoutMessage = '';
  templateLayoutSaved = false;
  savingTemplateLayout = false;
  templatePreviewUrl: string | null = null;
  private previewObjectUrl: string | null = null;
  private dragState: { index: number; mode: 'move' | 'resize'; startX: number; startY: number; originX: number; originY: number; originWidth: number; originHeight: number } | null = null;

  draftForm!: any;

  constructor(
    private readonly invoicesApi: InvoicesApiService,
    private readonly fb: FormBuilder,
  ) {
    this.draftForm = this.fb.nonNullable.group({
      customerName: ['', Validators.required],
      customerAddress: '',
      customerEmail: '',
      currencyCode: 'USD',
      issueDate: [new Date().toISOString().slice(0, 10)],
      lineItems: this.fb.nonNullable.array([this.createLineItemGroup()]),
    });
  }

  ngOnInit(): void {
    this.loadInvoices();
    this.loadSettings();
    this.loadTemplates();
  }

  ngOnDestroy(): void {
    this.clearTemplatePreview();
    this.dragState = null;
    window.removeEventListener('pointermove', this.handlePointerMove);
    window.removeEventListener('pointerup', this.handlePointerUp);
  }

  onTemplateFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedTemplateFile = input.files?.[0] ?? null;
  }

  uploadTemplate(): void {
    if (!this.selectedTemplateFile) {
      this.templateUploadError = 'Choose a PDF or image template before uploading.';
      return;
    }

    this.uploadingTemplate = true;
    this.templateUploadError = '';
    this.templateUploadSuccess = '';

    this.invoicesApi.uploadTemplate({
      name: this.templateName.trim() || this.selectedTemplateFile.name,
      description: this.templateDescription,
      file: this.selectedTemplateFile,
    }).subscribe({
      next: (result) => {
        this.uploadingTemplate = false;
        if (!result) {
          this.templateUploadError = 'Template upload failed. Check the file type and backend validation.';
          return;
        }

        this.templateUploadSuccess = `Template "${result.name}" uploaded successfully.`;
        this.templateName = '';
        this.templateDescription = '';
        this.selectedTemplateFile = null;
        const input = document.querySelector<HTMLInputElement>('.template-upload input[type="file"]');
        if (input) {
          input.value = '';
        }
        this.loadTemplates();
      },
      error: () => {
        this.uploadingTemplate = false;
        this.templateUploadError = 'Template upload failed. Validate the file and try again.';
      },
    });
  }

  private loadSettings(): void {
    this.invoicesApi.getSettings().subscribe({
      next: (settings) => {
        this.settings = settings ?? null;
      },
      error: () => {
        this.settings = null;
      },
    });
  }

  private loadTemplates(): void {
    this.invoicesApi.listTemplates().subscribe({
      next: (templates) => {
        this.templates = templates ?? [];
        if (!this.selectedTemplate && this.templates.length > 0) {
          this.selectTemplate(this.templates[0]);
        }
      },
      error: () => {
        this.templates = [];
      },
    });
  }

  selectTemplate(template: InvoiceTemplateSummary): void {
    this.selectedTemplate = template;
    this.selectedEditorIndex = -1;
    this.editorFields = this.getDefaultEditorFields();
    this.templateLayoutMessage = '';
    this.clearTemplatePreview();
  }

  previewTemplate(): void {
    if (!this.selectedTemplate) {
      return;
    }

    this.invoicesApi.previewTemplate(this.selectedTemplate.id).subscribe({
      next: (blob) => {
        if (!blob || blob.size === 0) {
          this.templateLayoutSaved = false;
          this.templateLayoutMessage = 'The selected template could not be previewed.';
          return;
        }

        this.clearTemplatePreview();
        this.previewObjectUrl = URL.createObjectURL(blob);
        this.templatePreviewUrl = this.previewObjectUrl;
        this.templateLayoutSaved = true;
        this.templateLayoutMessage = 'Template preview loaded.';
      },
      error: () => {
        this.templateLayoutSaved = false;
        this.templateLayoutMessage = 'Template preview is unavailable for this template.';
      },
    });
  }

  private clearTemplatePreview(): void {
    if (this.previewObjectUrl) {
      URL.revokeObjectURL(this.previewObjectUrl);
      this.previewObjectUrl = null;
    }
    this.templatePreviewUrl = null;
  }

  startFieldDrag(event: PointerEvent, index: number, mode: 'move' | 'resize'): void {
    event.preventDefault();
    event.stopPropagation();

    const field = this.editorFields[index];
    if (!field) {
      return;
    }

    this.selectedEditorIndex = index;
    this.dragState = {
      index,
      mode,
      startX: event.clientX,
      startY: event.clientY,
      originX: field.x ?? 10,
      originY: field.y ?? 10,
      originWidth: field.width ?? 18,
      originHeight: field.height ?? 10,
    };

    window.addEventListener('pointermove', this.handlePointerMove);
    window.addEventListener('pointerup', this.handlePointerUp, { once: true });
  }

  private readonly handlePointerMove = (event: PointerEvent) => {
    if (!this.dragState) {
      return;
    }

    const field = this.editorFields[this.dragState.index];
    if (!field) {
      return;
    }

    const deltaX = event.clientX - this.dragState.startX;
    const deltaY = event.clientY - this.dragState.startY;
    const pageWidth = 720;
    const pageHeight = 1015;

    if (this.dragState.mode === 'move') {
      field.x = this.clampPercent(this.dragState.originX + (deltaX / pageWidth) * 100, 0, 100 - (field.width ?? 18));
      field.y = this.clampPercent(this.dragState.originY + (deltaY / pageHeight) * 100, 0, 100 - (field.height ?? 10));
      return;
    }

    const nextWidth = this.clampPercent(this.dragState.originWidth + (deltaX / pageWidth) * 100, 5, 100 - (field.x ?? 0));
    const nextHeight = this.clampPercent(this.dragState.originHeight + (deltaY / pageHeight) * 100, 5, 100 - (field.y ?? 0));
    field.width = nextWidth;
    field.height = nextHeight;
  };

  private readonly handlePointerUp = () => {
    this.dragState = null;
    window.removeEventListener('pointermove', this.handlePointerMove);
    window.removeEventListener('pointerup', this.handlePointerUp);
  };

  addField(fieldType: string): void {
    const nextIndex = this.editorFields.length + 1;
    this.editorFields.push({
      name: `${fieldType.toLowerCase()}${nextIndex}`,
      type: fieldType,
      x: 10 + ((nextIndex - 1) % 3) * 15,
      y: 10 + Math.floor((nextIndex - 1) / 3) * 16,
      width: fieldType === 'Table' ? 52 : 22,
      height: fieldType === 'Table' ? 18 : 10,
    });
    this.selectedEditorIndex = this.editorFields.length - 1;
  }

  selectEditorField(index: number): void {
    this.selectedEditorIndex = index;
  }

  clearEditorSelection(): void {
    this.selectedEditorIndex = -1;
  }

  private clampPercent(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
  }

  saveTemplateLayout(): void {
    if (!this.selectedTemplate) {
      return;
    }

    const normalizedFields = this.editorFields
      .map((field) => ({
        name: String(field.name ?? '').trim(),
        type: String(field.type ?? '').trim(),
        x: Number(field.x ?? 0),
        y: Number(field.y ?? 0),
        width: Number(field.width ?? 18),
        height: Number(field.height ?? 10),
      }))
      .filter((field) => field.name.length > 0 && field.type.length > 0);

    if (normalizedFields.length === 0) {
      this.templateLayoutSaved = false;
      this.templateLayoutMessage = 'Add at least one valid field before saving the template layout.';
      return;
    }

    this.savingTemplateLayout = true;
    this.templateLayoutMessage = '';

    const payload = {
      schemaVersion: 1,
      name: this.selectedTemplate.name,
      fields: normalizedFields,
    };

    this.invoicesApi.saveTemplateLayout(this.selectedTemplate.id, payload).subscribe({
      next: (result) => {
        this.savingTemplateLayout = false;
        if (!result) {
          this.templateLayoutSaved = false;
          this.templateLayoutMessage = 'Template layout could not be saved. Check the backend validation.';
          return;
        }

        this.templateLayoutSaved = true;
        this.templateLayoutMessage = `Layout saved to template version ${result.versionNumber ?? 1}.`;
      },
      error: () => {
        this.savingTemplateLayout = false;
        this.templateLayoutSaved = false;
        this.templateLayoutMessage = 'Layout save failed. Review the fields and try again.';
      },
    });
  }

  private getDefaultEditorFields(): TemplateFieldBinding[] {
    return [
      { name: 'invoiceNumber', type: 'Text', x: 14, y: 10, width: 24, height: 10 },
      { name: 'customerName', type: 'Text', x: 14, y: 23, width: 38, height: 10 },
      { name: 'issueDate', type: 'Date', x: 62, y: 15, width: 22, height: 10 },
      { name: 'grandTotal', type: 'Currency', x: 62, y: 78, width: 22, height: 10 },
      { name: 'lineItems', type: 'Table', x: 10, y: 42, width: 76, height: 28 },
    ];
  }

  selectInvoice(invoice: InvoiceSummary): void {
    this.selectedInvoice = invoice;
    this.actionMessage = '';
  }

  previewInvoice(invoiceId: string): void {
    this.previewingInvoice = true;
    this.actionMessage = '';

    this.invoicesApi.previewInvoice(invoiceId).subscribe({
      next: (blob) => {
        this.previewingInvoice = false;
        if (!blob || blob.size === 0) {
          this.actionMessage = 'Preview unavailable for this invoice right now.';
          return;
        }

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        link.click();
        URL.revokeObjectURL(url);
        this.actionMessage = 'PDF preview opened in a new tab.';
      },
      error: () => {
        this.previewingInvoice = false;
        this.actionMessage = 'The PDF preview request failed.';
      },
    });
  }

  generateInvoice(invoiceId: string): void {
    this.generatingInvoice = true;
    this.actionMessage = '';

    this.invoicesApi.generateInvoice(invoiceId).subscribe({
      next: (result) => {
        this.generatingInvoice = false;
        if (!result) {
          this.actionMessage = 'Invoice generation failed. Check the backend state and try again.';
          return;
        }

        this.actionMessage = `Invoice ${result.invoiceNumber} was generated successfully.`;
        this.loadInvoices();
      },
      error: () => {
        this.generatingInvoice = false;
        this.actionMessage = 'Invoice generation could not be completed.';
      },
    });
  }

  get lineItems() {
    return this.draftForm.get('lineItems') as any;
  }

  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.draftErrorMessage = '';
      this.draftSuccessMessage = '';
      this.draftForm.reset({
        customerName: '',
        customerAddress: '',
        customerEmail: '',
        currencyCode: 'USD',
        issueDate: new Date().toISOString().slice(0, 10),
        lineItems: [this.createLineItemGroup().getRawValue()],
      });
    }
  }

  addLineItem(): void {
    this.lineItems.push(this.createLineItemGroup());
  }

  removeLineItem(index: number): void {
    if (this.lineItems.length > 1) {
      this.lineItems.removeAt(index);
    }
  }

  submitDraft(): void {
    if (this.draftForm.invalid) {
      this.draftErrorMessage = 'Customer name and at least one line item are required.';
      return;
    }

    const payload = {
      customerName: String(this.draftForm.value.customerName ?? ''),
      customerAddress: String(this.draftForm.value.customerAddress ?? ''),
      customerEmail: String(this.draftForm.value.customerEmail ?? ''),
      customerPhone: '',
      customerRegistrationNumber: '',
      customerTaxNumber: '',
      notes: '',
      terms: '',
      currencyCode: String(this.draftForm.value.currencyCode ?? 'USD'),
      issueDate: this.draftForm.value.issueDate ?? new Date().toISOString().slice(0, 10),
      dueDate: null,
      lineItems: (this.draftForm.value.lineItems ?? []).map((line: any) => ({
        description: String(line.description ?? ''),
        quantity: Number(line.quantity ?? 1),
        unitPrice: Number(line.unitPrice ?? 0),
        discountPercent: Number(line.discountPercent ?? 0),
        taxRate: Number(line.taxRate ?? 0),
      })),
    };

    this.submittingDraft = true;
    this.draftErrorMessage = '';
    this.draftSuccessMessage = '';

    this.invoicesApi.createDraft(payload).subscribe({
      next: (result) => {
        this.submittingDraft = false;
        if (!result) {
          this.draftErrorMessage = 'Draft creation failed. Verify the backend is running and you are authenticated.';
          return;
        }

        this.draftSuccessMessage = `Draft ${result.invoiceNumber} created successfully.`;
        this.showCreateForm = false;
        this.loadInvoices();
      },
      error: () => {
        this.submittingDraft = false;
        this.draftErrorMessage = 'Draft creation failed. Please review the form input and try again.';
      },
    });
  }

  private loadInvoices(): void {
    this.loading = true;
    this.errorMessage = '';

    this.invoicesApi.getInvoices().subscribe({
      next: (items) => {
        this.invoices = items ?? [];
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load invoices. Check the API connection and authentication status.';
        this.loading = false;
      },
    });
  }

  private createLineItemGroup() {
    return this.fb.nonNullable.group({
      description: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      discountPercent: [0, [Validators.min(0)]],
      taxRate: [0, [Validators.min(0)]],
    });
  }
}
