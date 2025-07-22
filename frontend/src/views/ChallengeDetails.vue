<!-- ABOUTME: Challenge details view showing real-time progress and comparison -->
<!-- ABOUTME: Displays progress bars, activity counts, and winner determination -->
<template>
  <div class="challenge-details">
    <div v-if="loading" class="text-center">
      <p>Loading challenge details...</p>
    </div>

    <div v-else-if="error" class="card text-center">
      <p class="error-text">{{ error }}</p>
      <router-link to="/" class="btn btn-primary mt-4">
        Back to Dashboard
      </router-link>
    </div>

    <div v-else-if="progress">
      <div class="challenge-header">
        <div>
          <h2>{{ progress.challenge.name }}</h2>
          <div class="challenge-meta">
            <span :class="['badge', `badge-${progress.challenge.status.toLowerCase()}`]">
              {{ progress.challenge.status }}
            </span>
            <span class="challenge-type">{{ formatChallengeType(progress.challenge.type) }}</span>
            <span class="challenge-period">{{ formatDateRange(progress.challenge.startDate, progress.challenge.endDate) }}</span>
          </div>
        </div>
        <router-link to="/" class="btn btn-secondary">
          ← Back to Dashboard
        </router-link>
      </div>

      <div class="progress-overview">
        <div class="winner-section" v-if="progress.challenge.status === 'Completed'">
          <div class="winner-announcement">
            <h3>🏆 {{ progress.winner === 'Tie' ? 'It\'s a Tie!' : `${progress.winner} Wins!` }}</h3>
          </div>
        </div>

        <div class="target-info" v-if="progress.challenge.targetValue">
          <h4>Target: {{ formatTargetValue(progress.challenge.targetValue, progress.challenge.type) }}</h4>
        </div>

        <div class="participants-progress">
          <div class="participant-card">
            <div class="participant-header">
              <h4>{{ progress.creator.name }}</h4>
              <div class="participant-stats">
                <span class="progress-value">{{ formatProgress(progress.creator.progress, progress.challenge.type) }}</span>
                <span class="activity-count">{{ progress.creator.activityCount }} activities</span>
              </div>
            </div>
            <div class="progress-bar-container">
              <div class="progress-bar">
                <div 
                  class="progress-fill creator-progress"
                  :style="{ width: getProgressPercentage(progress.creator.progress, progress.opponent.progress, progress.challenge.targetValue) + '%' }"
                ></div>
              </div>
            </div>
          </div>

          <div class="vs-divider">
            <span class="vs-text">VS</span>
          </div>

          <div class="participant-card">
            <div class="participant-header">
              <h4>{{ progress.opponent.name }}</h4>
              <div class="participant-stats">
                <span class="progress-value">{{ formatProgress(progress.opponent.progress, progress.challenge.type) }}</span>
                <span class="activity-count">{{ progress.opponent.activityCount }} activities</span>
              </div>
            </div>
            <div class="progress-bar-container">
              <div class="progress-bar">
                <div 
                  class="progress-fill opponent-progress"
                  :style="{ width: getProgressPercentage(progress.opponent.progress, progress.creator.progress, progress.challenge.targetValue) + '%' }"
                ></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="challenge-info-section">
        <div class="card">
          <h4>Challenge Information</h4>
          <div class="info-grid">
            <div class="info-item">
              <strong>Type:</strong> {{ formatChallengeType(progress.challenge.type) }}
            </div>
            <div class="info-item">
              <strong>Status:</strong> {{ progress.challenge.status }}
            </div>
            <div class="info-item" v-if="progress.challenge.targetValue">
              <strong>Target:</strong> {{ formatTargetValue(progress.challenge.targetValue, progress.challenge.type) }}
            </div>
            <div class="info-item">
              <strong>Period:</strong> {{ formatDateRange(progress.challenge.startDate, progress.challenge.endDate) }}
            </div>
          </div>
        </div>
      </div>

      <div class="refresh-section text-center">
        <button @click="refreshProgress" :disabled="refreshing" class="btn btn-primary">
          {{ refreshing ? 'Refreshing...' : '🔄 Refresh Progress' }}
        </button>
        <small class="refresh-help">
          Progress updates automatically when new activities are synced from Garmin
        </small>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { challengeApi } from '../services/api'
import type { ChallengeProgress } from '../types'

const route = useRoute()
const challengeId = computed(() => parseInt(route.params.id as string))

const progress = ref<ChallengeProgress | null>(null)
const loading = ref(true)
const refreshing = ref(false)
const error = ref('')

