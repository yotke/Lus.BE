import { Component, Input, OnInit } from '@angular/core';
import { Emitters } from 'src/app/Infrastructure/Emitters/Emitters';

@Component({
  selector: 'app-main-headline',
  templateUrl: './main-headline.component.html',
  styleUrl: './main-headline.component.scss'
})
export class MainHeadlineComponent implements OnInit {
  @Input() title: string | null = 'כותרת ראשית';
  @Input() subTitle: string | null = "";
  @Input() class: string = 'regular';
  @Input() color: string = 'primary';
  @Input() freeText: string | null = null;
  constructor() {


    Emitters.MainHeadLineSubTitleEmitter.subscribe(
      subTitle => {
        this.subTitle = subTitle.subTitle;
        this.freeText = subTitle.freeText;


      }
    )
  }
  ngOnInit() {


  }
}
