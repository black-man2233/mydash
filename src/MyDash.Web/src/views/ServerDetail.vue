<template>
  <div>
    <!-- Breadcrumb -->
    <v-breadcrumbs :items="breadcrumbs" class="pa-0 mb-4">
      <template #divider>
        <v-icon>mdi-chevron-right</v-icon>
      </template>
    </v-breadcrumbs>

    <!-- Loading -->
    <div v-if="loading && !server" class="d-flex justify-center py-12">
      <v-progress-circular indeterminate color="primary" size="56" />
    </div>

    <!-- Not Found -->
    <div v-else-if="!server" class="text-center py-12">
      <v-icon size="64" color="medium-emphasis" class="mb-3">mdi-server-off</v-icon>
      <div class="text-h6">Server not found</div>
      <v-btn class="mt-4" :to="'/'" prepend-icon="mdi-arrow-left">Back to Dashboard</v-btn>
    </div>

    <template v-else>
      <!-- Server Header -->
      <v-card color="surface" class="rounded-xl mb-6 pa-4" style="border: 1px solid rgba(255,255,255,0.07);">
        <div class="d-flex align-center flex-wrap gap-4">
          <v-avatar :color="server.color || 'primary'" size="56">
            <span class="text-h5 font-weight-bold text-white">
              {{ server.initial || server.name?.charAt(0)?.toUpperCase() }}
            </span>
          </v-avatar>

          <div class="flex-grow-1">
            <div class="d-flex align-center gap-3 flex-wrap">
              <span class="text-h5 font-weight-bold">{{ server.name }}</span>
              <v-chip :color="statusColor" size="small" variant="tonal">
                <v-icon start size="10">mdi-circle</v-icon>
                {{ server.status }}
              </v-chip>
            </div>
            <div class="mono text-body-2 text-medium-emphasis mt-1">{{ server.fullName }}</div>
            <div class="d-flex flex-wrap gap-4 mt-2">
              <span class="text-caption text-medium-emphasis">
                <v-icon size="12" class="me-1">mdi-console</v-icon>
                {{ server.os || 'linux' }}
              </span>
              <span class="text-caption mono text-medium-emphasis">
                <v-icon size="12" class="me-1">mdi-tag</v-icon>
                Agent {{ server.agentVersion }}
              </span>
              <span class="text-caption mono text-medium-emphasis">
                <v-icon size="12" class="me-1">mdi-heart-pulse</v-icon>
                Last heartbeat: {{ formatTime(server.lastHeartbeat) }}
              </span>
            </div>
          </div>

          <v-btn
            color="error"
            variant="outlined"
            size="small"
            prepend-icon="mdi-trash-can"
            @click="confirmDelete = true"
          >
            Remove Server
          </v-btn>
        </div>
      </v-card>

      <!-- Metric Gauges -->
      <v-row class="mb-6">
        <v-col
          v-for="metric in metrics"
          :key="metric.label"
          cols="12" sm="4"
        >
          <v-card color="surface" class="rounded-xl pa-6 text-center" style="border: 1px solid rgba(255,255,255,0.07);">
            <v-progress-circular
              :model-value="metric.value"
              :color="gaugeColor(metric.value)"
              :size="96"
              :width="8"
              bg-color="rgba(255,255,255,0.08)"
              class="mb-3"
            >
              <span class="text-h6 font-weight-bold">{{ metric.value }}%</span>
            </v-progress-circular>
            <div class="text-body-1 font-weight-medium">{{ metric.label }}</div>
            <div class="text-caption text-medium-emphasis">{{ metric.sub }}</div>
          </v-card>
        </v-col>
      </v-row>

      <!-- Services -->
      <div class="d-flex align-center justify-space-between mb-4">
        <div class="text-h6 font-weight-semibold">Services</div>
        <v-btn
          color="primary"
          size="small"
          prepend-icon="mdi-plus"
          @click="addDialog = true"
        >
          Add Service
        </v-btn>
      </div>

      <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
        <v-data-table
          :headers="serviceHeaders"
          :items="services"
          :loading="servicesLoading"
          density="comfortable"
          no-data-text="No services configured"
          hide-default-footer
          :items-per-page="-1"
        >
          <template #item.status="{ item }">
            <v-chip
              :color="item.status === 'running' ? 'success' : 'warning'"
              size="x-small"
              variant="tonal"
            >
              {{ item.status || 'unknown' }}
            </v-chip>
          </template>
          <template #item.iconGlyph="{ item }">
            <v-icon :color="item.iconColor || 'primary'" size="20">
              {{ item.iconGlyph || 'mdi-application' }}
            </v-icon>
          </template>
          <template #item.port="{ item }">
            <span class="mono text-caption">{{ item.port }}</span>
          </template>
          <template #item.dockerImage="{ item }">
            <span class="mono text-caption text-medium-emphasis">{{ item.dockerImage || '—' }}</span>
          </template>
          <template #item.actions="{ item }">
            <v-btn
              icon="mdi-trash-can"
              size="x-small"
              variant="text"
              color="error"
              @click="handleDeleteService(item)"
            />
          </template>
          <template #bottom />
        </v-data-table>
      </v-card>
    </template>

    <!-- Add Service Dialog -->
    <v-dialog v-model="addDialog" max-width="480">
      <v-card color="surface" class="rounded-xl">
        <v-card-title class="pa-6 pb-0">
          <span class="text-h6">Add Service</span>
        </v-card-title>
        <v-card-text class="pa-6">
          <v-text-field
            v-model="newService.name"
            label="Service Name"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model.number="newService.port"
            label="Port"
            variant="outlined"
            density="comfortable"
            type="number"
            class="mb-3"
          />
          <v-select
            v-model="newService.serviceType"
            :items="serviceTypes"
            label="Service Type"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="newService.description"
            label="Description (optional)"
            variant="outlined"
            density="comfortable"
          />
        </v-card-text>
        <v-card-actions class="pa-6 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="addDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :loading="addingService"
            :disabled="!newService.name || !newService.port"
            @click="handleAddService"
          >
            Add Service
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirm Dialog -->
    <v-dialog v-model="confirmDelete" max-width="400">
      <v-card color="surface" class="rounded-xl">
        <v-card-title class="pa-6 pb-0">Remove Server</v-card-title>
        <v-card-text class="pa-6">
          Are you sure you want to remove <strong>{{ server?.name }}</strong>? This action cannot be undone.
        </v-card-text>
        <v-card-actions class="pa-6 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="confirmDelete = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" :loading="deleting" @click="handleDeleteServer">
            Remove
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000" location="bottom right">
      {{ snackbar.text }}
      <template #actions>
        <v-btn variant="text" @click="snackbar.show = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getServer, listServices, addService, deleteService, deleteServer } from '@/api/index.js'

