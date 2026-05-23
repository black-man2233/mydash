<template>
  <v-card
    :color="cardColor"
    class="server-card rounded-xl cursor-pointer"
    hover
    @click="navigate"
    style="border: 1px solid rgba(255,255,255,0.07);"
  >
    <!-- Card Header -->
    <v-card-text class="pb-2 pt-4">
      <div class="d-flex align-center mb-3">
        <v-avatar
          :color="server.color || 'primary'"
          size="40"
          class="me-3 flex-shrink-0"
        >
          <span class="text-body-1 font-weight-bold text-white">
            {{ server.initial || server.name?.charAt(0)?.toUpperCase() || '?' }}
          </span>
        </v-avatar>

        <div class="flex-grow-1 overflow-hidden">
          <div class="text-body-1 font-weight-semibold text-truncate">{{ server.name }}</div>
          <div class="text-caption text-medium-emphasis mono text-truncate">{{ server.fullName || server.name }}</div>
        </div>

        <v-chip
          :color="statusColor"
          size="x-small"
          variant="tonal"
          class="ms-2 flex-shrink-0"
        >
          <v-icon start size="8">mdi-circle</v-icon>
          {{ server.status || 'Unknown' }}
        </v-chip>
      </div>

      <!-- Tags -->
      <div v-if="server.tags?.length" class="d-flex flex-wrap gap-1 mb-3">
        <v-chip
          v-for="tag in server.tags.slice(0, 3)"
          :key="tag"
          size="x-small"
          variant="outlined"
          color="secondary"
          class="text-caption"
        >
          {{ tag }}
        </v-chip>
      </div>
    </v-card-text>

    <!-- Metric Gauges -->
    <v-card-text class="pt-0 pb-3">
      <div class="d-flex justify-space-around align-center">
        <div
          v-for="metric in metrics"
          :key="metric.label"
          class="d-flex flex-column align-center"
        >
          <v-progress-circular
            :model-value="metric.value"
            :color="gaugeColor(metric.value)"
            :size="56"
            :width="5"
            bg-color="rgba(255,255,255,0.08)"
          >
            <span class="text-caption font-weight-bold">{{ metric.value }}%</span>
          </v-progress-circular>
          <div class="text-caption text-medium-emphasis mt-1">{{ metric.label }}</div>
        </div>
      </div>
    </v-card-text>

    <v-divider class="border-opacity-15" />

    <!-- Footer -->
    <v-card-text class="py-2 px-4">
      <div class="d-flex justify-space-between align-center">
        <div class="d-flex align-center">
          <v-icon size="12" color="medium-emphasis" class="me-1">mdi-clock-outline</v-icon>
          <span class="text-caption text-medium-emphasis">{{ uptimeFormatted }}</span>
        </div>
        <div class="d-flex align-center">
          <v-icon size="12" color="medium-emphasis" class="me-1">mdi-console</v-icon>
          <span class="text-caption mono text-medium-emphasis">{{ server.os || 'linux' }}</span>
        </div>
        <div class="d-flex align-center">
          <v-icon size="12" color="medium-emphasis" class="me-1">mdi-tag</v-icon>
          <span class="text-caption mono text-medium-emphasis">{{ server.agentVersion || 'v?' }}</span>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'

const props = defineProps({
  server: {
    type: Object,
    required: true
  }
})

const router = useRouter()

const cardColor = 'surface'

const statusColor = computed(() => {
  const s = (props.server.status || '').toLowerCase()
  if (s === 'up' || s === 'online') return 'success'
  if (s === 'down' || s === 'offline') return 'error'
  if (s === 'degraded') return 'warning'
  return 'grey'
})

const metrics = computed(() => [
  { label: 'CPU', value: Math.round(props.server.cpu ?? 0) },
  { label: 'MEM', value: Math.round(props.server.mem ?? 0) },
  { label: 'DISK', value: Math.round(props.server.disk ?? 0) }
])

function gaugeColor(val) {
  if (val >= 90) return 'error'
  if (val >= 70) return 'warning'
  return 'success'
}

const uptimeFormatted = computed(() => {
  const secs = props.server.uptimeSeconds || 0
  if (secs === 0) return 'Unknown'
  const d = Math.floor(secs / 86400)
  const h = Math.floor((secs % 86400) / 3600)
  const m = Math.floor((secs % 3600) / 60)
  if (d > 0) return `${d}d ${h}h`
  if (h > 0) return `${h}h ${m}m`
  return `${m}m`
})

function navigate() {
  router.push(`/servers/${props.server.id}`)
}
</script>

<style scoped>
.server-card {
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.server-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0,0,0,0.4) !important;
}
.cursor-pointer {
  cursor: pointer;
}
.mono {
  font-family: 'JetBrains Mono', monospace;
}
.gap-1 {
  gap: 4px;
}
</style>
