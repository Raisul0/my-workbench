import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, of } from 'rxjs';
import { ApiService } from '@shared/services';
import {
  InvoiceSummary,
  CreateInvoiceDraftRequest,
  InvoiceSettings,
  InvoiceTemplateSummary,
  TemplateLayoutDocument
} from '../models';

/**
 * Invoices API Service
 * Handles all HTTP communication with the invoices API endpoint
 * Extends the base ApiService for common HTTP methods
 */
@Injectable({ providedIn: 'root' })
export class InvoicesApiService extends ApiService {
  constructor(http: HttpClient) {
    super(http);
  }

  getInvoices() {
    return this.get<InvoiceSummary[]>('/api/invoices').pipe(
      catchError(() => of([] as InvoiceSummary[]))
    );
  }

  createDraft(request: CreateInvoiceDraftRequest) {
    return this.post<{ id: string; invoiceNumber: string; status: string; grandTotal: number; createdAtUtc: string }>(
      '/api/invoices/drafts',
      request
    ).pipe(
      catchError(() => of(null))
    );
  }

  getSettings() {
    return this.get<InvoiceSettings>('/api/invoices/settings').pipe(
      catchError(() => of(null))
    );
  }

  listTemplates() {
    return this.get<InvoiceTemplateSummary[]>('/api/invoices/templates').pipe(
      catchError(() => of([] as InvoiceTemplateSummary[]))
    );
  }

  uploadTemplate(request: { name: string; description?: string; file: File }) {
    const formData = new FormData();
    formData.append('name', request.name);
    formData.append('description', request.description ?? '');
    formData.append('file', request.file);

    return this.postFormData<{ id: string; name: string; activeVersionId?: string; activeVersion?: { versionNumber?: number; sourceType?: string } }>(
      '/api/invoices/templates/upload',
      formData
    ).pipe(
      catchError(() => of(null))
    );
  }

  saveTemplateLayout(templateId: string, layout: TemplateLayoutDocument) {
    return this.post<{ id: string; versionNumber?: number; layoutSchemaVersion?: number; templateId?: string }>(
      `/api/invoices/templates/${templateId}/layout`,
      layout
    ).pipe(
      catchError(() => of(null))
    );
  }

  previewTemplate(templateId: string) {
    return this.getFile(`/api/invoices/templates/${templateId}/preview`).pipe(
      catchError(() => of(new Blob([], { type: 'application/pdf' })))
    );
  }

  previewInvoice(invoiceId: string) {
    return this.getFile(`/api/invoices/${invoiceId}/preview`).pipe(
      catchError(() => of(new Blob([], { type: 'application/pdf' })))
    );
  }

  generateInvoice(invoiceId: string) {
    return this.post<{ id: string; invoiceNumber: string; status: string; grandTotal: number; pdfStorageKey: string; pdfSha256: string; generatedAtUtc: string }>(
      `/api/invoices/${invoiceId}/generate`,
      {}
    ).pipe(
      catchError(() => of(null))
    );
  }
}

