import { Component, EventEmitter, Output, Input, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-excel-btn',
  templateUrl: './excel-btn.component.html',
  styleUrls: ['./excel-btn.component.scss']
})
export class ExcelBtnComponent {
  title: string = 'ייצוא לאקסל'
  @Input('disabled') disabledBtn: boolean = false;
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
  ngOnChanges(changes: SimpleChanges) {
    // Check if the property hasCollision has changed
    if (changes['disabledBtn']) {
      const previousValue: boolean = changes['disabledBtn'].previousValue;
      const currentValue: boolean = changes['disabledBtn'].currentValue;

      if (previousValue !== currentValue) {
        this.handleHasCollisionChange(currentValue);
      }
    }
  }

  handleHasCollisionChange(newState: boolean) {
    if (newState) {
      this.title = 'כל עוד יש התנגשות בזמנים, אינך יכול/ה להגיש את חודש זה.'
    } else {
      this.title = 'ייצוא לאקסל'
    }
  }
}

