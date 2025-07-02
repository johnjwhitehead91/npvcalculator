// src/app/app.routes.ts
import {Routes} from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/homepage/homepage.component').then(m => m.HomepageComponent)
  },
  {
    path: 'calculator',
    loadComponent: () => import('./features/npv-calculator/npv-calculator.component').then(m => m.NpvCalculatorComponent)
  },
  // {
  //   path: 'results/:id',
  //   loadComponent: () => import('./features/npv-results/npv-results.component').then(m => m.NpvResultsComponent)
  // },
  {
    path: '**',
    redirectTo: ''
  }
];
