// assets/js/api/auth.api.js
import { http } from '../core/http.js';

export const authApi = {
  register(body) {
    /** body: { email, password, displayName } */
    return http.post('/api/v1/auth/register', body);
  },

  verifyEmail(body) {
    /** body: { email, token } */
    return http.post('/api/v1/auth/verify-email', body);
  },

  login(body) {
    /** body: { email, password } -> AuthResponse */
    return http.post('/api/v1/auth/login', body);
  },
};
