/**
 * Invoice domain models and DTOs
 */

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
  total: number;
  otherColumns: string;
}

export interface InvoiceTemplateSummary {
  id: string;
  name: string;
  description?: string;
  activeVersionId?: string;
  isArchived?: boolean;
  versionNumber?: number;
  sourceType?: string;
  layoutJson?: string;
  updatedAtUtc?: string;
}

export interface InvoiceSettings {
  id: string;
  issuerName: string;
  defaultCurrencyCode: string;
  invoicePrefix: string;
  nextSequence: number;
  defaultTaxRate: number;
  defaultTerms?: string;
  lineItemColumns?: string[];
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
  backgroundImagePath?: string;
}

export interface CreateInvoiceDraftRequest {
  businessProfileId?: string;
  notes?: string;
  terms?: string;
  currencyCode?: string;
  issueDate?: string;
  dueDate?: string | null;
  lineItemColumns?: string[];
  lineItems: InvoiceDraftLineItem[];
}

export interface BusinessProfileSummary {
  id: string;
  issuerName: string;
  issuerEmail?: string;
  defaultCurrencyCode: string;
  invoicePrefix: string;
  nextSequence: number;
  defaultTaxRate: number;
  taxLabel?: string;
  updatedAtUtc: string;
}

export interface BusinessProfileDetail extends BusinessProfileSummary {
  issuerAddress?: string;
  issuerPhone?: string;
  issuerRegistrationNumber?: string;
  issuerTaxNumber?: string;
  defaultTerms?: string;
  lineItemColumns?: string;
  createdAtUtc: string;
}
