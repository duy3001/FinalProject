// assets/js/features/questions/question-detail.js
import { questionApi } from '../../api/question.api.js';
import { commentApi } from '../../api/comment.api.js';
import { questionRevisionApi } from '../../api/question-revision.api.js'; // mới
import { commentRevisionApi } from '../../api/comment-revision.api.js'; 

export function initQuestionDetailPage() {
  const url = new URL(window.location.href);
  const questionId = Number(url.searchParams.get('id'));

  const questionEl = document.querySelector('[data-role="question-detail"]');
  const commentListEl = document.querySelector('[data-role="comment-list"]');

  if (!questionId || !questionEl) {
    if (questionEl) {
      questionEl.innerHTML = '<p>Không xác định được câu hỏi.</p>';
    }
    return;
  }

  loadQuestion(questionId, questionEl);
  loadComments(questionId, commentListEl);
  initAddCommentForm(questionId, commentListEl);
  initQuestionDetailActions(questionId);
  initHistorySection(questionId);                      
  initCommentHistoryActions(questionId, commentListEl); 
}

async function loadQuestion(id, container) {
  try {
    const question = await questionApi.getById(id);
    container.innerHTML = renderQuestionDetail(question);
  } catch (err) {
    console.error(err);
    container.innerHTML = '<p>Không tải được nội dung câu hỏi.</p>';
  }
}

async function loadComments(questionId, container) {
  if (!container) return;

  container.innerHTML = '<p>Đang tải bình luận...</p>';

  try {
    const paged = await commentApi.getByPost(questionId, {
      page: 1,
      pageSize: 20,
    });
    const items = paged.items || [];
    if (items.length === 0) {
      container.innerHTML = '<p>Chưa có bình luận nào.</p>';
      return;
    }

    container.innerHTML = items.map(renderComment).join('');
  } catch (err) {
    console.error(err);
    container.innerHTML = '<p>Không tải được bình luận.</p>';
  }
}

function initAddCommentForm(questionId, commentListEl) {
  const form = document.querySelector('[data-form="add-comment"]');
  if (!form) return;

  const errorEl = form.querySelector('[data-role="comment-error"]');
  const textarea = form.querySelector('textarea[name="body"]');
  const submitBtn = form.querySelector('button[type="submit"]');

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (errorEl) errorEl.textContent = '';

    const body = textarea.value.trim();
    if (!body) {
      if (errorEl) errorEl.textContent = 'Vui lòng nhập nội dung bình luận.';
      return;
    }

    submitBtn.disabled = true;
    const originalText = submitBtn.textContent;
    submitBtn.textContent = 'Đang gửi...';

    try {
      const newComment = await commentApi.create(questionId, {
        body,
        parentCommentId: null,
      });

      textarea.value = '';

      if (commentListEl) {
        const old = commentListEl.innerHTML;
        const html = renderComment(newComment);
        if (old.includes('Chưa có bình luận nào')) {
          commentListEl.innerHTML = html;
        } else {
          commentListEl.innerHTML = html + old;
        }
      }
    } catch (err) {
      console.error(err);
      if (errorEl) {
        errorEl.textContent =
          'Không gửi được bình luận (có thể cần đăng nhập hoặc server lỗi).';
      }
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = originalText;
    }
  });
}

function initQuestionDetailActions(questionId) {
  const root = document.querySelector('[data-role="question-detail"]');
  if (!root) return;

  root.addEventListener('click', async (e) => {
    const target = e.target;

    if (target.matches('[data-action="edit-question"]')) {
      window.location.href = `/page/question/question-edit.html?id=${questionId}`;
      return;
    }

    if (target.matches('[data-action="vote-up"]')) {
      await handleVote(questionId, 1);
      return;
    }

    if (target.matches('[data-action="vote-down"]')) {
      await handleVote(questionId, -1);
      return;
    }
  });
}

async function handleVote(questionId, value) {
  try {
    await questionApi.vote(questionId, value);
    // vote xong, load lại detail để score & myVote cập nhật
    const questionEl = document.querySelector('[data-role="question-detail"]');
    if (questionEl) {
      await loadQuestion(questionId, questionEl);
      // re-init actions vì innerHTML đã thay
      initQuestionDetailActions(questionId);
    }
  } catch (e) {
    console.error('Vote failed', e);
    // Bạn có thể hiển thị toast / message lỗi nếu muốn
  }
}

