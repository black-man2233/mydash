import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.js'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue')
      },
      {
        path: 'servers/:id',
        name: 'ServerDetail',
        component: () => import('@/views/ServerDetail.vue')
      },
      {
        path: 'fleet',
        name: 'Fleet',
        component: () => import('@/views/Fleet.vue')
      },
      {
        path: 'audit',
        name: 'AuditLog',
        component: () => import('@/views/AuditLog.vue')
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('@/views/Settings.vue')
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth === false) {
    return true
  }

  if (!auth.user) {
    await auth.fetchMe()
  }

  if (!auth.user) {
    return { name: 'Login' }
  }

  return true
})

export default router
