import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Drawer,
  Stack,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { CloseRounded, VisibilityRounded, LibraryBooksRounded } from '@mui/icons-material'
import { useOrders } from './useOrders'
import type { Order, OrderStatus } from '../../shared/types/order'
import { useAuth } from '../../app/AuthContext'

const statusColor: Record<OrderStatus, 'warning' | 'info' | 'success' | 'error'> = {
  PaymentPending: 'warning',
  Confirmed: 'info',
  InProduction: 'info',
  QualityCheck: 'warning',
  Shipping: 'info',
  Delivered: 'success',
  Cancelled: 'error',
}

const statusLabel: Record<OrderStatus, string> = {
  PaymentPending: 'Chờ thanh toán',
  Confirmed: 'Đã xác nhận',
  InProduction: 'Đang sản xuất',
  QualityCheck: 'Kiểm tra chất lượng',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
}

const fmt = (n: number) => n.toLocaleString('vi-VN')
const fmtDate = (d: string) => new Date(d + 'T00:00:00').toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
const fmtDateTime = (d: string) => new Date(d).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })

export default function OrdersPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const { data, isLoading, isError } = useOrders()
  const [selected, setSelected] = useState<Order | null>(null)

  const rows = useMemo(() => data ?? [], [data])

  const columns: GridColDef<Order>[] = [
    { field: 'orderNumber', headerName: 'Mã đơn', width: 200 },
    {
      field: 'status',
      headerName: 'Trạng thái',
      width: 170,
      renderCell: (params) => (
        <Chip size="small" label={statusLabel[params.row.status] ?? params.row.status} color={statusColor[params.row.status] ?? 'info'} />
      ),
    },
    {
      field: 'totalAmount',
      headerName: 'Tổng tiền',
      width: 150,
      valueFormatter: (v: number) => `${fmt(v ?? 0)} đ`,
    },
    {
      field: 'promisedDeliveryDate',
      headerName: 'Ngày giao hẹn',
      width: 140,
      valueFormatter: (v: string) => (v ? fmtDate(v) : '—'),
    },
    {
      field: 'createdAt',
      headerName: 'Ngày tạo',
      width: 170,
      valueFormatter: (v: string) => fmtDateTime(v),
    },
    {
      field: 'actions',
      headerName: '',
      width: 70,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" onClick={() => setSelected(params.row)} startIcon={<VisibilityRounded fontSize="small" />} sx={{ color: 'text.primary' }}>
          Xem
        </Button>
      ),
    },
  ]

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Đơn hàng của tôi
            </Typography>
            <Typography color="text.secondary">Theo dõi trạng thái các đơn đã đặt</Typography>
          </Box>
          <Stack direction="row" spacing={1.5}>
            <Button variant="outlined" color="inherit" startIcon={<LibraryBooksRounded />} onClick={() => navigate('/models')} sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}>
              Thư viện model
            </Button>
            <Button variant="outlined" color="error" onClick={() => { logout(); navigate('/login', { replace: true }) }}>
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {isError ? (
          <Alert severity="error">Không tải được danh sách đơn hàng.</Alert>
        ) : (
          <Box sx={{ border: '1px solid rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper' }}>
            <DataGrid
              rows={rows}
              columns={columns}
              loading={isLoading}
              autoHeight
              disableRowSelectionOnClick
              initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
              pageSizeOptions={[10, 25, 50]}
              slots={{ loadingOverlay: () => (
                <Box sx={{ display: 'grid', placeItems: 'center', height: '100%' }}><CircularProgress size={40} /></Box>
              ) }}
              sx={{
                '&.MuiDataGrid-root': { border: 'none' },
                '& .MuiDataGrid-columnHeaders': { bgcolor: 'rgba(255,255,255,0.03)' },
              }}
            />
          </Box>
        )}
      </Stack>

      {/* ── Order detail drawer ── */}
      <Drawer
        anchor="right"
        open={selected !== null}
        onClose={() => setSelected(null)}
        PaperProps={{ sx: { width: { xs: 320, sm: 420 }, bgcolor: '#0A0A0A', backgroundImage: 'none' } }}
      >
        {selected && (
          <Stack sx={{ height: '100%' }}>
            <Box sx={{ p: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <Box>
                <Typography sx={{ fontSize: '1.25rem', fontWeight: 800, color: 'text.primary' }}>{selected.orderNumber}</Typography>
                <Chip size="small" label={statusLabel[selected.status] ?? selected.status} color={statusColor[selected.status] ?? 'info'} sx={{ mt: 0.75 }} />
              </Box>
              <Button onClick={() => setSelected(null)} size="small" sx={{ color: 'text.secondary', minWidth: 0 }} aria-label="Đóng"><CloseRounded /></Button>
            </Box>
            <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
            <Box sx={{ p: 3, flex: 1, overflowY: 'auto' }}>
              <Stack spacing={2.5}>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="caption" color="text.disabled">TỔNG TIỀN</Typography>
                  <Typography sx={{ fontWeight: 800, color: 'text.primary' }}>{fmt(selected.totalAmount)} {selected.currency}</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="caption" color="text.disabled">NGÀY GIAO HẸN</Typography>
                  <Typography color="text.primary">{fmtDate(selected.promisedDeliveryDate)}</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between">
                  <Typography variant="caption" color="text.disabled">NGÀY TẠO</Typography>
                  <Typography color="text.primary">{fmtDateTime(selected.createdAt)}</Typography>
                </Stack>

                <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
                <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.primary' }}>Chi tiết ({selected.items.length})</Typography>
                {selected.items.map((it) => (
                  <Box key={it.id} sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.08)', p: 1.5 }}>
                    <Typography sx={{ fontSize: '0.9rem', fontWeight: 600, color: 'text.primary' }}>
                      {it.materialCode} · {it.colorCode} · ×{it.quantity}
                    </Typography>
                    <Typography variant="caption" color="text.disabled">
                      Lớp {it.layerHeightMm}mm · Infill {it.infillPercent}% · {fmt(it.unitPrice)} đ/cái
                    </Typography>
                  </Box>
                ))}

                <Button
                  variant="contained"
                  color="primary"
                  fullWidth
                  onClick={() => { setSelected(null); navigate('/models') }}
                  startIcon={<LibraryBooksRounded />}
                >
                  Đặt in model mới
                </Button>
              </Stack>
            </Box>
          </Stack>
        )}
      </Drawer>
    </Box>
  )
}