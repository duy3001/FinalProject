// assets/js/features/search/search.js
import { searchApi } from '../../api/search.api.js';

export function initSearchPage() {
  const resultsEl = document.querySelector('[data-role="search-results"]');
  const queryLabelEl = document.querySelector(
    '[data-role="search-query-label"]'
  );
  const form = document.querySelector('[data-form="page-search"]');
  const input = form?.querySelector('input[name="q"]');
  const tabs = document.querySelectorAll('.search-tab');

  if (!resultsEl || !form || !input) return;

  // đọc q & type từ URL
  const url = new URL(window.location.href);
  const q = (url.searchParams.get('q') || '').toString();
  const type = (url.searchParams.get('type') || 'posts').toLowerCase();

  input.value = q;
  if (queryLabelEl && q) {
    queryLabelEl.textContent = `for "${q}"`;
  }

  // set active tab
  tabs.forEach((tab) => {
    const tabType = tab.dataset.type;
    if (tabType === type) {
      tab.classList.add('search-tab--active');
    }
    tab.addEventListener('click', () => {
      // đổi type, giữ nguyên q
      const newUrl = new URL(window.location.href);
      newUrl.searchParams.set('type', tabType);
      newUrl.searchParams.set('q', input.value.trim());
      window.location.href = newUrl.toString();
    });
  });

  // submit form trên trang
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    const newQ = input.value.trim();
    if (!newQ) return;
    const newUrl = new URL(window.location.href);
    newUrl.searchParams.set('q', newQ);
    // giữ type hiện tại
    newUrl.searchParams.set('type', type);
    window.location.href = newUrl.toString();
  });

  if (!q) {
    resultsEl.innerHTML =
      '<p class="text-muted">Nhập từ khóa để bắt đầu tìm kiếm.</p>';
    return;
  }

  loadResults({ q, type, resultsEl }).catch(console.error);
}

async function loadResults({ q, type, resultsEl }) {
  resultsEl.innerHTML = '<p class="text-muted">Đang tìm kiếm...</p>';

  try {
    const res = await searchApi.search({ q, type, page: 1, pageSize: 10 });

    // BE trả về dạng phân trang: { page, pageSize, total, items }
    const items = res.items || res.results || [];

    if (!items.length) {
      resultsEl.innerHTML = '<p class="text-muted">Không tìm thấy kết quả.</p>';
      return;
    }

    if (type === 'posts') {
      resultsEl.innerHTML = renderPostResults(items);
    } else if (type === 'users') {
      resultsEl.innerHTML = renderUserResults(items);
    } else if (type === 'tags') {
      resultsEl.innerHTML = renderTagResults(items);
    } else {
      // type = all → bạn có thể custom thêm sau
      resultsEl.innerHTML =
        '<p class="text-muted">Kiểu "all" sẽ được xử lý sau.</p>';
    }
  } catch (e) {
    console.error(e);
    resultsEl.innerHTML =
      '<p class="text-muted">Có lỗi khi gọi API tìm kiếm.</p>';
  }
}

/** ========== Render helpers ========== */

/* TODO: chỉnh lại field theo SearchPosts DTO thật của bạn */
function renderPostResults(posts) {
  return `
    <div class="search-section">
      <h2 class="search-section__title">Posts</h2>
      <div class="search-posts">
        ${posts
          .map(
            (p) => `
          <article class="search-post card">
            <h3 class="search-post__title">
              <a href="/page/question/question-detail.html?id=${p.id}">
                ${p.title || '(no title)'}
              </a>
            </h3>
            ${
              p.excerpt
                ? `<p class="search-post__excerpt">${p.excerpt}</p>`
                : ''
            }
            <div class="search-post__meta">
              <span>${p.voteCount ?? 0} votes</span>
              <span>${p.answerCount ?? 0} answers</span>
              <span>${p.viewCount ?? 0} views</span>
            </div>
            <div class="search-post__tags">
              ${(p.tags || [])
                .map((t) => `<span class="tag-pill">${t}</span>`)
                .join('')}
            </div>
          </article>
        `
          )
          .join('')}
      </div>
    </div>
  `;
}

/* TODO: chỉnh field UserSearchDto cho đúng: */
function renderUserResults(users) {
  return `
    <div class="search-section">
      <h2 class="search-section__title">Users</h2>
      <div class="search-users">
        ${users
          .map(
            (u) => `
          <article class="search-user card">
            <div class="search-user__avatar">
              ${(u.displayName || u.name || 'U')[0]}
            </div>
            <div class="search-user__info">
              <h3 class="search-user__name">
                <a href="/page/user/profile.html?id=${u.id}">
                  ${u.displayName || u.name || 'Unknown'}
                </a>
              </h3>
              ${
                u.bio
                  ? `<p class="search-user__bio text-muted">${u.bio}</p>`
                  : ''
              }
            </div>
          </article>
        `
          )
          .join('')}
      </div>
    </div>
  `;
}

/* TODO: chỉnh field TagSearchDto cho đúng: */
function renderTagResults(tags) {
  return `
    <div class="search-section">
      <h2 class="search-section__title">Tags</h2>
      <div class="search-tags">
        ${tags
          .map(
            (t) => `
          <article class="search-tag card">
            <h3 class="search-tag__name">
              <a href="/page/question/questions.html?tag=${encodeURIComponent(
                t.slug || t.name
              )}">
                ${t.name}
              </a>
            </h3>
            <p class="text-muted">
              ${t.count ?? 0} questions
            </p>
          </article>
        `
          )
          .join('')}
      </div>
    </div>
  `;
}
