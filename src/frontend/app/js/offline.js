var offline = {
  banner: null,

  init: function () {
    this.banner = document.getElementById('offline-banner');
    window.addEventListener('online', this.updateStatus.bind(this));
    window.addEventListener('offline', this.updateStatus.bind(this));
    this.updateStatus();
  },

  updateStatus: function () {
    if (navigator.onLine) {
      this.hideBanner();
    } else {
      this.showBanner();
    }
  },

  showBanner: function () {
    this.banner.classList.add('visible');
    this.banner.classList.add('flash');
    this.banner.addEventListener('animationend', function () {
      this.classList.remove('flash');
    }, { once: true });
  },

  hideBanner: function () {
    this.banner.classList.remove('visible', 'flash');
  }
};
