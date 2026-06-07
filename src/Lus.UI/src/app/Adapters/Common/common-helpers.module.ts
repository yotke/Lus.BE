import { NgModule } from '@angular/core';
import { AutoCompleteComponent } from './Inputs/auto-complete/auto-complete.component';
import { NgSelectModule } from '@ng-select/ng-select';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DatePickerComponent } from './Inputs/date-picker/date-picker.component';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RegularInputComponent } from './Inputs/regular-input/regular-input.component';
import { TimePickerComponent } from './Inputs/time-picker/time-picker.component';
import { TextAreaInputComponent } from './Inputs/text-area-input/text-area-input.component';
import { TableComponent } from './Inputs/table/table.component';
import { RadioButtonComponent } from './Inputs/radio-button/radio-button.component';
import { MonthPickerComponent } from './Inputs/month-picker/month-picker.component';
import { CheckboxInputComponent } from './Inputs/checkbox-input/checkbox-input.component';
import { AddBtnComponent } from './buttons/add-btn/add-btn.component';
import { NextBtnComponent } from './buttons/next-btn/next-btn.component';
import { BackBtnComponent } from './buttons/back-btn/back-btn.component';
import { ExcelBtnComponent } from './buttons/excel-btn/excel-btn.component';
import { MinusBtnComponent } from './buttons/minus-btn/minus-btn.component';
import { ReturnBtnComponent } from './buttons/return-btn/return-btn.component';
import { SubmitBtnComponent } from './buttons/submit-btn/submit-btn.component';
import { MainHeadlineComponent } from './Tools/TextsTools/main-headline/main-headline.component';
import { EditBtnComponent } from './buttons/edit-btn/edit-btn.component';
import { DeleteBtnComponent } from './buttons/delete-btn/delete-btn.component';
import { BtnsContainerComponent } from './Tools/GeneralTools/btns-container-component/btns-container.component';
import { SignatureInputComponent } from './Inputs/signature-input/signature-input.component';
import { RegularBtnComponent } from './buttons/regular-btn/regular-btn.component';
import { EditOverlayComponent } from './Tools/GeneralTools/edit-overlay/edit-overlay.component';
import { SaveBtnComponent } from './buttons/save-btn/save-btn.component';
import { InputsRowComponent } from './Tools/InputsTools/inputs-row/inputs-row.component';
import { TriangleButtonComponent } from './buttons/triangle-button/triangle-button.component';
import { IconBtnComponent } from './buttons/icon-btn/icon-btn.component';
import { TablesModule } from './Tables/tables.module';
import { TackBtnComponent } from './buttons/tack-btn/tack-btn.component';
import { MultiSelectComponent } from './Inputs/multi-select/multi-select.component';
import { MultiSelectCheckboxComponent } from './Inputs/multi-select-checkbox/multi-select-checkbox.component';
import { RegularHeadlineComponent } from './Tools/TextsTools/regular-headline/regular-headline.component';
import { MaterialModule } from 'src/app/Infrastructure/material/material.module';
import { GenericModalComponent } from '../Shared/generic-modal/generic-modal.component';
import { NgxMaterialTimepickerModule } from 'ngx-material-timepicker';

@NgModule({
  declarations: [SignatureInputComponent, DatePickerComponent, AutoCompleteComponent, CheckboxInputComponent, RegularInputComponent, TimePickerComponent, MonthPickerComponent,
    TextAreaInputComponent, TableComponent, RadioButtonComponent,
    AddBtnComponent, SaveBtnComponent, RegularBtnComponent, IconBtnComponent, EditBtnComponent, DeleteBtnComponent, NextBtnComponent, BackBtnComponent, ExcelBtnComponent, MinusBtnComponent, ReturnBtnComponent, SubmitBtnComponent,
    MainHeadlineComponent, RegularHeadlineComponent, BtnsContainerComponent, EditOverlayComponent, TriangleButtonComponent,
    InputsRowComponent, GenericModalComponent, TackBtnComponent, MultiSelectComponent, MultiSelectCheckboxComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    MaterialModule,
    NgSelectModule,
    ReactiveFormsModule,  // Add this import
    NgxMaterialTimepickerModule.setOpts('he-IL', 'latn'),

    MatFormFieldModule,   // Import Angular Material Form Field module
    MatInputModule,       // Import Angular Material Input module
    MatDatepickerModule,  // Import Angular Material Datepicker module
    MatNativeDateModule,  // Import Native Date Module (necessary for datepicker)
    MatButtonModule,      // Import Angular Material Button module (optional)
    TablesModule,
    MatIconModule,
  ],

  exports: [SignatureInputComponent, DatePickerComponent, AutoCompleteComponent, CheckboxInputComponent, RegularInputComponent, TimePickerComponent, MonthPickerComponent,
    TextAreaInputComponent, TableComponent, RadioButtonComponent,
    AddBtnComponent, SaveBtnComponent, RegularBtnComponent, IconBtnComponent, EditBtnComponent, DeleteBtnComponent, NextBtnComponent, BackBtnComponent, ExcelBtnComponent, MinusBtnComponent, ReturnBtnComponent, SubmitBtnComponent,
    MainHeadlineComponent, RegularHeadlineComponent, BtnsContainerComponent, EditOverlayComponent, TriangleButtonComponent,
    InputsRowComponent, GenericModalComponent, TackBtnComponent, MultiSelectComponent, MultiSelectCheckboxComponent
  ],
  providers: [DatePipe],  // Add DatePipe to providers here
})

export class CommonHelpersModule { }
