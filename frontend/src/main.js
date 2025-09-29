import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import './assets/styles/main.css'
import BaseModal from '@/components/BaseModal.vue'

const app = createApp(App)

app.use(store)
app.use(router)
app.component('BaseModal', BaseModal)
app.mount('#app')