// assets/js/core/auth.js
import { storage } from './storage.js';

const ACCESS_TOKEN_KEY = 'qa_access_token';
const REFRESH_TOKEN_KEY = 'qa_refresh_token';
const EXPIRES_AT_KEY = 'qa_expires_at';
const ME_KEY = 'qa_me';

export const auth = {
  /** Lưu AuthResponse sau khi login / register */
  setAuth(authResponse) {
    const { accessToken, refreshToken, expiresIn } = authResponse || {};

    if (accessToken) {
      storage.set(ACCESS_TOKEN_KEY, accessToken);
    }
    if (refreshToken) {
      storage.set(REFRESH_TOKEN_KEY, refreshToken);
    }
    if (expiresIn) {
      const expiresAt = Date.now() + expiresIn * 1000; // ms
      storage.set(EXPIRES_AT_KEY, expiresAt);
    }
  },

  clearAuth() {
    storage.remove(ACCESS_TOKEN_KEY);
    storage.remove(REFRESH_TOKEN_KEY);
    storage.remove(EXPIRES_AT_KEY);
    storage.remove(ME_KEY);
  },

  /** Lấy accessToken, nếu hết hạn thì clear luôn */
  getAccessToken() {
    const token = storage.get(ACCESS_TOKEN_KEY);
    if (!token) return null;

    const expiresAt = storage.get(EXPIRES_AT_KEY);
    if (expiresAt && Date.now() > expiresAt) {
      this.clearAuth();
      return null;
    }
    return token;
  },

  getRefreshToken() {
    return storage.get(REFRESH_TOKEN_KEY);
  },

  getCurrentUser() {
    return storage.get(ME_KEY);
  },

  setCurrentUser(me) {
    storage.set(ME_KEY, me);
  },

  isAuthenticated() {
    return !!this.getAccessToken();
  },
};
