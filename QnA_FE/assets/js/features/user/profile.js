// assets/js/features/user/profile.js
import { profileApi } from '../../api/profile.api.js';
import { env } from '../../core/env.js';

export async function initProfilePage() {
  const url = new URL(window.location.href);
  const idParam = url.searchParams.get('id');

  const profileEl = document.querySelector('[data-role="profile-details"]');
  const activityEl = document.querySelector('[data-role="user-activity"]');
  // const statsEl = document.querySelector('[data-role="user-stats"]'); // nếu sau muốn dùng

  if (!profileEl) return;

  const isMe = !idParam;
  try {
    if (isMe) {
      const me = await profileApi.getMe();
      renderMyProfile(me, profileEl);
      if (activityEl) {
        const userId = me.id ?? me.userId;
        await loadActivities(userId, activityEl);
      }
      initProfileEditForm(me, profileEl);
      initAvatarUpload(profileEl);
    } else {
      const userId = Number(idParam);
      if (!userId) {
        profileEl.innerHTML = '<p>Không xác định được người dùng.</p>';
        if (activityEl) activityEl.innerHTML = '';
        return;
      }

      const publicUser = await profileApi.getPublicUser(userId);
      renderPublicProfile(publicUser, profileEl);
      if (activityEl) {
        await loadActivities(publicUser.id, activityEl);
      }
      // public profile: không cho sửa, không init form/avatar
    }
  } catch (err) {
    console.error(err);
    profileEl.innerHTML = '<p>Không tải được thông tin người dùng.</p>';
    if (activityEl) {
      activityEl.innerHTML = '<p class="text-muted">Không tải được hoạt động.</p>';
    }
  }
}

/* ---------- RENDER PROFILE ---------- */

function resolveAvatarUrl(url) {
  if (!url) return '';

  // Nếu BE trả full url (http/https) thì dùng luôn
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }

  // Nếu BE trả "/uploads/..." hoặc "uploads/..."
  const base = env.BASE_API_URL.replace(/\/+$/, ''); // bỏ / cuối nếu có
  const path = url.startsWith('/') ? url : `/${url}`;
  return base + path;
}