/* ---------- HISTORY PANEL (Question + Comment revisions) ---------- */

const HISTORY_MODE = {
  QUESTION: 'question',
  COMMENT: 'comment',
};

function initHistorySection(questionId) {
  const wrapper = document.querySelector('.question-history');
  if (!wrapper) return;

  const listEl = wrapper.querySelector('[data-role="history-list"]');
  const detailEl = wrapper.querySelector('[data-role="history-detail"]');
  const titleEl = wrapper.querySelector('[data-role="history-title"]');
  const subtitleEl = wrapper.querySelector('[data-role="history-subtitle"]');
  const backBtn = wrapper.querySelector(
    '[data-action="history-back-to-question"]'
  );

  if (!listEl || !detailEl || !titleEl || !subtitleEl) return;

  // lưu context cho panel
  wrapper.dataset.mode = HISTORY_MODE.QUESTION;
  wrapper.dataset.questionId = String(questionId);

  // nút quay lại (chỉ hiện khi đang xem lịch sử comment)
  if (backBtn) {
    backBtn.addEventListener('click', () => {
      showQuestionHistory(wrapper);
    });
  }

  // load lịch sử câu hỏi lần đầu
  showQuestionHistory(wrapper);
}

function showQuestionHistory(wrapper) {
  const questionId = Number(wrapper.dataset.questionId);
  if (!questionId) return;

  const listEl = wrapper.querySelector('[data-role="history-list"]');
  const detailEl = wrapper.querySelector('[data-role="history-detail"]');
  const titleEl = wrapper.querySelector('[data-role="history-title"]');
  const subtitleEl = wrapper.querySelector('[data-role="history-subtitle"]');
  const backBtn = wrapper.querySelector(
    '[data-action="history-back-to-question"]'
  );

  titleEl.textContent = 'Lịch sử câu hỏi';
  subtitleEl.textContent = 'Các lần chỉnh sửa nội dung câu hỏi.';
  if (backBtn) backBtn.hidden = true;

  wrapper.dataset.mode = HISTORY_MODE.QUESTION;
  delete wrapper.dataset.commentId; // clear context comment

  loadHistoryList({
    mode: HISTORY_MODE.QUESTION,
    questionId,
    listEl,
    detailEl,
  });
}

function showCommentHistory(wrapper, commentId) {
  const questionId = Number(wrapper.dataset.questionId);
  if (!questionId || !commentId) return;

  const listEl = wrapper.querySelector('[data-role="history-list"]');
  const detailEl = wrapper.querySelector('[data-role="history-detail"]');
  const titleEl = wrapper.querySelector('[data-role="history-title"]');
  const subtitleEl = wrapper.querySelector('[data-role="history-subtitle"]');
  const backBtn = wrapper.querySelector(
    '[data-action="history-back-to-question"]'
  );

  titleEl.textContent = 'Lịch sử bình luận';
  subtitleEl.textContent = 'Các lần chỉnh sửa bình luận này.';
  if (backBtn) backBtn.hidden = false;

  wrapper.dataset.mode = HISTORY_MODE.COMMENT;
  wrapper.dataset.commentId = String(commentId);

  loadHistoryList({
    mode: HISTORY_MODE.COMMENT,
    questionId,
    commentId,
    listEl,
    detailEl,
  });
}

