import { Component, Output, EventEmitter, Input } from '@angular/core';

@Component({
  selector: 'app-submit-btn',
  templateUrl: './submit-btn.component.html',
  styleUrls: ['./submit-btn.component.scss']
})
export class SubmitBtnComponent {
  @Input() title: string = 'הצג תוצאות';
  @Input() btnTitle: string = 'הצג תוצאות';
  @Input() backgroundColor: string = "rgb(63 81 181)";
  @Input() color: string = "rgb(63 81 181)";
  @Input('disabled') disabledBtn = false;
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
