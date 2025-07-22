// ABOUTME: Main entry point for Vue.js application
// ABOUTME: Sets up Vue app with router and global styles
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './style.css'

const app = createApp(App)

app.use(router)

app.mount('#app')