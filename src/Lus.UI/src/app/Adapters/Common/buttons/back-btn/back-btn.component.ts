import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-back-btn',
  templateUrl: './back-btn.component.html',
  styleUrls: ['./back-btn.component.scss']
})
export class BackBtnComponent {
  @Input() title: string = 'הוסף איבר חדש';
  @Input() color: string = "rgb(63 81 181)"
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}
