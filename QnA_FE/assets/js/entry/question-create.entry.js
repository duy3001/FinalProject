// assets/js/entry/question-create.entry.js
import { initLayout } from '../core/layout.js';
import { initQuestionCreatePage } from '../features/questions/question-form.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initQuestionCreatePage();
});
