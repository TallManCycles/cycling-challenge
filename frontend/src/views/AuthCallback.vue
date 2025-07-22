<!-- ABOUTME: OAuth callback handler for Garmin authentication flow -->
<!-- ABOUTME: Processes authorization code and completes user authentication -->
<template>
  <div class="auth-callback">
    <div class="callback-card card text-center">
      <div v-if="processing">
        <h2>🔄 Completing Authentication...</h2>
        <p>Please wait while we connect your Garmin account.</p>
      </div>
      
      <div v-else-if="success">
        <h2>✅ Authentication Successful!</h2>
        <p>Welcome, {{ userName }}! Your Garmin account has been connected.</p>
        <router-link to="/" class="btn btn-primary mt-4">
          Go to Dashboard
        </router-link>
      </div>
      
      <div v-else>
        <h2>❌ Authentication Failed</h2>
        <p>{{ error }}</p>
        <router-link to="/" class="btn btn-primary mt-4">
          Try Again
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authApi } from '../services/api'

const route = useRoute()
const router = useRouter()

const processing = ref(true)
const success = ref(false)
const error = ref('')
const userName = ref('')

const handleCallback = async () => {
  try {
    const code = route.query.code as string
    const state = route.query.state as string
    
    if (!code || !state) {
      throw new Error('Missing authorization code or state')
    }
    
    const authState = localStorage.getItem('garminAuthState')
    if (!authState) {
      throw new Error('No authentication state found')
    }
    
    const { codeVerifier, state: expectedState } = JSON.parse(authState)
    
    if (state !== expectedState) {
      throw new Error('Invalid state parameter')
    }
    
    // Get user info for the callback
    const name = prompt('Please enter your name:') || 'User'
    const email = prompt('Please enter your email:') || 'user@example.com'
    
    const result = await authApi.handleCallback({
      code,
      state,
      codeVerifier,
      name,
      email
    })
    
    if (result.success) {
      // Store user info
      localStorage.setItem('cyclingChallengeUser', JSON.stringify({
        id: result.userId,
        name: result.name,
        email: result.email
      }))
      
      localStorage.removeItem('garminAuthState')
      
      userName.value = result.name
      success.value = true
      
      // Redirect to dashboard after a short delay
      setTimeout(() => {
        router.push('/')
      }, 2000)
    } else {
      throw new Error('Authentication failed')
    }
    
  } catch (err) {
    console.error('Authentication error:', err)
    error.value = err instanceof Error ? err.message : 'Unknown error occurred'
  } finally {
    processing.value = false
  }
}

onMounted(() => {
  handleCallback()
})
</script>

<style scoped>
.auth-callback {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 60vh;
}

.callback-card {
  max-width: 500px;
  width: 100%;
}

.callback-card h2 {
  margin-bottom: 1rem;
  color: var(--gray-900);
}

.callback-card p {
  margin-bottom: 1.5rem;
  color: var(--gray-700);
}
</style>