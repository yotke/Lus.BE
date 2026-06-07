import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.scss']
})
export class TableComponent {
  @Input() tenderDisplayedColumns = ['Id', 'filed1', 'field2']
  allInputIsvalid: boolean = true;
  newInputNeedValid: boolean = false;

  checkType(element: any, field: any): string {
    if (typeof element[field] === 'string') {
      return 'text'
    }
    else if (typeof element[field] === 'boolean') {
      return "checkbox"
    }
    else {
      return typeof element[field];
    }
  }
  checkValidate(element: any, field: any): boolean {
    return true;

  }
  invalidInput(field: any, id: any): boolean {
    var name = this.setName(field, id)
    let inputEl = document.getElementById(`${name}`);
    if (inputEl) {
      if (inputEl?.classList?.contains('ng-invalid')) {
        this.allInputIsvalid = false;
        if (inputEl?.classList?.contains('ng-dirty')) {
          return true;
        }
      }
    }
    this.newInputNeedValid = false;
    return false;
  }
  setName(name: string, id: Number): string {
    return `${name}_${id}`;
  }
}
