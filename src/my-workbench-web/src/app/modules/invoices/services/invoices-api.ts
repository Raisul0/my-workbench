import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, of } from 'rxjs';
import { ApiService } from '@shared/services';
import {
  InvoiceSummary,
  CreateInvoiceDraftRequest,
  InvoiceSettings,
  InvoiceTemplateSummary,
  TemplateLayoutDocument,
  BusinessProfileSummary,
  BusinessProfileDetail
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
    return this.get<BusinessProfileSummary[]>('/api/invoices/settings').pipe(
      catchError(() => of([] as BusinessProfileSummary[]))
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
    return this.get<{ html: string }>(`/api/invoices/${invoiceId}/preview`).pipe(
      catchError(() => of(null))
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

  deleteInvoice(invoiceId: string) {
    return this.delete(`/api/invoices/${invoiceId}`).pipe(
      catchError(() => of(null))
    );
  }

  // Business Profile Management
  listProfiles() {
    return this.get<BusinessProfileSummary[]>('/api/invoices/profiles').pipe(
      catchError(() => of([] as BusinessProfileSummary[]))
    );
  }

  getProfile(profileId: string) {
    return this.get<BusinessProfileDetail>(`/api/invoices/profiles/${profileId}`).pipe(
      catchError(() => of(null))
    );
  }

  createProfile(request: Partial<BusinessProfileDetail>) {
    return this.post<BusinessProfileSummary>(
      '/api/invoices/profiles',
      request
    ).pipe(
      catchError(() => of(null))
    );
  }

  updateProfile(profileId: string, request: Partial<BusinessProfileDetail>) {
    return this.put<BusinessProfileSummary>(
      `/api/invoices/profiles/${profileId}`,
      request
    ).pipe(
      catchError(() => of(null))
    );
  }

  deleteProfile(profileId: string) {
    return this.delete(`/api/invoices/profiles/${profileId}`).pipe(
      catchError(() => of(null))
    );
  }

  uploadBackgroundImage(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    return this.postFormData<{ imagePath: string; fileName: string; uploadedAtUtc: string }>(
      '/api/invoices/templates/upload-background',
      formData
    ).pipe(
      catchError(() => of(null))
    );
  }

  getBackgroundImageAsBase64(fileName: string) {
    return this.get<{ imageBase64: string }>(
      `/api/invoices/templates/background/${encodeURIComponent(fileName)}`
    ).pipe(
      catchError(() => of(null))
    );
  }
}

