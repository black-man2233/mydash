import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getMe, verifyPin as apiVerifyPin, logout as apiLogout } from '@/api/index.js'

export const useAuthStore = defineStore('auth', () => {
  const user = ref(null)
  const loading = ref(false)

  async function fetchMe() {
    loading.value = true
    try {
      const data = await getMe()
      user.value = data
      return data
    } catch {
      user.value = null
      return null
    } finally {
      loading.value = false
    }
  }

  async function login(challengeId, code) {
    loading.value = true
    try {
      const result = await apiVerifyPin(challengeId, code)
      if (result.ok) {
        await fetchMe()
      }
      return result
    } finally {
      loading.value = false
    }
  }

  async function logout() {
    loading.value = true
    try {
      await apiLogout()
    } finally {
      user.value = null
      loading.value = false
    }
  }

  return { user, loading, fetchMe, login, logout }
})
