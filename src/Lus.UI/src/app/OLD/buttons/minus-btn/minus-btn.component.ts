import { Component, EventEmitter, Input, Output } from '@angular/core';
@Component({
  selector: 'app-minus-btn',
  templateUrl: './minus-btn.component.html',
  styleUrls: ['./minus-btn.component.scss']
})
export class MinusBtnComponent {
  @Input() title: string = 'הוסף איבר חדש'
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
