import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-edit-btn',
  templateUrl: './edit-btn.component.html',
  styleUrls: ['./edit-btn.component.scss']
})
export class EditBtnComponent {
  @Input() title: string = 'ערוך';
  @Input() color: string = "rgb(63 81 181)"
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
