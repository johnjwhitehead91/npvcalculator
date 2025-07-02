import {Component} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MenubarModule} from 'primeng/menubar';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [CommonModule, MenubarModule, RouterLink],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})
export class NavMenuComponent {
  items = [
    {label: 'Home', icon: 'pi pi-home', routerLink: '/'},
    {label: 'Calculator', icon: 'pi pi-calculator', routerLink: '/calculator'},
    {label: 'How It Works', icon: 'pi pi-cog', url: '#how-it-works'}
  ];
}
