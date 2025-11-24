// assets/js/features/auth/login.js
import { authApi } from '../../api/auth.api.js';
import { auth } from '../../core/auth.js';

export function initLoginPage() {
  const form = document.querySelector('[data-form="login"]');
  if (!form) return;

  const errorEl = form.querySelector('[data-role="form-error"]');
  const submitBtn = form.querySelector('button[type="submit"]');

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (errorEl) errorEl.textContent = '';

    const formData = new FormData(form);
    const email = formData.get('email')?.toString().trim();
    const password = formData.get('password')?.toString();

    if (!email || !password) {
      if (errorEl) errorEl.textContent = 'Vui lòng nhập email và mật khẩu.';
      return;
    }

    submitBtn.disabled = true;
    const originalText = submitBtn.textContent;
    submitBtn.textContent = 'Signing in...';

    try {
      const res = await authApi.login({ email, password });
      auth.setAuth(res);

      // TODO: sau này có thể gọi /api/v1/me rồi auth.setCurrentUser(me)
      // Tạm thời login xong thì chuyển về trang questions
      window.location.href = '/page/question/questions.html';
    } catch (err) {
      console.error(err);
      if (errorEl) {
        errorEl.textContent = 'Email hoặc mật khẩu không đúng (hoặc server lỗi).';
      }
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = originalText;
    }
  });
}
