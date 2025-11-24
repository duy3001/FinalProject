// assets/js/features/notification/notifications.js
import { notificationApi } from '../../api/notification.api.js';

export async function initNotificationsPage() {
  const listEl = document.querySelector('[data-role="notification-list"]');
  if (!listEl) return;

  listEl.innerHTML = '<p class="text-muted">Loading...</p>';

  try {
    const paged = await notificationApi.getList({ page: 1, pageSize: 20 });
    const items = paged.items || [];

    if (!items.length) {
      listEl.innerHTML = '<p class="text-muted">Không có thông báo.</p>';
      return;
    }

    listEl.innerHTML = items.map(renderNotificationRow).join('');

    listEl.querySelectorAll('.notifications-row').forEach((row) => {
      row.addEventListener('click', async () => {
        const id = Number(row.dataset.id);
        const url = row.dataset.actionUrl;

        if (!row.classList.contains('notifications-row--read')) {
          try {
            await notificationApi.markRead([id]);
            row.classList.add('notifications-row--read');
          } catch (e) {
            console.error(e);
          }
        }

        if (url) window.location.href = url;
      });
    });
  } catch (e) {
    console.error(e);
    listEl.innerHTML =
      '<p class="text-muted">Không tải được danh sách thông báo.</p>';
  }
}

function renderNotificationRow(n) {
  const isUnread = !n.isRead;
  return `
    <article class="notifications-row ${
      isUnread ? 'notifications-row--unread' : 'notifications-row--read'
    }"
      data-id="${n.id}"
      data-action-url="${n.actionUrl || ''}">
      <div class="notifications-row__content">
        <h2 class="notifications-row__title">
          ${n.postTitle || n.type || 'Notification'}
        </h2>
        ${
          n.excerpt
            ? `<p class="notifications-row__excerpt">${n.excerpt}</p>`
            : ''
        }
        <p class="notifications-row__meta text-muted">
          ${n.actorName ? `${n.actorName} · ` : ''}${n.createdAt || ''}
        </p>
      </div>
    </article>
  `;
}
