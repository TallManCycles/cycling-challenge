// ABOUTME: API service for communicating with the Azure Functions backend
// ABOUTME: Handles authentication, challenges, and progress tracking
import axios from 'axios'
import type { 
  Challenge, 
  ChallengeProgress, 
  AuthResponse, 
  CallbackData, 
  AuthResult 
} from '../types'

const API_BASE = '/api'

const api = axios.create({
  baseURL: API_BASE,
  timeout: 10000
})

export const authApi = {
  startGarminAuth: (): Promise<AuthResponse> =>
    api.get('/auth/garmin/start').then(res => res.data),
    
  handleCallback: (data: CallbackData): Promise<AuthResult> =>
    api.post('/auth/garmin/callback', data).then(res => res.data)
}

export const challengeApi = {
  getChallenges: (userId: number): Promise<Challenge[]> =>
    api.get('/challenges', { params: { userId } }).then(res => res.data),
    
  createChallenge: (data: {
    creatorId: number
    opponentId: number
    name: string
    type: string
    targetValue?: number
  }): Promise<Challenge> =>
    api.post('/challenges', data).then(res => res.data),
    
  acceptChallenge: (challengeId: number, userId: number): Promise<Challenge> =>
    api.post(`/challenges/${challengeId}/accept`, { userId }).then(res => res.data),
    
  getChallengeProgress: (challengeId: number): Promise<ChallengeProgress> =>
    api.get(`/challenges/${challengeId}/progress`).then(res => res.data)
}

export default api