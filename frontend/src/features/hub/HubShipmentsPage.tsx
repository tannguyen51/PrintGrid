import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { LocalShippingRounded, InventoryRounded, LogoutRounded, CheckRounded } from '@mui/icons-material'
import { useShipmentQueue, useShipOrder, useDeliverOrder, type ShipmentOrder } from './useShipments'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'

const fmtDate = (d: string | null) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')

const statusLabel: Record<ShipmentOrder['status'], string> = {
  QualityCheck: 'Sẵn sàng giao',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
}

const statusColor: Record<ShipmentOrder['status'], 'warning' | 'info' | 'success'> = {
  QualityCheck: 'warning',
  Shipping: 'info',
  Delivered: 'success',
}

/**
 * Hub shipment console (FR-HUB tail of the core flow):
 * - "Ghi vận đơn" ships a quality-checked order (tracking number required, BR).
 * - "Xác nhận đã giao" records the physical handover; the customer confirms receipt separately.
 */
export default function HubShipmentsPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const queue = useShipmentQueue()
  const ship = useShipOrder()
  const deliver = useDeliverOrder()

  const [shipTarget, setShipTarget] = useState<ShipmentOrder | null>(null)
  const [tracking, setTracking] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const orders = queue.data ?? []

  function openShipDialog(order: ShipmentOrder) {
    setShipTarget(order)
    setTracking(order.trackingNumber ?? '')
    setErrorMessage(null)
  }

  async function handleShip() {
    if (!shipTarget) return
    const value = tracking.trim()
    if (!value) {
      setErrorMessage('Bắt buộc nhập mã vận đơn trước khi ghi.')
      return
    }
    try {
      await ship.mutateAsync({ id: shipTarget.id, trackingNumber: value })
      setShipTarget(null)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể ghi vận đơn.')
    }
  }

  async function handleDeliver(order: ShipmentOrder) {
    try {
      await deliver.mutateAsync(order.id)
    } catch (err: any) {
      // Surface on the row-less alert; the queue refetches on success only.
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể xác nhận đã giao.')
    }
  }

  const busy = ship.isPending || deliver.isPending

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Giao hàng — Hub
              </Typography>
              <Typography color="text.secondary">Ghi vận đơn và xác nhận giao hàng cho đơn đã qua QC (FR-HUB)</Typography>
            </Box>
          </Stack>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/', { replace: true }) }}>
            Đăng xuất
          </Button>
        </Stack>

        {errorMessage && (
          <Alert severity="error" onClose={() => setErrorMessage(null)}>
            {errorMessage}
          </Alert>
        )}

        {queue.isError ? <Alert severity="error">Không tải được danh sách cần giao.</Alert> : null}
        {queue.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!queue.isLoading && orders.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <InventoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có đơn nào chờ ghi vận đơn hoặc xác nhận giao.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {orders.map((o) => (
              <Box key={o.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{o.orderNumber}</Typography>
                      <Chip label={statusLabel[o.status]} size="small" color={statusColor[o.status]} />
                      {o.status === 'Delivered' && (
                        <Chip label={o.receiptConfirmed ? 'Khách đã xác nhận' : 'Chờ khách xác nhận'} size="small" variant="outlined" color={o.receiptConfirmed ? 'success' : 'default'} />
                      )}
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Hạn giao: {fmtDate(o.promisedDeliveryDate)}
                      {o.trackingNumber ? ` · Mã vận đơn: ${o.trackingNumber}` : ' · Chưa có vận đơn'}
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={1.5} justifyContent={{ xs: 'flex-start', md: 'flex-end' }}>
                    {o.status === 'QualityCheck' && (
                      <Button variant="contained" color="primary" startIcon={<LocalShippingRounded />} disabled={busy} onClick={() => openShipDialog(o)}>
                        Ghi vận đơn
                      </Button>
                    )}
                    {o.status === 'Shipping' && (
                      <Button
                        variant="contained"
                        color="success"
                        startIcon={deliver.isPending ? <CircularProgress size={16} color="inherit" /> : <CheckRounded />}
                        disabled={busy}
                        onClick={() => handleDeliver(o)}
                      >
                        Xác nhận đã giao
                      </Button>
                    )}
                  </Stack>
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>

      <Dialog
        open={shipTarget !== null}
        onClose={() => { if (!ship.isPending) setShipTarget(null) }}
        PaperProps={{ sx: { bgcolor: '#121212', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.15)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 800 }}>Ghi vận đơn cho đơn {shipTarget?.orderNumber}</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Mã vận đơn là bắt buộc (BR). Đơn sẽ chuyển sang trạng thái &quot;Đang giao&quot;.
          </Typography>
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage(null)}>
              {errorMessage}
            </Alert>
          )}
          <TextField
            label="Mã vận đơn"
            value={tracking}
            onChange={(e) => setTracking(e.target.value)}
            fullWidth
            autoFocus
            required
            placeholder="VD: VNPost-123456789"
            inputProps={{ maxLength: 128 }}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, pt: 1, borderTop: '1px solid rgba(255,255,255,0.08)' }}>
          <Button onClick={() => setShipTarget(null)} color="inherit" disabled={ship.isPending} sx={{ color: 'text.secondary' }}>
            Hủy
          </Button>
          <Button
            variant="contained"
            color="primary"
            startIcon={ship.isPending ? <CircularProgress size={16} color="inherit" /> : <LocalShippingRounded />}
            disabled={ship.isPending || !tracking.trim()}
            onClick={handleShip}
          >
            {ship.isPending ? 'Đang lưu…' : 'Xác nhận ghi vận đơn'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
