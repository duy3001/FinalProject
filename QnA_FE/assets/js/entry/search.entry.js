// assets/js/entry/search.entry.js
import { initLayout } from '../core/layout.js';
import { initSearchPage } from '../features/search/search.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initSearchPage();
});
