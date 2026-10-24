import { CommonModule, DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoicesApiService } from '../../services';
import { CustomerProfileDetail, CustomerProfileSummary, InvoiceTemplateSummary, LineItemColumnConfig, TemplateFieldBinding, TemplateLayoutDocument } from '../../models';

interface ContentOption {
  contentSource: string;
  label: string;
  icon: string;
  type: string;
  defaultWidth: number;
  defaultHeight: number;
  defaultX: number;
  defaultY: number;
}

const CONTENT_OPTIONS: ContentOption[] = [
  { contentSource: 'invoiceNumber', label: 'Invoice No.', icon: '#', type: 'Text', defaultWidth: 55, defaultHeight: 9, defaultX: 130, defaultY: 15 },
  { contentSource: 'issueDate', label: 'Issue Date', icon: '📅', type: 'Date', defaultWidth: 50, defaultHeight: 8, defaultX: 130, defaultY: 30 },
  { contentSource: 'dueDate', label: 'Due Date', icon: '⏰', type: 'Date', defaultWidth: 50, defaultHeight: 8, defaultX: 130, defaultY: 42 },
  { contentSource: 'customerName', label: 'Customer Name', icon: '👤', type: 'Text', defaultWidth: 80, defaultHeight: 9, defaultX: 10, defaultY: 55 },
  { contentSource: 'customerAddress', label: 'Customer Address', icon: '📍', type: 'Address', defaultWidth: 80, defaultHeight: 22, defaultX: 10, defaultY: 68 },
  { contentSource: 'customerEmail', label: 'Customer Email', icon: '✉', type: 'Text', defaultWidth: 80, defaultHeight: 8, defaultX: 10, defaultY: 93 },
  { contentSource: 'grandTotal', label: 'Grand Total', icon: '💰', type: 'Currency', defaultWidth: 55, defaultHeight: 10, defaultX: 140, defaultY: 250 },
  { contentSource: 'subtotal', label: 'Subtotal', icon: '∑', type: 'Currency', defaultWidth: 55, defaultHeight: 8, defaultX: 140, defaultY: 235 },
  { contentSource: 'lineItems', label: 'Line Items Table', icon: '▤', type: 'Table', defaultWidth: 180, defaultHeight: 110, defaultX: 15, defaultY: 105 },
  { contentSource: 'custom', label: 'Custom Text', icon: 'T', type: 'Text', defaultWidth: 60, defaultHeight: 9, defaultX: 10, defaultY: 10 },
];

@Component({
  selector: 'app-invoices-template-design',
  standalone: true,
  imports: [CommonModule, DecimalPipe, FormsModule],
  templateUrl: './invoices-template-design.component.html',
  styleUrl: './invoices-template-design.component.scss',
})
export class InvoicesTemplateDesignComponent implements OnInit, OnDestroy {
  template: InvoiceTemplateSummary | null = null;
  templateId: string = '';
  loading = true;
  errorMessage = '';
  successMessage = '';
  savingLayout = false;

  // Canvas (A4 in mm)
  canvasWidth = 210;
  canvasHeight = 297;
  pixelsPerMm = 3.78;

  // Background
  backgroundImageFile: File | null = null;
  backgroundImagePreview: string | null = null;
  backgroundImagePath: string | null = null;
  backgroundUploadError = '';

  // Fields
  fields: TemplateFieldBinding[] = [];
  selectedFieldIndex: number = -1;

  // Content palette
  readonly contentOptions = CONTENT_OPTIONS;

  // Profiles
  profiles: CustomerProfileSummary[] = [];
  selectedProfileId = '';
  selectedProfile: CustomerProfileDetail | null = null;

  // Drag state
  private dragState: {
    fieldIndex: number;
    mode: 'move' | 'resize';
    startX: number;
    startY: number;
    originX: number;
    originY: number;
    originWidth: number;
    originHeight: number;
  } | null = null;