async function loadHistoryList({
  mode,
  questionId,
  commentId,
  listEl,
  detailEl,
}) {
  if (!listEl || !detailEl) return;

  listEl.innerHTML =
    '<p class="text-muted">Đang tải lịch sử chỉnh sửa...</p>';
  detailEl.innerHTML = '';

  try {
    let paged;
    if (mode === HISTORY_MODE.QUESTION) {
      paged = await questionRevisionApi.getList(questionId, {
        page: 1,
        pageSize: 20,
      });
    } else {
      paged = await commentRevisionApi.getList(questionId, commentId, {
        page: 1,
        pageSize: 20,
      });
    }

    const items = paged.items || paged.content || [];
    if (!items.length) {
      listEl.innerHTML = '<p class="text-muted">Chưa có lần chỉnh sửa nào.</p>';
      detailEl.innerHTML = '';
      return;
    }

    listEl.innerHTML = items.map(renderHistoryListItem).join('');

    // chọn item đầu tiên
    const first = listEl.querySelector('[data-history-id]');
    if (first) {
      first.classList.add('is-active');
      const historyId = Number(first.dataset.historyId);
      await loadHistoryDetail({
        mode,
        questionId,
        commentId,
        revisionId: historyId,
        detailEl,
        listEl,
      });
    }

    // gắn click cho từng item
    listEl.querySelectorAll('[data-history-id]').forEach((itemEl) => {
      itemEl.addEventListener('click', async () => {
        const historyId = Number(itemEl.dataset.historyId);
        listEl
          .querySelectorAll('.history-item')
          .forEach((el) => el.classList.remove('is-active'));
        itemEl.classList.add('is-active');

        await loadHistoryDetail({
          mode,
          questionId,
          commentId,
          revisionId: historyId,
          detailEl,
          listEl,
        });
      });
    });
  } catch (err) {
    console.error(err);
    listEl.innerHTML =
      '<p class="text-muted">Không tải được lịch sử chỉnh sửa.</p>';
  }
}

async function loadHistoryDetail({
  mode,
  questionId,
  commentId,
  revisionId,
  detailEl,
}) {
  if (!detailEl) return;

  detailEl.innerHTML = '<p class="text-muted">Đang tải chi tiết...</p>';

  try {
    let dto;
    if (mode === HISTORY_MODE.QUESTION) {
      dto = await questionRevisionApi.getDetail(questionId, revisionId);
      detailEl.innerHTML = renderQuestionRevisionDetail(dto);
    } else {
      dto = await commentRevisionApi.getDetail(
        questionId,
        commentId,
        revisionId
      );
      detailEl.innerHTML = renderCommentRevisionDetail(dto);
    }
  } catch (err) {
    console.error(err);
    detailEl.innerHTML =
      '<p class="text-muted">Không tải được chi tiết chỉnh sửa.</p>';
  }
}

function renderHistoryListItem(r) {
  const created = formatDate(r.createdAt);

  return `
    <div class="history-item" data-history-id="${r.id}">
      <div class="history-item__summary">
        ${escapeHtml(r.summary || 'Chỉnh sửa')}
      </div>
      <div class="history-item__meta">
        <span>${escapeHtml(r.editorDisplayName || 'Unknown')}</span>
        <span>${created}</span>
      </div>
    </div>
  `;
}

function renderQuestionRevisionDetail(r) {
  const created = formatDate(r.createdAt);

  return `
    <div class="history-detail__header">
      <div>
        <div class="history-detail__editor">
          ${escapeHtml(r.editorDisplayName || 'Unknown')}
        </div>
        <div class="history-detail__time text-muted">${created}</div>
      </div>
      ${
        r.summary
          ? `<div class="history-detail__summary">${escapeHtml(
              r.summary
            )}</div>`
          : ''
      }
    </div>

    <div class="history-detail__content">
      <div class="history-detail__block">
        <h3 class="history-detail__block-title">Trước khi sửa</h3>
        ${
          r.beforeTitle
            ? `<h4 class="history-detail__title">${escapeHtml(
                r.beforeTitle
              )}</h4>`
            : ''
        }
        ${
          r.beforeBody
            ? `<pre class="history-detail__body">${escapeHtml(
                r.beforeBody
              )}</pre>`
            : '<p class="text-muted">Không có nội dung.</p>'
        }
      </div>

      <div class="history-detail__block">
        <h3 class="history-detail__block-title">Sau khi sửa</h3>
        ${
          r.afterTitle
            ? `<h4 class="history-detail__title">${escapeHtml(
                r.afterTitle
              )}</h4>`
            : ''
        }
        ${
          r.afterBody
            ? `<pre class="history-detail__body">${escapeHtml(
                r.afterBody
              )}</pre>`
            : '<p class="text-muted">Không có nội dung.</p>'
        }
      </div>
    </div>
  `;
}