const route = useRoute()
const router = useRouter()

const server = ref(null)
const services = ref([])
const loading = ref(false)
const servicesLoading = ref(false)
const addDialog = ref(false)
const addingService = ref(false)
const confirmDelete = ref(false)
const deleting = ref(false)
const snackbar = ref({ show: false, text: '', color: 'error' })

const newService = ref({ name: '', port: null, serviceType: 'web', description: '' })
const serviceTypes = ['web', 'api', 'database', 'cache', 'proxy', 'monitoring', 'media', 'storage', 'other']

const breadcrumbs = computed(() => [
  { title: 'Dashboard', to: '/', disabled: false },
  { title: server.value?.name || 'Server', disabled: true }
])

const statusColor = computed(() => {
  const s = (server.value?.status || '').toLowerCase()
  if (s === 'up' || s === 'online') return 'success'
  if (s === 'down' || s === 'offline') return 'error'
  if (s === 'degraded') return 'warning'
  return 'grey'
})

const metrics = computed(() => {
  if (!server.value) return []
  return [
    {
      label: 'CPU Usage',
      value: Math.round(server.value.cpu ?? 0),
      sub: 'processor load'
    },
    {
      label: 'Memory Usage',
      value: Math.round(server.value.mem ?? 0),
      sub: 'RAM utilization'
    },
    {
      label: 'Disk Usage',
      value: Math.round(server.value.disk ?? 0),
      sub: 'storage utilization'
    }
  ]
})

const serviceHeaders = [
  { title: '', key: 'iconGlyph', width: '40px', sortable: false },
  { title: 'Name', key: 'name' },
  { title: 'Port', key: 'port', width: '80px' },
  { title: 'Type', key: 'serviceType', width: '120px' },
  { title: 'Status', key: 'status', width: '100px' },
  { title: 'Docker Image', key: 'dockerImage' },
  { title: 'Actions', key: 'actions', width: '60px', sortable: false }
]

function gaugeColor(val) {
  if (val >= 90) return 'error'
  if (val >= 70) return 'warning'
  return 'success'
}

function formatTime(val) {
  if (!val) return '—'
  const ts = typeof val === 'number' ? val * 1000 : new Date(val).getTime()
  return new Date(ts).toLocaleString('en-US', {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit', hour12: false
  })
}

async function loadServer() {
  loading.value = true
  try {
    server.value = await getServer(route.params.id)
  } catch {
    snackbar.value = { show: true, text: 'Failed to load server', color: 'error' }
    server.value = null
  } finally {
    loading.value = false
  }
}

async function loadServices() {
  servicesLoading.value = true
  try {
    services.value = await listServices(route.params.id)
  } catch {
    snackbar.value = { show: true, text: 'Failed to load services', color: 'error' }
  } finally {
    servicesLoading.value = false
  }
}

async function handleAddService() {
  addingService.value = true
  try {
    await addService(route.params.id, { ...newService.value })
    newService.value = { name: '', port: null, serviceType: 'web', description: '' }
    addDialog.value = false
    await loadServices()
    snackbar.value = { show: true, text: 'Service added', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Failed to add service', color: 'error' }
  } finally {
    addingService.value = false
  }
}

async function handleDeleteService(item) {
  try {
    await deleteService(item.id)
    await loadServices()
    snackbar.value = { show: true, text: 'Service removed', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Failed to remove service', color: 'error' }
  }
}

async function handleDeleteServer() {
  deleting.value = true
  try {
    await deleteServer(route.params.id)
    router.push('/')
  } catch {
    snackbar.value = { show: true, text: 'Failed to remove server', color: 'error' }
    confirmDelete.value = false
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  loadServer()
  loadServices()
})
</script>

<style scoped>
.mono { font-family: 'JetBrains Mono', monospace; }
.gap-3 { gap: 12px; }
.gap-4 { gap: 16px; }
</style>
