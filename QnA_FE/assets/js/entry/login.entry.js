// assets/js/entry/login.entry.js
import { initLayout } from '../core/layout.js';
import { initLoginPage } from '../features/auth/login.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();   // header/footer + search, nav...
  initLoginPage();      // logic riêng trang login
});
