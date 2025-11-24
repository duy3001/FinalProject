// assets/js/features/category/categories.js
import { categoryApi } from '../../api/category.api.js';

export async function initCategoriesPage() {
  const listEl = document.querySelector('[data-role="category-list"]');
  if (!listEl) return;

  listEl.innerHTML = '<p>Đang tải danh sách chuyên mục...</p>';

  try {
    /** @type {CategoryDto[]} */
    const categories = await categoryApi.getAll();
    if (!categories || categories.length === 0) {
      listEl.innerHTML = '<p>Chưa có chuyên mục nào.</p>';
      return;
    }

    listEl.innerHTML = categories
      .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
      .map((c) => renderCategoryItem(c))
      .join('');
  } catch (error) {
    console.error(error);
    listEl.innerHTML = '<p>Không tải được danh sách chuyên mục.</p>';
  }
}

function renderCategoryItem(c) {
  // Sau này bạn có thể truyền thêm thông tin số câu hỏi vào đây nếu BE support
  const link = `/page/question/questions.html?category=${encodeURIComponent(
    c.slug
  )}`;

  return `
    <article class="category-card">
      <div class="category-card__body">
        <h2 class="category-card__name">
          <a href="${link}">${c.name}</a>
        </h2>
        ${
          c.isHidden
            ? '<span class="category-card__badge category-card__badge--hidden">Hidden</span>'
            : ''
        }
        ${
          c.parentId
            ? `<p class="category-card__parent text-muted">Parent ID: ${c.parentId}</p>`
            : ''
        }
      </div>
      <div class="category-card__meta text-muted">
        <span>Slug: <code>${c.slug}</code></span>
        <span>Order: ${c.displayOrder ?? '-'}</span>
      </div>
    </article>
  `;
}
