import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import './assets/styles/main.css'
import BaseModal from '@/components/BaseModal.vue'
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";
const options = {
  position: "top-right",
  timeout: 4000,
  closeOnClick: true,
  pauseOnFocusLoss: true,
  pauseOnHover: true,
  draggable: true,
  draggablePercent: 0.6,
  showCloseButtonOnHover: false,
  hideProgressBar: false,
  closeButton: "button",
  icon: true,
  rtl: false,
  transition: "Vue-Toastification__bounce",
  maxToasts: 5,
  newestOnTop: true
};

const app = createApp(App)

app.use(store)
app.use(router)
app.component('BaseModal', BaseModal)
app.use(Toast, options);
app.mount('#app')