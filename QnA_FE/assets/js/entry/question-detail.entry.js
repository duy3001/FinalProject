// assets/js/entry/question-detail.entry.js
import { initLayout } from '../core/layout.js';
import { initQuestionDetailPage } from '../features/questions/question-detail.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initQuestionDetailPage();
});
