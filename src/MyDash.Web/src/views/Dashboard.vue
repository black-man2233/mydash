<template>
  <div>
    <!-- Stat Cards Row -->
    <v-row class="mb-6">
      <v-col
        v-for="stat in statCards"
        :key="stat.label"
        cols="12" sm="6" md="3"
      >
        <v-card color="surface" class="rounded-xl pa-4" style="border: 1px solid rgba(255,255,255,0.07);">
          <div class="d-flex align-center justify-space-between">
            <div>
              <div class="text-caption text-medium-emphasis text-uppercase mb-1" style="letter-spacing: 0.08em;">
                {{ stat.label }}
              </div>
              <div class="text-h4 font-weight-bold">{{ stat.value }}</div>
              <div v-if="stat.sub" class="text-caption text-medium-emphasis mt-1">{{ stat.sub }}</div>
            </div>
            <v-icon :color="stat.color" size="40" class="opacity-70">{{ stat.icon }}</v-icon>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <!-- Servers Section -->
    <div class="d-flex align-center justify-space-between mb-4">
      <div class="text-h6 font-weight-semibold">Servers</div>
      <div class="d-flex align-center gap-2">
        <v-chip size="small" variant="tonal" color="medium-emphasis" prepend-icon="mdi-refresh">
          Auto-refresh 10s
        </v-chip>
        <v-btn
          variant="text"
          icon="mdi-refresh"
          size="small"
          @click="loadData"
          :loading="loading"
        />
      </div>
    </div>

    <div v-if="loading && !servers.length" class="d-flex justify-center py-10">
      <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <div v-else-if="!servers.length" class="text-center py-10">
      <v-icon size="64" color="medium-emphasis" class="mb-3">mdi-server-off</v-icon>
      <div class="text-h6 text-medium-emphasis">No servers enrolled</div>
      <div class="text-body-2 text-medium-emphasis mt-1">Go to Fleet to connect your first server.</div>
      <v-btn class="mt-4" color="primary" :to="'/fleet'" prepend-icon="mdi-server-plus">
        Enroll Server
      </v-btn>
    </div>

    <v-row v-else>
      <v-col
        v-for="server in servers"
        :key="server.id"
        cols="12"
        md="6"
        lg="4"
      >
        <server-card :server="server" />
      </v-col>
    </v-row>

    <!-- Recent Activity -->
    <div class="mt-8 mb-4">
      <div class="text-h6 font-weight-semibold mb-4">Recent Activity</div>

      <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
        <v-data-table
          :headers="auditHeaders"
          :items="recentAudit"
          :loading="auditLoading"
          density="compact"
          hide-default-footer
          :items-per-page="-1"
          no-data-text="No recent activity"
        >
          <template #item.atUnix="{ item }">
            <span class="mono text-caption text-medium-emphasis">
              {{ formatTime(item.atUnix) }}
            </span>
          </template>
          <template #item.action="{ item }">
            <span class="text-caption font-weight-medium">{{ item.action }}</span>
          </template>
          <template #item.target="{ item }">
            <span class="mono text-caption">{{ item.target }}</span>
          </template>
          <template #item.ip="{ item }">
            <span class="mono text-caption text-medium-emphasis">{{ item.ip }}</span>
          </template>
          <template #item.outcome="{ item }">
            <v-chip
              :color="item.outcome === 'ok' ? 'success' : 'error'"
              size="x-small"
              variant="tonal"
            >
              {{ item.outcome }}
            </v-chip>
          </template>
          <template #bottom />
        </v-data-table>
      </v-card>
    </div>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000" location="bottom right">
      {{ snackbar.text }}
      <template #actions>
        <v-btn variant="text" @click="snackbar.show = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { listServers, listAudit } from '@/api/index.js'
import ServerCard from '@/components/ServerCard.vue'

const servers = ref([])
const audit = ref([])
const loading = ref(false)
const auditLoading = ref(false)
const snackbar = ref({ show: false, text: '', color: 'error' })

let refreshTimer = null

const statCards = computed(() => {
  const upServers = servers.value.filter(s => s.status?.toLowerCase() === 'up' || s.status?.toLowerCase() === 'online')
  const avgCpu = servers.value.length
    ? Math.round(servers.value.reduce((a, s) => a + (s.cpu || 0), 0) / servers.value.length)
    : 0
  const avgMem = servers.value.length
    ? Math.round(servers.value.reduce((a, s) => a + (s.mem || 0), 0) / servers.value.length)
    : 0

  return [
    {
      label: 'Total Servers',
      value: servers.value.length,
      sub: `${upServers.length} online`,
      icon: 'mdi-server',
      color: 'primary'
    },
    {
      label: 'Online',
      value: upServers.length,
      sub: `${servers.value.length - upServers.length} offline`,
      icon: 'mdi-check-circle',
      color: 'success'
    },
    {
      label: 'Avg CPU',
      value: `${avgCpu}%`,
      sub: 'across all servers',
      icon: 'mdi-chip',
      color: avgCpu >= 80 ? 'error' : avgCpu >= 60 ? 'warning' : 'secondary'
    },
    {
      label: 'Avg Memory',
      value: `${avgMem}%`,
      sub: 'across all servers',
      icon: 'mdi-memory',
      color: avgMem >= 80 ? 'error' : avgMem >= 60 ? 'warning' : 'secondary'
    }
  ]
})

const recentAudit = computed(() => audit.value.slice(0, 10))

const auditHeaders = [
  { title: 'Time', key: 'atUnix', width: '160px' },
  { title: 'Action', key: 'action' },
  { title: 'Target', key: 'target' },
  { title: 'IP', key: 'ip', width: '140px' },
  { title: 'Outcome', key: 'outcome', width: '100px' }
]

function formatTime(unix) {
  if (!unix) return '—'
  return new Date(unix * 1000).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
  })
}

async function loadData() {
  loading.value = true
  try {
    servers.value = await listServers()
  } catch {
    snackbar.value = { show: true, text: 'Failed to load servers', color: 'error' }
  } finally {
    loading.value = false
  }

  auditLoading.value = true
  try {
    audit.value = await listAudit(10)
  } catch {
    // audit failing silently is ok
  } finally {
    auditLoading.value = false
  }
}

onMounted(() => {
  loadData()
  refreshTimer = setInterval(loadData, 10000)
})

onUnmounted(() => clearInterval(refreshTimer))
</script>

<style scoped>
.mono { font-family: 'JetBrains Mono', monospace; }
.gap-2 { gap: 8px; }
</style>
