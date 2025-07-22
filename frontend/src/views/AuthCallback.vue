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
    // Check if this is OAuth 1.0 success callback
    const successParam = route.query.success as string
    const errorParam = route.query.error as string
    
    if (successParam === 'true') {
      // OAuth 1.0 success flow - user data already provided by backend
      const name = route.query.name as string || 'User'
      const userId = route.query.userId as string
      
      if (!userId) {
        throw new Error('Missing user ID from authentication')
      }
      
      // Store user info
      localStorage.setItem('cyclingChallengeUser', JSON.stringify({
        id: userId,
        name: decodeURIComponent(name),
        email: 'user@example.com' // Default email for OAuth 1.0
      }))
      
      // Clean up OAuth state
      localStorage.removeItem('garminAuthState')
      
      userName.value = decodeURIComponent(name)
      success.value = true
      
      // Redirect to dashboard after a short delay
      setTimeout(() => {
        router.push('/')
      }, 2000)
      
      return
    }
    
    if (errorParam) {
      throw new Error(`Authentication failed: ${errorParam}`)
    }
    
    // If no success or error parameters, this is not a valid OAuth 1.0 callback
    throw new Error('Invalid callback - no OAuth 1.0 parameters found')
    
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