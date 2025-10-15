<template>
  <div id="app">
    <div class="app-container">
      <router-view />
    </div>
    
    <!-- Loading Overlay -->
    <div v-if="isLoading" class="loading-overlay">
      <LoadingSpinner />
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