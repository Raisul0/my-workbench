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

export interface Invoice extends InvoiceSummary {
  customerProfileId: string;
  templateId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  lineItemColumns: string[];
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
  customerName: string;
  defaultCurrencyCode: string;
  invoicePrefix: string;
  nextSequence: number;
  defaultTaxRate: number;
  defaultTerms?: string;
  lineItemColumns?: string[];
}

export interface LineItemColumnConfig {
  key: string;
  label: string;
  widthMm: number;
  type: string;
}

export interface TemplateFieldBinding {
  name: string;
  type: string;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  customText?: string;
  contentSource?: string;
  fontSize?: number;
  fontStyle?: string;
  textColor?: string;
  backgroundColor?: string;
  borderSize?: number;
  lineItemColumns?: LineItemColumnConfig[];
}

export interface TemplateLayoutDocument {
  schemaVersion: number;
  name: string;
  fields: TemplateFieldBinding[];
  backgroundImagePath?: string;
}

export interface CreateInvoiceDraftRequest {
  customerProfileId?: string;
  templateId?: string;
  notes?: string;
  terms?: string;
  currencyCode?: string;
  issueDate?: string;
  dueDate?: string | null;
  lineItemColumns?: string[];
  lineItems: InvoiceDraftLineItem[];
}

export interface CustomerProfileSummary {
  id: string;
  customerName: string;
  customerEmail?: string;
  defaultCurrencyCode: string;
  invoicePrefix: string;
  nextSequence: number;
  defaultTaxRate: number;
  taxLabel?: string;
  updatedAtUtc: string;
}

export interface CustomerProfileDetail extends CustomerProfileSummary {
  customerAddress?: string;
  customerPhone?: string;
  customerRegistrationNumber?: string;
  customerTaxNumber?: string;
  defaultTerms?: string;
  lineItemColumns?: string;
  createdAtUtc: string;
}
