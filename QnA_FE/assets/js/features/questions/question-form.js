// assets/js/features/questions/question-form.js
import { questionApi } from '../../api/question.api.js';
import { categoryApi } from '../../api/category.api.js';

export function initQuestionCreatePage() {
  initQuestionForm({ mode: 'create' });
}

export function initQuestionEditPage() {
  initQuestionForm({ mode: 'edit' });
}

async function initQuestionForm({ mode }) {
  const form = document.querySelector('[data-form="question"]');
  const errorEl = form?.querySelector('[data-role="form-error"]');
  const categorySelect = form?.querySelector('select[name="categorySlug"]');

  if (!form) return;

  // tải category cho select
  if (categorySelect) {
    try {
      const categories = await categoryApi.getAll();
      categories
        .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
        .forEach((c) => {
          const opt = document.createElement('option');
          opt.value = c.slug;
          opt.textContent = c.name;
          categorySelect.appendChild(opt);
        });
    } catch (e) {
      console.error('Không tải được categories', e);
    }
  }

  let questionId = null;

  if (mode === 'edit') {
    const url = new URL(window.location.href);
    questionId = Number(url.searchParams.get('id'));
    if (!questionId) {
      if (errorEl) errorEl.textContent = 'Không xác định được câu hỏi.';
      return;
    }
    await fillFormForEdit(questionId, form, categorySelect, errorEl);
  }

  const submitBtn = form.querySelector('button[type="submit"]');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (errorEl) errorEl.textContent = '';

    const formData = new FormData(form);
    const title = formData.get('title')?.toString().trim();
    const body = formData.get('body')?.toString().trim();
    const tagString = formData.get('tags')?.toString().trim();
    const categorySlug = formData.get('categorySlug')?.toString().trim();

    if (!title || !body) {
      if (errorEl) errorEl.textContent = 'Vui lòng nhập đầy đủ tiêu đề và nội dung.';
      return;
    }

    const tags =
      tagString
        ?.split(',')
        .map((t) => t.trim())
        .filter(Boolean) || [];

    const payload = {
      title,
      body,
      tags,
      categorySlug: categorySlug || null,
      categoryId: null, // tạm không dùng
    };

    submitBtn.disabled = true;
    const originalText = submitBtn.textContent;
    submitBtn.textContent = mode === 'create' ? 'Đang đăng...' : 'Đang lưu...';

    try {
      let res;
      if (mode === 'create') {
        res = await questionApi.create(payload);
      } else {
        res = await questionApi.update(questionId, payload);
      }

      // res là PostDetailDto → chuyển sang trang detail
      window.location.href = `/page/question/question-detail.html?id=${res.id}`;
    } catch (err) {
      console.error(err);
      if (errorEl) {
        errorEl.textContent =
          'Không lưu được câu hỏi (có thể cần đăng nhập hoặc server lỗi).';
      }
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = originalText;
    }
  });
}

async function fillFormForEdit(id, form, categorySelect, errorEl) {
  try {
    const q = await questionApi.getById(id);
    form.querySelector('input[name="title"]').value = q.title || '';
    form.querySelector('textarea[name="body"]').value = q.body || '';
    form.querySelector('input[name="tags"]').value = (q.tags || []).join(', ');

    if (categorySelect && q.categorySlug) {
      categorySelect.value = q.categorySlug;
    }

    const titleEl = document.querySelector('.question-form__title');
    if (titleEl) titleEl.textContent = 'Edit question';
  } catch (e) {
    console.error(e);
    if (errorEl) errorEl.textContent = 'Không tải được dữ liệu câu hỏi.';
  }
}
