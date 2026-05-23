<template>
  <div class="login-bg d-flex align-center justify-center" style="min-height: 100vh;">
    <v-card
      width="420"
      color="surface"
      class="pa-2 rounded-xl"
      elevation="8"
    >
      <!-- Header -->
      <v-card-text class="text-center pb-0 pt-6">
        <div class="d-flex align-center justify-center mb-4">
          <v-icon color="primary" size="40" class="me-2">mdi-view-dashboard-variant</v-icon>
          <span class="text-h4 font-weight-bold text-primary" style="letter-spacing: 0.05em;">MyDash</span>
        </div>
        <div class="text-body-2 text-medium-emphasis mb-2">Homelab Dashboard</div>
      </v-card-text>

      <v-divider class="mx-4 mt-4 border-opacity-25" />

      <v-card-text class="pa-6">
        <!-- Error Alert -->
        <v-alert
          v-if="error"
          type="error"
          variant="tonal"
          class="mb-4"
          density="compact"
          closable
          @click:close="error = ''"
        >
          {{ error }}
        </v-alert>

        <!-- Lockout Alert -->
        <v-alert
          v-if="lockoutMessage"
          type="warning"
          variant="tonal"
          class="mb-4"
          density="compact"
          prepend-icon="mdi-lock-clock"
        >
          {{ lockoutMessage }}
        </v-alert>

        <!-- Step 1: Request PIN -->
        <div v-if="step === 'request'">
          <div class="text-center mb-6">
            <v-icon size="48" color="primary" class="mb-3">mdi-message-lock</v-icon>
            <div class="text-h6 font-weight-medium mb-1">Sign In</div>
            <div class="text-body-2 text-medium-emphasis">
              We'll send a verification code to your phone
            </div>
          </div>

          <v-btn
            color="primary"
            size="large"
            block
            :loading="loading"
            prepend-icon="mdi-message-text"
            @click="handleRequestPin"
            class="mt-2"
          >
            Send Verification Code
          </v-btn>
        </div>

        <!-- Step 2: Enter PIN -->
        <div v-else-if="step === 'verify'">
          <div class="text-center mb-5">
            <v-icon size="48" color="secondary" class="mb-3">mdi-cellphone-key</v-icon>
            <div class="text-h6 font-weight-medium mb-1">Enter Code</div>
            <div class="text-body-2 text-medium-emphasis">
              Code sent to
              <span class="mono text-secondary">{{ phoneMasked }}</span>
              <span v-if="provider" class="text-caption ms-1">({{ provider }})</span>
            </div>
            <div v-if="expiresIn > 0" class="text-caption text-medium-emphasis mt-1">
              Expires in {{ expiresIn }}s
            </div>
          </div>

          <v-otp-input
            v-if="useOtp"
            v-model="code"
            length="6"
            type="number"
            variant="outlined"
            :disabled="loading"
            @finish="handleVerifyPin"
            class="mb-4"
          />
          <v-text-field
            v-else
            v-model="code"
            label="Verification code"
            variant="outlined"
            density="comfortable"
            type="text"
            inputmode="numeric"
            autocomplete="one-time-code"
            maxlength="8"
            :disabled="loading"
            class="mono mb-4"
            prepend-inner-icon="mdi-pound"
            autofocus
            @keyup.enter="handleVerifyPin"
          />

          <v-btn
            color="primary"
            size="large"
            block
            :loading="loading"
            :disabled="!code || code.length < 4"
            prepend-icon="mdi-login"
            @click="handleVerifyPin"
          >
            Sign In
          </v-btn>

          <div class="text-center mt-4">
            <v-btn
              variant="text"
              size="small"
              color="secondary"
              :disabled="loading"
              prepend-icon="mdi-refresh"
              @click="handleResend"
            >
              Send new code
            </v-btn>
          </div>
        </div>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup>
import { ref, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { requestPin } from '@/api/index.js'
import { useAuthStore } from '@/stores/auth.js'

const router = useRouter()
const auth = useAuthStore()

const step = ref('request')
const loading = ref(false)
const error = ref('')
const lockoutMessage = ref('')

const challengeId = ref('')
const phoneMasked = ref('')
const provider = ref('')
const expiresIn = ref(0)
const code = ref('')
const useOtp = ref(false)

let countdownTimer = null

function startCountdown(seconds) {
  expiresIn.value = seconds
  clearInterval(countdownTimer)
  countdownTimer = setInterval(() => {
    if (expiresIn.value > 0) {
      expiresIn.value--
    } else {
      clearInterval(countdownTimer)
    }
  }, 1000)
}

onUnmounted(() => clearInterval(countdownTimer))

async function handleRequestPin() {
  loading.value = true
  error.value = ''
  lockoutMessage.value = ''

  try {
    const data = await requestPin()
    challengeId.value = data.challengeId
    phoneMasked.value = data.phoneMasked || ''
    provider.value = data.provider || ''
    expiresIn.value = data.expiresInSeconds || 0
    code.value = ''
    step.value = 'verify'

    if (data.expiresInSeconds) {
      startCountdown(data.expiresInSeconds)
    }
  } catch (err) {
    const msg = err.response?.data?.message || err.response?.data || 'Failed to send code. Please try again.'
    error.value = typeof msg === 'string' ? msg : JSON.stringify(msg)
  } finally {
    loading.value = false
  }
}

async function handleVerifyPin() {
  if (!code.value || code.value.length < 4) return

  loading.value = true
  error.value = ''
  lockoutMessage.value = ''

  try {
    const result = await auth.login(challengeId.value, code.value)

    if (result.ok) {
      router.push('/')
    } else if (result.lockedOut) {
      lockoutMessage.value = `Account locked. Try again in ${result.lockoutSeconds}s.`
      code.value = ''
    } else {
      const remaining = result.attemptsRemaining
      error.value = `Incorrect code. ${remaining} attempt${remaining !== 1 ? 's' : ''} remaining.`
      code.value = ''
    }
  } catch (err) {
    const msg = err.response?.data?.message || err.response?.data || 'Verification failed. Please try again.'
    error.value = typeof msg === 'string' ? msg : JSON.stringify(msg)
    code.value = ''
  } finally {
    loading.value = false
  }
}

async function handleResend() {
  step.value = 'request'
  code.value = ''
  error.value = ''
  lockoutMessage.value = ''
  clearInterval(countdownTimer)
  await handleRequestPin()
}
</script>

<style scoped>
.login-bg {
  background: linear-gradient(135deg, #0D1117 0%, #0a0e1a 50%, #0D1117 100%);
}

.mono {
  font-family: 'JetBrains Mono', monospace;
}
</style>
