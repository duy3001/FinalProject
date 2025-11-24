// assets/js/entry/notifications.entry.js
import { initLayout } from '../core/layout.js';
import { initNotificationsPage } from '../features/notification/notifications.js';

document.addEventListener('DOMContentLoaded', async () => {
  await initLayout();
  initNotificationsPage();
});
