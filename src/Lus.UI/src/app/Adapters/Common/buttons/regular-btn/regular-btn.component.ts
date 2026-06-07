import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-regular-btn',
  templateUrl: './regular-btn.component.html',
  styleUrl: './regular-btn.component.scss'
})
export class RegularBtnComponent {
  @Input() disabled: boolean = false;
  @Input() title: string = 'כפתור';
  @Input() color: string = "rgb(63 81 181)"
  @Output() buttonClicked = new EventEmitter<void>();
  onClick() {
    this.buttonClicked.emit();
  }
}

