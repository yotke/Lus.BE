import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-return-btn',
  templateUrl: './return-btn.component.html',
  styleUrls: ['./return-btn.component.scss']
})
export class ReturnBtnComponent {
  title: string = 'חזור לדף קודם'
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}

