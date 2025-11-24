// assets/js/entry/questions.entry.js
import { initLayout } from '../core/layout.js';
import { initQuestionsPage } from '../features/questions/questions.js';
import { initOverviewStats } from '../features/stats/overview-stats.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initOverviewStats();
  initQuestionsPage();
});
