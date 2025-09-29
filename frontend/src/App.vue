<template>
  <div id="app">
    <div class="app-container">
      <router-view />
    </div>
    
    <!-- Loading Overlay -->
    <div v-if="isLoading" class="loading-overlay">
      <LoadingSpinner />
    </div>
    
    <!-- Error Toast -->
    <div v-if="error" class="error-toast" @click="clearError">
      <div class="error-content">
        <span class="error-icon">⚠️</span>
        <span class="error-message">{{ error }}</span>
        <span class="error-close">✕</span>
      </div>
    </div>
  </div>
</template>

<script>
import LoadingSpinner from './components/LoadingSpinner.vue'
import { mapGetters, mapActions } from 'vuex'

export default {
  name: 'App',
  components: {
    LoadingSpinner
  },
  computed: {
    ...mapGetters(['isLoading', 'error'])
  },
  methods: {
    ...mapActions(['clearError'])
  }
}
</script>

<style>
.app-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999;
}

.error-toast {
  position: fixed;
  top: 20px;
  right: 20px;
  background: #ff4757;
  color: white;
  padding: 15px 20px;
  border-radius: 10px;
  box-shadow: 0 4px 12px rgba(255, 71, 87, 0.3);
  cursor: pointer;
  z-index: 1000;
  animation: slideIn 0.3s ease-out;
}

.error-content {
  display: flex;
  align-items: center;
  gap: 10px;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}
</style>