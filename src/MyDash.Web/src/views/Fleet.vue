<template>
  <div>
    <div class="text-h5 font-weight-bold mb-6">Fleet Management</div>

    <v-tabs v-model="tab" color="primary" class="mb-6">
      <v-tab value="agents" prepend-icon="mdi-server">Enrolled Agents</v-tab>
      <v-tab value="tokens" prepend-icon="mdi-key">Enrollment Tokens</v-tab>
      <v-tab value="downloads" prepend-icon="mdi-download">Agent Downloads</v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <!-- ─── Agents Tab ─── -->
      <v-window-item value="agents">
        <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
          <v-card-text class="pa-0">
            <v-data-table
              :headers="agentHeaders"
              :items="servers"
              :loading="serversLoading"
              density="comfortable"
              no-data-text="No agents enrolled"
              hide-default-footer
              :items-per-page="-1"
            >
              <template #item.name="{ item }">
                <div class="d-flex align-center py-2">
                  <v-avatar :color="item.color || 'primary'" size="32" class="me-3">
                    <span class="text-caption font-weight-bold">{{ item.initial || item.name?.charAt(0)?.toUpperCase() }}</span>
                  </v-avatar>
                  <div>
                    <div class="text-body-2 font-weight-medium">{{ item.name }}</div>
                    <div class="mono text-caption text-medium-emphasis">{{ item.fullName }}</div>
                  </div>
                </div>
              </template>
              <template #item.status="{ item }">
                <v-chip
                  :color="statusColor(item.status)"
                  size="x-small"
                  variant="tonal"
                >
                  <v-icon start size="8">mdi-circle</v-icon>
                  {{ item.status || 'unknown' }}
                </v-chip>
              </template>
              <template #item.lastHeartbeat="{ item }">
                <span class="mono text-caption text-medium-emphasis">{{ formatTime(item.lastHeartbeat) }}</span>
              </template>
              <template #item.agentVersion="{ item }">
                <span class="mono text-caption">{{ item.agentVersion }}</span>
              </template>
              <template #item.actions="{ item }">
                <v-btn
                  icon="mdi-trash-can"
                  size="x-small"
                  variant="text"
                  color="error"
                  @click="revokeServer(item)"
                />
              </template>
              <template #bottom />
            </v-data-table>
          </v-card-text>
        </v-card>
      </v-window-item>

      <!-- ─── Tokens Tab ─── -->
      <v-window-item value="tokens">
        <div class="d-flex align-center justify-space-between mb-4">
          <div class="text-body-1 font-weight-medium text-medium-emphasis">
            Enrollment tokens let new servers join your fleet.
          </div>
          <v-btn
            color="primary"
            prepend-icon="mdi-plus"
            @click="tokenDialog = true"
          >
            Create Token
          </v-btn>
        </div>

        <!-- New Token Result -->
        <v-card
          v-if="newToken"
          color="success"
          variant="tonal"
          class="rounded-xl mb-6 pa-4"
        >
          <div class="d-flex align-center mb-3">
            <v-icon color="success" class="me-2">mdi-check-circle</v-icon>
            <span class="text-body-1 font-weight-semibold">Token Created — Save it now!</span>
            <v-btn
              icon="mdi-close"
              size="x-small"
              variant="text"
              class="ms-auto"
              @click="newToken = null"
            />
          </div>
          <div class="text-caption text-medium-emphasis mb-2">Enrollment Token (shown once):</div>
          <div class="d-flex align-center gap-2 mb-4">
            <v-text-field
              :model-value="newToken.tokenPlaintext"
              readonly
              variant="outlined"
              density="compact"
              class="mono flex-grow-1"
              hide-details
            />
            <v-btn
              icon="mdi-content-copy"
              variant="tonal"
              color="success"
              @click="copyToClipboard(newToken.tokenPlaintext)"
            />
          </div>
          <div class="text-caption text-medium-emphasis mb-2">Docker Compose snippet:</div>
          <v-sheet
            color="rgba(0,0,0,0.4)"
            class="rounded pa-3 mono text-caption"
            style="white-space: pre; overflow-x: auto;"
          >{{ dockerComposeSnippet(newToken) }}</v-sheet>
          <v-btn
            size="small"
            variant="text"
            color="success"
            class="mt-2"
            prepend-icon="mdi-content-copy"
            @click="copyToClipboard(dockerComposeSnippet(newToken))"
          >
            Copy compose snippet
          </v-btn>
        </v-card>

        <!-- Tokens Table -->
        <v-card color="surface" class="rounded-xl" style="border: 1px solid rgba(255,255,255,0.07);">
          <v-data-table
            :headers="tokenHeaders"
            :items="tokens"
            :loading="tokensLoading"
            density="comfortable"
            no-data-text="No tokens"
            hide-default-footer
            :items-per-page="-1"
          >
            <template #item.name="{ item }">
              <span class="font-weight-medium">{{ item.name }}</span>
            </template>
            <template #item.createdAtUnix="{ item }">
              <span class="mono text-caption text-medium-emphasis">{{ formatDate(item.createdAtUnix) }}</span>
            </template>
            <template #item.expiresAtUnix="{ item }">
              <span class="mono text-caption" :class="isExpired(item.expiresAtUnix) ? 'text-error' : 'text-medium-emphasis'">
                {{ formatDate(item.expiresAtUnix) }}
              </span>
            </template>
            <template #item.status="{ item }">
              <v-chip
                :color="item.consumed ? 'success' : isExpired(item.expiresAtUnix) ? 'error' : 'primary'"
                size="x-small"
                variant="tonal"
              >
                {{ item.consumed ? 'Used' : isExpired(item.expiresAtUnix) ? 'Expired' : 'Active' }}
              </v-chip>
            </template>
            <template #item.actions="{ item }">
              <v-btn
                icon="mdi-trash-can"
                size="x-small"
                variant="text"
                color="error"
                @click="revokeToken(item)"
              />
            </template>
            <template #bottom />
          </v-data-table>
        </v-card>
      </v-window-item>

      <!-- ─── Downloads Tab ─── -->
      <v-window-item value="downloads">
        <v-row>
          <v-col cols="12" md="6">
            <v-card color="surface" class="rounded-xl pa-4 mb-4" style="border: 1px solid rgba(255,255,255,0.07);">
              <div class="d-flex align-center mb-3">
                <v-icon color="primary" class="me-2">mdi-docker</v-icon>
                <span class="text-body-1 font-weight-semibold">Docker Compose</span>
                <v-btn
                  icon="mdi-content-copy"
                  size="x-small"
                  variant="text"
                  class="ms-auto"
                  @click="copyToClipboard(dockerInstallSnippet)"
                />
              </div>
              <v-sheet
                color="rgba(0,0,0,0.4)"
                class="rounded pa-3 mono text-caption"
                style="white-space: pre; overflow-x: auto;"
              >{{ dockerInstallSnippet }}</v-sheet>
            </v-card>
          </v-col>

          <v-col cols="12" md="6">
            <v-card color="surface" class="rounded-xl pa-4 mb-4" style="border: 1px solid rgba(255,255,255,0.07);">
              <div class="d-flex align-center mb-3">
                <v-icon color="success" class="me-2">mdi-linux</v-icon>
                <span class="text-body-1 font-weight-semibold">Linux (systemd)</span>
                <v-btn
                  icon="mdi-content-copy"
                  size="x-small"
                  variant="text"
                  class="ms-auto"
                  @click="copyToClipboard(linuxInstallSnippet)"
                />
              </div>
              <v-sheet
                color="rgba(0,0,0,0.4)"
                class="rounded pa-3 mono text-caption"
                style="white-space: pre; overflow-x: auto;"
              >{{ linuxInstallSnippet }}</v-sheet>
            </v-card>

            <v-card color="surface" class="rounded-xl pa-4" style="border: 1px solid rgba(255,255,255,0.07);">
              <div class="d-flex align-center mb-3">
                <v-icon color="info" class="me-2">mdi-microsoft-windows</v-icon>
                <span class="text-body-1 font-weight-semibold">Windows (PowerShell)</span>
                <v-btn
                  icon="mdi-content-copy"
                  size="x-small"
                  variant="text"
                  class="ms-auto"
                  @click="copyToClipboard(windowsInstallSnippet)"
                />
              </div>
              <v-sheet
                color="rgba(0,0,0,0.4)"
                class="rounded pa-3 mono text-caption"
                style="white-space: pre; overflow-x: auto;"
              >{{ windowsInstallSnippet }}</v-sheet>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>

    <!-- Create Token Dialog -->
    <v-dialog v-model="tokenDialog" max-width="440">
      <v-card color="surface" class="rounded-xl">
        <v-card-title class="pa-6 pb-2">Create Enrollment Token</v-card-title>
        <v-card-text class="pa-6">
          <v-text-field
            v-model="tokenForm.name"
            label="Server Name"
            placeholder="e.g. nas-01"
            variant="outlined"
            density="comfortable"
            class="mb-4"
            autofocus
          />
          <v-select
            v-model="tokenForm.ttlMinutes"
            :items="ttlOptions"
            item-title="label"
            item-value="value"
            label="Token Expires In"
            variant="outlined"
            density="comfortable"
          />
        </v-card-text>
        <v-card-actions class="pa-6 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="tokenDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :loading="creatingToken"
            :disabled="!tokenForm.name"
            @click="handleCreateToken"
          >
            Create Token
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Revoke Server Confirm -->
    <v-dialog v-model="revokeServerDialog" max-width="400">
      <v-card color="surface" class="rounded-xl">
        <v-card-title class="pa-6 pb-0">Remove Server</v-card-title>
        <v-card-text class="pa-6">
          Remove <strong>{{ selectedServer?.name }}</strong> from the fleet? This cannot be undone.
        </v-card-text>
        <v-card-actions class="pa-6 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="revokeServerDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" :loading="revokingServer" @click="confirmRevokeServer">Remove</v-btn>
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
import { ref, onMounted } from 'vue'
import { listServers, deleteServer, createToken, listTokens, deleteToken } from '@/api/index.js'

