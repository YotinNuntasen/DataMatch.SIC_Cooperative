<template>
  <div class="login-page">
    <div class="login-container">
      <!-- Left Side: Branding -->
      <div class="login-left">
        <div class="brand-content">
          <div class="brand-logo">
            <div class="logo-icon">📊</div>
            <h1>Data Match Portal</h1>
          </div>
          <p class="brand-description">

            Enhance your data matching process with our platform.

          </p>

          <div class="features-list">
            <div class="feature-item">
              <span class="feature-icon">🔗</span>
              <span>Match New Business Opportuntity & Enterprise Resource Planning </span>
            </div>
            <div class="feature-item">
              <span class="feature-icon">🤖</span>
              <span>AI-Powered Calculate Similarity</span>
            </div>
            <div class="feature-item">
              <span class="feature-icon">📈</span>
              <span>Export & Update Results</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Side: Login Card -->
      <div class="login-right">
        <div class="login-card">
          <div class="login-header">
            <h2>Welcome Back</h2>
            <p>Sign in to your Microsoft account to continue</p>
          </div>

          <div class="login-form">
            <LoginCard @login="handleLogin" :loading="loading" />
          </div>

          <div class="login-footer">
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import LoginCard from '../components/LoginCard.vue'
import { mapActions, mapGetters } from 'vuex'

export default {
  name: 'LoginPage',
  components: {
    LoginCard
  },
  data(){
    return{
      someOtherVariable: null,
      safeSharePointData: [],
    };
  },
  computed: {
    ...mapGetters(['isLoading']),
    loading() {
      return this.isLoading
    }
  },
  methods: {
    ...mapActions('auth', ['login']),

    async handleLogin() {
      try {
        await this.login()
        this.$router.push('/matching')
      } catch (error) {
        // Error handling is done in the store
        console.error('Login failed:', error)
      }
    }
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  max-width: 1200px;
  width: 100%;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  backdrop-filter: blur(20px);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  min-height: 600px;
}

.login-left {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60px 40px;
  color: white;
}

.brand-content {
  max-width: 400px;
}

.brand-logo {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 30px;
}

.logo-icon {
  font-size: 3rem;
  background: rgba(255, 255, 255, 0.2);
  width: 80px;
  height: 80px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.brand-content h1 {
  font-size: 2.5rem;
  font-weight: 700;
  margin: 0;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.brand-description {
  font-size: 1.1rem;
  line-height: 1.6;
  opacity: 0.9;
  margin-bottom: 40px;
}

.features-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 15px;
  font-size: 1rem;
}

.feature-icon {
  font-size: 1.5rem;
  width: 40px;
  height: 40px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-right {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60px 40px;
  background: rgba(255, 255, 255, 0.95);
}

.login-card {
  width: 100%;
  max-width: 400px;
}

.login-header {
  text-align: center;
  margin-bottom: 40px;
}

.login-header h2 {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
  margin-bottom: 10px;
}

.login-header p {
  color: #666;
  font-size: 1rem;
}

.login-form {
  margin-bottom: 30px;
}

.login-footer {
  text-align: center;
}

.security-note {
  color: #888;
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

@media (max-width: 768px) {
  .login-container {
    grid-template-columns: 1fr;
    margin: 20px;
  }

  .login-left {
    padding: 40px 20px;
    text-align: center;
  }

  .brand-content h1 {
    font-size: 2rem;
  }

  .login-right {
    padding: 40px 20px;
  }
}
</style>