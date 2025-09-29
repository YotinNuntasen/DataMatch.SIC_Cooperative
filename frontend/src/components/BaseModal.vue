<template>
  <Teleport to="body">
    <Transition name="modal">
      <div 
        v-if="show" 
        class="modal-overlay"
        @click="handleOverlayClick"
        :class="{ 'modal-overlay--blur': blurBackground }"
      >
        <div 
          class="modal-content"
          @click.stop
          :class="[
            `modal-content--${size}`,
            { 'modal-content--fullscreen': fullscreen }
          ]"
        >
          <!-- Header -->
          <header class="modal-header" v-if="showHeader">
            <div class="modal-title">
              <slot name="title">
                <h3>{{ title }}</h3>
              </slot>
            </div>
            
            <button 
              v-if="showCloseButton"
              @click="$emit('close')" 
              class="modal-close-btn"
              :aria-label="closeButtonLabel"
            >
              <slot name="close-icon">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                  <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2"/>
                </svg>
              </slot>
            </button>
          </header>

          <!-- Body -->
          <main class="modal-body" :class="{ 'modal-body--scrollable': scrollable }">
            <slot name="body">
              <slot></slot>
            </slot>
          </main>

          <!-- Footer -->
          <footer class="modal-footer" v-if="$slots.footer || showDefaultFooter">
            <slot name="footer">
              <div class="modal-footer-actions" v-if="showDefaultFooter">
                <button 
                  v-if="showCancelButton"
                  @click="$emit('cancel')" 
                  class="btn btn--secondary"
                  :disabled="loading"
                >
                  {{ cancelText }}
                </button>
                <button 
                  v-if="showConfirmButton"
                  @click="$emit('confirm')" 
                  class="btn btn--primary"
                  :disabled="loading || confirmDisabled"
                  :class="{ 'btn--loading': loading }"
                >
                  {{ loading ? loadingText : confirmText }}
                </button>
              </div>
            </slot>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script>
export default {
  name: 'BaseModal',
  props: {
    // Visibility
    show: {
      type: Boolean,
      default: false
    },
    
    // Content
    title: {
      type: String,
      default: ''
    },
    
    // Behavior
    closeOnOverlay: {
      type: Boolean,
      default: true
    },
    closeOnEscape: {
      type: Boolean,
      default: true
    },
    
    // Appearance
    size: {
      type: String,
      default: 'medium',
      validator: value => ['small', 'medium', 'large', 'xlarge'].includes(value)
    },
    fullscreen: {
      type: Boolean,
      default: false
    },
    blurBackground: {
      type: Boolean,
      default: true
    },
    scrollable: {
      type: Boolean,
      default: true
    },
    
    // Header
    showHeader: {
      type: Boolean,
      default: true
    },
    showCloseButton: {
      type: Boolean,
      default: true
    },
    closeButtonLabel: {
      type: String,
      default: 'Close modal'
    },
    
    // Footer
    showDefaultFooter: {
      type: Boolean,
      default: false
    },
    showCancelButton: {
      type: Boolean,
      default: true
    },
    showConfirmButton: {
      type: Boolean,
      default: true
    },
    cancelText: {
      type: String,
      default: 'Cancel'
    },
    confirmText: {
      type: String,
      default: 'Confirm'
    },
    confirmDisabled: {
      type: Boolean,
      default: false
    },
    
    // Loading state
    loading: {
      type: Boolean,
      default: false
    },
    loadingText: {
      type: String,
      default: 'Loading...'
    }
  },
  
  emits: ['close', 'cancel', 'confirm', 'overlay-click'],
  
  mounted() {
    if (this.closeOnEscape) {
      document.addEventListener('keydown', this.handleEscape)
    }
  },
  
  beforeUnmount() {
    document.removeEventListener('keydown', this.handleEscape)
  },
  
  methods: {
    handleOverlayClick() {
      this.$emit('overlay-click')
      if (this.closeOnOverlay) {
        this.$emit('close')
      }
    },
    
    handleEscape(e) {
      if (e.key === 'Escape' && this.show) {
        this.$emit('close')
      }
    }
  }
}
</script>

<style scoped>
/* Base Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-overlay--blur {
  backdrop-filter: blur(4px);
}

.modal-content {
  background: white;
  border-radius: 8px;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  max-height: 90vh;
  width: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* Size variants */
.modal-content--small { max-width: 400px; }
.modal-content--medium { max-width: 600px; }
.modal-content--large { max-width: 800px; }
.modal-content--xlarge { max-width: 1200px; }

.modal-content--fullscreen {
  max-width: 95vw;
  max-height: 95vh;
  width: 95vw;
  height: 95vh;
}

/* Header */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
  flex-shrink: 0;
}

.modal-title h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: #111827;
}

.modal-close-btn {
  background: none;
  border: none;
  padding: 0.5rem;
  cursor: pointer;
  color: #6b7280;
  border-radius: 4px;
  transition: all 0.2s;
}

.modal-close-btn:hover {
  background-color: #f3f4f6;
  color: #374151;
}

/* Body */
.modal-body {
  padding: 1.5rem;
  flex: 1;
  min-height: 0;
}

.modal-body--scrollable {
  overflow-y: auto;
}

/* Footer */
.modal-footer {
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
  flex-shrink: 0;
}

.modal-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

/* Buttons */
.btn {
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid transparent;
  min-width: 80px;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn--primary {
  background-color: #3b82f6;
  color: white;
}

.btn--primary:hover:not(:disabled) {
  background-color: #2563eb;
}

.btn--secondary {
  background-color: white;
  color: #374151;
  border-color: #d1d5db;
}

.btn--secondary:hover:not(:disabled) {
  background-color: #f9fafb;
}

.btn--loading {
  position: relative;
}

.btn--loading::after {
  content: '';
  position: absolute;
  width: 16px;
  height: 16px;
  margin: auto;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

/* Animations */
.modal-enter-active, .modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from, .modal-leave-to {
  opacity: 0;
}

.modal-enter-active .modal-content,
.modal-leave-active .modal-content {
  transition: transform 0.3s ease;
}

.modal-enter-from .modal-content,
.modal-leave-to .modal-content {
  transform: scale(0.9) translateY(-50px);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Responsive */
@media (max-width: 640px) {
  .modal-overlay {
    padding: 0;
  }
  
  .modal-content {
    border-radius: 0;
    max-height: 100vh;
    height: 100vh;
  }
  
  .modal-content--fullscreen {
    width: 100vw;
    height: 100vh;
    max-width: 100vw;
    max-height: 100vh;
  }
}
</style>