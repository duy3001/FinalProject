// assets/js/api/category.api.js
import { http } from '../core/http.js';

export const categoryApi = {
  /** GET /api/v1/categories -> CategoryDto[] */
  getAll() {
    return http.get('/api/v1/categories');
  },

  /** GET /api/v1/categories/{slug} -> CategoryDto */
  getBySlug(slug) {
    return http.get(`/api/v1/categories/${slug}`);
  },

  /** GET /api/v1/categories/{slug}/posts -> PostDtoPaged */
  getPostsBySlug(slug, query) {
    return http.get(`/api/v1/categories/${slug}/posts`, { params: query });
  },

  /** Admin – sau này dùng */
  create(payload) {
    return http.post('/api/v1/admin/categories', payload);
  },

  update(id, payload) {
    return http.put(`/api/v1/admin/categories/${id}`, payload);
  },
};
