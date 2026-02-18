import React from 'react'
import axios from 'axios'
import camelcaseKeys from 'camelcase-keys'
import toast from 'react-hot-toast'
import { connectionManager } from './connectionManager'
import { showMaintenanceOverlay } from '../components/MaintenanceOverlay'
import { setSubscriptionGraceFromResponse } from '../components/SubscriptionGraceBanner'

// API base URL: single source of truth so production never uses localhost
import { getApiBaseUrl } from './apiConfig'
const API_BASE_URL = getApiBaseUrl()

// Error throttling to prevent flooding
let lastErrorToast = null
let errorToastCount = 0
let lastNetworkErrorToast = null
const ERROR_THROTTLE_MS = 3000 // Show max 1 error toast per 3 seconds for general errors
const SERVER_ERROR_THROTTLE_MS = 12000 // Show max 1 server (500) toast per 12 seconds to avoid spam
const NETWORK_ERROR_THROTTLE_MS = 15000 // Show max 1 network error toast per 15 seconds
let lastServerErrorToast = null

// Client-reported errors: queue when backend is down, send to backend when it's back (Super Admin sees in Error Logs)
const clientErrorQueue = []
const MAX_QUEUED_CLIENT_ERRORS = 50
function queueClientError(message, path) {
  const p = path || (typeof window !== 'undefined' ? window.location?.pathname : '') || ''
  clientErrorQueue.push({ message, path: p })
  if (clientErrorQueue.length > MAX_QUEUED_CLIENT_ERRORS) clientErrorQueue.shift()
}
function flushClientErrorsToBackend() {
  if (clientErrorQueue.length === 0) return
  const items = clientErrorQueue.splice(0, clientErrorQueue.length)
  const message = items[0]?.message || 'Service temporarily unavailable'
  const path = items.length === 1 ? (items[0]?.path || '') : `multiple (${items.length} requests failed)`
  api.post('/error-logs/client', { message, path, count: items.length }).catch(() => {})
}

// Request deduplication and throttling to prevent 429 errors
const pendingRequests = new Map()
const requestThrottle = new Map() // Track last request time per endpoint
const REQUEST_THROTTLE_MS = 50 // Only throttle very rapid duplicates (< 50ms) - prevents double-clicks
const MAX_CONCURRENT_REQUESTS = 20 // Increased concurrent requests limit

// Generate request key for deduplication
const getRequestKey = (config) => {
  if (!config) {
    return `UNKNOWN_${Date.now()}_${Math.random()}`
  }
  const method = (config.method || 'GET').toUpperCase()
  const url = config.url || ''
  const params = config.params || {}
  return `${method}_${url}_${JSON.stringify(params)}`
}

