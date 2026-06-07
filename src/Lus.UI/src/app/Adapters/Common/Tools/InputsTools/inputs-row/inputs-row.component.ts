import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-inputs-row',
  templateUrl: './inputs-row.component.html',
  styleUrl: './inputs-row.component.scss'
})
export class InputsRowComponent {
  @Input() height: number | null = null;
  @Input() margin: number | null = null;
  @Input() withDeleteRow: boolean = false;
  @Output() deleteButtonClicked = new EventEmitter<void>();
  onClick() {
    this.deleteButtonClicked.emit();
  }
}