const tab = ref('agents')
const servers = ref([])
const tokens = ref([])
const newToken = ref(null)
const serversLoading = ref(false)
const tokensLoading = ref(false)
const tokenDialog = ref(false)
const creatingToken = ref(false)
const revokeServerDialog = ref(false)
const revokingServer = ref(false)
const selectedServer = ref(null)
const snackbar = ref({ show: false, text: '', color: 'error' })

const tokenForm = ref({ name: '', ttlMinutes: 60 })
const ttlOptions = [
  { label: '15 minutes', value: 15 },
  { label: '1 hour', value: 60 },
  { label: '6 hours', value: 360 },
  { label: '24 hours', value: 1440 },
  { label: '7 days', value: 10080 }
]

const agentHeaders = [
  { title: 'Server', key: 'name' },
  { title: 'Status', key: 'status', width: '100px' },
  { title: 'OS', key: 'os', width: '100px' },
  { title: 'Version', key: 'agentVersion', width: '100px' },
  { title: 'Last Heartbeat', key: 'lastHeartbeat' },
  { title: '', key: 'actions', width: '50px', sortable: false }
]

const tokenHeaders = [
  { title: 'Name', key: 'name' },
  { title: 'Created', key: 'createdAtUnix' },
  { title: 'Expires', key: 'expiresAtUnix' },
  { title: 'Status', key: 'status', width: '90px' },
  { title: '', key: 'actions', width: '50px', sortable: false }
]

