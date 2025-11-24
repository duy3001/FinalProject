// assets/js/api/comment-revision.api.js
import { http } from '../core/http.js';

export const commentRevisionApi = {
  /** GET /api/v1/posts/{postId}/comments/{commentId}/revisions -> Paged<CommentRevisionDto> */
  getList(questionId, commentId, query) {
    return http.get(
      `/api/v1/posts/${questionId}/comments/${commentId}/revisions`,
      { params: query }
    );
  },

  /** GET /api/v1/posts/{postId}/comments/{commentId}/revisions/{revId} */
  getDetail(questionId, commentId, revisionId) {
    return http.get(
      `/api/v1/posts/${questionId}/comments/${commentId}/revisions/${revisionId}`
    );
  },
};
