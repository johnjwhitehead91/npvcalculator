// src/app/features/homepage/homepage.component.ts
import {Component} from '@angular/core';
import {Router} from '@angular/router';
import {ButtonModule} from 'primeng/button';
import {InputTextModule} from 'primeng/inputtext';
import {CardModule} from 'primeng/card';

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [ButtonModule, InputTextModule, CardModule], // ✅ Import required modules!
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.scss']
})
export class HomepageComponent {

  constructor(private router: Router) {
  }

  startCalculating(): void {
    this.router.navigate(['/calculator']);
  }

  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    }
  }

  watchDemo(): void {
    // Implement demo functionality
    console.log('Demo clicked');
  }
}
