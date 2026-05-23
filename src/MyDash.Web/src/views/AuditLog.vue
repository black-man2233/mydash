<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-6">
      <div class="text-h5 font-weight-bold">Audit Log</div>
      <div class="d-flex align-center gap-3">
        <v-select
          v-model="limit"
          :items="limitOptions"
          item-title="label"
          item-value="value"
          label="Show"
          variant="outlined"
          density="compact"
          hide-details
          style="width: 120px;"
          @update:model-value="loadAudit"
        />
        <v-btn
          icon="mdi-refresh"
          variant="tonal"
          size="small"
          :loading="loading"
          @click="loadAudit"
        />
      </div>
    </div>

    <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
      <v-data-table
        :headers="headers"
        :items="entries"
        :loading="loading"
        density="comfortable"
        no-data-text="No audit entries"
        :items-per-page="limit"
        :items-per-page-options="[50, 100, 200]"
        @update:items-per-page="val => { limit = val; loadAudit() }"
      >
        <template #item.atUnix="{ item }">
          <span class="mono text-caption">{{ formatTime(item.atUnix) }}</span>
        </template>
        <template #item.actor="{ item }">
          <span class="text-body-2 font-weight-medium">{{ item.actor || '—' }}</span>
        </template>
        <template #item.action="{ item }">
          <v-chip
            :color="actionColor(item.action)"
            size="x-small"
            variant="tonal"
            class="mono"
          >
            {{ item.action }}
          </v-chip>
        </template>
        <template #item.target="{ item }">
          <span class="mono text-caption">{{ item.target || '—' }}</span>
        </template>
        <template #item.ip="{ item }">
          <span class="mono text-caption text-medium-emphasis">{{ item.ip || '—' }}</span>
        </template>
        <template #item.outcome="{ item }">
          <v-chip
            :color="item.outcome === 'ok' ? 'success' : 'error'"
            size="x-small"
            variant="tonal"
          >
            <v-icon start size="8">{{ item.outcome === 'ok' ? 'mdi-check' : 'mdi-close' }}</v-icon>
            {{ item.outcome }}
          </v-chip>
        </template>
      </v-data-table>
    </v-card>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000" location="bottom right">
      {{ snackbar.text }}
      <template #actions>
        <v-btn variant="text" @click="snackbar.show = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { listAudit } from '@/api/index.js'

const entries = ref([])
const loading = ref(false)
const limit = ref(50)
const snackbar = ref({ show: false, text: '', color: 'error' })

const limitOptions = [
  { label: '50', value: 50 },
  { label: '100', value: 100 },
  { label: '200', value: 200 }
]

const headers = [
  { title: 'Time', key: 'atUnix', width: '180px' },
  { title: 'Actor', key: 'actor', width: '140px' },
  { title: 'Action', key: 'action', width: '160px' },
  { title: 'Target', key: 'target' },
  { title: 'IP Address', key: 'ip', width: '140px' },
  { title: 'Outcome', key: 'outcome', width: '100px' }
]

function formatTime(unix) {
  if (!unix) return '—'
  return new Date(unix * 1000).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
  })
}

function actionColor(action) {
  if (!action) return 'grey'
  const a = action.toLowerCase()
  if (a.includes('delete') || a.includes('remove') || a.includes('revoke')) return 'error'
  if (a.includes('create') || a.includes('add') || a.includes('enroll')) return 'success'
  if (a.includes('login') || a.includes('auth')) return 'primary'
  if (a.includes('update') || a.includes('modify')) return 'warning'
  return 'secondary'
}

async function loadAudit() {
  loading.value = true
  try {
    entries.value = await listAudit(limit.value)
  } catch {
    snackbar.value = { show: true, text: 'Failed to load audit log', color: 'error' }
  } finally {
    loading.value = false
  }
}

onMounted(loadAudit)
</script>

<style scoped>
.mono { font-family: 'JetBrains Mono', monospace; }
.gap-3 { gap: 12px; }
</style>
