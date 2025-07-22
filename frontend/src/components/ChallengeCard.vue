<!-- ABOUTME: Challenge card component displaying challenge info and actions -->
<!-- ABOUTME: Shows challenge details, status, and allows accepting pending challenges -->
<template>
  <div class="challenge-card card">
    <div class="challenge-header">
      <h4>{{ challenge.name }}</h4>
      <span :class="['badge', `badge-${challenge.status.toLowerCase()}`]">
        {{ challenge.status }}
      </span>
    </div>
    
    <div class="challenge-info">
      <div class="challenge-type">
        <strong>Type:</strong> {{ formatChallengeType(challenge.type) }}
      </div>
      
      <div v-if="challenge.targetValue" class="challenge-target">
        <strong>Target:</strong> {{ formatTargetValue(challenge.targetValue, challenge.type) }}
      </div>
      
      <div class="challenge-period">
        <strong>Period:</strong> {{ formatDateRange(challenge.startDate, challenge.endDate) }}
      </div>
    </div>

    <div class="challenge-participants">
      <div class="participant">
        <span class="participant-name">{{ challenge.creator.name }}</span>
        <span v-if="challenge.creator.id === currentUserId" class="participant-badge">(You)</span>
      </div>
      <div class="vs">vs</div>
      <div class="participant">
        <span class="participant-name">{{ challenge.opponent.name }}</span>
        <span v-if="challenge.opponent.id === currentUserId" class="participant-badge">(You)</span>
      </div>
    </div>

    <div class="challenge-actions">
      <router-link 
        :to="`/challenge/${challenge.id}`" 
        class="btn btn-primary"
        v-if="challenge.status !== 'Pending'"
      >
        View Details
      </router-link>
      
      <button 
        v-if="challenge.status === 'Pending' && challenge.opponent.id === currentUserId"
        @click="$emit('accept', challenge.id)"
        class="btn btn-success"
      >
        Accept Challenge
      </button>
      
      <span v-if="challenge.status === 'Pending' && challenge.creator.id === currentUserId"
            class="pending-text">
        Waiting for {{ challenge.opponent.name }} to accept...
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Challenge } from '../types'

interface Props {
  challenge: Challenge
  currentUserId: number
}

defineProps<Props>()
defineEmits<{
  accept: [challengeId: number]
}>()

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
</script>

<style scoped>
.challenge-card {
  border: 1px solid var(--gray-200);
  transition: box-shadow 0.2s;
}

.challenge-card:hover {
  box-shadow: 0 4px 12px 0 rgba(0, 0, 0, 0.15);
}

.challenge-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.challenge-header h4 {
  margin: 0;
  color: var(--gray-900);
}

.challenge-info {
  margin-bottom: 1rem;
}

.challenge-info > div {
  margin-bottom: 0.5rem;
  color: var(--gray-700);
}

.challenge-participants {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
  padding: 0.75rem;
  background-color: var(--gray-100);
  border-radius: 0.375rem;
}

.participant {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.participant-name {
  font-weight: 500;
  color: var(--gray-900);
}

.participant-badge {
  font-size: 0.75rem;
  color: var(--primary-color);
  font-weight: 500;
}

.vs {
  color: var(--gray-500);
  font-weight: bold;
}

.challenge-actions {
  display: flex;
  justify-content: center;
}

.pending-text {
  color: var(--gray-500);
  font-style: italic;
  text-align: center;
}
</style>