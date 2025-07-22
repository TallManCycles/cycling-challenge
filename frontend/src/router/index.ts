// ABOUTME: Vue Router configuration for application navigation
// ABOUTME: Defines routes for dashboard, challenge details, auth callback, and settings
import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import ChallengeDetails from '../views/ChallengeDetails.vue'
import AuthCallback from '../views/AuthCallback.vue'
import NameEntry from '../views/NameEntry.vue'
import Settings from '../views/Settings.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'Dashboard',
      component: Dashboard
    },
    {
      path: '/challenge/:id',
      name: 'ChallengeDetails',
      component: ChallengeDetails,
      props: true
    },
    {
      path: '/auth/callback',
      name: 'AuthCallback',
      component: AuthCallback
    },
    {
      path: '/auth/name-entry',
      name: 'NameEntry',
      component: NameEntry
    },
    {
      path: '/settings',
      name: 'Settings',
      component: Settings
    }
  ]
})

export default router