// assets/js/features/stats/user-stats.js
import { statsApi } from '../../api/stats.api.js';

export async function initUserStats() {
  const container = document.querySelector('[data-role="user-stats"]');
  if (!container) return;

  container.innerHTML = '<p class="text-muted">Đang tải thống kê...</p>';

  const url = new URL(window.location.href);
  const userIdParam = url.searchParams.get('userId');
  const userId = userIdParam ? Number(userIdParam) : null;

  try {
    let stats;
    if (userId) {
      stats = await statsApi.getUserStats(userId);
    } else {
      // mặc định: stats của chính mình
      stats = await statsApi.getMeStats();
    }

    container.innerHTML = `
      <div class="user-stats-grid">
        ${renderUserStat('Questions', stats.questions)}
        ${renderUserStat('Comments', stats.comments)}
        ${renderUserStat('Votes', stats.votes)}
        ${renderUserStat('Reputation', stats.reputation)}
      </div>
    `;
  } catch (err) {
    console.error(err);
    container.innerHTML =
      '<p class="text-muted">Không tải được thống kê của người dùng (có thể cần đăng nhập).</p>';
  }
}

function renderUserStat(label, value) {
  return `
    <div class="user-stat">
      <div class="user-stat__value">${value ?? 0}</div>
      <div class="user-stat__label">${label}</div>
    </div>
  `;
}
