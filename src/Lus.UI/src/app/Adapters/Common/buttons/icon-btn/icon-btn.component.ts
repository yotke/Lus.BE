import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonHelpersModule } from "../../common-helpers.module";

@Component({
  selector: 'app-icon-btn',
  templateUrl: './icon-btn.component.html',
  styleUrls: ['./icon-btn.component.scss'],
})
export class IconBtnComponent {
  @Input() disabled: boolean = false;
  @Input() title: string = 'כפתור';
  @Input() color: string = "rgb(63 81 181)";
  @Input() icon: string = '';
  @Output() buttonClicked = new EventEmitter<void>();

  onClick() {
    if (!this.disabled) {
      this.buttonClicked.emit();
    }
  }
}
