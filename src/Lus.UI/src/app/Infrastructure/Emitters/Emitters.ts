import { EventEmitter } from '@angular/core';


export class Emitters {
  static FilterMenuEmitter = new EventEmitter<any>();
  static isLoadingEmitter = new EventEmitter<any>();
  static RegularHeadLineSubTitleEmitter = new EventEmitter<any>();
  static MainHeadLineSubTitleEmitter = new EventEmitter<any>();
}
