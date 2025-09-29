<template>
  <div class="login-card-component">
    <div class="microsoft-login">
      <button 
        @click="handleMicrosoftLogin"
        :disabled="loading"
        class="microsoft-btn"
      >
        <span v-if="!loading" class="microsoft-icon">
          <svg width="20" height="20" viewBox="0 0 21 21">
            <rect x="1" y="1" width="9" height="9" fill="#f25022"/>
            <rect x="12" y="1" width="9" height="9" fill="#00a4ef"/>
            <rect x="1" y="12" width="9" height="9" fill="#00bcf2"/>
            <rect x="12" y="12" width="9" height="9" fill="#ffb900"/>
          </svg>
        </span>
        
        <div v-if="loading" class="loading-spinner"></div>
        
        <span class="button-text">
          {{ loading ? 'Signing in...' : 'Sign in with Microsoft' }}
        </span>
      </button>
    </div>
    
    <div class="divider">
      <span>Secure Authentication</span>
    </div>
    
    <div class="login-info">
      <div class="info-item">
        <span class="info-icon">🏢</span>
        <span>Organization login required</span>
      </div>
      <div class="info-item">
        <span class="info-icon">🔐</span>
        <span>Azure AD protected</span>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'LoginCard',
  props: {
    loading: {
      type: Boolean,
      default: false
    }
  },
  emits: ['login'],
  methods: {
    handleMicrosoftLogin() {
      if (!this.loading) {
        this.$emit('login')
      }
    }
  }
}
</script>

<style scoped>
.login-card-component {
  width: 100%;
}

.microsoft-login {
  margin-bottom: 30px;
}

.microsoft-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 15px;
  padding: 15px 20px;
  background: #fff;
  border: 2px solid #e1e5e9;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: #333;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.microsoft-btn:hover:not(:disabled) {
  border-color: #0078d4;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.microsoft-btn:active:not(:disabled) {
  transform: translateY(0);
}

.microsoft-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
  transform: none;
}

.microsoft-icon {
  display: flex;
  align-items: center;
  justify-content: center;
}

.loading-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid #f3f3f3;
  border-top: 2px solid #0078d4;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.button-text {
  font-size: 1rem;
  font-weight: 600;
}

.divider {
  position: relative;
  text-align: center;
  margin: 30px 0;
}

.divider::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background: #e1e5e9;
}

.divider span {
  background: rgba(255, 255, 255, 0.95);
  padding: 0 20px;
  color: #666;
  font-size: 0.9rem;
}

.login-info {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.info-item {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #666;
  font-size: 0.9rem;
}

.info-icon {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8f9fa;
  border-radius: 6px;
}
</style>