import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-return-btn',
  templateUrl: './return-btn.component.html',
  styleUrls: ['./return-btn.component.scss']
})
export class ReturnBtnComponent {
  @Input() title: string = 'חזור לדף קודם';
  @Input() color: string = "rgb(63 81 181)";

  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}

