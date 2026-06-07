import { Component, Input, ViewChild, ElementRef, forwardRef, AfterViewInit, HostListener, OnDestroy } from '@angular/core';
import { ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR } from '@angular/forms';
import SignaturePad from 'signature_pad';

@Component({
  selector: 'app-signature-input',
  templateUrl: './signature-input.component.html',
  styleUrls: ['./signature-input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SignatureInputComponent),
      multi: true
    }
  ]
})
export class SignatureInputComponent implements ControlValueAccessor, AfterViewInit, OnDestroy {
  @Input() placeholder: string = 'חתום כאן';
  @Input() formGroup: FormGroup | null = null;
  @Input() formControlName: string | null = null;
  @Input() selectedValue: string | null = null;
  @Input() title: string | null = 'אנא חתום כאן';
  @Input() defaultWidth: number = 35 * 16; // Default width in pixels (35rem)
  @Input() defaultHeight: number = 10 * 16; // Default height in pixels (10rem)

  @ViewChild('canvas', { static: true }) canvas!: ElementRef;
  public sigPad: SignaturePad | null = null;

  ngAfterViewInit() {
    this.initSignaturePad();
    this.resizeCanvas();
  }

  @HostListener('window:resize')
  onResize() {
    this.resizeCanvas();
  }

  ngOnDestroy() {
    // Clean up global event listeners when component is destroyed
    window.removeEventListener('mouseup', this.saveSignature.bind(this));
    window.removeEventListener('touchend', this.saveSignature.bind(this));
  }

  writeValue(value: any) {
    this.selectedValue = value;
    if (this.sigPad) {
      if (value) {
        this.sigPad.fromDataURL(value);
      } else {
        this.sigPad.clear();
      }
    }
  }

  registerOnChange(fn: any) {
    this.onChange = fn;
  }

  registerOnTouched(fn: any) {
    this.onTouched = fn;
  }

  private onChange: any = () => { };
  private onTouched: any = () => { };

  get control(): FormControl | null {
    if (this.formControlName) {
      return this.formGroup?.get(this.formControlName) as FormControl;
    }
    return null;
  }

  initSignaturePad() {
    if (this.canvas) {
      this.sigPad = new SignaturePad(this.canvas.nativeElement);
      if (this.selectedValue) {
        this.sigPad.fromDataURL(this.selectedValue);
      }

      // Attach event listeners to the window to capture events outside the canvas
      window.addEventListener('mouseup', this.saveSignature.bind(this));
      window.addEventListener('touchend', this.saveSignature.bind(this));
    }
  }

  resizeCanvas() {
    if (this.canvas) {
      const parentElement = this.canvas.nativeElement.parentElement;
      if (parentElement) {
        const parentWidth = parentElement.offsetWidth;
        const parentHeight = parentElement.offsetHeight;

        // Set canvas size based on parent container's size, respecting the default sizes
        const newWidth = parentWidth ? Math.min(parentWidth * 0.9, this.defaultWidth) : this.defaultWidth;
        const newHeight = parentHeight ? Math.min(parentHeight * 0.5, this.defaultHeight) : this.defaultHeight;

        this.canvas.nativeElement.width = newWidth;
        this.canvas.nativeElement.height = newHeight;

        if (this.sigPad) {
          this.sigPad.clear(); // Clear and resize signature pad
        }
      }
    }
  }

  saveSignature() {
    if (this.sigPad && !this.sigPad.isEmpty()) {
      const signature = this.sigPad.toDataURL();
      this.onChange(signature);
      if (this.control) {
        this.control.setValue(signature);
      }
    }
  }

  clearSignature() {
    if (this.sigPad) {
      this.sigPad.clear();
      this.onChange(null);
      if (this.control) {
        this.control.setValue(null);
      }
    }
  }

  onTouchedEvent() {
    this.onTouched();
  }
}
