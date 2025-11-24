// assets/js/entry/profile.entry.js
import { initLayout } from '../core/layout.js';
import { initProfilePage } from '../features/user/profile.js';
import { initUserStats } from '../features/stats/user-stats.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initProfilePage();
  initUserStats();
});