function renderCommentRevisionDetail(r) {
  const created = formatDate(r.createdAt);

  return `
    <div class="history-detail__header">
      <div>
        <div class="history-detail__editor">
          ${escapeHtml(r.editorDisplayName || 'Unknown')}
        </div>
        <div class="history-detail__time text-muted">${created}</div>
      </div>
      ${
        r.summary
          ? `<div class="history-detail__summary">${escapeHtml(
              r.summary
            )}</div>`
          : ''
      }
    </div>

    <div class="history-detail__content history-detail__content--single">
      <div class="history-detail__block">
        <h3 class="history-detail__block-title">Trước khi sửa</h3>
        ${
          r.beforeBody
            ? `<pre class="history-detail__body">${escapeHtml(
                r.beforeBody
              )}</pre>`
            : '<p class="text-muted">Không có nội dung.</p>'
        }
      </div>

      <div class="history-detail__block">
        <h3 class="history-detail__block-title">Sau khi sửa</h3>
        ${
          r.afterBody
            ? `<pre class="history-detail__body">${escapeHtml(
                r.afterBody
              )}</pre>`
            : '<p class="text-muted">Không có nội dung.</p>'
        }
      </div>
    </div>
  `;
}

function initCommentHistoryActions(questionId, commentListEl) {
  const wrapper = document.querySelector('.question-history');
  if (!wrapper || !commentListEl) return;

  commentListEl.addEventListener('click', async (e) => {
    const historyBtn = e.target.closest('[data-action="view-comment-history"]');
    if (historyBtn) {
      const commentId = Number(historyBtn.dataset.commentId);
      if (!commentId) return;
      showCommentHistory(wrapper, commentId);
      return;
    }

    const editBtn = e.target.closest('[data-action="edit-comment"]');
    if (editBtn) {
      const commentId = Number(editBtn.dataset.commentId);
      if (!commentId) return;
      const article = editBtn.closest('.comment-card');
      if (!article) return;
      startEditComment(questionId, commentId, article);
      return;
    }

    const saveBtn = e.target.closest('[data-action="save-comment"]');
    if (saveBtn) {
      const commentId = Number(saveBtn.dataset.commentId);
      if (!commentId) return;
      const article = saveBtn.closest('.comment-card');
      if (!article) return;
      await saveEditedComment(questionId, commentId, article, saveBtn);
      return;
    }

    const cancelBtn = e.target.closest('[data-action="cancel-edit-comment"]');
    if (cancelBtn) {
      const article = cancelBtn.closest('.comment-card');
      if (!article) return;
      cancelEditComment(article);
      return;
    }
  });
}



/* ---------- Render helpers ---------- */

function renderQuestionDetail(q) {
  const {
    id,
    title,
    body,
    authorDisplayName,
    createdAt,
    score,
    tags,
    categoryName,
    myVote,
  } = q;

  const created = formatDate(createdAt);
  const tagList = tags || [];

  // vote UI
  const voteUpActive = myVote === 1 ? 'is-active' : '';
  const voteDownActive = myVote === -1 ? 'is-active' : '';

  return `
    <header class="question-detail__header" data-question-id="${id}">
      <div class="question-detail__vote">
        <button class="vote-btn vote-btn--up ${voteUpActive}" data-action="vote-up">
          ▲
        </button>
        <span class="question-detail__score" data-role="question-score">${score ?? 0}</span>
        <button class="vote-btn vote-btn--down ${voteDownActive}" data-action="vote-down">
          ▼
        </button>
      </div>

      <div class="question-detail__info">
        <h1 class="question-detail__title">${escapeHtml(title)}</h1>
        <div class="question-detail__meta">
          ${
            categoryName
              ? `<span class="tag-pill tag-pill--category">${escapeHtml(
                  categoryName
                )}</span>`
              : ''
          }
          <span class="question-detail__author">
            by <strong>${escapeHtml(authorDisplayName || 'Unknown')}</strong>
          </span>
          <span class="question-detail__time">${created}</span>
        </div>
        <button class="btn btn-ghost" data-action="edit-question">
          Edit question
        </button>
      </div>
    </header>

    <div class="question-detail__body">
      ${body ? body : ''}
    </div>

    <footer class="question-detail__tags">
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
    </footer>
  `;
}

