<!-- ABOUTME: Main Vue.js application component with navigation and user state -->
<!-- ABOUTME: Provides layout structure and handles global authentication state -->
<template>
  <div id="app">
    <nav class="navbar">
      <div class="container">
        <div class="nav-content">
          <router-link to="/" class="nav-brand">
            <h1>🚴 Cycling Challenge</h1>
          </router-link>
          
          <div class="nav-links" v-if="currentUser">
            <router-link to="/" class="nav-link">Dashboard</router-link>
            <router-link to="/settings" class="nav-link">Settings</router-link>
            <span class="user-info">{{ currentUser.name }}</span>
          </div>
          
          <div v-else>
            <button @click="startAuth" class="btn btn-primary">
              Connect with Garmin
            </button>
          </div>
        </div>
      </div>
    </nav>

    <main class="main-content">
      <div class="container">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { authApi } from './services/api'
import type { User } from './types'

const currentUser = ref<User | null>(null)

const loadUser = () => {
  const userData = localStorage.getItem('cyclingChallengeUser')
  if (userData) {
    currentUser.value = JSON.parse(userData)
  }
}

const startAuth = async () => {
  try {
    const authData = await authApi.startGarminAuth()
    
    localStorage.setItem('garminAuthState', JSON.stringify({
      codeVerifier: authData.codeVerifier,
      state: authData.state
    }))
    
    window.location.href = authData.authUrl
  } catch (error) {
    console.error('Failed to start authentication:', error)
    alert('Failed to start Garmin authentication')
  }
}

onMounted(() => {
  loadUser()
})
</script>

<style scoped>
.navbar {
  background: white;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
  padding: 1rem 0;
}

.nav-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nav-brand {
  text-decoration: none;
  color: var(--gray-900);
}

.nav-brand h1 {
  margin: 0;
  font-size: 1.5rem;
}

.nav-links {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.nav-link {
  text-decoration: none;
  color: var(--gray-700);
  font-weight: 500;
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  transition: background-color 0.2s;
}

.nav-link:hover,
.nav-link.router-link-active {
  background-color: var(--gray-100);
  color: var(--primary-color);
}

.user-info {
  color: var(--gray-700);
  font-weight: 500;
  padding: 0.5rem 1rem;
  background-color: var(--gray-100);
  border-radius: 0.375rem;
}

.main-content {
  padding: 2rem 0;
  min-height: calc(100vh - 100px);
}

@media (max-width: 768px) {
  .nav-content {
    flex-direction: column;
    gap: 1rem;
  }
  
  .nav-links {
    order: -1;
  }
}
</style>