const dockerInstallSnippet = `services:
  mydash-agent:
    image: ghcr.io/mydash/agent:latest
    restart: unless-stopped
    environment:
      HUB_URL: http://hub:8080
      ENROLLMENT_TOKEN: YOUR_TOKEN_HERE
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
    privileged: false`

const linuxInstallSnippet = `# Download and install the agent
curl -fsSL https://hub/install/agent.sh | bash

# Or manually:
wget https://hub/agent/latest/mydash-agent-linux-amd64
chmod +x mydash-agent-linux-amd64
sudo mv mydash-agent-linux-amd64 /usr/local/bin/mydash-agent

# Configure and start
sudo mydash-agent --hub http://hub:8080 --token YOUR_TOKEN_HERE install
sudo systemctl enable --now mydash-agent`

const windowsInstallSnippet = `# PowerShell (Run as Administrator)
$url = "https://hub/agent/latest/mydash-agent-windows-amd64.exe"
$dest = "C:\\Program Files\\MyDash\\mydash-agent.exe"
New-Item -ItemType Directory -Force -Path "C:\\Program Files\\MyDash"
Invoke-WebRequest -Uri $url -OutFile $dest

# Install as Windows service
& $dest --hub http://hub:8080 --token YOUR_TOKEN_HERE install
Start-Service mydash-agent`

