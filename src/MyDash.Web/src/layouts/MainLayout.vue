<template>
  <v-navigation-drawer
    permanent
    :width="260"
    color="surface"
    class="border-e border-opacity-25"
  >
    <!-- Logo / Brand -->
    <div class="d-flex align-center px-4 py-4" style="min-height: 64px;">
      <v-icon color="primary" size="28" class="me-2">mdi-view-dashboard-variant</v-icon>
      <span class="text-h6 font-weight-bold text-primary" style="letter-spacing: 0.05em;">MyDash</span>
    </div>

    <v-divider class="border-opacity-25" />

    <v-list nav class="py-2 px-2">
      <v-list-item
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.label"
        rounded="lg"
        class="mb-1"
        active-color="primary"
        :active="isActive(item)"
      />
    </v-list>

    <template #append>
      <v-divider class="border-opacity-25" />
      <div class="pa-3">
        <v-card
          variant="tonal"
          color="surface-variant"
          class="pa-2 rounded-lg"
        >
          <div class="d-flex align-center">
            <v-avatar size="32" color="primary" class="me-2">
              <span class="text-caption font-weight-bold">
                {{ userInitial }}
              </span>
            </v-avatar>
            <div class="flex-grow-1 overflow-hidden">
              <div class="text-body-2 font-weight-medium text-truncate">
                {{ auth.user?.name || 'User' }}
              </div>
              <div class="text-caption text-medium-emphasis">Signed in</div>
            </div>
            <v-btn
              icon="mdi-logout"
              variant="text"
              size="small"
              @click="handleLogout"
              :loading="loggingOut"
              title="Sign out"
            />
          </div>
        </v-card>
      </div>
    </template>
  </v-navigation-drawer>

  <v-app-bar
    color="surface"
    flat
    border="b"
    class="border-opacity-25"
    :height="64"
  >
    <v-app-bar-title>
      <span class="text-body-1 font-weight-medium text-medium-emphasis">
        {{ currentPageTitle }}
      </span>
    </v-app-bar-title>

    <template #append>
      <v-chip
        size="small"
        color="success"
        variant="tonal"
        prepend-icon="mdi-circle"
        class="me-3"
      >
        Connected
      </v-chip>
    </template>
  </v-app-bar>

  <v-main style="background-color: rgb(var(--v-theme-background));">
    <v-container fluid class="pa-6">
      <router-view />
    </v-container>
  </v-main>

  <v-snackbar
    v-model="snackbar.show"
    :color="snackbar.color"
    :timeout="4000"
    location="bottom right"
  >
    {{ snackbar.text }}
    <template #actions>
      <v-btn variant="text" @click="snackbar.show = false">Dismiss</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.js'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const loggingOut = ref(false)
const snackbar = ref({ show: false, text: '', color: 'error' })

const navItems = [
  { to: '/', label: 'Dashboard', icon: 'mdi-view-dashboard', exact: true },
  { to: '/fleet', label: 'Fleet', icon: 'mdi-server-network' },
  { to: '/audit', label: 'Audit Log', icon: 'mdi-clipboard-list' },
  { to: '/settings', label: 'Settings', icon: 'mdi-cog' }
]

const pageTitles = {
  Dashboard: 'Dashboard',
  Fleet: 'Fleet Management',
  AuditLog: 'Audit Log',
  Settings: 'Settings',
  ServerDetail: 'Server Details'
}

const currentPageTitle = computed(() => pageTitles[route.name] || 'MyDash')

const userInitial = computed(() => {
  const name = auth.user?.name || 'U'
  return name.charAt(0).toUpperCase()
})

function isActive(item) {
  if (item.exact) return route.path === item.to
  return route.path.startsWith(item.to)
}

async function handleLogout() {
  loggingOut.value = true
  try {
    await auth.logout()
    router.push('/login')
  } catch {
    snackbar.value = { show: true, text: 'Logout failed', color: 'error' }
  } finally {
    loggingOut.value = false
  }
}
</script>
