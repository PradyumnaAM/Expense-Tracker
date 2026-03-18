// Auto-dismiss toasts
document.addEventListener('DOMContentLoaded', function () {
  var toasts = document.querySelectorAll('.toast');
  toasts.forEach(function (t) {
    setTimeout(function () { t.remove(); }, 3500);
  });
});
