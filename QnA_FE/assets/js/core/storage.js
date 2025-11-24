// assets/js/core/storage.js
const ACCESS_TOKEN_KEY = 'qa_access_token';
const ME_KEY = 'qa_me';

export const storage = {
  get(key) {
    try {
      const raw = localStorage.getItem(key);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  },
  set(key, value) {
    localStorage.setItem(key, JSON.stringify(value));
  },
  remove(key) {
    localStorage.removeItem(key);
  },

  getAccessToken() {
    return this.get(ACCESS_TOKEN_KEY);
  },
  setAccessToken(token) {
    this.set(ACCESS_TOKEN_KEY, token);
  },
  removeAccessToken() {
    this.remove(ACCESS_TOKEN_KEY);
  },

  getMe() {
    return this.get(ME_KEY);
  },
  setMe(me) {
    this.set(ME_KEY, me);
  },
  removeMe() {
    this.remove(ME_KEY);
  },
};
