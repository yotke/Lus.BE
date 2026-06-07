import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-tack-btn',
  templateUrl: './tack-btn.component.html',
  styleUrls: ['./tack-btn.component.scss']
})
export class TackBtnComponent {
  @Input() isPined: boolean = false;

  @Output() tackClicked = new EventEmitter<void>();

  onClick() {
    this.tackClicked.emit();
  }
}
