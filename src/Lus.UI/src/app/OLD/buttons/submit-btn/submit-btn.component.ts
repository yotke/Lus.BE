import { Component, Output, EventEmitter, Input } from '@angular/core';

@Component({
  selector: 'app-submit-btn',
  templateUrl: './submit-btn.component.html',
  styleUrls: ['./submit-btn.component.scss']
})
export class SubmitBtnComponent {
  @Input() btnTitle: string = 'הצג תוצאות'
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
