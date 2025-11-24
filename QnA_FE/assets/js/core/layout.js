// assets/js/core/layout.js
import { auth } from './auth.js';
import { qs, qsa } from './dom.js';
import { notificationApi } from '../api/notification.api.js';


async function loadPartial(selector, url) {
  const container = qs(selector);
  if (!container) return;
  const res = await fetch(url);
  container.innerHTML = await res.text();
}

async function loadPartials() {
  await Promise.all([
    loadPartial('[data-partial="header"]', '/partials/header.html'),
    loadPartial('[data-partial="footer"]', '/partials/footer.html'),
  ]);
}

function setActiveNav() {
  const path = location.pathname;
  const current = path.split('/').pop(); // ví dụ questions.html

  qsa('.main-nav a').forEach((a) => {
    const href = a.getAttribute('href') || '';
    const last = href.split('/').pop();
    if (last === current) {
      a.classList.add('active');
    } else {
      a.classList.remove('active');
    }
  });
}

function initHeaderSearch() {
  const btn = qs('[data-action="toggle-search"]');
  const form = qs('[data-role="header-search"]');

  if (!btn || !form) return;

  btn.addEventListener('click', () => {
    form.classList.toggle('is-open');
    const input = form.querySelector('input[name="q"]');
    if (form.classList.contains('is-open') && input) input.focus();
  });

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    const formData = new FormData(form);
    const q = (formData.get('q') || '').toString().trim();
    if (!q) return;

    // TRƯỚC: /search.html?q=...
    // SAU: thêm type=posts
    location.href = `/page/search/search.html?q=${encodeURIComponent(
      q
    )}&type=posts`;
});
}

function initUserMenu() {
  const userMenu = qs('[data-role="user-menu"]');
  if (!userMenu) return;

  const me = auth.getCurrentUser();

  if (!me) {
    userMenu.innerHTML = `
      <button class="btn btn-ghost" data-action="login-popup">
        Đăng nhập
      </button>
    `;
  } else {
    userMenu.innerHTML = `
      <div class="user-menu__inner">
        <span class="user-menu__name">${me.displayName || me.email}</span>
        <button class="btn btn-ghost" data-action="logout">Đăng xuất</button>
      </div>
    `;

    userMenu
      .querySelector('[data-action="logout"]')
      .addEventListener('click', () => {
        auth.clearAuth();
        location.href = '/page/auth/login.html';
      });
  }
}

function initNotificationWidget() {
  const wrapper = document.querySelector('[data-role="notification"]');
  if (!wrapper) return;

  const toggleBtn = wrapper.querySelector(
    '[data-action="toggle-notifications"]'
  );
  const dropdown = wrapper.querySelector(
    '[data-role="notification-dropdown"]'
  );
  const itemsContainer = wrapper.querySelector(
    '[data-role="notification-items"]'
  );
  const badge = wrapper.querySelector('[data-role="notification-badge"]');
  const viewAllBtn = wrapper.querySelector(
    '[data-action="view-all-notifications"]'
  );

  if (!toggleBtn || !dropdown || !itemsContainer) return;

  // Ẩn nếu chưa đăng nhập (tuỳ bạn)
  // import { auth } ở đầu nếu muốn dùng
  // if (!auth.isAuthenticated()) {
  //   wrapper.style.display = 'none';
  //   return;
  // }

  toggleBtn.addEventListener('click', async (e) => {
    e.stopPropagation();
    const isOpen = dropdown.classList.contains('is-open');
    document
      .querySelectorAll('.notification-dropdown.is-open')
      .forEach((el) => el.classList.remove('is-open'));

    if (!isOpen) {
      dropdown.classList.add('is-open');
      await loadNotificationsPreview(itemsContainer, badge);
    }
  });

  // click ngoài dropdown thì đóng
  document.addEventListener('click', () => {
    dropdown.classList.remove('is-open');
  });
  dropdown.addEventListener('click', (e) => e.stopPropagation());

  // View all -> sang trang notifications
  if (viewAllBtn) {
    viewAllBtn.addEventListener('click', () => {
      window.location.href = '/page/notification/notifications.html';
    });
  }

  // load badge lần đầu
  loadUnreadCount(badge).catch(console.error);
}

async function loadUnreadCount(badgeEl) {
  if (!badgeEl) return;
  try {
    const count = await notificationApi.getUnreadCount();
    if (!count || count <= 0) {
      badgeEl.classList.remove('is-visible');
      badgeEl.textContent = '';
    } else {
      badgeEl.textContent = count > 9 ? '9+' : String(count);
      badgeEl.classList.add('is-visible');
    }
  } catch (e) {
    console.error(e);
  }
}

async function loadNotificationsPreview(itemsContainer, badgeEl) {
  try {
    itemsContainer.innerHTML = '<p class="text-muted">Loading...</p>';
    const paged = await notificationApi.getList({ page: 1, pageSize: 5 });

    const items = paged.items || [];
    if (!items.length) {
      itemsContainer.innerHTML = '<p class="text-muted">No notifications.</p>';
      badgeEl?.classList.remove('is-visible');
      badgeEl.textContent = '';
      return;
    }

    itemsContainer.innerHTML = items
      .map(
        (n) => `
        <div class="notification-item ${
          n.isRead ? '' : 'notification-item--unread'
        }"
             data-id="${n.id}"
             data-action-url="${n.actionUrl || ''}">
          <div class="notification-item__title">
            ${n.postTitle || n.type || 'Notification'}
          </div>
          ${
            n.excerpt
              ? `<div class="notification-item__excerpt">${n.excerpt}</div>`
              : ''
          }
          <span class="notification-item__time">
            ${n.actorName ? `${n.actorName} · ` : ''}${n.createdAt || ''}
          </span>
        </div>
      `
      )
      .join('');

    // click 1 item -> mark read + chuyển trang (nếu có actionUrl)
    itemsContainer
      .querySelectorAll('.notification-item')
      .forEach((itemEl) => {
        itemEl.addEventListener('click', async () => {
          const id = Number(itemEl.dataset.id);
          const url = itemEl.dataset.actionUrl;

          try {
            await notificationApi.markRead([id]);
            itemEl.classList.remove('notification-item--unread');
            // cập nhật badge
            await loadUnreadCount(badgeEl);
          } catch (e) {
            console.error(e);
          }

          if (url) {
            window.location.href = url;
          }
        });
      });
  } catch (e) {
    console.error(e);
    itemsContainer.innerHTML =
      '<p class="text-muted">Không tải được notifications.</p>';
  }
}

function initAskQuestionButton() {
  const btn = qs('[data-action="ask-question"]');
  if (!btn) return;
  btn.addEventListener('click', () => {
    window.location.href = '/page/question/question-create.html';
  });
}

export async function initLayout() {
  await loadPartials(); // inject header/footer
  setActiveNav();       // tô màu nav hiện tại
  initHeaderSearch();   // behavior search chung
  initUserMenu();       // hiển thị user / login
  initNotificationWidget(); // hiển thị notifications
  initAskQuestionButton(); // nút hỏi question
}
