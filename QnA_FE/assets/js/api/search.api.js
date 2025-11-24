// assets/js/api/search.api.js
import { http } from '../core/http.js';

export const searchApi = {
  /**
   * q: string
   * type: 'posts' | 'users' | 'tags' | 'all'
   * page, pageSize, sort: optional
   */
  search({ q, type = 'posts', page = 1, pageSize = 10, sort } = {}) {
    const params = { q, type, page, pageSize };
    if (sort) params.sort = sort;
    return http.get('/api/v1/search', { params });
  },
};
