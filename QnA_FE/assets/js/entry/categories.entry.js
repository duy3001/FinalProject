// assets/js/entry/categories.entry.js
import { initLayout } from '../core/layout.js';
import { initCategoriesPage } from '../features/category/categories.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();    // header/footer + nav, search
  initCategoriesPage();  // logic riêng của trang
});
