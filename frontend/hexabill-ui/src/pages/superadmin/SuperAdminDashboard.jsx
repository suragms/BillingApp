import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Building2,
  CheckCircle,
  Ban,
  DollarSign,
  Users,
  Database,
  Activity,
  AlertTriangle,
  ChevronRight,
  Wallet,
  Zap,
  TrendingUp
} from 'lucide-react'
import { superAdminAPI } from '../../services'
import { formatCurrency } from '../../utils/currency'
import { LoadingCard } from '../../components/Loading'
import toast from 'react-hot-toast'

/**
 * Platform overview – 6 metrics only (enterprise structure).
 * Total Companies, Active Companies, Suspended Companies, Monthly Revenue, Total Active Users, Total DB Storage Used.
 */
const SuperAdminDashboard = () => {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)
  const [dashboard, setDashboard] = useState(null)
  const [tenantActivity, setTenantActivity] = useState([])
  const [activityLoading, setActivityLoading] = useState(false)
  const [extendingTrialId, setExtendingTrialId] = useState(null)

  useEffect(() => {
    fetchDashboard()
  }, [])

  useEffect(() => {
    let interval
    const fetchActivity = async () => {
      try {
        setActivityLoading(true)
        const res = await superAdminAPI.getTenantActivity()
        if (res?.success && Array.isArray(res?.data)) setTenantActivity(res.data)
      } catch {
        setTenantActivity([])
      } finally {
        setActivityLoading(false)
      }
    }
    fetchActivity()
    interval = setInterval(fetchActivity, 60000)
    return () => clearInterval(interval)
  }, [])

  const fetchDashboard = async () => {
    try {
      setLoading(true)
      const response = await superAdminAPI.getPlatformDashboard()
      if (response.success) {
        setDashboard(response.data)
      } else {
        toast.error(response.message || 'Failed to load dashboard')
      }
    } catch (error) {
      console.error('Dashboard error:', error)
      if (!error?._handledByInterceptor) toast.error('Failed to load platform dashboard')
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-[#F8FAFC] p-4 sm:p-6">
        <LoadingCard />
      </div>
    )
  }

  if (!dashboard) {
    return (
      <div className="min-h-screen bg-[#F8FAFC] p-4 sm:p-6">
        <div className="max-w-5xl mx-auto">
          <div className="bg-white border border-neutral-200 rounded-xl p-6">
            <p className="text-neutral-800 font-medium">Failed to load dashboard data</p>
          </div>
        </div>
      </div>
    )
  }

  const metrics = [
    {
      title: 'Total Companies',
      value: dashboard.totalTenants,
      icon: Building2,
      desc: 'All companies on platform'
    },
    {
      title: 'Active Companies',
      value: dashboard.activeTenants,
      icon: CheckCircle,
      desc: `${(dashboard.totalTenants > 0 && Number.isFinite(Number(dashboard.activeTenants)))
        ? ((Number(dashboard.activeTenants) / dashboard.totalTenants) * 100).toFixed(0)
        : 0}% Active · ${dashboard.trialTenants ?? 0} on trial`
    },
    {
      title: 'Suspended Companies',
      value: dashboard.suspendedTenants + (dashboard.expiredTenants || 0),
      icon: Ban,
      desc: 'Suspended + expired'
    },
    {
      title: 'Total Tenant Sales',
      value: formatCurrency(Number(dashboard?.platformRevenue) || 0),
      icon: DollarSign,
      desc: 'All companies\' sales combined (not platform earnings)'
    },
    {
      title: 'Platform MRR',
      value: formatCurrency(Number(dashboard?.mrr) || 0),
      icon: Wallet,
      desc: 'Subscription revenue from active plans'
    },
    {
      title: 'Total Active Users',
      value: (dashboard?.totalUsers || 0).toLocaleString(),
      icon: Users,
      desc: 'Across all companies'
    },
    {
      title: 'Total DB Storage Used',
      value: `${(dashboard?.estimatedStorageUsedMb ?? 0).toFixed(0)} MB`,
      icon: Database,
      desc: 'Estimated (row-based)'
    }
  ]

  return (
    <div className="min-h-screen bg-[#F8FAFC]">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
          <div>
            <h1 className="text-2xl font-bold text-neutral-900">Platform overview</h1>
            <p className="text-neutral-600 text-sm mt-1">Key metrics at a glance</p>
          </div>
          <div className="flex items-center gap-2 text-sm text-neutral-500">
            <Activity className="h-4 w-4" />
            <span>Live</span>
          </div>
        </div>

        {/* Live Activity - Top tenants by API calls (last 60 min) */}
        <div className="mb-6 p-4 bg-white border border-neutral-200 rounded-xl">
          <h3 className="font-semibold text-neutral-800 flex items-center gap-2 mb-3">
            <Zap className="h-5 w-5 text-amber-500" />
            Live Activity — Top 10 by API calls (last 60 min)
          </h3>
          {activityLoading && tenantActivity.length === 0 ? (
            <p className="text-sm text-neutral-500">Loading…</p>
          ) : tenantActivity.length === 0 ? (
            <p className="text-sm text-neutral-500">No tenant activity in the last hour</p>
          ) : (
            <ul className="space-y-2">
              {tenantActivity.map((t, idx) => (
                <li
                  key={t.tenantId}
                  className={`flex items-center justify-between p-2 rounded-lg border cursor-pointer transition-colors ${
                    t.isHighVolume
                      ? 'bg-red-50 border-red-200 hover:bg-red-100'
                      : 'bg-neutral-50 border-neutral-100 hover:bg-neutral-100'
                  }`}
                  onClick={() => navigate(`/superadmin/tenants/${t.tenantId}`)}
                >
                  <span className="flex items-center gap-2">
                    <span className="text-neutral-500 font-mono text-sm w-6">#{idx + 1}</span>
                    <span className="font-medium text-neutral-900">{t.tenantName}</span>
                    {t.isHighVolume && (
                      <span className="text-xs font-medium text-red-600 bg-red-100 px-1.5 py-0.5 rounded">
                        High volume
                      </span>
                    )}
                  </span>
                  <span className="text-sm text-neutral-600">
                    {t.requestCount} req · last {new Date(t.lastActiveAt).toLocaleTimeString()}
                  </span>
                  <ChevronRight className="h-4 w-4 text-neutral-400" />
                </li>
              ))}
            </ul>
          )}
        </div>

        {dashboard?.trialsExpiringThisWeek?.length > 0 && (
          <div className="mb-6 p-4 bg-amber-50 border border-amber-200 rounded-xl">
            <h3 className="font-semibold text-amber-800 flex items-center gap-2 mb-3">
              <AlertTriangle className="h-5 w-5" />
              Trials Expiring This Week ({dashboard.trialsExpiringThisWeek.length})
            </h3>
            <ul className="space-y-2">
              {dashboard.trialsExpiringThisWeek.slice(0, 10).map((t) => {
                const endDate = new Date(t.trialEndDate)
                const daysLeft = Math.ceil((endDate - new Date()) / (24 * 60 * 60 * 1000))
                return (
                  <li
                    key={t.tenantId}
                    className="flex items-center justify-between p-2 bg-white rounded-lg border border-amber-100 hover:bg-amber-50"
                  >
                    <span
                      className="font-medium text-neutral-900 cursor-pointer flex-1"
                      onClick={() => navigate(`/superadmin/tenants/${t.tenantId}`)}
                    >
                      {t.name}
                    </span>
                    <span className="text-sm text-amber-700 mr-2">
                      {endDate.toLocaleDateString()} · {daysLeft} day{daysLeft !== 1 ? 's' : ''} left
                    </span>
                    <button
                      type="button"
                      disabled={extendingTrialId === t.tenantId}
                      onClick={async (e) => {
                        e.stopPropagation()
                        try {
                          setExtendingTrialId(t.tenantId)
                          const newEnd = new Date(endDate)
                          newEnd.setDate(newEnd.getDate() + 7)
                          const res = await superAdminAPI.updateTenant(t.tenantId, {
                            trialEndDate: newEnd.toISOString()
                          })
                          if (res?.success) {
                            toast.success(`Trial extended for ${t.name}`, { id: 'trial-extend' })
                            fetchDashboard()
                          } else {
                            toast.error(res?.message || 'Failed to extend trial', { id: 'trial-extend' })
                          }
                        } catch (err) {
                          if (!err?._handledByInterceptor) {
                            toast.error(err?.response?.data?.message || 'Failed to extend trial', { id: 'trial-extend' })
                          }
                        } finally {
                          setExtendingTrialId(null)
                        }
                      }}
                      className="px-3 py-1 text-xs font-medium bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {extendingTrialId === t.tenantId ? 'Extending…' : 'Extend 7 days'}
                    </button>
                    <ChevronRight
                      className="h-4 w-4 text-neutral-400 cursor-pointer ml-1"
                      onClick={() => navigate(`/superadmin/tenants/${t.tenantId}`)}
                    />
                  </li>
                )
              })}
            </ul>
            {dashboard.trialsExpiringThisWeek.length > 10 && (
              <p className="mt-2 text-sm text-amber-700">
                +{dashboard.trialsExpiringThisWeek.length - 10} more. View in{' '}
                <button
                  type="button"
                  onClick={() => navigate('/superadmin/subscriptions')}
                  className="underline font-medium"
                >
                  Subscriptions
                </button>
              </p>
            )}
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {metrics.map((m) => {
            const Icon = m.icon
            return (
              <div
                key={m.title}
                className="bg-white rounded-xl border border-neutral-200 shadow-sm p-6"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <p className="text-sm font-medium text-neutral-500">{m.title}</p>
                    <p className="text-2xl font-bold text-neutral-900 mt-1">{m.value}</p>
                    {m.desc && (
                      <p className="text-xs text-neutral-400 mt-2">{m.desc}</p>
                    )}
                  </div>
                  <div className="p-2 rounded-lg bg-neutral-100 text-neutral-600">
                    <Icon className="h-5 w-5" />
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export default SuperAdminDashboard
