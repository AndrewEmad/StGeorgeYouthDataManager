import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ServantHeaderComponent } from '../servant-header/servant-header';

@Component({
  selector: 'app-servant-layout',
  standalone: true,
  imports: [RouterOutlet, ServantHeaderComponent],
  templateUrl: './servant-layout.html',
  styleUrls: ['./servant-layout.css']
})
export class ServantLayoutComponent {}