function dockerComposeSnippet(token) {
  return `services:
  mydash-agent:
    image: ghcr.io/mydash/agent:latest
    restart: unless-stopped
    environment:
      HUB_URL: http://hub:8080
      ENROLLMENT_TOKEN: ${token.tokenPlaintext}
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro`
}

function statusColor(status) {
  const s = (status || '').toLowerCase()
  if (s === 'up' || s === 'online') return 'success'
  if (s === 'down' || s === 'offline') return 'error'
  if (s === 'degraded') return 'warning'
  return 'grey'
}

function formatTime(val) {
  if (!val) return '—'
  const ts = typeof val === 'number' ? val * 1000 : new Date(val).getTime()
  return new Date(ts).toLocaleString('en-US', {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit', hour12: false
  })
}

function formatDate(unix) {
  if (!unix) return '—'
  return new Date(unix * 1000).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric'
  })
}

function isExpired(unix) {
  if (!unix) return false
  return Date.now() > unix * 1000
}

async function copyToClipboard(text) {
  try {
    await navigator.clipboard.writeText(text)
    snackbar.value = { show: true, text: 'Copied to clipboard', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Copy failed', color: 'error' }
  }
}

async function loadServers() {
  serversLoading.value = true
  try {
    servers.value = await listServers()
  } catch {
    snackbar.value = { show: true, text: 'Failed to load servers', color: 'error' }
  } finally {
    serversLoading.value = false
  }
}

async function loadTokens() {
  tokensLoading.value = true
  try {
    tokens.value = await listTokens()
  } catch {
    snackbar.value = { show: true, text: 'Failed to load tokens', color: 'error' }
  } finally {
    tokensLoading.value = false
  }
}

async function handleCreateToken() {
  creatingToken.value = true
  try {
    const result = await createToken(tokenForm.value.name, tokenForm.value.ttlMinutes)
    newToken.value = result
    tokenDialog.value = false
    tokenForm.value = { name: '', ttlMinutes: 60 }
    await loadTokens()
  } catch {
    snackbar.value = { show: true, text: 'Failed to create token', color: 'error' }
  } finally {
    creatingToken.value = false
  }
}

function revokeServer(server) {
  selectedServer.value = server
  revokeServerDialog.value = true
}

async function confirmRevokeServer() {
  if (!selectedServer.value) return
  revokingServer.value = true
  try {
    await deleteServer(selectedServer.value.id)
    revokeServerDialog.value = false
    await loadServers()
    snackbar.value = { show: true, text: 'Server removed', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Failed to remove server', color: 'error' }
  } finally {
    revokingServer.value = false
    selectedServer.value = null
  }
}

async function revokeToken(token) {
  try {
    await deleteToken(token.id)
    await loadTokens()
    snackbar.value = { show: true, text: 'Token revoked', color: 'success' }
  } catch {
    snackbar.value = { show: true, text: 'Failed to revoke token', color: 'error' }
  }
}

onMounted(() => {
  loadServers()
  loadTokens()
})
</script>

<style scoped>
.mono { font-family: 'JetBrains Mono', monospace; }
.gap-2 { gap: 8px; }
</style>
