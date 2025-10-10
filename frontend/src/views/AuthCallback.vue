<template>
  <div class="auth-callback-container">
    <div class="callback-box">
      <div class="spinner"></div>
      <h1 class="title">Authenticating</h1>
      <p class="message">Please wait, we're securely logging you in...</p>
    </div>
  </div>
</template>

<script>
import { mapActions, mapState } from 'vuex';

export default {
  name: 'AuthCallback',
  computed: {
    ...mapState('auth', ['msalInstance'])
  },
  methods: {
    ...mapActions('auth', ['initializeMsal', 'setAuth'])
  },
  async created() {
    let msalInstance = this.msalInstance;
    if (!msalInstance) {
      msalInstance = await this.initializeMsal();
    }
    if (msalInstance) {
        msalInstance.handleRedirectPromise()
          .then(response => {
            if (response) {
              console.log('AuthCallback successful:', response);
              msalInstance.setActiveAccount(response.account);
              this.setAuth(response); 
              this.$router.push({ name: 'DataMatching' });
            } else {
               
                console.log('AuthCallback: No response from redirect. Navigating home.');
                this.$router.push('/');
            }
          })
          .catch(error => {
            console.error('AuthCallback error:', error);
            this.$router.push('/');
          });
    } else {
        console.error("MSAL instance not available in AuthCallback.");
        this.$router.push('/');
    }
  }
};
</script>

<style scoped>
.auth-callback-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #f0f2f5; 
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.callback-box {
  text-align: center;
  padding: 40px;
  background-color: #ffffff;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.title {
  font-size: 24px;
  font-weight: 600;
  color: #333;
  margin-top: 20px;
  margin-bottom: 10px;
}

.message {
  font-size: 16px;
  color: #666;
}

.spinner {
  border: 4px solid rgba(0, 0, 0, 0.1);
  width: 48px;
  height: 48px;
  border-radius: 50%;
  border-left-color: #0078d4; /* สีหลักของ Microsoft */
  animation: spin 1s ease infinite;
  margin: 0 auto;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}
</style>