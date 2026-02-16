import React, { useEffect } from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './hooks/useAuth'
import { BrandingProvider } from './contexts/TenantBrandingContext'
import toast, { Toaster, useToasterStore } from 'react-hot-toast'
import App from './App.jsx'
import './index.css'
import './styles/tokens.css'

const TOAST_LIMIT = 3

const LimitedToaster = () => {
  const { toasts } = useToasterStore()

  useEffect(() => {
    const visible = toasts.filter((t) => t.visible)
    if (visible.length > TOAST_LIMIT) {
      visible
        .slice(0, visible.length - TOAST_LIMIT)
        .forEach((t) => toast.dismiss(t.id))
    }
  }, [toasts])

  return (
    <Toaster
      position="top-right"
      toastOptions={{
        duration: 3000,
        style: {
          background: '#363636',
          color: '#fff',
        },
        success: {
          duration: 3000,
          iconTheme: {
            primary: '#10b981',
            secondary: '#fff',
          },
        },
        error: {
          duration: 4000,
          iconTheme: {
            primary: '#ef4444',
            secondary: '#fff',
          },
        },
      }}
      containerStyle={{ top: 20 }}
      gutter={8}
    />
  )
}

// Force favicon update on load (for cache-busting)
const updateFavicon = () => {
  const timestamp = Date.now()
  const favicon = document.querySelector("link[rel='icon']") || document.querySelector("link[rel='shortcut icon']")
  if (favicon) {
    const newHref = favicon.href.split('?')[0] + '?v=' + timestamp
    favicon.href = newHref
  }

  // Update apple touch icon
  const appleIcon = document.querySelector("link[rel='apple-touch-icon']")
  if (appleIcon) {
    const newHref = appleIcon.href.split('?')[0] + '?v=' + timestamp
    appleIcon.href = newHref
  }

  // Update manifest
  const manifest = document.querySelector("link[rel='manifest']")
  if (manifest) {
    const newHref = manifest.href.split('?')[0] + '?v=' + timestamp
    manifest.href = newHref
  }
}

// Update favicon on mount
updateFavicon()

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter future={{ v7_relativeSplatPath: true, v7_startTransition: true }}>
      <AuthProvider>
        <BrandingProvider>
          <App />
        </BrandingProvider>
      </AuthProvider>
      <LimitedToaster />
    </BrowserRouter>
  </React.StrictMode>,
)
