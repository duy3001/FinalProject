// assets/js/features/questions/questions.js
import { questionApi } from '../../api/question.api.js';
import { categoryApi } from '../../api/category.api.js';

export async function initQuestionsPage() {
  const listEl = document.querySelector('.questions-list');
  const headingEl = document.querySelector('[data-role="questions-heading"]');
  const filterInfoEl = document.querySelector('[data-role="filter-info"]');

  if (!listEl) return;

  const url = new URL(window.location.href);
  const page = Number(url.searchParams.get('page') || 1);
  const pageSize = Number(url.searchParams.get('pageSize') || 10);
  const categorySlug = url.searchParams.get('category');
  const tag = url.searchParams.get('tag');

  listEl.innerHTML = '<p>Đang tải danh sách câu hỏi...</p>';
  if (filterInfoEl) filterInfoEl.textContent = '';

  try {
    let paged;
    let filterLabel = '';

    if (categorySlug) {
      paged = await categoryApi.getPostsBySlug(categorySlug, {
        page,
        pageSize,
      });
      filterLabel = `Category: ${categorySlug}`;
    } else if (tag) {
      paged = await questionApi.getByTag(tag, { page, pageSize });
      filterLabel = `Tag: ${tag}`;
    } else {
      paged = await questionApi.getAll({ page, pageSize });
    }

    const items = paged.items || [];
    if (items.length === 0) {
      listEl.innerHTML = '<p>Không có câu hỏi nào.</p>';
    } else {
      listEl.innerHTML = items.map(renderQuestionCard).join('');
    }

    if (headingEl) {
      headingEl.textContent = `Questions ${paged.total ?? ''}`;
    }
    if (filterInfoEl && filterLabel) {
      filterInfoEl.textContent = filterLabel;
    }
  } catch (error) {
    console.error(error);
    listEl.innerHTML = '<p>Không tải được danh sách câu hỏi.</p>';
  }
}

function renderQuestionCard(q) {
  const {
    id,
    title,
    authorDisplayName,
    createdAt,
    score,
    tags,
    previewBody,
    categoryName,
    categorySlug,
    categoryId,
  } = q;

  const created = formatDate(createdAt);
  const tagList = tags || [];

  const detailLink = `/page/question/question-detail.html?id=${id}`;

  const categoryLabel =
    categoryName ||
    (categorySlug && `Category: ${categorySlug}`) ||
    (categoryId && `Category #${categoryId}`) ||
    '';

  return `
    <article class="question-card card">
      <div class="question-card__stats">
        <div class="question-stat">
          <span class="question-stat__value">${score ?? 0}</span>
          <span class="question-stat__label">Votes</span>
        </div>
        <div class="question-stat">
          <span class="question-stat__value">0</span>
          <span class="question-stat__label">Answers</span>
        </div>
        <div class="question-stat">
          <span class="question-stat__value">0</span>
          <span class="question-stat__label">Views</span>
        </div>
      </div>

      <div class="question-card__main">
        <a href="${detailLink}" class="question-card__title">
          ${escapeHtml(title)}
        </a>

        <p class="question-card__excerpt">
          ${escapeHtml(previewBody || '')}
        </p>

        <div class="question-card__meta">
          <div class="question-card__tags">
            ${
              categoryLabel
                ? `<span class="tag-pill tag-pill--category">${escapeHtml(
                    categoryLabel
                  )}</span>`
                : ''
            }
            ${tagList
              .map(
                (t) => `
              <a class="tag-pill" href="/page/question/questions.html?tag=${encodeURIComponent(
                t
              )}">
                ${escapeHtml(t)}
              </a>`
              )
              .join('')}
          </div>

          <div class="question-card__author">
            <div class="avatar-small">
              ${escapeHtml((authorDisplayName || 'U')[0].toUpperCase())}
            </div>
            <div class="question-card__author-info">
              <span class="question-card__author-name">
                ${escapeHtml(authorDisplayName || 'Unknown')}
              </span>
              <span class="question-card__time">
                ${created ? created : ''}
              </span>
            </div>
          </div>
        </div>
      </div>
    </article>
  `;
}

function formatDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString('vi-VN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function escapeHtml(str = '') {
  return str
    .toString()
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}
