// assets/js/features/stats/overview-stats.js
import { statsApi } from '../../api/stats.api.js';

export async function initOverviewStats() {
  const container = document.querySelector('[data-role="overview-stats"]');
  if (!container) return;

  container.innerHTML = '<p class="text-muted">Đang tải thống kê...</p>';

  try {
    /** @type {OverviewStatsDto} */
    const stats = await statsApi.getOverview();

    container.innerHTML = `
      <div class="overview-stats__header">
        <h2 class="overview-stats__title">Site stats</h2>
      </div>
      <div class="overview-stats__grid">
        ${renderStatItem('Questions', stats.totalQuestions)}
        ${renderStatItem('Comments', stats.totalComments)}
        ${renderStatItem('Votes', stats.totalVotes)}
        ${renderStatItem('Users', stats.totalUsers)}
      </div>
      <div class="overview-stats__tags">
        <h3 class="overview-stats__tags-title">Top tags</h3>
        <div class="overview-stats__tags-list">
          ${(stats.topTags || [])
            .map(
              (t) => `
            <a href="/page/question/questions.html?tag=${encodeURIComponent(
              t.slug
            )}" class="tag-pill overview-tag">
              <span class="overview-tag__name">${escapeHtml(t.name)}</span>
              <span class="overview-tag__count">${t.count}</span>
            </a>
          `
            )
            .join('')}
        </div>
      </div>
    `;
  } catch (err) {
    console.error(err);
    container.innerHTML =
      '<p class="text-muted">Không tải được thống kê tổng quan.</p>';
  }
}

function renderStatItem(label, value) {
  return `
    <div class="overview-stats__item">
      <div class="overview-stats__value">${value ?? 0}</div>
      <div class="overview-stats__label">${label}</div>
    </div>
  `;
}

function escapeHtml(str = '') {
  return str
    .toString()
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}
