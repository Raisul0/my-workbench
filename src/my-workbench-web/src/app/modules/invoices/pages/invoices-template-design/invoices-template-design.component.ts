import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoicesApiService } from '../../services';
import { InvoiceTemplateSummary, TemplateFieldBinding, TemplateLayoutDocument } from '../../models';

interface LayoutZone {
  id: string;
  name: string;
  description: string;
  suggestedX: number;
  suggestedY: number;
  suggestedWidth: number;
  suggestedHeight: number;
}

@Component({
  selector: 'app-invoices-template-design',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  // Canvas properties
  canvasWidth = 210; // A4 width in mm
  canvasHeight = 297; // A4 height in mm
  pixelsPerMm = 3.78; // 96 DPI / 25.4 mm per inch

  // Background
  backgroundImageFile: File | null = null;
  backgroundImagePreview: string | null = null;
  backgroundImagePath: string | null = null;
  backgroundUploadError = '';

  // Field management
  fields: TemplateFieldBinding[] = [];
  selectedFieldIndex: number = -1;
  fieldTypes = ['Text', 'Number', 'Date', 'Currency', 'Address', 'Table', 'Image'];

  // Layout zones
  suggestedZones: LayoutZone[] = [
    {
      id: 'header',
      name: 'Header',
      description: 'Company name, logo, contact info',
      suggestedX: 10,
      suggestedY: 10,
      suggestedWidth: 190,
      suggestedHeight: 30,
    },
    {
      id: 'customerInfo',
      name: 'Customer Info',
      description: 'Billing address, email, phone',
      suggestedX: 10,
      suggestedY: 45,
      suggestedWidth: 190,
      suggestedHeight: 25,
    },
    {
      id: 'lineItems',
      name: 'Line Items',
      description: 'Invoice line items table',
      suggestedX: 10,
      suggestedY: 75,
      suggestedWidth: 190,
      suggestedHeight: 120,
    },
    {
      id: 'summary',
      name: 'Summary',
      description: 'Subtotal, tax, grand total',
      suggestedX: 130,
      suggestedY: 200,
      suggestedWidth: 70,
      suggestedHeight: 30,
    },
    {
      id: 'footer',
      name: 'Footer',
      description: 'Page number, terms, notes',
      suggestedX: 10,
      suggestedY: 240,
      suggestedWidth: 190,
      suggestedHeight: 30,
    },
  ];

  // Drag & resize state
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

    this.loadTemplate();
  }

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
    // Initialize with default fields or load from backend
    this.fields = [
      { name: 'companyName', type: 'Text', x: 15, y: 15, width: 60, height: 8 },
      { name: 'billingAddress', type: 'Address', x: 15, y: 50, width: 80, height: 20 },
      { name: 'billingEmail', type: 'Text', x: 15, y: 72, width: 80, height: 5 },
      { name: 'lineItems', type: 'Table', x: 15, y: 85, width: 170, height: 100 },
      { name: 'grandTotal', type: 'Currency', x: 140, y: 190, width: 45, height: 8 },
      { name: 'pageNumber', type: 'Number', x: 190, y: 280, width: 15, height: 5 },
    ];

    // Load saved layout if available
    if (this.template?.layoutJson) {
      try {
        console.log('Parsing layout JSON:', this.template.layoutJson);
        const layout = JSON.parse(this.template.layoutJson);
        console.log('Parsed layout:', layout);
        
        // Handle PascalCase property names from C# backend
        if (layout.Fields && Array.isArray(layout.Fields)) {
          this.fields = layout.Fields.map((field: any) => ({
            name: field.Name,
            type: field.Type,
            x: field.X,
            y: field.Y,
            width: field.Width,
            height: field.Height,
          }));
        }
        if (layout.BackgroundImagePath) {
          console.log('Setting background image path:', layout.BackgroundImagePath);
          this.backgroundImagePath = layout.BackgroundImagePath;
          
          // Extract the file name from the path
          const fileName = layout.BackgroundImagePath.split('/').pop();
          if (fileName) {
            // Get the image as base64 from the API
            this.invoicesApi.getBackgroundImageAsBase64(fileName).subscribe({
              next: (result) => {
                if (result && result.imageBase64) {
                  this.backgroundImagePreview = result.imageBase64;
                  console.log('Background image loaded as base64');
                  this.cdr.markForCheck();
                }
              },
              error: (err) => {
                console.error('Failed to load background image:', err);
              }
            });
          }
        } else {
          console.log('No BackgroundImagePath in layout');
        }
      } catch (e) {
        // Layout parsing failed, use defaults
        console.error('Failed to parse template layout', e);
      }
    } else {
      console.log('No layoutJson in template');
    }

    this.loading = false;
    this.cdr.markForCheck();
  }

  private getApiBaseUrl(): string {
    const location = window.location;
    
    // If already on API origin (has :5109, :5001, etc.), use it directly
    if (location.port === '5109' || location.port === '5001' || location.port === '5000') {
      return `${location.protocol}//${location.hostname}:${location.port}`;
    }
    
    // Default to localhost:5109 for development
    return 'http://localhost:5109';
  }

  onBackgroundImageChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.backgroundUploadError = 'Please select a valid image file.';
      return;
    }

    this.backgroundImageFile = file;
    this.backgroundUploadError = '';

    // Create preview
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

  addFieldFromZone(zone: LayoutZone): void {
    const newField: TemplateFieldBinding = {
      name: `${zone.id}_${this.fields.length}`,
      type: this.getDefaultTypeForZone(zone.id),
      x: zone.suggestedX,
      y: zone.suggestedY,
      width: zone.suggestedWidth,
      height: zone.suggestedHeight,
    };
    this.fields.push(newField);
    this.selectedFieldIndex = this.fields.length - 1;
    this.cdr.markForCheck();
  }

  addField(): void {
    const newField: TemplateFieldBinding = {
      name: `field_${this.fields.length}`,
      type: 'Text',
      x: 20,
      y: 20 + this.fields.length * 30,
      width: 60,
      height: 10,
    };
    this.fields.push(newField);
    this.selectedFieldIndex = this.fields.length - 1;
    this.cdr.markForCheck();
  }

  removeField(index: number): void {
    this.fields.splice(index, 1);
    if (this.selectedFieldIndex === index) {
      this.selectedFieldIndex = -1;
    }
    this.cdr.markForCheck();
  }

  selectField(index: number): void {
    this.selectedFieldIndex = this.selectedFieldIndex === index ? -1 : index;
  }

  updateFieldProperty(index: number, property: string, value: any): void {
    if (index >= 0 && index < this.fields.length) {
      const field = this.fields[index];
      (field as any)[property] = value;
      this.cdr.markForCheck();
    }
  }

  @HostListener('document:pointermove', ['$event'])
  onPointerMove(event: PointerEvent): void {
    if (!this.dragState) {
      return;
    }

    const deltaX = (event.clientX - this.dragState.startX) / this.pixelsPerMm;
    const deltaY = (event.clientY - this.dragState.startY) / this.pixelsPerMm;

    const field = this.fields[this.dragState.fieldIndex];
    if (!field) {
      return;
    }

    if (this.dragState.mode === 'move') {
      field.x = Math.max(0, this.dragState.originX + deltaX);
      field.y = Math.max(0, this.dragState.originY + deltaY);
    } else if (this.dragState.mode === 'resize') {
      field.width = Math.max(20, this.dragState.originWidth + deltaX);
      field.height = Math.max(10, this.dragState.originHeight + deltaY);
    }

    this.cdr.markForCheck();
  }

  @HostListener('document:pointerup')
  onPointerUp(): void {
    this.dragState = null;
  }

  startFieldDrag(
    event: PointerEvent,
    index: number,
    mode: 'move' | 'resize'
  ): void {
    event.preventDefault();
    event.stopPropagation();

    const field = this.fields[index];
    if (!field) {
      return;
    }

    this.dragState = {
      fieldIndex: index,
      mode,
      startX: event.clientX,
      startY: event.clientY,
      originX: field.x || 0,
      originY: field.y || 0,
      originWidth: field.width || 60,
      originHeight: field.height || 10,
    };

    this.selectedFieldIndex = index;
  }

  saveLayout(): void {
    if (!this.template) {
      this.errorMessage = 'No template selected.';
      return;
    }

    this.savingLayout = true;
    this.errorMessage = '';
    this.successMessage = '';

    // If there's a new background image file, upload it first
    if (this.backgroundImageFile) {
      this.invoicesApi.uploadBackgroundImage(this.backgroundImageFile).subscribe({
        next: (result) => {
          if (result && result.imagePath) {
            this.backgroundImagePath = result.imagePath;
            this.saveLayoutWithBackgroundPath();
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
      // No new image, save layout directly with existing path
      this.saveLayoutWithBackgroundPath();
    }
  }

  private saveLayoutWithBackgroundPath(): void {
    if (!this.template) {
      this.errorMessage = 'No template selected.';
      this.savingLayout = false;
      return;
    }

    const layout: TemplateLayoutDocument = {
      schemaVersion: 1,
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

        // Redirect after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/invoices']);
        }, 2000);
      },
      error: () => {
        this.savingLayout = false;
        this.errorMessage = 'Failed to save template layout. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/invoices']);
  }

  private getDefaultTypeForZone(zoneId: string): string {
    switch (zoneId) {
      case 'header':
        return 'Text';
      case 'customerInfo':
        return 'Address';
      case 'lineItems':
        return 'Table';
      case 'summary':
        return 'Currency';
      case 'footer':
        return 'Text';
      default:
        return 'Text';
    }
  }

  getCanvasPixelWidth(): number {
    return this.canvasWidth * this.pixelsPerMm;
  }

  getCanvasPixelHeight(): number {
    return this.canvasHeight * this.pixelsPerMm;
  }

  getFieldPixelX(field: TemplateFieldBinding): number {
    return (field.x || 0) * this.pixelsPerMm;
  }

  getFieldPixelY(field: TemplateFieldBinding): number {
    return (field.y || 0) * this.pixelsPerMm;
  }

  getFieldPixelWidth(field: TemplateFieldBinding): number {
    return (field.width || 60) * this.pixelsPerMm;
  }

  getFieldPixelHeight(field: TemplateFieldBinding): number {
    return (field.height || 10) * this.pixelsPerMm;
  }

  ngOnDestroy(): void {
    this.dragState = null;
  }
}
