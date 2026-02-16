import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { MapPin, ArrowLeft, Plus, Trash2, Edit, Printer, Users, Receipt, BarChart3 } from 'lucide-react'
import { formatCurrency } from '../../utils/currency'
import toast from 'react-hot-toast'
import { routesAPI, salesAPI } from '../../services'
import Modal from '../../components/Modal'
import { Input } from '../../components/Form'
import { isAdminOrOwner } from '../../utils/roles'
import ConfirmDangerModal from '../../components/ConfirmDangerModal'

const EXPENSE_CATEGORIES = ['Fuel', 'Staff', 'Delivery', 'Vehicle Maintenance', 'Toll/Parking', 'Misc']
const ROUTE_TABS = ['overview', 'customers', 'sales', 'expenses', 'staff', 'performance']

const RouteDetailPage = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState('overview')
  const [route, setRoute] = useState(null)
  const [summary, setSummary] = useState(null)
  const [expenses, setExpenses] = useState([])
  const [loading, setLoading] = useState(true)
  const defaultFrom = (() => {
    const d = new Date()
    d.setMonth(d.getMonth() - 1)
    return d.toISOString().split('T')[0]
  })()
  const defaultTo = new Date().toISOString().split('T')[0]
  const [fromDate, setFromDate] = useState(defaultFrom)
  const [toDate, setToDate] = useState(defaultTo)
  const [dateDraftFrom, setDateDraftFrom] = useState(defaultFrom)
  const [dateDraftTo, setDateDraftTo] = useState(defaultTo)
  const [showExpenseModal, setShowExpenseModal] = useState(false)
  const [saving, setSaving] = useState(false)
  const [expenseCategory, setExpenseCategory] = useState('Misc')
  const [expenseAmount, setExpenseAmount] = useState('')
  const [expenseDate, setExpenseDate] = useState(new Date().toISOString().split('T')[0])
  const [expenseDescription, setExpenseDescription] = useState('')
  const [selectedExpenseForEdit, setSelectedExpenseForEdit] = useState(null)
  const [dangerModal, setDangerModal] = useState({
    isOpen: false,
    title: '',
    message: '',
    onConfirm: () => { }
  })
  const [collectionSheet, setCollectionSheet] = useState(null)
  const [collectionSheetDate, setCollectionSheetDate] = useState(new Date().toISOString().split('T')[0])
  const [loadingSheet, setLoadingSheet] = useState(false)
  const [routeSales, setRouteSales] = useState([])
  const [routeSalesLoading, setRouteSalesLoading] = useState(false)

  const canManage = isAdminOrOwner(JSON.parse(localStorage.getItem('user') || '{}'))

  const loadRoute = async () => {
    if (!id) return
    try {
      const res = await routesAPI.getRoute(id)
      if (res?.success && res?.data) setRoute(res.data)
      else setRoute(null)
    } catch {
      setRoute(null)
    }
  }

  const loadSummary = async () => {
    if (!id) return
    try {
      const res = await routesAPI.getRouteSummary(id, fromDate, toDate)
      if (res?.success && res?.data) setSummary(res.data)
      else setSummary(null)
    } catch {
      setSummary(null)
    }
  }

  const loadExpenses = async () => {
    if (!id) return
    try {
      const res = await routesAPI.getRouteExpenses(id, fromDate, toDate)
      if (res?.success && res?.data) setExpenses(res.data)
      else setExpenses([])
    } catch {
      setExpenses([])
    }
  }

  const applyDateRange = () => {
    setFromDate(dateDraftFrom)
    setToDate(dateDraftTo)
  }

  // Load route on mount; load summary/expenses when id or applied date range changes.
  useEffect(() => {
    if (!id) return
    loadRoute()
  }, [id])

  useEffect(() => {
    if (!id) return
    setLoading(true)
    Promise.all([loadSummary(), loadExpenses()]).finally(() => setLoading(false))
  }, [id, fromDate, toDate])

  useEffect(() => {
    if (activeTab === 'sales' && id) {
      setRouteSalesLoading(true)
      salesAPI.getSales({ routeId: parseInt(id, 10), pageSize: 100 })
        .then(res => {
          const items = res?.data?.items ?? res?.items ?? []
          setRouteSales(Array.isArray(items) ? items : [])
        })
        .catch(() => setRouteSales([]))
        .finally(() => setRouteSalesLoading(false))
    }
  }, [activeTab, id])

  const handleAddExpense = async (e) => {
    e?.preventDefault()
    const amount = parseFloat(expenseAmount)
    if (isNaN(amount) || amount <= 0) {
      toast.error('Enter a valid amount')
      return
    }
    try {
      setSaving(true)
      const res = await routesAPI.createRouteExpense(id, {
        category: expenseCategory,
        amount: amount,
        expenseDate: expenseDate,
        description: expenseDescription?.trim() || undefined
      })
      if (res?.success) {
        toast.success('Expense added', { id: 'route-expense-add' })
        setShowExpenseModal(false)
        resetExpenseForm()
        loadExpenses()
        loadSummary()
      } else {
        toast.error(res?.message || 'Failed to add expense')
      }
    } catch (e) {
      if (!e?._handledByInterceptor) toast.error(e?.message || 'Failed to add expense')
    } finally {
      setSaving(false)
    }
  }

  const resetExpenseForm = () => {
    setExpenseCategory('Misc')
    setExpenseAmount('')
    setExpenseDate(new Date().toISOString().split('T')[0])
    setExpenseDescription('')
    setSelectedExpenseForEdit(null)
  }

  const openAddExpenseModal = () => {
    resetExpenseForm()
    setShowExpenseModal(true)
  }

  const openEditExpenseModal = (expense) => {
    setSelectedExpenseForEdit(expense)
    setExpenseCategory(expense.category || 'Misc')
    setExpenseAmount(String(expense.amount ?? ''))
    setExpenseDate((expense.expenseDate || expense.ExpenseDate || '').toString().split('T')[0] || new Date().toISOString().split('T')[0])
    setExpenseDescription(expense.description || expense.Description || '')
    setShowExpenseModal(true)
  }

  const handleUpdateExpense = async (e) => {
    e?.preventDefault()
    if (!selectedExpenseForEdit) return
    const amount = parseFloat(expenseAmount)
    if (isNaN(amount) || amount <= 0) {
      toast.error('Enter a valid amount')
      return
    }
    try {
      setSaving(true)
      const res = await routesAPI.updateRouteExpense(id, selectedExpenseForEdit.id, {
        category: expenseCategory,
        amount: amount,
        expenseDate: expenseDate,
        description: expenseDescription?.trim() || undefined
      })
      if (res?.success) {
        toast.success('Expense updated', { id: 'route-expense-update' })
        setShowExpenseModal(false)
        resetExpenseForm()
        loadExpenses()
        loadSummary()
      } else {
        toast.error(res?.message || 'Failed to update expense')
      }
    } catch (e) {
      if (!e?._handledByInterceptor) toast.error(e?.message || 'Failed to update expense')
    } finally {
      setSaving(false)
    }
  }

  const openCollectionSheet = async () => {
    if (!id) return
    setLoadingSheet(true)
    try {
      const res = await routesAPI.getRouteCollectionSheet(id, collectionSheetDate)
      if (res?.success && res?.data) setCollectionSheet(res.data)
      else setCollectionSheet(null)
    } catch {
      setCollectionSheet(null)
      toast.error('Failed to load collection sheet')
    } finally {
      setLoadingSheet(false)
    }
  }

  const closeCollectionSheet = () => setCollectionSheet(null)

  const printCollectionSheet = () => {
    window.print()
  }

  const handleDeleteExpense = (expenseId) => {
    setDangerModal({
      isOpen: true,
      title: 'Delete expense?',
      message: 'This expense will be permanently removed from this route.',
      confirmLabel: 'Delete',
      onConfirm: async () => {
        try {
          const res = await routesAPI.deleteRouteExpense(id, expenseId)
          if (res?.success) {
            toast.success('Expense deleted', { id: 'route-expense-delete' })
            loadExpenses()
            loadSummary()
          } else {
            toast.error(res?.message || 'Failed to delete')
          }
        } catch (e) {
          if (!e?._handledByInterceptor) toast.error(e?.message || 'Failed to delete')
        }
      }
    })
  }

  if (loading && !route) {
    return (
      <div className="p-6 flex items-center justify-center min-h-[200px]">
        <div className="animate-spin rounded-full h-10 w-10 border-2 border-primary-600 border-t-transparent" />
      </div>
    )
  }

  if (!route) {
    return (
      <div className="p-6">
        <p className="text-neutral-600">Route not found.</p>
        <Link to="/branches?tab=routes" className="text-primary-600 hover:underline mt-2 inline-block">Back to Routes</Link>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <Link to="/branches?tab=routes" className="inline-flex items-center gap-1 text-primary-600 hover:underline mb-4">
        <ArrowLeft className="h-4 w-4" />
        Back to Routes
      </Link>
      <div className="flex items-center gap-3 mb-6">
        <div className="p-2 rounded-lg bg-primary-50">
          <MapPin className="h-6 w-6 text-primary-600" />
        </div>
        <div>
          <h1 className="text-xl font-semibold text-neutral-900">{route.name}</h1>
          <p className="text-sm text-neutral-500">{route.branchName ?? '—'}</p>
        </div>
      </div>

      <div className="border-b border-neutral-200 mb-4">
        <nav className="-mb-px flex gap-6">
          {ROUTE_TABS.map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`py-3 px-1 border-b-2 font-medium text-sm ${activeTab === tab ? 'border-primary-600 text-primary-600' : 'border-transparent text-neutral-500 hover:text-neutral-700'}`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </nav>
      </div>

      {activeTab === 'overview' && (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <input type="date" value={dateDraftFrom} onChange={(e) => setDateDraftFrom(e.target.value)} className="border border-neutral-300 rounded px-2 py-1 text-sm" />
            <input type="date" value={dateDraftTo} onChange={(e) => setDateDraftTo(e.target.value)} className="border border-neutral-300 rounded px-2 py-1 text-sm" />
            <button type="button" onClick={applyDateRange} className="px-3 py-1.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium">Apply</button>
          </div>
          {summary && (
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Total Sales</p>
                <p className="text-lg font-semibold text-neutral-900">{formatCurrency(summary.totalSales)}</p>
              </div>
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Total Expenses</p>
                <p className="text-lg font-semibold text-neutral-900">{formatCurrency(summary.totalExpenses)}</p>
              </div>
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Profit</p>
                <p className={`text-lg font-semibold ${summary.profit >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>{formatCurrency(summary.profit)}</p>
              </div>
            </div>
          )}
          {route.branchId && (
            <p className="text-sm text-neutral-500">
              Branch: <Link to={`/branches/${route.branchId}`} className="text-primary-600 hover:underline">{route.branchName || 'View'}</Link>
            </p>
          )}
        </>
      )}

      {activeTab === 'customers' && (
        <div>
          {(!route.customers || route.customers.length === 0) ? (
            <p className="text-neutral-500 py-6">No customers on this route. Add customers and assign them to this route.</p>
          ) : (
            <div className="overflow-x-auto border border-neutral-200 rounded-lg">
              <table className="min-w-full divide-y divide-neutral-200">
                <thead className="bg-neutral-50 sticky top-0">
                  <tr>
                    <th className="px-4 py-2 text-left text-xs font-medium text-neutral-600 uppercase">Customer</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-neutral-600 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-200 bg-white">
                  {route.customers.map(rc => (
                    <tr key={rc.customerId || rc.id} className="hover:bg-neutral-50">
                      <td className="px-4 py-2 font-medium">{rc.customerName}</td>
                      <td className="px-4 py-2 text-right"><button type="button" onClick={() => navigate(`/ledger?customerId=${rc.customerId}`)} className="text-primary-600 hover:underline text-sm">Ledger</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'sales' && (
        <div>
          {routeSalesLoading ? (
            <div className="py-8 flex justify-center"><div className="animate-spin rounded-full h-8 w-8 border-2 border-primary-600 border-t-transparent" /></div>
          ) : routeSales.length === 0 ? (
            <p className="text-neutral-500 py-6">No invoices for this route yet.</p>
          ) : (
            <div className="overflow-x-auto border border-neutral-200 rounded-lg">
              <table className="min-w-full divide-y divide-neutral-200">
                <thead className="bg-neutral-50 sticky top-0">
                  <tr>
                    <th className="px-4 py-2 text-left text-xs font-medium text-neutral-600 uppercase">Invoice No</th>
                    <th className="px-4 py-2 text-left text-xs font-medium text-neutral-600 uppercase">Customer</th>
                    <th className="px-4 py-2 text-left text-xs font-medium text-neutral-600 uppercase">Date</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-neutral-600 uppercase">Total</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-neutral-600 uppercase">Status</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-neutral-600 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-200 bg-white">
                  {routeSales.map(s => (
                    <tr key={s.id} className="hover:bg-neutral-50">
                      <td className="px-4 py-2 font-medium">{s.invoiceNumber || s.invoiceNo || s.id}</td>
                      <td className="px-4 py-2 text-sm">{s.customerName || '—'}</td>
                      <td className="px-4 py-2 text-sm">{s.invoiceDate ? new Date(s.invoiceDate).toLocaleDateString() : '—'}</td>
                      <td className="px-4 py-2 text-sm text-right">{formatCurrency(s.grandTotal ?? s.total ?? 0)}</td>
                      <td className="px-4 py-2 text-right"><span className={`px-2 py-0.5 rounded text-xs font-medium ${(s.status || '').toLowerCase() === 'paid' ? 'bg-green-100 text-green-700' : (s.status || '').toLowerCase() === 'partial' ? 'bg-amber-100 text-amber-700' : 'bg-red-100 text-red-700'}`}>{s.status || 'Pending'}</span></td>
                      <td className="px-4 py-2 text-right"><button type="button" onClick={() => navigate(`/sales-ledger?invoiceId=${s.id}`)} className="text-primary-600 hover:underline text-sm">View</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'expenses' && (
        <>
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
        <h2 className="text-lg font-medium text-neutral-800">Route expenses</h2>
        <div className="flex items-center gap-2">
          <input
            type="date"
            value={collectionSheetDate}
            onChange={(e) => setCollectionSheetDate(e.target.value)}
            className="border border-neutral-300 rounded px-2 py-1 text-sm print:hidden"
          />
          <button
            type="button"
            onClick={openCollectionSheet}
            disabled={loadingSheet}
            className="inline-flex items-center gap-1 px-3 py-1.5 bg-white border border-neutral-300 text-neutral-700 rounded-lg hover:bg-neutral-50 text-sm print:hidden"
          >
            <Printer className="h-4 w-4" />
            {loadingSheet ? 'Loading…' : 'Print Collection Sheet'}
          </button>
          {canManage && (
            <button
              type="button"
              onClick={openAddExpenseModal}
              className="inline-flex items-center gap-1 px-3 py-1.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm"
            >
              <Plus className="h-4 w-4" />
              Add expense
            </button>
          )}
        </div>
      </div>
      {expenses.length === 0 ? (
        <p className="text-neutral-500 text-sm">No expenses in this date range.</p>
      ) : (
        <ul className="space-y-2">
          {expenses.map((e) => (
            <li key={e.id} className="flex items-center justify-between p-3 bg-white rounded-lg border border-neutral-200">
              <div>
                <span className="font-medium">{e.category}</span>
                <span className="text-neutral-600 ml-2">{formatCurrency(e.amount)}</span>
                {e.description && <p className="text-sm text-neutral-500">{e.description}</p>}
                <p className="text-xs text-neutral-400">{e.expenseDate?.split('T')[0]}</p>
              </div>
              {canManage && (
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => openEditExpenseModal(e)}
                    className="p-1.5 text-primary-600 hover:bg-primary-50 rounded"
                    aria-label="Edit expense"
                  >
                    <Edit className="h-4 w-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDeleteExpense(e.id)}
                    className="p-1.5 text-red-600 hover:bg-red-50 rounded"
                    aria-label="Delete expense"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
        </>
      )}

      {activeTab === 'staff' && (
        <div>
          <div className="space-y-3 mb-4">
            {route.assignedStaffName && (
              <div className="p-3 bg-neutral-50 rounded-lg border border-neutral-200">
                <p className="text-sm text-neutral-500">Primary assigned staff</p>
                <p className="font-medium">{route.assignedStaffName}</p>
              </div>
            )}
            {(!route.staff || route.staff.length === 0) && !route.assignedStaffName ? (
              <p className="text-neutral-500 py-6">No staff assigned to this route. Assign from the Users page.</p>
            ) : (
              <div className="overflow-x-auto border border-neutral-200 rounded-lg">
                <table className="min-w-full divide-y divide-neutral-200">
                  <thead className="bg-neutral-50 sticky top-0">
                    <tr>
                      <th className="px-4 py-2 text-left text-xs font-medium text-neutral-600 uppercase">Name</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-200 bg-white">
                    {route.assignedStaffName && <tr><td className="px-4 py-2 font-medium">{route.assignedStaffName} (Primary)</td></tr>}
                    {(route.staff || []).filter(s => s.userName !== route.assignedStaffName).map(s => (
                      <tr key={s.userId}><td className="px-4 py-2 font-medium">{s.userName}</td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'performance' && (
        <div>
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <input type="date" value={dateDraftFrom} onChange={(e) => setDateDraftFrom(e.target.value)} className="border border-neutral-300 rounded px-2 py-1 text-sm" />
            <input type="date" value={dateDraftTo} onChange={(e) => setDateDraftTo(e.target.value)} className="border border-neutral-300 rounded px-2 py-1 text-sm" />
            <button type="button" onClick={applyDateRange} className="px-3 py-1.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium">Apply</button>
          </div>
          {summary && (
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Total Sales</p>
                <p className="text-xl font-semibold text-neutral-900">{formatCurrency(summary.totalSales)}</p>
              </div>
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Total Expenses</p>
                <p className="text-xl font-semibold text-neutral-900">{formatCurrency(summary.totalExpenses)}</p>
              </div>
              <div className="bg-white rounded-lg border border-neutral-200 p-4">
                <p className="text-sm text-neutral-500">Net Profit</p>
                <p className={`text-xl font-semibold ${summary.profit >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>{formatCurrency(summary.profit)}</p>
              </div>
            </div>
          )}
          <p className="mt-4 text-sm text-neutral-500 flex items-center gap-1"><BarChart3 className="h-4 w-4" />Route performance for selected date range.</p>
        </div>
      )}

      {showExpenseModal && (
        <Modal
          isOpen={true}
          title={selectedExpenseForEdit ? 'Edit route expense' : 'Add route expense'}
          onClose={() => !saving && (setShowExpenseModal(false), resetExpenseForm())}
        >
          <form onSubmit={selectedExpenseForEdit ? handleUpdateExpense : handleAddExpense} className="space-y-4">
            <div className="space-y-1">
              <label className="block text-sm font-semibold text-neutral-700">Category</label>
              <select
                value={expenseCategory}
                onChange={(e) => setExpenseCategory(e.target.value)}
                className="block w-full px-3 py-2.5 bg-white border border-neutral-200 rounded-xl shadow-sm text-neutral-900 sm:text-sm"
              >
                {EXPENSE_CATEGORIES.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
            <Input
              label="Amount"
              type="number"
              step="0.01"
              min="0"
              value={expenseAmount}
              onChange={(e) => setExpenseAmount(e.target.value)}
              required
              placeholder="0.00"
            />
            <Input
              label="Date"
              type="date"
              value={expenseDate}
              onChange={(e) => setExpenseDate(e.target.value)}
              required
            />
            <Input
              label="Description (optional)"
              value={expenseDescription}
              onChange={(e) => setExpenseDescription(e.target.value)}
              placeholder="Notes"
            />
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => { setShowExpenseModal(false); resetExpenseForm() }} className="px-4 py-2 border border-neutral-300 rounded-lg">
                Cancel
              </button>
              <button type="submit" disabled={saving} className="px-4 py-2 bg-primary-600 text-white rounded-lg disabled:opacity-50">
                {saving ? 'Saving...' : selectedExpenseForEdit ? 'Update' : 'Add'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {collectionSheet && (
        <Modal isOpen={true} title="Daily Collection Sheet" onClose={closeCollectionSheet}>
          <div id="collection-sheet-print" className="space-y-4">
            <div className="text-sm text-neutral-600">
              <p><strong>Route:</strong> {collectionSheet.routeName}</p>
              <p><strong>Branch:</strong> {collectionSheet.branchName}</p>
              <p><strong>Date:</strong> {collectionSheet.date}</p>
              {collectionSheet.staffName && <p><strong>Staff:</strong> {collectionSheet.staffName}</p>}
            </div>
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-neutral-300">
                  <th className="text-left py-2 px-2">#</th>
                  <th className="text-left py-2 px-2">Customer</th>
                  <th className="text-left py-2 px-2">Phone</th>
                  <th className="text-right py-2 px-2">Outstanding</th>
                  <th className="text-right py-2 px-2">Today&apos;s Invoice</th>
                  <th className="text-center py-2 px-2 w-16">✓</th>
                </tr>
              </thead>
              <tbody>
                {collectionSheet.customers?.map((c, i) => (
                  <tr key={c.customerId} className="border-b border-neutral-200">
                    <td className="py-2 px-2">{i + 1}</td>
                    <td className="py-2 px-2 font-medium">{c.customerName}</td>
                    <td className="py-2 px-2">{c.phone || '—'}</td>
                    <td className="py-2 px-2 text-right">{formatCurrency(c.outstandingBalance)}</td>
                    <td className="py-2 px-2 text-right">{c.todayInvoiceAmount != null ? formatCurrency(c.todayInvoiceAmount) : '—'}</td>
                    <td className="py-2 px-2 text-center">□</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <p className="text-right font-semibold">Total Outstanding: {formatCurrency(collectionSheet.totalOutstanding)}</p>
            <div className="flex justify-between pt-4 print:hidden">
              <button type="button" onClick={closeCollectionSheet} className="px-4 py-2 border border-neutral-300 rounded-lg">
                Close
              </button>
              <button type="button" onClick={printCollectionSheet} className="inline-flex items-center gap-1 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700">
                <Printer className="h-4 w-4" />
                Print
              </button>
            </div>
          </div>
        </Modal>
      )}

      <ConfirmDangerModal
        isOpen={dangerModal.isOpen}
        title={dangerModal.title}
        message={dangerModal.message}
        confirmLabel={dangerModal.confirmLabel}
        onConfirm={dangerModal.onConfirm}
        onClose={() => setDangerModal(prev => ({ ...prev, isOpen: false }))}
      />
    </div>
  )
}

export default RouteDetailPage

