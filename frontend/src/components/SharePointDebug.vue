<!-- components/SharePointDebug.vue -->
<template>
  <div class="debug-panel">
    <div class="debug-header">
      <h3>🔧 SharePoint Debug Panel</h3>
      <p>Test and diagnose SharePoint User Context integration</p>
    </div>
    
    <div class="debug-actions">
      <button @click="testToken" :disabled="testing" class="debug-btn">
        {{ testing ? 'Testing Token...' : 'Test SharePoint Token' }}
      </button>
      
      <button @click="testConnection" :disabled="testing" class="debug-btn">
        {{ testing ? 'Testing...' : 'Test Backend Connection' }}
      </button>
      
      <button @click="loadData" :disabled="testing" class="debug-btn">
        {{ testing ? 'Loading...' : 'Load SharePoint Data' }}
      </button>

      <button @click="validatePermissions" :disabled="testing" class="debug-btn">
        {{ testing ? 'Validating...' : 'Validate Permissions' }}
      </button>

      <button @click="clearResults" class="debug-btn clear">
        Clear Results
      </button>
    </div>
    
    <div v-if="debugResults.length > 0" class="debug-results">
      <h4>Debug Results:</h4>
      <div class="result-item" v-for="(result, index) in debugResults" :key="index">
        <div class="result-header">
          <span class="result-step">Step {{ index + 1 }}:</span>
          <span class="result-name">{{ result.name }}</span>
          <span class="result-status" :class="{ success: result.success, error: !result.success }">
            {{ result.success ? '✅ Success' : '❌ Failed' }}
          </span>
        </div>
        <div class="result-details">
          <pre>{{ JSON.stringify(result.data, null, 2) }}</pre>
        </div>
        <div v-if="result.error" class="result-error">
          <strong>Error:</strong> {{ result.error }}
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  methods: {
    async loadData() {
      try {
        this.loading = true;
        
        // ใน development mode ไม่ส่ง token
        const isDevelopment = process.env.NODE_ENV === 'development';
        const headers = {};
        
        if (!isDevelopment) {
          const token = await this.$auth.getSharePointToken();
          headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await fetch('/api/sharepoint/contacts', {
          headers: headers
        });
        
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const data = await response.json();
        this.sharePointData = data;
      } catch (error) {
        console.error('Error loading SharePoint data:', error);
        this.error = error.message;
      } finally {
        this.loading = false;
      }
    },

    async testToken() {
      try {
        this.loading = true;
        
        const isDevelopment = process.env.NODE_ENV === 'development';
        
        if (isDevelopment) {
          this.tokenResult = {
            success: true,
            message: 'Development mode - authentication bypassed',
            hasToken: true,
            tokenLength: 20
          };
          return;
        }
        
        // ... rest of method for production
      } catch (error) {
        this.tokenResult = {
          success: false,
          message: error.message,
          hasToken: false
        };
      } finally {
        this.loading = false;
      }
    }
  }
}
</script>

<style scoped>
.debug-panel {
  background: linear-gradient(135deg, #fff3cd 0%, #ffeaa7 100%);
  border: 2px solid #ffc107;
  border-radius: 8px;
  margin: 10px 0;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(255, 193, 7, 0.2);
}

.debug-header h3 {
  margin: 0 0 5px 0;
  color: #856404;
  font-size: 1.1rem;
  font-weight: 600;
}

.debug-header p {
  margin: 0 0 20px 0;
  color: #856404;
  font-size: 0.9rem;
}

.debug-actions {
  display: flex;
  gap: 10px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.debug-btn {
  padding: 8px 16px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.debug-btn:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.debug-btn:hover:not(:disabled) {
  background: #0056b3;
  transform: translateY(-1px);
}

.debug-btn.clear {
  background: #dc3545;
}

.debug-btn.clear:hover {
  background: #c82333;
}

.debug-results {
  background: white;
  border-radius: 4px;
  padding: 15px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.debug-results h4 {
  margin: 0 0 15px 0;
  color: #495057;
  font-size: 1rem;
  font-weight: 600;
}

.result-item {
  margin-bottom: 15px;
  border: 1px solid #dee2e6;
  border-radius: 4px;
  overflow: hidden;
}

.result-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 15px;
  background: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
}

.result-step {
  font-weight: bold;
  color: #495057;
}

.result-name {
  flex: 1;
  color: #495057;
  font-weight: 500;
}

.result-status.success {
  color: #28a745;
  font-weight: 600;
}

.result-status.error {
  color: #dc3545;
  font-weight: 600;
}

.result-details {
  padding: 10px 15px;
  font-family: 'Monaco', 'Menlo', 'Consolas', monospace;
  font-size: 0.8rem;
  background: #f8f9fa;
  max-height: 200px;
  overflow-y: auto;
}

.result-details pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  color: #495057;
}

.result-error {
  padding: 10px 15px;
  background: #f8d7da;
  color: #721c24;
  border-top: 1px solid #f5c6cb;
  font-weight: 500;
}

/* Responsive Design */
@media (max-width: 768px) {
  .debug-panel {
    margin: 5px;
    padding: 15px;
  }
  
  .debug-header h3 {
    font-size: 1rem;
  }
  
  .debug-header p {
    font-size: 0.85rem;
  }

  .debug-actions {
    flex-direction: column;
  }

  .debug-btn {
    width: 100%;
    text-align: center;
  }

  .result-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 5px;
  }

  .result-details {
    font-size: 0.75rem;
    max-height: 150px;
  }
}

/* Scrollbar styling for result details */
.result-details::-webkit-scrollbar {
  width: 4px;
}

.result-details::-webkit-scrollbar-track {
  background: #e9ecef;
  border-radius: 2px;
}

.result-details::-webkit-scrollbar-thumb {
  background: #ced4da;
  border-radius: 2px;
}

.result-details::-webkit-scrollbar-thumb:hover {
  background: #adb5bd;
}
</style>