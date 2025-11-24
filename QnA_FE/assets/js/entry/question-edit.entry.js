// assets/js/entry/question-edit.entry.js
import { initLayout } from '../core/layout.js';
import { initQuestionEditPage } from '../features/questions/question-form.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initQuestionEditPage();
});
