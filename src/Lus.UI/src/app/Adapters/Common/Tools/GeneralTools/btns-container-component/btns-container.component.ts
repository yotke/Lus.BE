import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-btns-container',
  templateUrl: './btns-container.component.html',
  styleUrl: './btns-container.component.scss'
})
export class BtnsContainerComponent {
  @Input() flexContentType:string=""
}
