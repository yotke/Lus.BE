import { Component, Input, OnInit } from '@angular/core';
import { Emitters } from 'src/app/Infrastructure/Emitters/Emitters';

@Component({
  selector: 'app-regular-headline',
  templateUrl: './regular-headline.component.html',
  styleUrl: './regular-headline.component.scss'
})
export class RegularHeadlineComponent implements OnInit {
  @Input() title: string | null = 'כותרת ראשית';
  @Input() subTitle: string | null = "";
  @Input() class: string = 'regular';
  @Input() color: string = 'primary';
  @Input() freeText: string | null = null;
  constructor() {


    Emitters.RegularHeadLineSubTitleEmitter.subscribe(
      subTitle => {
        this.subTitle = subTitle.subTitle;
        this.freeText = subTitle.freeText;
      }
    )
  }
  ngOnInit() {


  }
}
