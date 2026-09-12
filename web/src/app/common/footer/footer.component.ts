import { Component, computed, inject } from '@angular/core';
import {
  LucideMail,
  LucideMapPin,
} from '@lucide/angular';
import { BlogService } from '../../core/services/blog.service';
import { SocialIconComponent } from '../social-icon/social-icon.component';

@Component({
  selector: 'app-footer',
  imports: [
    LucideMail,
    LucideMapPin,
    SocialIconComponent,
  ],
  templateUrl: './footer.component.html',
})
export class FooterComponent {
  private readonly blogService = inject(BlogService);

  readonly profile = this.blogService.profile;
  readonly theme = this.blogService.theme;

  readonly isDarkTheme = computed(() => {
    const t = this.theme();
    return t === 'night' || t === 'twilight';
  });

  readonly logoUrl = computed(() => {
    return this.isDarkTheme()
      ? '/assets/images/logo-transparent-dark.png'
      : '/assets/images/logo-transparent.png';
  });
}
