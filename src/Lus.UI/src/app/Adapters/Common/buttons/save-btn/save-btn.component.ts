import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-save-btn',
  templateUrl: './save-btn.component.html',
  styleUrls: ['./save-btn.component.scss']
})
export class SaveBtnComponent {
  @Input() title: string = 'שמור';
  @Input() color: string = "rgb(63 81 181)"
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
