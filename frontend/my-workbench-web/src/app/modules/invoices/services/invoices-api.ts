import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, of } from 'rxjs';

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  status: string;
  currencyCode: string;
  grandTotal: number;
  issueDate: string | null;
  customerName: string;
}

export interface InvoiceDraftLineItem {
  description: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxRate: number;
}

export interface InvoiceTemplateSummary {
  id: string;
  name: string;
  description?: string;
  activeVersionId?: string;
  isArchived?: boolean;
  activeVersion?: {
    versionNumber?: number;
    sourceType?: string;
  };
  updatedAtUtc?: string;
}

export interface InvoiceSettings {
  id: string;
  issuerName: string;
  defaultCurrencyCode: string;
  invoicePrefix: string;
  nextSequence: number;
  defaultTaxRate: number;
  defaultTerms: string;
}

export interface TemplateFieldBinding {
  name: string;
  type: string;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

export interface TemplateLayoutDocument {
  schemaVersion: number;
  name: string;
  fields: TemplateFieldBinding[];
}

export interface CreateInvoiceDraftRequest {
  customerName: string;
  customerAddress?: string;
  customerEmail?: string;
  customerPhone?: string;
  customerRegistrationNumber?: string;
  customerTaxNumber?: string;
  notes?: string;
  terms?: string;
  currencyCode?: string;
  issueDate?: string;
  dueDate?: string | null;
  lineItems: InvoiceDraftLineItem[];
}

@Injectable({ providedIn: 'root' })
export class InvoicesApiService {
  constructor(private readonly http: HttpClient) {}

  getInvoices() {
    return this.http.get<InvoiceSummary[]>('/api/invoices').pipe(
      catchError(() => of([] as InvoiceSummary[])),
    );
  }

  createDraft(request: CreateInvoiceDraftRequest) {
    return this.http.post<{ id: string; invoiceNumber: string; status: string; grandTotal: number; createdAtUtc: string }>('/api/invoices/drafts', request)
      .pipe(
        catchError(() => of(null)),
      );
  }

  getSettings() {
    return this.http.get<InvoiceSettings>('/api/invoices/settings').pipe(
      catchError(() => of(null)),
    );
  }

  listTemplates() {
    return this.http.get<InvoiceTemplateSummary[]>('/api/invoices/templates').pipe(
      catchError(() => of([] as InvoiceTemplateSummary[])),
    );
  }

  uploadTemplate(request: { name: string; description?: string; file: File }) {
    const formData = new FormData();
    formData.append('name', request.name);
    formData.append('description', request.description ?? '');
    formData.append('file', request.file);

    return this.http.post<{ id: string; name: string; activeVersionId?: string; activeVersion?: { versionNumber?: number; sourceType?: string } }>('/api/invoices/templates/upload', formData)
      .pipe(
        catchError(() => of(null)),
      );
  }

  saveTemplateLayout(templateId: string, layout: TemplateLayoutDocument) {
    return this.http.post<{ id: string; versionNumber?: number; layoutSchemaVersion?: number; templateId?: string }>('/api/invoices/templates/' + templateId + '/layout', layout)
      .pipe(
        catchError(() => of(null)),
      );
  }

  previewTemplate(templateId: string) {
    return this.http.post(`/api/invoices/templates/${templateId}/preview`, {}, { responseType: 'blob' }).pipe(
      catchError(() => of(new Blob([], { type: 'application/pdf' }))),
    );
  }

  previewInvoice(invoiceId: string) {
    return this.http.post(`/api/invoices/${invoiceId}/preview`, {}, { responseType: 'blob' }).pipe(
      catchError(() => of(new Blob([], { type: 'application/pdf' }))),
    );
  }

  generateInvoice(invoiceId: string) {
    return this.http.post<{ id: string; invoiceNumber: string; status: string; grandTotal: number; pdfStorageKey: string; pdfSha256: string; generatedAtUtc: string }>(`/api/invoices/${invoiceId}/generate`, {})
      .pipe(
        catchError(() => of(null)),
      );
  }
}
