// ABOUTME: TypeScript type definitions for the cycling challenge application
// ABOUTME: Defines interfaces for User, Challenge, Activity, and API responses
export interface User {
  id: number
  name: string
  email: string
  garminUserId: string
  createdAt: string
}

export interface Challenge {
  id: number
  name: string
  type: 'Distance' | 'Climbing' | 'AverageSpeed'
  status: 'Pending' | 'Active' | 'Completed' | 'Cancelled'
  targetValue?: number
  startDate: string
  endDate: string
  creator: User
  opponent: User
  createdAt: string
}

export interface Activity {
  id: number
  garminActivityId: string
  activityType: string
  distance: number
  elevationGain?: number
  averageSpeed?: number
  activityDate: string
  userId: number
  challengeId?: number
  uploadedAt: string
}

export interface ChallengeProgress {
  challenge: Challenge
  creator: {
    id: number
    name: string
    progress: number
    activityCount: number
  }
  opponent: {
    id: number
    name: string
    progress: number
    activityCount: number
  }
  winner: string
}

export interface AuthResponse {
  authUrl: string
  requestToken: string
}

export interface CallbackData {
  code: string
  state: string
  codeVerifier: string
  email: string
  name: string
}

export interface AuthResult {
  userId: number
  name: string
  email: string
  success: boolean
}