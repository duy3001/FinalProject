// assets/js/api/notification.api.js
import { http } from '../core/http.js';

export const notificationApi = {
  /** GET /api/v1/notifications -> NotificationDtoPaged */
  getList(query) {
    return http.get('/api/v1/notifications', { params: query });
  },

  /** GET /api/v1/notifications/unread-count -> integer */
  getUnreadCount() {
    return http.get('/api/v1/notifications/unread-count');
  },

  /** POST /api/v1/notifications/mark-read (ids: number[]) */
  markRead(ids) {
    return http.post('/api/v1/notifications/mark-read', { ids });
  },
};
