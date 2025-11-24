// assets/js/api/comment.api.js
import { http } from '../core/http.js';

export const commentApi = {
  /** GET /api/v1/posts/{postId}/comments -> CommentDtoPaged */
  getByPost(postId, query) {
    return http.get(`/api/v1/posts/${postId}/comments`, { params: query });
  },

  /** POST /api/v1/posts/{postId}/comments -> CommentDto */
  create(postId, body) {
    /** body: CreateCommentRequest { body, parentCommentId } */
    return http.post(`/api/v1/posts/${postId}/comments`, body);
  },

  /** PUT /api/v1/posts/{postId}/comments/{commentId} -> CommentDto */
  update(postId, commentId, body) {
    /** body: UpdateCommentRequest { body, summary } */
    return http.put(
      `/api/v1/posts/${postId}/comments/${commentId}`,
      body
    );
  },

  /** POST /api/v1/posts/{postId}/comments/{commentId}/vote (VoteRequest) */
  vote(postId, commentId, value) {
    return http.post(
      `/api/v1/posts/${postId}/comments/${commentId}/vote`,
      { value }
    );
  },
};