const loadProgress = async () => {
  try {
    progress.value = await challengeApi.getChallengeProgress(challengeId.value)
    error.value = ''
  } catch (err) {
    console.error('Failed to load challenge progress:', err)
    error.value = 'Failed to load challenge details'
  } finally {
    loading.value = false
  }
}

const refreshProgress = async () => {
  refreshing.value = true
  try {
    progress.value = await challengeApi.getChallengeProgress(challengeId.value)
  } catch (err) {
    console.error('Failed to refresh progress:', err)
  } finally {
    refreshing.value = false
  }
}

const formatChallengeType = (type: string): string => {
  switch (type) {
    case 'Distance': return 'Total Distance'
    case 'Climbing': return 'Total Elevation'
    case 'AverageSpeed': return 'Average Speed'
    default: return type
  }
}

const formatTargetValue = (value: number, type: string): string => {
  switch (type) {
    case 'Distance': return `${value} km`
    case 'Climbing': return `${value} m`
    case 'AverageSpeed': return `${value} km/h`
    default: return value.toString()
  }
}

const formatProgress = (value: number, type: string): string => {
  switch (type) {
    case 'Distance': return `${value.toFixed(1)} km`
    case 'Climbing': return `${Math.round(value)} m`
    case 'AverageSpeed': return `${value.toFixed(1)} km/h`
    default: return value.toFixed(1)
  }
}

const formatDateRange = (start: string, end: string): string => {
  const startDate = new Date(start)
  const endDate = new Date(end)
  
  const startMonth = startDate.toLocaleDateString('en-US', { month: 'short' })
  const endMonth = endDate.toLocaleDateString('en-US', { month: 'short' })
  const year = startDate.getFullYear()
  
  if (startMonth === endMonth) {
    return `${startMonth} ${year}`
  } else {
    return `${startMonth} - ${endMonth} ${year}`
  }
}

const getProgressPercentage = (userProgress: number, opponentProgress: number, target?: number): number => {
  const maxProgress = target || Math.max(userProgress, opponentProgress) || 1
  return Math.min((userProgress / maxProgress) * 100, 100)
}

onMounted(() => {
  loadProgress()
})
</script>

<style scoped>
.challenge-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
}

.challenge-meta {
  display: flex;
  gap: 1rem;
  margin-top: 0.5rem;
  flex-wrap: wrap;
}

.challenge-type, .challenge-period {
  color: var(--gray-600);
  font-size: 0.9rem;
}

.progress-overview {
  margin-bottom: 2rem;
}

.winner-section {
  text-align: center;
  margin-bottom: 2rem;
}

.winner-announcement {
  padding: 1rem;
  background: linear-gradient(135deg, #fbbf24, #f59e0b);
  color: white;
  border-radius: 0.5rem;
  display: inline-block;
}

.winner-announcement h3 {
  margin: 0;
  font-size: 1.5rem;
}

.target-info {
  text-align: center;
  margin-bottom: 2rem;
  padding: 1rem;
  background-color: var(--gray-100);
  border-radius: 0.5rem;
}

.target-info h4 {
  margin: 0;
  color: var(--primary-color);
}

.participants-progress {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  gap: 2rem;
  align-items: center;
}

.participant-card {
  background: white;
  padding: 1.5rem;
  border-radius: 0.5rem;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
}

.participant-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.participant-header h4 {
  margin: 0;
  color: var(--gray-900);
}

.participant-stats {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.progress-value {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--primary-color);
}

.activity-count {
  font-size: 0.875rem;
  color: var(--gray-500);
}

.progress-bar-container {
  margin-top: 1rem;
}

.creator-progress {
  background-color: var(--primary-color);
}

.opponent-progress {
  background-color: var(--success-color);
}

.vs-divider {
  display: flex;
  justify-content: center;
  align-items: center;
}

.vs-text {
  font-weight: bold;
  font-size: 1.25rem;
  color: var(--gray-500);
  background: white;
  padding: 0.5rem 1rem;
  border-radius: 50%;
  border: 2px solid var(--gray-200);
}

.challenge-info-section {
  margin-bottom: 2rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.info-item {
  color: var(--gray-700);
}

.refresh-section {
  margin-top: 2rem;
}

.refresh-help {
  display: block;
  margin-top: 0.5rem;
  color: var(--gray-500);
}

.error-text {
  color: var(--danger-color);
}

@media (max-width: 768px) {
  .challenge-header {
    flex-direction: column;
    gap: 1rem;
  }
  
  .participants-progress {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .vs-divider {
    order: -1;
  }
}
</style>