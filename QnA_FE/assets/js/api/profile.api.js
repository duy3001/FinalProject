// assets/js/api/profile.api.js
import { http } from '../core/http.js';

export const profileApi = {
  /** GET /api/v1/me -> MeDto */
  getMe() {
    return http.get('/api/v1/me');
  },

  /** PUT /api/v1/me -> MeDto */
  updateMe(payload) {
    // payload: { displayName?: string, bio?: string }
    return http.put('/api/v1/me', payload);
  },

  /** PUT /api/v1/me/avatar (Multipart: file) -> FileDto */
  updateAvatar(file) {
    const formData = new FormData();
    formData.append('file', file);
    // http.js cần không set Content-Type khi body là FormData, để browser tự đặt boundary
    return http.put('/api/v1/me/avatar', formData);
  },

  /** GET /api/v1/users/{id} -> PublicUserDto */
  getPublicUser(userId) {
    return http.get(`/api/v1/users/${userId}`);
  },

  /** GET /api/v1/users/{id}/activities -> Paged<ActivityDto> */
  getActivities(userId, query) {
    return http.get(`/api/v1/users/${userId}/activities`, { params: query });
  },
};
