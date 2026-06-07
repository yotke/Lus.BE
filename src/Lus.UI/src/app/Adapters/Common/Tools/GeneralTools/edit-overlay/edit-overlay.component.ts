import { Component, EventEmitter, Input, Output, OnInit, ElementRef, AfterViewInit } from '@angular/core';

@Component({
  selector: 'app-edit-overlay',
  templateUrl: './edit-overlay.component.html',
  styleUrls: ['./edit-overlay.component.scss']
})
export class EditOverlayComponent implements OnInit, AfterViewInit {
  @Input() top: number;
  @Input() left: number;
  @Input() inputs: { key: string, value: any }[] = [];

  @Input() dictionary: Record<string, string> = {
    type: "סוג קלט",
    x: "מיקום בציר x",
    y: "מיקום בציר y",
    width: "רוחב הקלט",
    height: "אורך הקלט",
    radioName1: "שם רדיו קלט 1",
    radioName2: "שם רדיו קלט 2",
    formFieldName: "שם השדה באנגלית",
    placeholder: "תיאור",
    title: "כותרת",
  };

  @Output() save = new EventEmitter<{ [key: string]: any }>();
  @Output() cancel = new EventEmitter<void>();

  constructor(private el: ElementRef) { }

  ngOnInit() {

  }
  getFormData(): Record<string, any> {
    return this.inputs.reduce((formData: Record<string, any>, input) => {
      formData[input.key] = input.value;
      return formData;
    }, {} as Record<string, any>);
  }
  ngAfterViewInit() {

    this.adjustPositionIfNeeded();
  }

  onSave() {
    const updatedValues = this.inputs.reduce((acc: any, input) => {
      acc[input.key] = input.value;
      return acc;
    }, {});
    this.save.emit(updatedValues);
  }

  onCancel() {
    this.cancel.emit();
  }

  onInputChange(key: string, value: any) {
    const input = this.inputs.find(input => input.key === key);
    if (input) {
      input.value = value;
    }
  }

  adjustPositionIfNeeded() {
    const overlayElement = this.el.nativeElement.querySelector('.edit-overlay');
    const overlayRect = overlayElement.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;


    if (overlayRect.right > viewportWidth) {
      this.left -= (overlayRect.right - viewportWidth + 10); // Move more left if it goes out on the right
    }
    if (overlayRect.left < 0) {
      this.left = 10 ; // Move further to the right if it goes out on the left
    }
    if (overlayRect.bottom > viewportHeight) {
      this.top -= (overlayRect.bottom - viewportHeight + 10); // Move further up if it goes out at the bottom
    }
    if (overlayRect.top < 0) {
      this.top = 10 ; // Move further down if it goes out at the top
    }

    // Apply the adjusted position
    this.el.nativeElement.style.top = `${this.top}px`;
    this.el.nativeElement.style.left = `${this.left}px`;
  }

}
