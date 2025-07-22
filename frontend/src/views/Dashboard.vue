<!-- ABOUTME: Main dashboard view showing active challenges and creation interface -->
<!-- ABOUTME: Central hub for users to manage cycling challenges and view progress -->
<template>
  <div class="dashboard">
    <div v-if="!currentUser" class="welcome-card">
      <div class="card text-center">
        <h2>Welcome to Cycling Challenge!</h2>
        <p>Connect with Garmin to start creating and joining cycling challenges with friends.</p>
        <button @click="startAuth" class="btn btn-primary mt-4">
          🔗 Connect with Garmin
        </button>
      </div>
    </div>

    <div v-else>
      <div class="dashboard-header">
        <h2>Welcome back, {{ currentUser.name }}!</h2>
        <button @click="showCreateModal = true" class="btn btn-primary">
          ➕ Create New Challenge
        </button>
      </div>

      <div class="challenges-section">
        <h3>Your Challenges</h3>
        
        <div v-if="loading" class="text-center">
          <p>Loading challenges...</p>
        </div>
        
        <div v-else-if="challenges.length === 0" class="card text-center">
          <p>No challenges yet. Create your first challenge to get started!</p>
        </div>
        
        <div v-else class="challenges-grid">
          <ChallengeCard 
            v-for="challenge in challenges" 
            :key="challenge.id" 
            :challenge="challenge"
            :current-user-id="currentUser.id"
            @accept="acceptChallenge"
          />
        </div>
      </div>
    </div>

    <!-- Create Challenge Modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <CreateChallengeForm 
          :current-user="currentUser"
          @created="onChallengeCreated"
          @cancel="showCreateModal = false"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { challengeApi, authApi } from '../services/api'
import type { Challenge, User } from '../types'
import ChallengeCard from '../components/ChallengeCard.vue'
import CreateChallengeForm from '../components/CreateChallengeForm.vue'

const currentUser = ref<User | null>(null)
const challenges = ref<Challenge[]>([])
const loading = ref(false)
const showCreateModal = ref(false)

const loadUser = () => {
  const userData = localStorage.getItem('cyclingChallengeUser')
  if (userData) {
    currentUser.value = JSON.parse(userData)
  }
}

const loadChallenges = async () => {
  if (!currentUser.value) return
  
  loading.value = true
  try {
    challenges.value = await challengeApi.getChallenges(currentUser.value.id)
  } catch (error) {
    console.error('Failed to load challenges:', error)
  } finally {
    loading.value = false
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

const acceptChallenge = async (challengeId: number) => {
  if (!currentUser.value) return
  
  try {
    await challengeApi.acceptChallenge(challengeId, currentUser.value.id)
    await loadChallenges()
  } catch (error) {
    console.error('Failed to accept challenge:', error)
    alert('Failed to accept challenge')
  }
}

const onChallengeCreated = async () => {
  showCreateModal.value = false
  await loadChallenges()
}

const closeModal = () => {
  showCreateModal.value = false
}

onMounted(() => {
  loadUser()
  if (currentUser.value) {
    loadChallenges()
  }
})
</script>

<style scoped>
.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.challenges-section h3 {
  margin-bottom: 1rem;
  color: var(--gray-700);
}

.challenges-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 1rem;
}

.welcome-card {
  max-width: 500px;
  margin: 2rem auto;
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
  max-height: 90vh;
  overflow-y: auto;
}

@media (max-width: 768px) {
  .dashboard-header {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }
  
  .challenges-grid {
    grid-template-columns: 1fr;
  }
}
</style>