function startEditComment(questionId, commentId, articleEl) {
  if (articleEl.dataset.editing === 'true') return;
  articleEl.dataset.editing = 'true';

  const bodyEl = articleEl.querySelector('.comment-card__body');
  if (!bodyEl) return;

  const originalBody = bodyEl.textContent || '';
  articleEl.dataset.originalBody = originalBody;

  bodyEl.innerHTML = `
    <textarea class="form-input comment-edit-textarea">${originalBody}</textarea>
  `;

  // thêm nút Lưu / Hủy phía dưới meta
  let actionsEl = articleEl.querySelector('.comment-card__actions');
  if (!actionsEl) {
    actionsEl = document.createElement('div');
    actionsEl.className = 'comment-card__actions';
    const metaEl = articleEl.querySelector('.comment-card__meta');
    metaEl.appendChild(actionsEl);
  }

  actionsEl.innerHTML = `
    <button
      type="button"
      class="btn btn-primary btn-sm"
      data-action="save-comment"
      data-comment-id="${commentId}"
    >
      Lưu
    </button>
    <button
      type="button"
      class="btn btn-ghost btn-sm"
      data-action="cancel-edit-comment"
      data-comment-id="${commentId}"
    >
      Hủy
    </button>
  `;
}

async function saveEditedComment(questionId, commentId, articleEl, saveBtn) {
  const textarea = articleEl.querySelector('.comment-edit-textarea');
  const bodyEl = articleEl.querySelector('.comment-card__body');
  const actionsEl = articleEl.querySelector('.comment-card__actions');

  if (!textarea || !bodyEl || !actionsEl) return;

  const newBody = textarea.value.trim();
  if (!newBody) {
    alert('Nội dung bình luận không được để trống.');
    return;
  }

  saveBtn.disabled = true;
  const originalText = saveBtn.textContent;
  saveBtn.textContent = 'Đang lưu...';

  try {
    // gọi API update
    const updated = await commentApi.update(questionId, commentId, {
      body: newBody,
    });

    // cập nhật UI
    const finalBody = updated?.body || newBody;
    bodyEl.innerHTML = escapeHtml(finalBody);
    articleEl.dataset.originalBody = finalBody;
    articleEl.dataset.editing = 'false';
    actionsEl.innerHTML = ''; // ẩn nút Lưu / Hủy
  } catch (err) {
    console.error(err);
    alert('Không sửa được bình luận (có thể cần đăng nhập hoặc server lỗi).');
  } finally {
    saveBtn.disabled = false;
    saveBtn.textContent = originalText;
  }
}

function cancelEditComment(articleEl) {
  const bodyEl = articleEl.querySelector('.comment-card__body');
  const actionsEl = articleEl.querySelector('.comment-card__actions');
  if (!bodyEl) return;

  const originalBody = articleEl.dataset.originalBody || '';
  bodyEl.innerHTML = escapeHtml(originalBody);
  articleEl.dataset.editing = 'false';
  if (actionsEl) actionsEl.innerHTML = '';
}


function renderComment(c) {
  // dto có thể là { id, ... } hoặc { commentId, ... } nên mình handle cả 2
  const commentId = c.id ?? c.commentId;
  const { body, authorDisplayName, createdAt } = c;
  const created = formatDate(createdAt);

  return `
    <article class="comment-card" data-comment-id="${commentId ?? ''}">
      <div class="comment-card__body">
        ${escapeHtml(body)}
      </div>
      <div class="comment-card__meta">
        <span class="comment-card__author">
          ${escapeHtml(authorDisplayName || 'Unknown')}
        </span>
        <span class="comment-card__time">
          ${created}
        </span>

        <button
          type="button"
          class="link-button comment-card__history"
          data-action="view-comment-history"
          data-comment-id="${commentId ?? ''}"
        >
          Lịch sử
        </button>

        <button
          type="button"
          class="link-button comment-card__edit"
          data-action="edit-comment"
          data-comment-id="${commentId ?? ''}"
        >
          Sửa
        </button>
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