function renderMyProfile(me, container) {
  const joined = formatDateTime(me.createdAt);
  const firstLetter = (me.displayName || me.userId || 'U')
    .toString()
    .charAt(0)
    .toUpperCase();


    const avatarUrl = resolveAvatarUrl(me.profilePictureUrl);
    
  const roles = [];
  if (me.isAdministrator) roles.push('Administrator');
  if (me.isModerator) roles.push('Moderator');
  if (me.isTemporarilySuspended)
    roles.push('Temporarily suspended');

  container.innerHTML = `
    <div class="profile-card__header">
      <div class="profile-avatar-wrap">
        ${
          me.profilePictureUrl
            ? `<img src="${me.profilePictureUrl}" alt="${escapeHtml(
                me.displayName || 'Avatar'
              )}" class="profile-avatar" data-role="profile-avatar" />`
            : `<div class="profile-avatar-fallback" data-role="profile-avatar-fallback">
                 ${escapeHtml(firstLetter)}
               </div>`
        }
        <div class="profile-avatar-upload">
          <label class="profile-avatar-upload-label">
            Đổi avatar
            <input type="file" accept="image/*" data-role="avatar-input" />
          </label>
        </div>
      </div>

      <div class="profile-card__info">
        <h1 class="profile-card__name">
          ${escapeHtml(me.displayName || 'Không tên')}
        </h1>
        <p class="profile-card__meta">
          Tham gia từ: ${joined || 'Không rõ'}
        </p>

        ${
          roles.length
            ? `
          <div class="profile-role-badges">
            ${roles
              .map(
                (r) => `<span class="profile-role-badge">${escapeHtml(r)}</span>`
              )
              .join('')}
          </div>
        `
            : ''
        }

        ${
          me.isTemporarilySuspended && me.temporarySuspensionEndAt
            ? `<p class="text-muted">
                 Tạm khóa đến: ${formatDateTime(
                   me.temporarySuspensionEndAt
                 )}<br/>
                 Lý do: ${escapeHtml(me.temporarySuspensionReason || '')}
               </p>`
            : ''
        }

        <p class="profile-card__bio">
          ${escapeHtml(me.bio || 'Chưa có giới thiệu.')}
        </p>
      </div>
    </div>

    <form class="profile-edit-form" data-form="profile-edit">
      <div class="form-group">
        <label class="form-label" for="displayName">Display name</label>
        <input
          id="displayName"
          name="displayName"
          class="form-input"
          type="text"
          value="${escapeHtml(me.displayName || '')}"
        />
      </div>

      <div class="form-group">
        <label class="form-label" for="bio">Bio</label>
        <textarea
          id="bio"
          name="bio"
          class="form-input"
          rows="3"
        >${escapeHtml(me.bio || '')}</textarea>
      </div>

      <p class="form-error" data-role="profile-error"></p>

      <button class="btn btn-primary" type="submit">
        Lưu thay đổi
      </button>
    </form>
  `;
}

function renderPublicProfile(user, container) {
  const joined = formatDateTime(user.joinedAt);
  const firstLetter = (user.displayName || user.id || 'U')
    .toString()
    .charAt(0)
    .toUpperCase();

  const avatarUrl = resolveAvatarUrl(user.profilePictureUrl);

  container.innerHTML = `
    <div class="profile-card__header">
      <div class="profile-avatar-wrap">
        ${
          user.profilePictureUrl
            ? `<img src="${user.profilePictureUrl}" alt="${escapeHtml(
                user.displayName || 'Avatar'
              )}" class="profile-avatar" />`
            : `<div class="profile-avatar-fallback">
                 ${escapeHtml(firstLetter)}
               </div>`
        }
      </div>

      <div class="profile-card__info">
        <h1 class="profile-card__name">
          ${escapeHtml(user.displayName || 'Không tên')}
        </h1>
        <p class="profile-card__meta">
          Tham gia từ: ${joined || 'Không rõ'}
        </p>
        <p class="profile-card__bio text-muted">
          Người dùng này chưa có thông tin giới thiệu.
        </p>
      </div>
    </div>
  `;
}

/* ---------- EDIT PROFILE (MeUpdateRequest) ---------- */

function initProfileEditForm(me, container) {
  const form = container.querySelector('[data-form="profile-edit"]');
  if (!form) return;

  const errorEl = form.querySelector('[data-role="profile-error"]');
  const submitBtn = form.querySelector('button[type="submit"]');

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (errorEl) errorEl.textContent = '';

    const formData = new FormData(form);
    const displayName = formData.get('displayName')?.toString().trim();
    const bio = formData.get('bio')?.toString().trim();

    if (!displayName) {
      if (errorEl) errorEl.textContent = 'Display name không được để trống.';
      return;
    }

    submitBtn.disabled = true;
    const originalText = submitBtn.textContent;
    submitBtn.textContent = 'Đang lưu...';

    try {
      const updated = await profileApi.updateMe({ displayName, bio });
      // render lại profile cho đồng bộ
      renderMyProfile(updated, container);
      initProfileEditForm(updated, container);
      initAvatarUpload(container);
    } catch (err) {
      console.error(err);
      if (errorEl) {
        errorEl.textContent =
          'Không lưu được thay đổi (có thể cần đăng nhập hoặc server lỗi).';
      }
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = originalText;
    }
  });
}

/* ---------- AVATAR UPLOAD ---------- */

async function initAvatarUpload(container) {
  const input = container.querySelector('[data-role="avatar-input"]');
  if (!input) return;

  input.addEventListener('change', async () => {
    if (!input.files || !input.files[0]) return;
    const file = input.files[0];

    try {
      const fileDto = await profileApi.updateAvatar(file);
      console.log('avatar fileDto', fileDto);
      const avatarUrl = resolveAvatarUrl(fileDto.url || fileDto.fileName || '');

      const imgEl = container.querySelector('[data-role="profile-avatar"]');
      const fallbackEl = container.querySelector(
        '[data-role="profile-avatar-fallback"]'
      );

      if (imgEl) {
        imgEl.src = avatarUrl;
      } else if (fallbackEl) {
        const wrapper = fallbackEl.parentElement;
        if (wrapper) {
          wrapper.innerHTML = `<img src="${avatarUrl}" class="profile-avatar" data-role="profile-avatar" />`;
        }
      }
    } catch (err) {
      console.error(err);
      alert('Không cập nhật được avatar.');
    } finally {
      input.value = '';
    }
  });
}


/* ---------- ACTIVITIES (ActivityDto) ---------- */

async function loadActivities(userId, container) {
  container.innerHTML = '<p class="text-muted">Đang tải hoạt động...</p>';

  try {
    const paged = await profileApi.getActivities(userId, {
      page: 1,
      pageSize: 20,
    });

    const items = paged.items || paged.content || [];
    if (!items.length) {
      container.innerHTML = '<p class="text-muted">Chưa có hoạt động nào.</p>';
      return;
    }

    container.innerHTML = `
      <div class="activity-list">
        ${items.map(renderActivityItem).join('')}
      </div>
    `;

    container.querySelectorAll('.activity-item').forEach((el) => {
      el.addEventListener('click', () => {
        const postId = Number(el.dataset.postId);
        if (postId) {
          window.location.href = `/page/question/question-detail.html?id=${postId}`;
        }
      });
    });
  } catch (err) {
    console.error(err);
    container.innerHTML =
      '<p class="text-muted">Không tải được hoạt động người dùng.</p>';
  }
}

function renderActivityItem(a) {
  const created = formatDateTime(a.createdAt);
  const typeLabel = mapActivityType(a.type);
  const title =
    a.postTitle ||
    a.excerpt ||
    (a.type === 'vote_post' || a.type === 'vote_comment'
      ? `Vote ${a.deltaScore || ''}`
      : 'Hoạt động');

  return `
    <div class="activity-item" data-post-id="${a.postId || ''}">
      <div class="activity-item__title">
        ${escapeHtml(title)}
      </div>
      ${
        a.excerpt
          ? `<div class="activity-item__excerpt">${escapeHtml(
              a.excerpt
            )}</div>`
          : ''
      }
      <div class="activity-item__meta">
        ${escapeHtml(typeLabel)} · ${created}
      </div>
    </div>
  `;
}

function mapActivityType(type) {
  switch (type) {
    case 'post':
      return 'Đặt câu hỏi / sửa câu hỏi';
    case 'comment':
      return 'Bình luận';
    case 'vote_post':
      return 'Vote câu hỏi';
    case 'vote_comment':
      return 'Vote bình luận';
    default:
      return type || 'Hoạt động';
  }
}

/* ---------- Helpers ---------- */

function formatDateTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function escapeHtml(str = '') {
  return str
    .toString()
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}
