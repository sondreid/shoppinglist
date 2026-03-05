const auth = {
  isAuthenticated: false,
  user: null,
  token: null,

  async init() {
    await this.loadGoogleIdentity();
    this.checkExistingAuth();
  },

  async loadGoogleIdentity() {
    return new Promise((resolve) => {
      if (window.google) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      document.head.appendChild(script);
    });
  },

  getApiBase() {
    return window.location.port === '4000' ? 'http://localhost:5058' : '/api';
  },

  async getClientId() {
    try {
      const response = await fetch(`${this.getApiBase()}/config`);
      const config = await response.json();
      return config.googleClientId;
    } catch (error) {
      console.error('Failed to get client ID:', error);
      return null;
    }
  },

  async initializeGoogleSignIn() {
    if (!window.google || !window.google.accounts) {
      await this.loadGoogleIdentity();
    }

    const clientId = await this.getClientId();
    if (!clientId) {
      console.error('Google Client ID not available');
      return;
    }

    if (!window.google || !window.google.accounts) {
      console.error('Google Identity Services not loaded');
      return;
    }

    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: this.handleCredentialResponse.bind(this)
    });

    const buttonContainer = document.getElementById('google-signin-button');
    if (buttonContainer) {
      window.google.accounts.id.renderButton(
        buttonContainer,
        { theme: 'outline', size: 'large' }
      );
    }
  },

  async handleCredentialResponse(response) {
    const idToken = response.credential;
    
    try {
      const result = await fetch(`${this.getApiBase()}/auth/google`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token: idToken })
      });

      if (result.ok) {
        const data = await result.json();
        this.token = data.sessionToken;
        this.user = data.user || { name: 'User', email: '' };
        this.isAuthenticated = true;
        localStorage.setItem('auth_token', data.sessionToken);
        localStorage.setItem('user', JSON.stringify(this.user));
        
        if (window.location.pathname.includes('index.html') || window.location.pathname === '/') {
          window.location.href = 'app.html';
        } else {
          this.updateAuthUI();
        }
      } else {
        console.error('Authentication failed');
      }
    } catch (error) {
      console.error('Auth error:', error);
    }
  },

  checkExistingAuth() {
    const token = localStorage.getItem('auth_token');
    const userStr = localStorage.getItem('user');
    
    if (token && userStr) {
      this.token = token;
      this.user = JSON.parse(userStr);
      this.isAuthenticated = true;
      
      if (window.location.pathname.includes('index.html') || window.location.pathname === '/') {
        window.location.href = 'app.html';
      } else {
        this.updateAuthUI();
      }
    } else {
      if (!window.location.pathname.includes('index.html') && window.location.pathname !== '/') {
        window.location.href = 'index.html';
      } else {
        this.showSignInButton();
      }
    }
  },

  signOut() {
    if (window.google) {
      window.google.accounts.id.disableAutoSelect();
    }
    
    this.isAuthenticated = false;
    this.user = null;
    this.token = null;
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user');
    window.location.href = 'index.html';
  },

  updateAuthUI() {
    const authContainer = document.getElementById('auth-container');
    if (!authContainer) return;

    if (this.isAuthenticated) {
      authContainer.innerHTML = `
        <div class="d-flex align-items-center">
          <span class="me-2">${this.user.name || this.user.email}</span>
          <button class="btn btn-sm btn-outline-secondary" onclick="auth.signOut()">Sign Out</button>
        </div>
      `;
    } else {
      this.showSignInButton();
    }
  },

  showSignInButton() {
    const authContainer = document.getElementById('auth-container');
    if (!authContainer) return;

    authContainer.innerHTML = '<div id="google-signin-button"></div>';
    this.initializeGoogleSignIn();
  },

  getAuthHeaders() {
    if (!this.isAuthenticated || !this.token) {
      return { 'Content-Type': 'application/json' };
    }
    
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.token}`
    };
  }
};
