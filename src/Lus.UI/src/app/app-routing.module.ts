import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './Adapters/HomePageComponents/home/home.component';
import { ProjectsManagerComponent } from './Activities/projects-manager/projects-manager.component';
import { HistoryProjectsComponent } from './Activities/history-projects/history-projects.component';

const routes: Routes = [
  { path: '', redirectTo: 'Home', pathMatch: 'full', data: { breadcrumb: 'דף הבית', roles: [] } },
  { path: 'Home', component: HomeComponent, data: { breadcrumb: 'דף הבית', roles: [] } },
  { path: 'TimeManagement', component: ProjectsManagerComponent, data: { breadcrumb: 'ניהול פרויקטים', roles: [] } },
  { path: 'History', component: HistoryProjectsComponent, data: { breadcrumb: 'צפייה בהיסטורית פרויקטים', roles: [] } },

]

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
