import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvoicesApiService } from '../../services';
import { InvoiceSummary, InvoiceTemplateSummary, TemplateFieldBinding } from '../../models';

@Component({
  selector: 'app-invoices-home',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, FormsModule, ReactiveFormsModule],
  templateUrl: './invoices-home.component.html',
  styleUrl: './invoices-home.component.scss',
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
  htmlPreviewUrl: string | SafeResourceUrl | null = null;
  private previewObjectUrl: string | null = null;
  private dragState: { index: number; mode: 'move' | 'resize'; startX: number; startY: number; originX: number; originY: number; originWidth: number; originHeight: number } | null = null;

  draftForm!: any;

  constructor(
    private readonly invoicesApi: InvoicesApiService,
    private readonly fb: FormBuilder,
    private readonly sanitizer: DomSanitizer,
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
    this.closeHtmlPreview();
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
        this.htmlPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
        this.actionMessage = 'Invoice preview loaded.';
      },
      error: () => {
        this.previewingInvoice = false;
        this.actionMessage = 'The preview request failed.';
      },
    });
  }

  closeHtmlPreview(): void {
    if (this.htmlPreviewUrl && typeof this.htmlPreviewUrl === 'string') {
      URL.revokeObjectURL(this.htmlPreviewUrl);
    }
    this.htmlPreviewUrl = null;
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
