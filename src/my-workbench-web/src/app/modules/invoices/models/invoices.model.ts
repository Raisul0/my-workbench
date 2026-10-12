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
