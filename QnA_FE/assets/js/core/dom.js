// assets/js/core/dom.js
export const qs = (selector, scope = document) => scope.querySelector(selector);
export const qsa = (selector, scope = document) =>
  Array.from(scope.querySelectorAll(selector));

export function on(event, selector, handler, scope = document) {
  scope.addEventListener(event, (e) => {
    if (e.target.matches(selector) || e.target.closest(selector)) {
      handler(e);
    }
  });
}
