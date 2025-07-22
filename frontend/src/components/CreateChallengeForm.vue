<!-- ABOUTME: Form component for creating new cycling challenges -->
<!-- ABOUTME: Handles challenge type selection, opponent email, and optional target values -->
<template>
  <div class="create-challenge-form card">
    <div class="form-header">
      <h3>Create New Challenge</h3>
      <button @click="$emit('cancel')" class="close-btn">&times;</button>
    </div>

    <form @submit.prevent="createChallenge">
      <div class="form-group">
        <label class="form-label" for="challengeName">Challenge Name *</label>
        <input
          id="challengeName"
          v-model="form.name"
          type="text"
          class="form-input"
          placeholder="e.g., December Distance Battle"
          required
        />
      </div>

      <div class="form-group">
        <label class="form-label" for="challengeType">Challenge Type *</label>
        <select
          id="challengeType"
          v-model="form.type"
          class="form-input"
          required
        >
          <option value="">Select challenge type...</option>
          <option value="Distance">Total Distance (km)</option>
          <option value="Climbing">Total Elevation Gain (m)</option>
          <option value="AverageSpeed">Average Speed (km/h)</option>
        </select>
      </div>

      <div class="form-group">
        <label class="form-label" for="opponentEmail">Opponent Email *</label>
        <input
          id="opponentEmail"
          v-model="form.opponentEmail"
          type="email"
          class="form-input"
          placeholder="friend@example.com"
          required
        />
        <small class="form-help">
          Your friend must be registered with Garmin Connect
        </small>
      </div>

      <div class="form-group">
        <label class="form-label" for="targetValue">
          Target Value (Optional)
        </label>
        <input
          id="targetValue"
          v-model.number="form.targetValue"
          type="number"
          min="0"
          step="0.1"
          class="form-input"
          :placeholder="getTargetPlaceholder()"
        />
        <small class="form-help">
          {{ getTargetDescription() }}
        </small>
      </div>

      <div class="challenge-period">
        <h4>Challenge Period</h4>
        <p>{{ formatCurrentMonth() }}</p>
        <small class="form-help">
          All challenges run for the current calendar month
        </small>
      </div>

      <div class="form-actions">
        <button type="button" @click="$emit('cancel')" class="btn btn-secondary">
          Cancel
        </button>
        <button type="submit" :disabled="submitting" class="btn btn-primary">
          {{ submitting ? 'Creating...' : 'Create Challenge' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { challengeApi } from '../services/api'
import type { User } from '../types'

interface Props {
  currentUser: User | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  created: []
  cancel: []
}>()

const form = ref({
  name: '',
  type: '',
  opponentEmail: '',
  targetValue: null as number | null
})

const submitting = ref(false)

const getTargetPlaceholder = (): string => {
  switch (form.value.type) {
    case 'Distance': return 'e.g., 200 (km)'
    case 'Climbing': return 'e.g., 5000 (meters)'
    case 'AverageSpeed': return 'e.g., 25 (km/h)'
    default: return ''
  }
}

const getTargetDescription = (): string => {
  switch (form.value.type) {
    case 'Distance': return 'Total kilometers to ride during the month'
    case 'Climbing': return 'Total elevation gain in meters for the month'
    case 'AverageSpeed': return 'Average speed across all rides in km/h'
    default: return 'Set an optional target for this challenge'
  }
}

const formatCurrentMonth = (): string => {
  const now = new Date()
  const monthName = now.toLocaleDateString('en-US', { month: 'long' })
  const year = now.getFullYear()
  return `${monthName} ${year}`
}

const createChallenge = async () => {
  if (!props.currentUser) return

  submitting.value = true
  
  try {
    // For demo: create challenge for any other user ID (will be waiting for opponent)
    // Use a different ID than the current user
    const opponentId = props.currentUser.id === 1 ? 2 : 1
    
    await challengeApi.createChallenge({
      creatorId: props.currentUser.id,
      opponentId: opponentId,
      name: form.value.name,
      type: form.value.type,
      targetValue: form.value.targetValue || undefined
    })

    // Reset form
    form.value = {
      name: '',
      type: '',
      opponentEmail: '',
      targetValue: null
    }

    alert('Challenge created successfully!')
    
    // Emit created event to close modal
    emit('created')
    
  } catch (error) {
    console.error('Failed to create challenge:', error)
    alert('Failed to create challenge. Please try again.')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.create-challenge-form {
  max-width: 500px;
}

.form-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--gray-200);
}

.form-header h3 {
  margin: 0;
  color: var(--gray-900);
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: var(--gray-500);
  cursor: pointer;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-btn:hover {
  color: var(--gray-700);
}

.form-help {
  display: block;
  margin-top: 0.25rem;
  color: var(--gray-500);
  font-size: 0.875rem;
}

.challenge-period {
  margin: 1.5rem 0;
  padding: 1rem;
  background-color: var(--gray-100);
  border-radius: 0.375rem;
}

.challenge-period h4 {
  margin: 0 0 0.5rem 0;
  color: var(--gray-900);
}

.challenge-period p {
  margin: 0;
  font-weight: 500;
  color: var(--primary-color);
}

.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 1.5rem;
}

.btn-secondary {
  background-color: var(--gray-200);
  color: var(--gray-700);
}

.btn-secondary:hover {
  background-color: var(--gray-300);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>