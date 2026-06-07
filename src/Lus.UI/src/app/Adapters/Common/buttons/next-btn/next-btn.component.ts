import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-next-btn',
  templateUrl: './next-btn.component.html',
  styleUrls: ['./next-btn.component.scss']
})
export class NextBtnComponent {
  @Input() title: string = 'הוסף איבר חדש';
  @Input() color: string = "rgb(63 81 181)"
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
