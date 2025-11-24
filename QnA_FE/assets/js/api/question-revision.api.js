// assets/js/api/question-revision.api.js
import { http } from '../core/http.js';

export const questionRevisionApi = {
  /** GET /api/v1/posts/{id}/revisions -> Paged<RevisionDto> */
  getList(questionId, query) {
    return http.get(`/api/v1/posts/${questionId}/revisions`, { params: query });
  },

  /** GET /api/v1/posts/{id}/revisions/{revId} -> RevisionDetailDto */
  getDetail(questionId, revisionId) {
    return http.get(
      `/api/v1/posts/${questionId}/revisions/${revisionId}`
    );
  },
};
