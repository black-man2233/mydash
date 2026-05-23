<template>
  <div>
    <div class="text-h5 font-weight-bold mb-6">Settings</div>

    <v-row>
      <v-col cols="12" md="7" lg="6">
        <!-- Dashboard Preferences -->
        <v-card color="surface" class="rounded-xl mb-4" style="border: 1px solid rgba(255,255,255,0.07);">
          <v-card-text class="pa-6">
            <div class="text-body-1 font-weight-semibold mb-1">Dashboard Preferences</div>
            <div class="text-caption text-medium-emphasis mb-5">Customize how your dashboard looks and behaves.</div>

            <div class="text-body-2 font-weight-medium mb-3">Server Card Layout</div>
            <v-radio-group v-model="prefs.dashboardLayout" hide-details class="mb-6">
              <v-radio value="grid" label="Grid View" color="primary">
                <template #label>
                  <div class="d-flex align-center gap-2">
                    <v-icon size="18">mdi-view-grid</v-icon>
                    <span>Grid View</span>
                    <span class="text-caption text-medium-emphasis">— cards in a responsive grid</span>
                  </div>
                </template>
              </v-radio>
              <v-radio value="list" color="primary" class="mt-2">
                <template #label>
                  <div class="d-flex align-center gap-2">
                    <v-icon size="18">mdi-view-list</v-icon>
                    <span>List View</span>
                    <span class="text-caption text-medium-emphasis">— compact list of servers</span>
                  </div>
                </template>
              </v-radio>
            </v-radio-group>

            <v-divider class="border-opacity-25 mb-5" />

            <div class="text-body-2 font-weight-medium mb-3">Refresh Interval</div>
            <v-select
              v-model="prefs.refreshInterval"
              :items="refreshOptions"
              item-title="label"
              item-value="value"
              variant="outlined"
              density="comfortable"
              hide-details
              style="max-width: 260px;"
            />
          </v-card-text>
          <v-card-actions class="pa-6 pt-0">
            <v-spacer />
            <v-btn
              color="primary"
              variant="flat"
              :loading="saving"
              :disabled="!dirty"
              prepend-icon="mdi-content-save"
              @click="handleSave"
            >
              Save Changes
            </v-btn>
          </v-card-actions>
        </v-card>

        <!-- About -->
        <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
          <v-card-text class="pa-6">
            <div class="text-body-1 font-weight-semibold mb-4">About MyDash</div>
            <v-list density="compact" class="pa-0" bg-color="transparent">
              <v-list-item class="px-0" prepend-icon="mdi-tag">
                <template #title>
                  <span class="text-caption text-medium-emphasis">Version</span>
                </template>
                <template #append>
                  <span class="mono text-caption">1.0.0</span>
                </template>
              </v-list-item>
              <v-list-item class="px-0" prepend-icon="mdi-web">
                <template #title>
                  <span class="text-caption text-medium-emphasis">Frontend</span>
                </template>
                <template #append>
                  <span class="mono text-caption">Vue 3 + Vuetify 3</span>
                </template>
              </v-list-item>
              <v-list-item class="px-0" prepend-icon="mdi-heart">
                <template #title>
                  <span class="text-caption text-medium-emphasis">Built for</span>
                </template>
                <template #append>
                  <span class="mono text-caption">Homelab enthusiasts</span>
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000" location="bottom right">
      {{ snackbar.text }}
      <template #actions>
        <v-btn variant="text" @click="snackbar.show = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { getPreferences, updatePreferences } from '@/api/index.js'

const prefs = ref({ dashboardLayout: 'grid', refreshInterval: 10 })
const originalPrefs = ref({ dashboardLayout: 'grid', refreshInterval: 10 })
const dirty = ref(false)
const saving = ref(false)
const snackbar = ref({ show: false, text: '', color: 'error' })

const refreshOptions = [
  { label: '5 seconds', value: 5 },
  { label: '10 seconds', value: 10 },
  { label: '30 seconds', value: 30 },
  { label: '1 minute', value: 60 },
  { label: 'Manual only', value: 0 }
]

watch(prefs, (val) => {
  dirty.value = JSON.stringify(val) !== JSON.stringify(originalPrefs.value)
}, { deep: true })

async function loadPreferences() {
  try {
    const data = await getPreferences()
    prefs.value = { dashboardLayout: 'grid', refreshInterval: 10, ...data }
    originalPrefs.value = { ...prefs.value }
    dirty.value = false
  } catch {
    // Use defaults if preferences endpoint fails
  }
}

async function handleSave() {
  saving.value = true
  try {
    await updatePreferences(prefs.value)
    originalPrefs.value = { ...prefs.value }
    dirty.value = false
    snackbar.value = { show: true, text: 'Settings saved', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Failed to save settings', color: 'error' }
  } finally {
    saving.value = false
  }
}

onMounted(loadPreferences)
</script>

<style scoped>
.mono { font-family: 'JetBrains Mono', monospace; }
.gap-2 { gap: 8px; }
</style>
