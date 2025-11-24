// assets/js/api/question.api.js
import { http } from '../core/http.js';

export const questionApi = {
  /** GET /api/v1/posts -> PostDtoPaged (Questions list) */
  getAll(query) {
    return http.get('/api/v1/posts', { params: query });
  },

  /** GET /api/v1/posts/{id} -> PostDetailDto (Question detail) */
  getById(id) {
    return http.get(`/api/v1/posts/${id}`);
  },

  /** POST /api/v1/posts -> PostDetailDto (Create question) */
  create(body) {
    return http.post('/api/v1/posts', body);
  },

  /** PUT /api/v1/posts/{id} -> PostDetailDto (Update question) */
  update(id, body) {
    return http.put(`/api/v1/posts/${id}`, body);
  },

  /** POST /api/v1/posts/{id}/vote (Vote question) */
  vote(id, value) {
    return http.post(`/api/v1/posts/${id}/vote`, { value });
  },

  /** GET /api/v1/tags/{tag}/posts -> PostDtoPaged (Questions by tag) */
  getByTag(tag, query) {
    return http.get(`/api/v1/tags/${encodeURIComponent(tag)}/posts`, {
      params: query,
    });
  },
};