const showThrottledError = (message, isNetworkError = false, options = {}) => {
  const now = Date.now()
  const throttleTime = isNetworkError ? NETWORK_ERROR_THROTTLE_MS : ERROR_THROTTLE_MS
  const lastToast = isNetworkError ? lastNetworkErrorToast : lastErrorToast

  if (lastToast && (now - lastToast) < throttleTime && !options.forceShow) {
    errorToastCount++
    return // Skip this error, already showing one
  }

  if (isNetworkError) {
    lastNetworkErrorToast = now
  } else {
    lastErrorToast = now
  }

  errorToastCount = 1

  const withRetry = options.withRetry !== false && (isNetworkError || options.isServerError)
  const toastContent = withRetry
    ? (t) =>
        React.createElement(
          'span',
          { className: 'flex items-center gap-2' },
          message,
          React.createElement(
            'button',
            {
              type: 'button',
              onClick: () => {
                toast.dismiss(t.id)
                window.location.reload()
              },
              className: 'ml-2 px-2 py-1 text-sm font-medium bg-primary-600 text-white rounded hover:bg-primary-700'
            },
            'Retry'
          )
        )
    : message

  if (isNetworkError) {
    toast.error(withRetry ? toastContent : message, {
      duration: withRetry ? 12000 : 6000,
      id: 'network-error',
      position: 'top-center'
    })
  } else {
    toast.error(withRetry ? toastContent : message, { duration: withRetry ? 10000 : 4000 })
  }
}

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000, // 30 seconds - show "request taking too long" instead of blank loading
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to add auth token, check connection, and throttle requests
api.interceptors.request.use(
  (config) => {
    // CRITICAL: Ensure config exists and has required properties
    if (!config) {
      config = {}
    }
    if (!config.method) {
      config.method = 'GET'
    }
    if (!config.url) {
      config.url = ''
    }

    // Check if we should allow this request
    if (!connectionManager.shouldAllowRequest()) {
      const error = new Error('Server connection unavailable. Please wait...')
      error.config = config
      error.isConnectionBlocked = true
      return Promise.reject(error)
    }

    // CRITICAL: Throttle requests to prevent 429 errors
    const requestKey = getRequestKey(config)
    const now = Date.now()
    const lastRequestTime = requestThrottle.get(requestKey) || 0
    const timeSinceLastRequest = now - lastRequestTime

    // CRITICAL: Remove aggressive throttling - only track, don't block
    // Only log very rapid duplicates for debugging, but allow all requests to proceed
    if (timeSinceLastRequest < REQUEST_THROTTLE_MS && !config._isRetry) {
      // Log duplicate but don't block - allows normal page loads
      if (pendingRequests.has(requestKey)) {
        console.log(`⏸️ Rapid duplicate request detected (${timeSinceLastRequest}ms): ${config.method} ${config.url} - allowing to proceed`)
      }
    }

    // CRITICAL: Don't block requests - only track for monitoring
    // Remove all blocking logic - let server handle rate limiting
    // The server's 429 response will be handled gracefully

    // Track this request (for monitoring only, not blocking)
    config._requestKey = requestKey
    config._requestTime = now

    // Update throttle time (for tracking only)
    requestThrottle.set(requestKey, now)

    // Clean up old throttle entries periodically (prevent memory leaks)
    if (requestThrottle.size > 200) {
      const oneMinuteAgo = now - 60000
      for (const [key, time] of requestThrottle.entries()) {
        if (time < oneMinuteAgo) {
          requestThrottle.delete(key)
        }
      }
    }

    // Add auth token
    const token = localStorage.getItem('token')
    if (token) {
      config.headers = config.headers || {}
      config.headers.Authorization = `Bearer ${token}`
    }

    // MULTI-TENANT IMPERSONATION: Add impersonation header for Super Admin
    const selectedTenantId = localStorage.getItem('selected_tenant_id')
    if (selectedTenantId) {
      config.headers = config.headers || {}
      config.headers['X-Tenant-Id'] = selectedTenantId
    }

    // Add retry configuration
    config._retryCount = config._retryCount || 0
    config._maxRetries = 3
    config._requestKey = requestKey // Store for cleanup in response interceptor

    // CRITICAL: Request interceptor must return config, not promise
    // Deduplication will be handled by tracking the request key
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Recursively convert PascalCase keys to camelCase in API responses.
// Eliminates need for fallbacks like entry.grandTotal || entry.GrandTotal
const transformResponseKeys = (obj) => {
  if (obj == null) return obj
  if (Array.isArray(obj)) return obj.map(transformResponseKeys)
  if (typeof obj === 'object' && obj.constructor === Object) {
    return camelcaseKeys(obj, { deep: true })
  }
  return obj
}

// Response interceptor to handle errors and cleanup
// Offline detection and error handling
api.interceptors.response.use(
  (response) => {
    // Mark connection as successful on any successful response
    connectionManager.markConnected()
    // Send any client-reported errors (e.g. connection refused) to backend so Super Admin can see them
    flushClientErrorsToBackend()

    // Clean up pending request tracking
    if (response.config?._requestKey) {
      pendingRequests.delete(response.config._requestKey)
    }

    // Normalize PascalCase to camelCase in response data
    if (response?.data != null) {
      try {
        if (typeof response.data === 'object') {
          response.data = transformResponseKeys(response.data)
        }
      } catch (_) { /* ignore transform errors */ }
    }

    // Subscription grace period: check for X-Subscription-Grace-Period header
    setSubscriptionGraceFromResponse(response)

    return response
  },
  async (error) => {
    // Handle connection blocked errors
    if (error.isConnectionBlocked) {
      return Promise.reject(error)
    }

    // Ping is best-effort; never trigger disconnect or toasts (avoids 404/network spam and health-check flood)
    const isPingRequest = (error.config?.url || '').includes('me/ping')
    if (isPingRequest) {
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle network/connection errors (includes timeout ECONNABORTED)
    const isTimeout = error.code === 'ECONNABORTED' || error.message?.includes('timeout')
    const isNetworkError = !error.response && (
      isTimeout ||
      error.message === 'Network Error' ||
      error.code === 'ERR_NETWORK' ||
      error.code === 'ERR_CORS' ||
      error.code === 'ECONNREFUSED' ||
      error.code === 'ETIMEDOUT' ||
      error.message?.includes('Failed to fetch') ||
      error.message?.includes('NetworkError')
    )

    if (isNetworkError) {
      // Mark connection as failed
      connectionManager.markDisconnected()

      // Login page shows its own message for auth/login — avoid duplicate toast
      const isLoginRequest = (error.config?.url || '').includes('auth/login')
      const errorMsg = isTimeout
        ? 'The request timed out. If using Render free tier, the backend may be starting (cold start). Wait 30-60 seconds and try again.'
        : error.code === 'ERR_CORS'
          ? 'Service is temporarily unavailable. Please try again or contact support.'
          : 'Service temporarily unavailable. Please try again or contact support.'

      if (!isLoginRequest) showThrottledError(errorMsg, true)
      error._handledByInterceptor = true
      // Queue so Super Admin sees it in Error Logs when backend is back
      queueClientError(errorMsg, error.config?.url || (typeof window !== 'undefined' ? window.location?.pathname : ''))

      // Notify Login page so it can show the banner without an extra health check
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('hexabill-backend-unavailable'))
      }

      return Promise.reject(error)
    }

    // Clean up pending request tracking on error (do this first)
    if (error.config?._requestKey) {
      pendingRequests.delete(error.config._requestKey)
    }

    // Handle 429 Too Many Requests - CRITICAL: Prevent request flooding
    if (error.response?.status === 429) {
      connectionManager.markConnected() // Server is responding

      const retryAfter = error.response?.headers?.['retry-after'] || 5
      const message = `Too many requests. Please wait ${retryAfter} seconds before trying again.`

      showThrottledError(message, false)
      error._handledByInterceptor = true

      // Don't log every 429 error to prevent console flooding
      if (errorToastCount === 1) {
        console.warn('⚠️ Rate limit exceeded (429). Requests are being throttled.')
      }

      return Promise.reject(error)
    }

    // Handle throttled requests (from interceptor) - silently reject
    if (error.isThrottled) {
      // Don't show error for throttled requests - they're expected behavior
      return Promise.reject(error)
    }

    // Handle rate limited requests
    if (error.isRateLimited) {
      showThrottledError('Too many requests in progress. Please wait...', false)
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle 400 Bad Request - validation errors (show errors array or message)
    if (error.response?.status === 400) {
      connectionManager.markConnected()
      const body = error.response?.data
      const errors = body?.errors
      const message = Array.isArray(errors) && errors.length > 0
        ? errors.join('. ')
        : (body?.message || 'Invalid request. Please check your input.')
      showThrottledError(message, false)
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle 404 Not Found (with special message for Branches/Routes API)
    if (error.response?.status === 404) {
      connectionManager.markConnected()
      const url = (error.config?.url || '').toLowerCase()
      const isBranchesOrRoutes = url.includes('/branches') || url.includes('/routes')
      let msg
      if (isBranchesOrRoutes && !sessionStorage.getItem('hexabill_branches_routes_404_shown')) {
        sessionStorage.setItem('hexabill_branches_routes_404_shown', '1')
        msg = 'Branches & Routes feature is currently unavailable. Please try again later or contact support.'
      } else {
        msg = error.response?.data?.message || 'Resource not found.'
      }
      showThrottledError(msg, false)
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle 502 Bad Gateway - show retry
    if (error.response?.status === 502) {
      connectionManager.markConnected()
      queueClientError('Server 502 Bad Gateway', error.config?.url)
      const msg = error.response?.data?.message || 'Server temporarily unavailable. Please try again.'
      showThrottledError(msg, false, { isServerError: true })
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    const url = (error.config?.url || '').toLowerCase()

    // Handle 403 Forbidden - tenant suspended/expired or insufficient permissions
    if (error.response?.status === 403) {
      connectionManager.markConnected()
      const body = error.response?.data
      const msg = typeof body === 'string'
        ? body
        : (body?.message || body?.errors?.[0] || 'Access denied')
      const isTenantBlock = /tenant|trial|suspended|expired/i.test(msg)
      const url = (error.config?.url || '').toLowerCase()
      const isAdminEndpoint = url.includes('/admin/') || url.includes('/backup/')
      const displayMsg = isTenantBlock
        ? `${msg} Please contact your administrator.`
        : isAdminEndpoint && !msg
          ? 'Admin or Owner access required for this feature.'
          : msg
      showThrottledError(displayMsg, false)
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle 503 Maintenance Mode - show branded maintenance screen, no toast
    if (error.response?.status === 503) {
      connectionManager.markConnected()
      const msg = error.response?.data?.message || 'System under maintenance. Back shortly.'
      showMaintenanceOverlay(msg)
      error._handledByInterceptor = true
      return Promise.reject(error)
    }

    // Handle 401 Unauthorized errors
    if (error.response?.status === 401) {
      connectionManager.markConnected() // Server is responding, just auth issue

      const hadToken = !!(error.config?.headers?.Authorization || localStorage.getItem('token'))
      const authFailure = error.response?.headers?.['x-auth-failure']
      const errorMessage = error.response?.data?.message || ''
      const tokenExpired = error.response?.headers?.['token-expired'] === 'true'
      const method = (error.config?.method || '').toUpperCase()
      const isMutating = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)

      // Only logout + redirect when we HAD a token (user was logged in, now session invalid).
      // If we had no token, this is a public/unauthenticated request (e.g. login page) - don't redirect.
      // For mutating requests (POST/PUT), only force logout on clear token-expired - avoid false logout from timing/validation bugs
      const isAuthFailure = hadToken && (
        tokenExpired ||
        authFailure === 'Token-Expired' ||
        (!isMutating && (authFailure ||
          errorMessage.toLowerCase().includes('session') ||
          errorMessage.toLowerCase().includes('expired') ||
          errorMessage.toLowerCase().includes('token') ||
          errorMessage.toLowerCase().includes('authentication') ||
          errorMessage.toLowerCase().includes('login')))
      )

      if (isAuthFailure) {
        // Clear auth data
        localStorage.removeItem('token')
        localStorage.removeItem('user')

        // Show appropriate error message (only once)
        const message = errorMessage ||
          (tokenExpired ? 'Your session has expired. Please login again.' :
            'Authentication required. Please login again.')

        toast.error(message, { duration: 3000 })
        error._handledByInterceptor = true

        // Small delay to let the toast show before redirect
        setTimeout(() => {
          window.location.href = '/login'
        }, 1500)
      } else if (hadToken) {
        // Had token but 401 - for POST/PUT/DELETE, likely session expired; for GET, permission issue
        const msg = isMutating
          ? (errorMessage || 'Your session may have expired. Please log in again.')
          : (errorMessage || 'You are not authorized to perform this action')
        showThrottledError(msg)
        error._handledByInterceptor = true
      }
      // If no token: silent fail (e.g. BrandingProvider on login page) - no toast, no redirect
    } else if (error.response?.status >= 500) {
      connectionManager.markConnected()
      // Duplicate payment check failure: do not show scary error; caller proceeds with payment
      const url = (error.config?.url || '').toLowerCase()
      if (url.includes('duplicate-check')) {
        error._handledByInterceptor = true
        return Promise.reject(error)
      }
      // Queue so Super Admin sees in Error Logs
      const baseMessage = error.response?.data?.message || 'Server error (500)'
      queueClientError(`Server 500: ${baseMessage}`, error.config?.url)
      // Server errors - throttle heavily to avoid repeated toasts for branches/routes/reports
      const now = Date.now()
      if (lastServerErrorToast && (now - lastServerErrorToast) < SERVER_ERROR_THROTTLE_MS) {
        return Promise.reject(error)
      }
      lastServerErrorToast = now
      const correlationId = error.response?.data?.correlationId || error.response?.headers?.['x-correlation-id']
      const errorMsg = correlationId ? `${baseMessage} (Ref: ${correlationId})` : baseMessage
      showThrottledError(errorMsg, false, { isServerError: true })
      error._handledByInterceptor = true
    } else if (error.response?.data?.message) {
      // Server is responding with message
      connectionManager.markConnected()
      const correlationId = error.response?.data?.correlationId || error.response?.headers?.['x-correlation-id']
      const baseMessage = error.response.data.message
      const errorMsg = correlationId ? `Something went wrong. Ref: ${correlationId}` : baseMessage
      showThrottledError(errorMsg)
      error._handledByInterceptor = true
    } else if (error.response?.data?.errors && Array.isArray(error.response.data.errors)) {
      // Server is responding with errors array
      connectionManager.markConnected()
      const correlationId = error.response?.data?.correlationId || error.response?.headers?.['x-correlation-id']
      const errorMsg = error.response.data.errors.join(', ')
      const finalMsg = correlationId ? `Something went wrong. Ref: ${correlationId}` : errorMsg
      showThrottledError(finalMsg)
      error._handledByInterceptor = true
    } else if (error.response?.data) {
      // Server is responding but structure is different
      connectionManager.markConnected()
      const correlationId = error.response?.data?.correlationId || error.response?.headers?.['x-correlation-id']
      const errorMsg = correlationId ? `Something went wrong. Ref: ${correlationId}` : 'An error occurred. Please try again.'
      showThrottledError(errorMsg)
      error._handledByInterceptor = true
    } else {
      // Unknown error
      connectionManager.markConnected()
      showThrottledError('An error occurred. Please try again.')
      error._handledByInterceptor = true
    }

    return Promise.reject(error)
  }
)

export default api
