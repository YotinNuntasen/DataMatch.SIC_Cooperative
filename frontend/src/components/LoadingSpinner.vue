<template>
  <div class="loading-spinner-container">
    <div class="spinner">
      <div class="spinner-ring"></div>
      <div class="spinner-ring"></div>
      <div class="spinner-ring"></div>
      <div class="spinner-ring"></div>
    </div>
    <p v-if="message" class="loading-message">{{ message }}</p>
  </div>
</template>

<script>
export default {
  name: 'LoadingSpinner',
  props: {
    message: {
      type: String,
      default: ''
    },
    size: {
      type: String,
      default: 'medium',
      validator: value => ['small', 'medium', 'large'].includes(value)
    }
  }
}
</script>

<style scoped>
.loading-spinner-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 15px;
}

.spinner {
  display: inline-block;
  position: relative;
  width: 64px;
  height: 64px;
}

.spinner-ring {
  box-sizing: border-box;
  display: block;
  position: absolute;
  width: 51px;
  height: 51px;
  margin: 6px;
  border: 6px solid transparent;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.spinner-ring:nth-child(1) {
  animation-delay: -0.45s;
}

.spinner-ring:nth-child(2) {
  animation-delay: -0.3s;
}

.spinner-ring:nth-child(3) {
  animation-delay: -0.15s;
}

.loading-message {
  font-size: 0.9rem;
  color: #666;
  margin: 0;
  text-align: center;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/* Size variants */
.loading-spinner-container.small .spinner {
  width: 32px;
  height: 32px;
}

.loading-spinner-container.small .spinner-ring {
  width: 25px;
  height: 25px;
  margin: 3px;
  border-width: 3px;
}

.loading-spinner-container.large .spinner {
  width: 96px;
  height: 96px;
}

.loading-spinner-container.large .spinner-ring {
  width: 76px;
  height: 76px;
  margin: 9px;
  border-width: 9px;
}
</style>