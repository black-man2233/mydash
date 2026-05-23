import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' }
})

// ─── Auth ───────────────────────────────────────────────────────────────────
export function requestPin() {
  return api.post('/auth/request-pin').then(r => r.data)
}

export function verifyPin(challengeId, code) {
  return api.post('/auth/verify-pin', { challengeId, code }).then(r => r.data)
}

export function logout() {
  return api.post('/auth/logout').then(r => r.data)
}

export function getMe() {
  return api.get('/auth/me').then(r => r.data)
}

// ─── Servers ─────────────────────────────────────────────────────────────────
export function listServers() {
  return api.get('/servers').then(r => r.data)
}

export function getServer(id) {
  return api.get(`/servers/${id}`).then(r => r.data)
}

export function deleteServer(id) {
  return api.delete(`/servers/${id}`)
}

// ─── Services ────────────────────────────────────────────────────────────────
export function listServices(serverId) {
  return api.get(`/servers/${serverId}/services`).then(r => r.data)
}

export function addService(serverId, data) {
  return api.post(`/servers/${serverId}/services`, data).then(r => r.data)
}

export function deleteService(id) {
  return api.delete(`/services/${id}`)
}

// ─── Enrollment ──────────────────────────────────────────────────────────────
export function createToken(name, ttlMinutes) {
  return api.post('/enrollment/tokens', { name, ttlMinutes }).then(r => r.data)
}

export function listTokens() {
  return api.get('/enrollment/tokens').then(r => r.data)
}

export function deleteToken(id) {
  return api.delete(`/enrollment/tokens/${id}`)
}

// ─── Audit ───────────────────────────────────────────────────────────────────
export function listAudit(limit = 50) {
  return api.get('/audit', { params: { limit } }).then(r => r.data)
}

// ─── Preferences ─────────────────────────────────────────────────────────────
export function getPreferences() {
  return api.get('/preferences').then(r => r.data)
}

export function updatePreferences(data) {
  return api.put('/preferences', data).then(r => r.data)
}

export default api
