import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InvoicesTemplateDesignComponent } from './invoices-template-design.component';
import { ActivatedRoute, Router } from '@angular/router';
import { InvoicesApiService } from '../../services';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('InvoicesTemplateDesignComponent', () => {
  let component: InvoicesTemplateDesignComponent;
  let fixture: ComponentFixture<InvoicesTemplateDesignComponent>;
  let mockInvoicesApi: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockInvoicesApi = {
      listTemplates: vi.fn(),
      saveTemplateLayout: vi.fn(),
      previewTemplate: vi.fn(),
    };

    mockRouter = {
      navigate: vi.fn(),
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: (key: string) => (key === 'id' ? 'test-template-id' : null),
        },
      },
    };

    await TestBed.configureTestingModule({
      imports: [InvoicesTemplateDesignComponent],
      providers: [
        { provide: InvoicesApiService, useValue: mockInvoicesApi },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
      ],
    }).compileComponents();

    mockInvoicesApi.listTemplates.mockReturnValue(
      of([
        {
          id: 'test-template-id',
          name: 'Test Template',
          description: 'A test template',
        } as any,
      ])
    );

    fixture = TestBed.createComponent(InvoicesTemplateDesignComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load template on init', () => {
    fixture.detectChanges();
    expect(component.template?.name).toBe('Test Template');
    expect(component.loading).toBeFalsy();
  });

  it('should initialize with default fields', () => {
    fixture.detectChanges();
    expect(component.fields.length).toBeGreaterThan(0);
    expect(component.fields.some((f) => f.name === 'companyName')).toBeTruthy();
    expect(component.fields.some((f) => f.name === 'pageNumber')).toBeTruthy();
  });

  it('should add field from suggested zone', () => {
    fixture.detectChanges();
    const initialCount = component.fields.length;
    const zone = component.suggestedZones[0];

    component.addFieldFromZone(zone);

    expect(component.fields.length).toBe(initialCount + 1);
    expect(component.fields[initialCount].name).toContain(zone.id);
  });

  it('should add custom field', () => {
    fixture.detectChanges();
    const initialCount = component.fields.length;

    component.addField();

    expect(component.fields.length).toBe(initialCount + 1);
    expect(component.fields[initialCount].name).toContain('field_');
  });

  it('should remove field', () => {
    fixture.detectChanges();
    const initialCount = component.fields.length;

    component.removeField(0);

    expect(component.fields.length).toBe(initialCount - 1);
  });

  it('should select and deselect field', () => {
    fixture.detectChanges();
    expect(component.selectedFieldIndex).toBe(-1);

    component.selectField(0);
    expect(component.selectedFieldIndex).toBe(0);

    component.selectField(0);
    expect(component.selectedFieldIndex).toBe(-1);
  });

  it('should update field property', () => {
    fixture.detectChanges();
    component.fields[0].x = 10;

    component.updateFieldProperty(0, 'x', 50);

    expect(component.fields[0].x).toBe(50);
  });

  it('should handle background image upload', () => {
    const file = new File(['test'], 'test.png', { type: 'image/png' });
    const event = { target: { files: [file] } } as any;

    component.onBackgroundImageChange(event);

    expect(component.backgroundImageFile).toBe(file);
    expect(component.backgroundUploadError).toBe('');
  });

  it('should reject non-image files', () => {
    const file = new File(['test'], 'test.txt', { type: 'text/plain' });
    const event = { target: { files: [file] } } as any;

    component.onBackgroundImageChange(event);

    expect(component.backgroundUploadError).toContain('valid image');
  });

  it('should clear background', () => {
    component.backgroundImageFile = new File(['test'], 'test.png', { type: 'image/png' });
    component.backgroundImagePreview = 'data:image/png;base64,...';

    component.clearBackground();

    expect(component.backgroundImageFile).toBeNull();
    expect(component.backgroundImagePreview).toBeNull();
    expect(component.backgroundUploadError).toBe('');
  });

  it('should save layout', () => {
    mockInvoicesApi.saveTemplateLayout.mockReturnValue(
      of({ id: 'test-id', versionNumber: 1 })
    );

    fixture.detectChanges();
    component.saveLayout();

    expect(mockInvoicesApi.saveTemplateLayout).toHaveBeenCalled();
    expect(component.savingLayout).toBeFalsy();
  });

  it('should navigate back', () => {
    component.goBack();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/invoices']);
  });

  it('should calculate pixel coordinates correctly', () => {
    const mmValue = 10;
    const pixelValue = component.getFieldPixelWidth({ name: 'test', type: 'Text', width: mmValue });

    expect(pixelValue).toBe(mmValue * component.pixelsPerMm);
  });

  it('should handle drag operations', () => {
    fixture.detectChanges();
    component.fields[0].x = 10;
    component.fields[0].y = 10;

    const pointerDownEvent = new PointerEvent('pointerdown', {
      clientX: 100,
      clientY: 100,
      bubbles: true,
    });

    component.startFieldDrag(pointerDownEvent, 0, 'move');

    expect(component['dragState']).toBeTruthy();
    expect(component['dragState']!.mode).toBe('move');
    expect(component.selectedFieldIndex).toBe(0);
  });
});

