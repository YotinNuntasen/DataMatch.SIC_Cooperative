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
    // 1. ตรวจสอบว่า MSAL instance พร้อมใช้งานหรือไม่ ถ้าไม่ ให้สร้างก่อน
    let msalInstance = this.msalInstance;
    if (!msalInstance) {
      msalInstance = await this.initializeMsal();
    }
    
    // 2. เรียกใช้ handleRedirectPromise เพื่อประมวลผลการ redirect
    if (msalInstance) {
        msalInstance.handleRedirectPromise()
          .then(response => {
            // 3. ถ้าการ redirect สำเร็จ และได้ response กลับมา
            if (response) {
              console.log('AuthCallback successful:', response);
              // ตั้งค่า active account
              msalInstance.setActiveAccount(response.account);
              // commit ข้อมูลลง Vuex store
              this.setAuth(response); 
              // 4. ส่งผู้ใช้ไปยังหน้า Data Matching
              this.$router.push({ name: 'DataMatching' });
            } else {
                // กรณีนี้อาจเกิดขึ้นเมื่อผู้ใช้เข้าหน้านี้โดยตรง ไม่ได้มาจากการ redirect
                // เราจะส่งเขากลับไปหน้าหลัก
                console.log('AuthCallback: No response from redirect. Navigating home.');
                this.$router.push('/');
            }
          })
          .catch(error => {
            // 5. หากเกิดข้อผิดพลาด
            console.error('AuthCallback error:', error);
            // สามารถ commit error ลง store หรือแสดงข้อความบางอย่างได้
            // แล้วส่งกลับไปหน้า Login
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
  background-color: #f0f2f5; /* สีพื้นหลังเหมือนหน้า Login */
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