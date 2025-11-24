// assets/js/core/http.js
import { env } from './env.js';
import { auth } from './auth.js';

async function request(method, url, options = {}) {
  const { params, body, headers } = options;

  let fullUrl = env.BASE_API_URL + url;

  // params -> query string
  if (params && typeof params === 'object') {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        searchParams.append(key, String(value));
      }
    });
    const qs = searchParams.toString();
    if (qs) fullUrl += (fullUrl.includes('?') ? '&' : '?') + qs;
  }

  const finalHeaders = {
    ...(headers || {}),
  };

  const token = auth.getAccessToken();
  if (token) {
    finalHeaders.Authorization = `Bearer ${token}`;
  }

  let fetchBody;

  // 🔹 Nếu body là FormData (upload file, multipart)
  if (body instanceof FormData) {
    // KHÔNG set Content-Type, để browser tự set boundary multipart/form-data
    fetchBody = body;
  } else if (body !== undefined && body !== null) {
    // 🔹 Body JSON bình thường
    finalHeaders['Content-Type'] = 'application/json';
    fetchBody = JSON.stringify(body);
  } else {
    fetchBody = undefined;
  }

  const res = await fetch(fullUrl, {
    method,
    headers: finalHeaders,
    body: fetchBody,
  });

  if (!res.ok) {
    const errorBody = await res.text();
    throw new Error(errorBody || `HTTP ${res.status}`);
  }

  // nếu không có body
  if (res.status === 204) return null;

  const contentType = res.headers.get('Content-Type') || '';
  if (contentType.includes('application/json')) {
    return res.json();
  }
  return res.text();
}

export const http = {
  get(url, options) {
    return request('GET', url, options);
  },
  post(url, body, options = {}) {
    return request('POST', url, { ...options, body });
  },
  put(url, body, options = {}) {
    return request('PUT', url, { ...options, body });
  },
  delete(url, options) {
    return request('DELETE', url, options);
  },
};
