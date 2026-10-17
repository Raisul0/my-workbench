import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { InvoicesApiService } from '../../services';
import { BusinessProfileDetail, BusinessProfileSummary, InvoiceSummary, InvoiceTemplateSummary, TemplateFieldBinding } from '../../models';

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
  deletingInvoice = false;
  settings: any = null;
  settingsList: any[] = [];
  selectedProfileForEdit: BusinessProfileDetail | null = null;
  selectedProfile: BusinessProfileDetail | null = null;
  selectedProfileLineItemColumns: Record<string, string> = {};
  lineItemColumnLabels: Record<string, string> = {};
  templates: InvoiceTemplateSummary[] = [];
  templateName = '';
  templateDescription = '';
  selectedTemplateFile: File | null = null;
  selectedBackgroundImageFile: File | null = null;
  uploadingTemplate = false;
  uploadingBackgroundImage = false;
  templateUploadError = '';
  templateUploadSuccess = '';
  selectedTemplate: InvoiceTemplateSummary | null = null;
  templateBackgroundImagePath: string | null = null;
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
  profiles: BusinessProfileSummary  [] = [];
  protected readonly JSON = JSON;

  constructor(
    private readonly invoicesApi: InvoicesApiService,
    private readonly fb: FormBuilder,
    private readonly sanitizer: DomSanitizer,
    private readonly cdr: ChangeDetectorRef,
    private readonly router: Router,
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
    this.loadProfiles();
    //this.loadSettings();
    this.loadTemplates();

  }

  onProfileSelected(profileId: string): void {
    if (!profileId) {
      this.selectedProfile = null;
      this.lineItemColumnLabels = {};
      this.cdr.markForCheck();
      return;
    }

    // Fetch full profile details from backend
    this.invoicesApi.getProfile(profileId).subscribe({
      next: (profile) => {
        if (!profile) {
          this.selectedProfile = null;
          this.lineItemColumnLabels = {};
          this.cdr.markForCheck();
          return;
        }

        this.selectedProfile = profile;

        // Update line item columns based on profile configuration
        this.selectedProfileLineItemColumns = this.convertLineItemColumnsToKeyValuePair(this.selectedProfile.lineItemColumns) || {};
        this.updateLineItemColumns();
        
        // Populate form fields from the fetched profile
        this.draftForm.patchValue({
          customerName: profile.issuerName || '',
          currencyCode: profile.defaultCurrencyCode || 'USD',
          customerAddress: profile.issuerAddress || '',
          customerEmail: profile.issuerEmail || '',
        });


        // Reset line items array with one empty line item using profile columns
        const lineItemsArray = this.draftForm.get('lineItems') as any;
        lineItemsArray.clear();
        lineItemsArray.push(this.createLineItemGroup()); 
        
        this.cdr.markForCheck();
      },
      error: () => {
        this.selectedProfile = null;
        this.lineItemColumnLabels = {};
        this.cdr.markForCheck();
      },
    });
  }

  private updateLineItemColumns(): void {
    this.lineItemColumnLabels= {};

    for (const [key, value] of Object.entries(this.selectedProfileLineItemColumns)) {
        const name = key;
        const columnLabel = name
          .replace(/([a-z])([A-Z])/g, '$1 $2')
          .replace(/[_-]+/g, ' ')
          .trim();

        this.lineItemColumnLabels[key] = columnLabel.charAt(0).toUpperCase() + columnLabel.slice(1);
    }
  }

  /**
   * Converts lineItemColumns from "name:type" format to key-value pair object
   * Handles both string (comma-separated) and array formats
   * @param columns - Array of strings or comma-separated string in "name:type" format
   * @returns Object with column names as keys and types as values
   * 
   * @example
   * Input (array): ["Description:Text", "Hours:Number", "Rate:Number"]
   * Input (string): "Description:Text, Hours:Number, Rate:Number"
   * Output: { Description: "Text", Hours: "Number", Rate: "Number" }
   */
  convertLineItemColumnsToKeyValuePair(columns?: string): Record<string, string> {
    let columnsList: string[] = [];
    
    // Handle both string and array formats
    if (typeof columns === 'string') {
      columnsList = columns.split(',');
    } else {
      columnsList = [];
    }
    
    const result: Record<string, string> = {};
    
    columnsList.forEach((col: string) => {
      const [name, type] = col.split(':').map((s) => s.trim());
      if (name && type) {
        result[name] = type;
      }
    });
    
    return result;
  }

  getSelectedProfileColumns(): Record<string, string>{
    // Convert the selectedProfileLineItemColumns object to an array of { name, type } objects
    return this.selectedProfileLineItemColumns || {};
  }

  getSelectedProfileColumnNames(): string []{
    // Convert the selectedProfileLineItemColumns object to an array of { name, type } objects
    return Object.keys(this.selectedProfileLineItemColumns) || [] ;
  }

  getSelectedProfileColumnsAsArray(): Array<{key: string; value: string}> {
    // Return columns as array maintaining insertion order (not alphabetical)
    // This avoids the keyvalue pipe which sorts alphabetically
    const columns: Array<{key: string; value: string}> = [];
    if (this.selectedProfileLineItemColumns) {
      for (const [key, value] of Object.entries(this.selectedProfileLineItemColumns)) {
        columns.push({key, value});
      }
    }
    return columns;
  }

  getInputTypeForColumn(columnName: string): string {
    // If already contains type info, use it; otherwise infer from name
    if (columnName.includes(':')) {
      const type = columnName.split(':')[1].trim().toLowerCase();
      if (type === 'date') return 'date';
      if (type === 'number') return 'decimal';
      return 'text';
    }
    
    // Fallback to name-based inference
    if (columnName.toLowerCase().includes('date')) {
      return 'date';
    }
    if (['rate', 'hours', 'total'].includes(columnName.toLowerCase())) {
      return 'decimal';
    }
    return 'text';
  }

  getFormControlNameForColumn(columnName: string): string {
    // Extract just the name if it's in "name:type" format
    const name = columnName.includes(':') ? columnName.split(':')[0].trim() : columnName;
    return columnName.charAt(0).toLowerCase() + name.slice(1);
  }

  calculateTotal(lineItem: any): number {
    const hours = lineItem?.get('hours')?.value || 0;
    const rate = lineItem?.get('rate')?.value || 0;
    return Number((hours * rate).toFixed(2));
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

  onBackgroundImageChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedBackgroundImageFile = input.files?.[0] ?? null;
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
      next: (settingsList) => {
        this.settingsList = Array.isArray(settingsList) ? [...settingsList] : [];
        // Use first profile as default if available
        if (this.settingsList.length > 0 && !this.settings) {
          this.settings = this.settingsList[0];
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.settingsList = [];
        this.settings = null;
        this.cdr.markForCheck();
      },
    });
  }

  private loadTemplates(): void {
    this.invoicesApi.listTemplates().subscribe({
      next: (templates) => {
        this.templates = Array.isArray(templates) ? [...templates] : [];
        if (!this.selectedTemplate && this.templates.length > 0) {
          this.selectTemplate(this.templates[0]);
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.templates = [];
        this.cdr.markForCheck();
      },
    });
  }

  selectTemplate(template: InvoiceTemplateSummary): void {
    this.selectedTemplate = template;
    this.selectedEditorIndex = -1;
    this.editorFields = this.getDefaultEditorFields();
    this.templateLayoutMessage = '';
    this.templateBackgroundImagePath = null;
    this.clearTemplatePreview();

    // Load layout if available to restore editor state and background image
    if (template?.versionNumber && template?.layoutJson) {
      try {
        const layout = JSON.parse(template?.layoutJson);
        // Handle PascalCase property names from C# backend
        if (layout.Fields && Array.isArray(layout.Fields)) {
          this.editorFields = layout.Fields.map((field: any) => ({
            name: field.Name,
            type: field.Type,
            x: field.X,
            y: field.Y,
            width: field.Width,
            height: field.Height,
          }));
        }
        if (layout.BackgroundImagePath) {
          this.templateBackgroundImagePath = layout.BackgroundImagePath;
        }
      } catch (e) {
        // Layout parsing failed, use defaults
        console.error('Failed to parse template layout', e);
      }
    }
  }

  navigateToDesign(template: InvoiceTemplateSummary): void {
    this.router.navigate(['/invoices/templates', template.id, 'design']);
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

    // If there's a background image file to upload, upload it first
    if (this.selectedBackgroundImageFile) {
      this.invoicesApi.uploadBackgroundImage(this.selectedBackgroundImageFile).subscribe({
        next: (result) => {
          if (result && result.imagePath) {
            this.saveLayoutWithBackgroundImage(normalizedFields, result.imagePath);
          } else {
            this.savingTemplateLayout = false;
            this.templateLayoutSaved = false;
            this.templateLayoutMessage = 'Background image upload failed.';
          }
        },
        error: () => {
          this.savingTemplateLayout = false;
          this.templateLayoutSaved = false;
          this.templateLayoutMessage = 'Failed to upload background image.';
        },
      });
    } else {
      this.saveLayoutWithBackgroundImage(normalizedFields, undefined);
    }
  }

  private saveLayoutWithBackgroundImage(normalizedFields: any[], backgroundImagePath?: string): void {
    const payload: any = {
      schemaVersion: 1,
      name: this.selectedTemplate!.name,
      fields: normalizedFields,
    };

    if (backgroundImagePath) {
      payload.backgroundImagePath = backgroundImagePath;
    }

    this.invoicesApi.saveTemplateLayout(this.selectedTemplate!.id, payload).subscribe({
      next: (result) => {
        this.savingTemplateLayout = false;
        if (!result) {
          this.templateLayoutSaved = false;
          this.templateLayoutMessage = 'Template layout could not be saved. Check the backend validation.';
          return;
        }

        this.templateLayoutSaved = true;
        this.templateLayoutMessage = `Layout saved to template version ${result.versionNumber ?? 1}.`;
        this.selectedBackgroundImageFile = null;
        const input = document.querySelector<HTMLInputElement>('input[accept="image/png,image/jpeg"]');
        if (input) {
          input.value = '';
        }
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

  deleteInvoice(invoiceId: string): void {
    if (!confirm('Are you sure you want to delete this invoice? This action cannot be undone.')) {
      return;
    }

    this.invoicesApi.deleteInvoice(invoiceId).subscribe({
      next: () => {
        this.actionMessage = 'Invoice deleted successfully.';
        this.selectedInvoice = null;
        this.loadInvoices();
      },
      error: () => {
        this.actionMessage = 'Failed to delete invoice. It may have been generated or archived.';
      },
    });
  }

  // Profile Management Methods
  showProfileForm = false;
  editingProfileId: string | null = null;
  profileForm: any;
  profileFormError = '';
  profileFormSuccess = '';
  savingProfile = false;

  loadProfiles(): void {
    this.invoicesApi.listProfiles().subscribe({
      next: (profiles) => {
        this.profiles = profiles;
        this.cdr.markForCheck();
      },
      error: () => {
        this.profiles = [];
        this.cdr.markForCheck();
      },
    });
    
  }

  openProfileForm(profile?: BusinessProfileDetail): void {
    this.editingProfileId = profile?.id || null;
    this.showProfileForm = true;
    this.profileFormError = '';
    this.profileFormSuccess = '';

    if (!this.profileForm) {
      this.profileForm = this.fb.nonNullable.group({
        issuerName: ['', Validators.required],
        issuerAddress: '',
        issuerEmail: ['', Validators.email],
        issuerPhone: '',
        issuerRegistrationNumber: '',
        issuerTaxNumber: '',
        defaultCurrencyCode: 'USD',
        invoicePrefix: 'INV',
        defaultTaxRate: 0,
        taxLabel: 'Tax',
        defaultTerms: '',
        lineItemColumns: this.fb.nonNullable.array([]),
      });
    }

    if (profile) {
      const lineColumnsArray = this.profileForm.get('lineItemColumns') as any;
      lineColumnsArray.clear();
      
      if (this.selectedProfileLineItemColumns && typeof this.selectedProfileLineItemColumns === 'object') {
        // selectedProfileLineItemColumns is Record<string, string> where key=name, value=type
        Object.entries(this.selectedProfileLineItemColumns).forEach(([name, type]) => {
          lineColumnsArray.push(this.fb.nonNullable.group({
            name: [name.trim(), Validators.required],
            type: [type?.trim() || 'Text', Validators.required],
          }));
        });
      }
      
      this.profileForm.patchValue({
        issuerName: profile.issuerName,
        issuerAddress: profile.issuerAddress,
        issuerEmail: profile.issuerEmail,
        issuerPhone: profile.issuerPhone,
        issuerRegistrationNumber: profile.issuerRegistrationNumber,
        issuerTaxNumber: profile.issuerTaxNumber,
        defaultCurrencyCode: profile.defaultCurrencyCode,
        invoicePrefix: profile.invoicePrefix,
        defaultTaxRate: profile.defaultTaxRate,
        taxLabel: profile.taxLabel,
        defaultTerms: profile.defaultTerms,
      });
    } else {
      const lineColumnsArray = this.profileForm.get('lineItemColumns') as any;
      lineColumnsArray.clear();
      
      this.profileForm.reset({
        defaultCurrencyCode: 'USD',
        invoicePrefix: 'INV',
        defaultTaxRate: 0,
        taxLabel: 'Tax',
        lineItemColumns: [],
      });
    }

    this.cdr.markForCheck();
  }

  closeProfileForm(): void {
    this.showProfileForm = false;
    this.editingProfileId = null;
    this.profileFormError = '';
    this.profileFormSuccess = '';
  }

  saveProfile(): void {
    if (!this.profileForm || this.profileForm.invalid) {
      this.profileFormError = 'Please fill in all required fields.';
      return;
    }

    this.savingProfile = true;
    const profileData = this.profileForm.getRawValue();
    
    // Transform lineItemColumns from array of objects to "name:type, name:type" format string
    if (profileData.lineItemColumns && Array.isArray(profileData.lineItemColumns)) {
      profileData.lineItemColumns = profileData.lineItemColumns
        .map((col: any) => `${col.name}:${col.type}`)
        .join(', ');
    } else {
      profileData.lineItemColumns = '';
    }

    if (this.editingProfileId) {
      this.invoicesApi.updateProfile(this.editingProfileId, profileData).subscribe({
        next: () => {
          this.profileFormSuccess = 'Profile updated successfully.';
          this.savingProfile = false;
          this.closeProfileForm();
          this.loadProfiles();
        },
        error: (err) => {
          this.profileFormError = err?.message || 'Failed to update profile.';
          this.savingProfile = false;
        },
      });
    } else {
      this.invoicesApi.createProfile(profileData).subscribe({
        next: () => {
          this.profileFormSuccess = 'Profile created successfully.';
          this.savingProfile = false;
          this.closeProfileForm();
          this.loadProfiles();
        },
        error: (err) => {
          this.profileFormError = err?.message || 'Failed to create profile.';
          this.savingProfile = false;
        },
      });
    }
  }

  selectProfileForEdit(profileId: any): void {
    this.selectedProfileForEdit = null;
    
    // Fetch full profile details from the API
    this.invoicesApi.getProfile(profileId).subscribe({
      next: (profileDetail) => {
        if (profileDetail) {
          this.selectedProfileForEdit = profileDetail;
          this.selectedProfileLineItemColumns = this.convertLineItemColumnsToKeyValuePair(this.selectedProfileForEdit.lineItemColumns) || {};
        

          this.openProfileForm(profileDetail);
        } else {
          this.profileFormError = 'Failed to load profile details.';
        }
      },
      error: () => {
        this.profileFormError = 'Unable to fetch profile details. Please try again.';
      },
    });
  }

  selectProfileForCreation(): void {
    this.openProfileForm();
  }

  deleteProfile(profileId: string, profileName: string): void {
    if (!confirm(`Are you sure you want to delete the profile "${profileName}"? This action cannot be undone.`)) {
      return;
    }

    this.invoicesApi.deleteProfile(profileId).subscribe({
      next: () => {
        this.profileFormSuccess = `Profile "${profileName}" deleted successfully.`;
        this.loadProfiles();
      },
      error: (err) => {
        this.profileFormError = err?.message || `Failed to delete profile. It may have associated invoices.`;
      },
    });
  }

  get lineItems() {
    return this.draftForm.get('lineItems') as any;
  }

  get profileLineItemColumns() {
    var columnsArray = this.profileForm?.get('lineItemColumns') as any;
    return columnsArray;
  }

  addLineItemColumn(): void {
    const columnsArray = this.profileLineItemColumns;
    if (columnsArray) {
      columnsArray.push(this.fb.nonNullable.group({
        name: ['', Validators.required],
        type: ['Text', Validators.required],
      }));
    }
  }

  removeLineItemColumn(index: number): void {
    const columnsArray = this.profileLineItemColumns;
    if (columnsArray && columnsArray.length > 0) {
      columnsArray.removeAt(index);
    }
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
    const group: Record<string, any> = {};

    if (!this.selectedProfileLineItemColumns || Object.keys(this.selectedProfileLineItemColumns).length === 0) {
      // Fallback to default controls if no profile columns
      group['description'] = ['', Validators.required];
      group['quantity'] = [1, [Validators.required, Validators.min(1)]];
      group['unitPrice'] = [0, [Validators.required, Validators.min(0)]];
      group['discountPercent'] = [0, [Validators.min(0)]];
      group['taxRate'] = [0, [Validators.min(0)]];
      return this.fb.nonNullable.group(group);
    }

    // Create form controls for each column in the order they appear in the Record
    for (const [key, type] of Object.entries(this.selectedProfileLineItemColumns)) {
      const controlName = this.getFormControlNameForColumn(key);
      
      if (key.toLowerCase() === 'total') {
        // Total is calculated, so make it optional
        group[controlName] = [0, [Validators.min(0)]];
      } else if (type.toLowerCase() === 'number') {
        group[controlName] = [0, [Validators.required, Validators.min(0)]];
      } else if (type.toLowerCase() === 'date') {
        group[controlName] = [''];
      } else {
        group[controlName] = ['', Validators.required];
      }
    }
    
    return this.fb.nonNullable.group(group);
  }
}
