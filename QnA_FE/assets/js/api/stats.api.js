// assets/js/api/stats.api.js
import { http } from '../core/http.js';

export const statsApi = {
  /** GET /api/v1/stats/overview -> OverviewStatsDto */
  getOverview() {
    return http.get('/api/v1/stats/overview');
  },

  /** GET /api/v1/stats/me -> UserStatsDto */
  getMeStats() {
    return http.get('/api/v1/stats/me');
  },

  /** GET /api/v1/users/{id}/stats -> UserStatsDto */
  getUserStats(userId) {
    return http.get(`/api/v1/users/${userId}/stats`);
  },
};
