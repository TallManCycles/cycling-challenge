<!-- ABOUTME: User settings page for account management and Garmin disconnection -->
<!-- ABOUTME: Allows users to view account info and disconnect from Garmin -->
<template>
  <div class="settings">
    <div class="settings-header">
      <h2>Account Settings</h2>
    </div>

    <div v-if="!currentUser" class="card text-center">
      <p>Please log in to view your settings.</p>
      <router-link to="/" class="btn btn-primary">
        Go to Dashboard
      </router-link>
    </div>

    <div v-else class="settings-content">
      <div class="card">
        <h3>Account Information</h3>
        <div class="info-grid">
          <div class="info-item">
            <strong>Name:</strong>
            <span>{{ currentUser.name }}</span>
          </div>
          <div class="info-item">
            <strong>Email:</strong>
            <span>{{ currentUser.email }}</span>
          </div>
          <div class="info-item">
            <strong>Garmin Connected:</strong>
            <span class="status-connected">✅ Connected</span>
          </div>
          <div class="info-item">
            <strong>Member Since:</strong>
            <span>{{ formatDate(currentUser.createdAt) }}</span>
          </div>
        </div>
      </div>

      <div class="card">
        <h3>Data & Privacy</h3>
        <div class="privacy-info">
          <p>
            Your cycling activity data is automatically synced from Garmin Connect 
            to track challenge progress. We only store activity metrics needed for 
            challenges (distance, elevation, speed) and do not access personal data.
          </p>
          <ul>
            <li>✅ Activity summaries (distance, elevation, speed)</li>
            <li>✅ Challenge progress and results</li>
            <li>❌ GPS routes or location data</li>
            <li>❌ Personal health metrics</li>
            <li>❌ Social data or contacts</li>
          </ul>
        </div>
      </div>

      <div class="card danger-zone">
        <h3>Disconnect Account</h3>
        <p class="danger-warning">
          ⚠️ <strong>Warning:</strong> Disconnecting your Garmin account will:
        </p>
        <ul class="danger-list">
          <li>Stop syncing new activities</li>
          <li>Remove access to your challenge data</li>
          <li>Cancel any active challenges you're participating in</li>
          <li>Permanently delete your account data</li>
        </ul>
        
        <div class="danger-actions">
          <button 
            @click="showDisconnectConfirm = true" 
            class="btn btn-danger"
          >
            Disconnect Garmin Account
          </button>
        </div>
      </div>
    </div>

    <!-- Disconnect Confirmation Modal -->
    <div v-if="showDisconnectConfirm" class="modal-overlay" @click="cancelDisconnect">
      <div class="modal-content" @click.stop>
        <div class="card">
          <h3>Confirm Account Disconnection</h3>
          <p>
            Are you sure you want to disconnect your Garmin account? 
            This action cannot be undone and will permanently delete all your data.
          </p>
          <div class="confirm-actions">
            <button @click="cancelDisconnect" class="btn btn-secondary">
              Cancel
            </button>
            <button 
              @click="disconnectAccount" 
              :disabled="disconnecting"
              class="btn btn-danger"
            >
              {{ disconnecting ? 'Disconnecting...' : 'Yes, Disconnect' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { User } from '../types'

const router = useRouter()

const currentUser = ref<User | null>(null)
const showDisconnectConfirm = ref(false)
const disconnecting = ref(false)

const loadUser = () => {
  const userData = localStorage.getItem('cyclingChallengeUser')
  if (userData) {
    currentUser.value = JSON.parse(userData)
  }
}

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

const cancelDisconnect = () => {
  showDisconnectConfirm.value = false
}

const disconnectAccount = async () => {
  if (!currentUser.value) return
  
  disconnecting.value = true
  
  try {
    // In a real app, this would call an API to properly disconnect
    // and clean up the user's data on the server
    
    // Clear local storage
    localStorage.removeItem('cyclingChallengeUser')
    localStorage.removeItem('garminAuthState')
    
    // Redirect to home
    router.push('/')
    
  } catch (error) {
    console.error('Failed to disconnect account:', error)
    alert('Failed to disconnect account. Please try again.')
  } finally {
    disconnecting.value = false
    showDisconnectConfirm.value = false
  }
}

onMounted(() => {
  loadUser()
})
</script>

<style scoped>
.settings-header {
  margin-bottom: 2rem;
}

.settings-content {
  max-width: 800px;
}

.info-grid {
  display: grid;
  gap: 1rem;
  margin-top: 1rem;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--gray-200);
}

.info-item:last-child {
  border-bottom: none;
}

.status-connected {
  color: var(--success-color);
  font-weight: 500;
}

.privacy-info {
  margin-top: 1rem;
}

.privacy-info p {
  margin-bottom: 1rem;
  color: var(--gray-700);
}

.privacy-info ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.privacy-info li {
  padding: 0.5rem 0;
  color: var(--gray-700);
}

.danger-zone {
  border: 1px solid var(--danger-color);
  background-color: #fef2f2;
}

.danger-zone h3 {
  color: var(--danger-color);
}

.danger-warning {
  color: var(--danger-color);
  margin: 1rem 0;
}

.danger-list {
  list-style: none;
  padding: 0;
  margin: 1rem 0;
}

.danger-list li {
  padding: 0.25rem 0;
  color: var(--gray-700);
}

.danger-list li::before {
  content: "• ";
  color: var(--danger-color);
  margin-right: 0.5rem;
}

.danger-actions {
  margin-top: 1.5rem;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  max-width: 500px;
  width: 90%;
}

.confirm-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 1.5rem;
}
</style>