  constructor(
    private readonly invoicesApi: InvoicesApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.templateId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.templateId) {
      this.errorMessage = 'Template ID is required.';
      this.loading = false;
      return;
    }
    this.loadProfiles();
    this.loadTemplate();
  }

  // ─── Profiles ────────────────────────────────────────────────────────────

  private loadProfiles(): void {
    this.invoicesApi.listProfiles().subscribe({
      next: (profiles) => {
        this.profiles = profiles;
        this.cdr.markForCheck();
      },
    });
  }

  onProfileSelected(profileId: string): void {
    this.selectedProfileId = profileId;
    if (!profileId) {
      this.selectedProfile = null;
      this.cdr.markForCheck();
      return;
    }
    this.invoicesApi.getProfile(profileId).subscribe({
      next: (profile) => {
        this.selectedProfile = profile;
        this.cdr.markForCheck();
      },
    });
  }

  // ─── Template loading ────────────────────────────────────────────────────

  private loadTemplate(): void {
    this.invoicesApi.listTemplates().subscribe({
      next: (templates) => {
        this.template = templates.find((t) => t.id === this.templateId) || null;
        if (!this.template) {
          this.errorMessage = 'Template not found.';
          this.loading = false;
          return;
        }
        this.loadTemplateLayout();
      },
      error: () => {
        this.errorMessage = 'Failed to load template.';
        this.loading = false;
      },
    });
  }

  private loadTemplateLayout(): void {
    this.fields = this.getDefaultFields();

    if (this.template?.layoutJson) {
      try {
        const layout = JSON.parse(this.template.layoutJson);
        if (layout.Fields && Array.isArray(layout.Fields)) {
          this.fields = layout.Fields.map((f: any): TemplateFieldBinding => ({
            name: f.Name ?? '',
            type: f.Type ?? 'Text',
            x: f.X ?? 0,
            y: f.Y ?? 0,
            width: f.Width ?? 60,
            height: f.Height ?? 10,
            contentSource: f.ContentSource ?? '',
            customText: f.CustomText ?? '',
            fontSize: f.FontSize ?? 12,
            fontStyle: f.FontStyle ?? 'normal',
            textColor: f.TextColor ?? '#000000',
            backgroundColor: f.BackgroundColor ?? '',
            borderSize: f.BorderSize ?? 0,
            lineItemColumns: Array.isArray(f.LineItemColumns)
              ? f.LineItemColumns.map((c: any): LineItemColumnConfig => ({
                  key: c.Key ?? '',
                  label: c.Label ?? '',
                  widthMm: c.WidthMm ?? 40,
                  type: c.Type ?? 'text',
                }))
              : [],
          }));
        }
        if (layout.BackgroundImagePath) {
          this.backgroundImagePath = layout.BackgroundImagePath;
          const fileName = layout.BackgroundImagePath.split('/').pop();
          if (fileName) {
            this.invoicesApi.getBackgroundImageAsBase64(fileName).subscribe({
              next: (result) => {
                if (result?.imageBase64) {
                  this.backgroundImagePreview = result.imageBase64;
                  this.cdr.markForCheck();
                }
              },
            });
          }
        }
      } catch {
        // Use defaults
      }
    }

    this.loading = false;
    this.cdr.markForCheck();
  }

  private getDefaultFields(): TemplateFieldBinding[] {
    return [
      { name: 'invoiceNumber', type: 'Text', contentSource: 'invoiceNumber', x: 130, y: 15, width: 55, height: 9, fontSize: 22, fontStyle: 'bold', textColor: '#0f172a', backgroundColor: '', borderSize: 0 },
      { name: 'issueDate', type: 'Date', contentSource: 'issueDate', x: 130, y: 30, width: 50, height: 8, fontSize: 12, fontStyle: 'normal', textColor: '#000000', backgroundColor: '', borderSize: 0 },
      { name: 'customerName', type: 'Text', contentSource: 'customerName', x: 10, y: 55, width: 80, height: 9, fontSize: 14, fontStyle: 'bold', textColor: '#0f172a', backgroundColor: '', borderSize: 0 },
      { name: 'customerAddress', type: 'Address', contentSource: 'customerAddress', x: 10, y: 68, width: 80, height: 22, fontSize: 12, fontStyle: 'normal', textColor: '#334155', backgroundColor: '', borderSize: 0 },
      { name: 'lineItems', type: 'Table', contentSource: 'lineItems', x: 15, y: 105, width: 180, height: 110, fontSize: 12, fontStyle: 'normal', textColor: '#475569', backgroundColor: '#f1f5f9', borderSize: 0, lineItemColumns: [] },
      { name: 'grandTotal', type: 'Currency', contentSource: 'grandTotal', x: 140, y: 250, width: 55, height: 10, fontSize: 14, fontStyle: 'bold', textColor: '#0f172a', backgroundColor: '', borderSize: 0 },
    ];
  }

  // ─── Selected field accessor ─────────────────────────────────────────────

  get selectedField(): TemplateFieldBinding | null {
    return this.selectedFieldIndex >= 0 ? this.fields[this.selectedFieldIndex] ?? null : null;
  }

  // ─── Content palette ─────────────────────────────────────────────────────

  addFieldWithContent(option: ContentOption): void {
    const existing = this.fields.find((f) => f.contentSource === option.contentSource);
    if (existing && option.contentSource !== 'custom') {
      this.selectedFieldIndex = this.fields.indexOf(existing);
      this.cdr.markForCheck();
      return;
    }
    const field: TemplateFieldBinding = {
      name: option.contentSource === 'custom' ? `custom_${this.fields.length}` : option.contentSource,
      type: option.type,
      contentSource: option.contentSource,
      x: option.defaultX,
      y: option.defaultY,
      width: option.defaultWidth,
      height: option.defaultHeight,
      fontSize: option.type === 'Table' ? 12 : 12,
      fontStyle: 'normal',
      textColor: '#000000',
      backgroundColor: option.type === 'Table' ? '#f1f5f9' : '',
      borderSize: 0,
      customText: '',
      lineItemColumns: option.type === 'Table' ? [] : undefined,
    };
    this.fields.push(field);
    this.selectedFieldIndex = this.fields.length - 1;
    this.cdr.markForCheck();
  }

  addCustomField(): void {
    const option = CONTENT_OPTIONS.find((o) => o.contentSource === 'custom')!;
    this.addFieldWithContent(option);
  }

  removeField(index: number): void {
    this.fields.splice(index, 1);
    if (this.selectedFieldIndex === index) {
      this.selectedFieldIndex = -1;
    } else if (this.selectedFieldIndex > index) {
      this.selectedFieldIndex--;
    }
    this.cdr.markForCheck();
  }

  selectField(index: number): void {
    this.selectedFieldIndex = this.selectedFieldIndex === index ? -1 : index;
  }

  updateFieldProperty(index: number, property: string, value: any): void {
    if (index >= 0 && index < this.fields.length) {
      (this.fields[index] as any)[property] = value;
      this.cdr.markForCheck();
    }
  }

  getContentLabel(contentSource?: string): string {
    return CONTENT_OPTIONS.find((o) => o.contentSource === contentSource)?.label ?? contentSource ?? '';
  }

  // ─── Column management ────────────────────────────────────────────────────

  importColumnsFromProfile(): void {
    if (!this.selectedField || !this.selectedProfile?.lineItemColumns) {
      return;
    }
    const raw = this.selectedProfile.lineItemColumns;
    const cols: LineItemColumnConfig[] = raw
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0)
      .map((segment) => {
        const [key, type] = segment.split(':').map((p) => p.trim());
        const label = key ? key.charAt(0).toUpperCase() + key.slice(1) : '';
        return { key: key || '', label, widthMm: 40, type: type || 'text' };
      });

    if (!this.selectedField.lineItemColumns) {
      this.selectedField.lineItemColumns = [];
    }
    this.selectedField.lineItemColumns = cols;
    this.cdr.markForCheck();
  }

  addColumn(): void {
    if (!this.selectedField) return;
    if (!this.selectedField.lineItemColumns) this.selectedField.lineItemColumns = [];
    this.selectedField.lineItemColumns.push({ key: '', label: 'Column', widthMm: 40, type: 'text' });
    this.cdr.markForCheck();
  }

  removeColumn(index: number): void {
    this.selectedField?.lineItemColumns?.splice(index, 1);
    this.cdr.markForCheck();
  }

  // ─── Background ──────────────────────────────────────────────────────────

  onBackgroundImageChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.backgroundUploadError = 'Please select a valid image file.';
      return;
    }
    this.backgroundImageFile = file;
    this.backgroundUploadError = '';
    const reader = new FileReader();
    reader.onload = (e) => {
      this.backgroundImagePreview = e.target?.result as string;
      this.cdr.markForCheck();
    };
    reader.readAsDataURL(file);
  }

  clearBackground(): void {
    this.backgroundImageFile = null;
    this.backgroundImagePreview = null;
    this.backgroundImagePath = null;
    this.backgroundUploadError = '';
  }

  // ─── Save layout ─────────────────────────────────────────────────────────

  saveLayout(): void {
    if (!this.template) {
      this.errorMessage = 'No template selected.';
      return;
    }
    this.savingLayout = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.backgroundImageFile) {
      this.invoicesApi.uploadBackgroundImage(this.backgroundImageFile).subscribe({
        next: (result) => {
          if (result?.imagePath) {
            this.backgroundImagePath = result.imagePath;
            this.doSaveLayout();
          } else {
            this.savingLayout = false;
            this.errorMessage = 'Failed to upload background image.';
            this.cdr.markForCheck();
          }
        },
        error: () => {
          this.savingLayout = false;
          this.errorMessage = 'Failed to upload background image. Please try again.';
          this.cdr.markForCheck();
        },
      });
    } else {
      this.doSaveLayout();
    }
  }

  private doSaveLayout(): void {
    if (!this.template) return;

    const layout: TemplateLayoutDocument = {
      schemaVersion: 2,
      name: this.template.name,
      fields: this.fields,
      backgroundImagePath: this.backgroundImagePath || undefined,
    };

    this.invoicesApi.saveTemplateLayout(this.template.id, layout).subscribe({
      next: (result) => {
        this.savingLayout = false;
        if (!result) {
          this.errorMessage = 'Failed to save template layout.';
          this.cdr.markForCheck();
          return;
        }
        this.successMessage = 'Template layout saved successfully!';
        this.backgroundImageFile = null;
        this.cdr.markForCheck();
        setTimeout(() => this.router.navigate(['/invoices']), 2000);
      },
      error: () => {
        this.savingLayout = false;
        this.errorMessage = 'Failed to save template layout. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  getTotalColumnWidth(): number {
    return this.selectedField?.lineItemColumns?.reduce((s, c) => s + (c.widthMm ?? 0), 0) ?? 0;
  }

  // ─── Canvas helpers ──────────────────────────────────────────────────────

  getCanvasPixelWidth(): number { return this.canvasWidth * this.pixelsPerMm; }
  getCanvasPixelHeight(): number { return this.canvasHeight * this.pixelsPerMm; }
  getFieldPixelX(f: TemplateFieldBinding): number { return (f.x ?? 0) * this.pixelsPerMm; }
  getFieldPixelY(f: TemplateFieldBinding): number { return (f.y ?? 0) * this.pixelsPerMm; }
  getFieldPixelWidth(f: TemplateFieldBinding): number { return (f.width ?? 60) * this.pixelsPerMm; }
  getFieldPixelHeight(f: TemplateFieldBinding): number { return (f.height ?? 10) * this.pixelsPerMm; }

  getFieldPreviewStyle(f: TemplateFieldBinding): Record<string, string> {
    const style: Record<string, string> = {};
    if (f.fontSize) style['fontSize'] = `${f.fontSize * 0.7}px`;
    if (f.fontStyle === 'bold') { style['fontWeight'] = 'bold'; }
    if (f.fontStyle === 'italic') { style['fontStyle'] = 'italic'; }
    if (f.fontStyle === 'bold italic') { style['fontWeight'] = 'bold'; style['fontStyle'] = 'italic'; }
    if (f.textColor) style['color'] = f.textColor;
    if (f.backgroundColor) style['backgroundColor'] = f.backgroundColor;
    if (f.borderSize) style['border'] = `${f.borderSize}px solid currentColor`;
    return style;
  }

  getFieldLabel(f: TemplateFieldBinding): string {
    if (f.contentSource === 'custom') return f.customText || 'Custom Text';
    return this.getContentLabel(f.contentSource) || f.name;
  }

  // ─── Drag & resize ───────────────────────────────────────────────────────

  @HostListener('document:pointermove', ['$event'])
  onPointerMove(event: PointerEvent): void {
    if (!this.dragState) return;
    const field = this.fields[this.dragState.fieldIndex];
    if (!field) return;

    const dX = (event.clientX - this.dragState.startX) / this.pixelsPerMm;
    const dY = (event.clientY - this.dragState.startY) / this.pixelsPerMm;

    if (this.dragState.mode === 'move') {
      field.x = Math.max(0, Math.min(this.canvasWidth - (field.width ?? 60), this.dragState.originX + dX));
      field.y = Math.max(0, Math.min(this.canvasHeight - (field.height ?? 10), this.dragState.originY + dY));
    } else {
      field.width = Math.max(15, this.dragState.originWidth + dX);
      field.height = Math.max(6, this.dragState.originHeight + dY);
    }
    this.cdr.markForCheck();
  }

  @HostListener('document:pointerup')
  onPointerUp(): void {
    this.dragState = null;
  }

  startFieldDrag(event: PointerEvent, index: number, mode: 'move' | 'resize'): void {
    event.preventDefault();
    event.stopPropagation();
    const field = this.fields[index];
    if (!field) return;
    this.dragState = {
      fieldIndex: index,
      mode,
      startX: event.clientX,
      startY: event.clientY,
      originX: field.x ?? 0,
      originY: field.y ?? 0,
      originWidth: field.width ?? 60,
      originHeight: field.height ?? 10,
    };
    this.selectedFieldIndex = index;
  }

  goBack(): void {
    this.router.navigate(['/invoices']);
  }

  ngOnDestroy(): void {
    this.dragState = null;
  }
}
