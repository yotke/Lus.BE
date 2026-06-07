import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../Infastructure/material/material.module';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { BreadcrumbComponent } from './breadcrumbs/breadcrumb/breadcrumb.component';
import { RouterModule } from '@angular/router';



@NgModule({
  declarations: [BreadcrumbComponent],
  imports: [
    CommonModule,
    RouterModule,
    MaterialModule,
    MatIconModule,
    MatButtonModule,      // Import Angular Material Button module (optional)
  ],
  exports: [BreadcrumbComponent]
})
export class SharedModule { }
