<template>
  <div class="name-entry-container">
    <div class="name-entry-card">
      <h1>Complete Your Registration</h1>
      <p class="subtitle">You've successfully connected with Garmin! Please provide your name to complete setup.</p>
      
      <form @submit.prevent="completeRegistration" class="name-entry-form">
        <div class="form-group">
          <label for="name">Your Name *</label>
          <input 
            type="text" 
            id="name"
            v-model="form.name"
            placeholder="Enter your full name"
            required
            :disabled="submitting"
          />
        </div>
        
        <div class="form-group">
          <label for="email">Email (Optional)</label>
          <input 
            type="email" 
            id="email"
            v-model="form.email"
            placeholder="Enter your email address"
            :disabled="submitting"
          />
          <small class="help-text">We'll use this for notifications and account recovery</small>
        </div>
        
        <div class="form-actions">
          <button type="submit" class="btn btn-primary" :disabled="submitting || !form.name.trim()">
            {{ submitting ? 'Creating Account...' : 'Complete Setup' }}
          </button>
        </div>
      </form>
      
      <div v-if="error" class="error-message">
        {{ error }}
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

const form = ref({
  name: '',
  email: ''
})

const submitting = ref(false)
const error = ref('')
const tempUserId = ref('')

onMounted(() => {
  // Get tempUserId from URL parameters
  tempUserId.value = route.query.tempUserId as string
  
  if (!tempUserId.value) {
    error.value = 'Invalid registration link. Please try authenticating again.'
  }
})

const completeRegistration = async () => {
  if (!tempUserId.value || !form.value.name.trim()) {
    return
  }
  
  submitting.value = true
  error.value = ''
  
  try {
    const response = await authApi.completeRegistration({
      tempUserId: tempUserId.value,
      name: form.value.name.trim(),
      email: form.value.email.trim() || undefined
    })
    
    // Store user info and redirect to dashboard
    localStorage.setItem('cyclingChallengeUser', JSON.stringify(response.user))
    
    // Trigger the parent App.vue to reload the user state
    window.dispatchEvent(new Event('userLogin'))
    
    await router.push('/')
    
  } catch (err: any) {
    console.error('Registration failed:', err)
    error.value = err.response?.data || 'Failed to complete registration. Please try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.name-entry-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 1rem;
}

.name-entry-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  padding: 2rem;
  width: 100%;
  max-width: 400px;
  text-align: center;
}

h1 {
  color: #2d3748;
  margin-bottom: 0.5rem;
  font-size: 1.75rem;
  font-weight: 600;
}

.subtitle {
  color: #4a5568;
  margin-bottom: 2rem;
  line-height: 1.5;
}

.name-entry-form {
  text-align: left;
}

.form-group {
  margin-bottom: 1.5rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #2d3748;
}

input {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.2s;
}

input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

input:disabled {
  background-color: #f7fafc;
  cursor: not-allowed;
}

.help-text {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: #718096;
}

.form-actions {
  margin-top: 2rem;
  text-align: center;
}

.btn {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.error-message {
  margin-top: 1rem;
  padding: 0.75rem;
  background-color: #fed7d7;
  border: 1px solid #fc8181;
  border-radius: 8px;
  color: #c53030;
  font-size: 0.875rem;
}
</style>