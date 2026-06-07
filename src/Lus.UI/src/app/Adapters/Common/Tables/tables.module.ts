import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxMaterialTimepickerModule } from 'ngx-material-timepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { GenericTableComponent } from './generic-table/generic-table.component';
import { MaterialModule } from 'src/app/Infrastructure/material/material.module';



@NgModule({
  declarations: [GenericTableComponent,
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
    MatIconModule,         // Import Angular Material Icon module (optional)
  ],
  exports: [GenericTableComponent
  ]
})
export class TablesModule { }
