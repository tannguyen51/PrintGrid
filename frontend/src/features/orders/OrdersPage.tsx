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
import { OrderTimelineView } from './OrderTimelineView'
import type { Order, OrderStatus } from '../../shared/types/order'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { ReprintRequestPanel } from './ReprintRequestPanel'
import { OrderPaymentPanel } from './OrderPaymentPanel'
import { OrderProductionProgress } from './OrderProductionProgress'
import { CodConfirmationPanel } from './CodConfirmationPanel'

const statusColor: Record<OrderStatus, 'warning' | 'info' | 'success' | 'error'> = {
  AwaitingApproval: 'warning',
  PaymentPending: 'warning',
  Confirmed: 'info',
  InProduction: 'info',
  QualityCheck: 'warning',
  Shipping: 'info',
  Delivered: 'success',
  Cancelled: 'error',
}

const statusLabel: Record<OrderStatus, string> = {
  AwaitingApproval: 'Đang chờ duyệt',
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
  const { data, isLoading, isError } = useOrders()
  const [selectedSnapshot, setSelected] = useState<Order | null>(null)

  const rows = useMemo(() => data ?? [], [data])
  const selected = rows.find(order => order.id === selectedSnapshot?.id) ?? selectedSnapshot

  const columns: GridColDef<Order>[] = [
    { field: 'orderNumber', headerName: 'Mã đơn', width: 200 },
    {
      field: 'status',
      headerName: 'Trạng thái',
      width: 230,
      renderCell: (params) => (
        <Stack spacing={0.25} sx={{ py: 0.75, height: '100%', justifyContent: 'center' }}>
          <Chip
            size="small"
            label={statusLabel[params.row.status] ?? params.row.status}
            color={statusColor[params.row.status] ?? 'info'}
            sx={{ alignSelf: 'flex-start' }}
          />
          {params.row.status === 'PaymentPending' && (
            <Typography variant="caption" sx={{ color: 'warning.main', lineHeight: 1.2 }}>
              Cần thanh toán tiền cọc để xác nhận đơn
            </Typography>
          )}
          {typeof params.row.depositAmount === 'number' && params.row.depositAmount > 0 && (
            <Typography variant="caption" color="text.disabled" sx={{ lineHeight: 1.2 }}>
              Cọc: {fmt(params.row.depositAmount)} đ
            </Typography>
          )}
          {typeof params.row.refundedAmount === 'number' && params.row.refundedAmount > 0 && (
            <Chip
              size="small"
              variant="outlined"
              color="error"
              label={`Đã hoàn ${fmt(params.row.refundedAmount)} đ`}
              sx={{ alignSelf: 'flex-start' }}
            />
          )}
        </Stack>
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
    { field: 'requestedDeliveryDate', headerName: 'Ngày mong muốn', width: 150, valueFormatter: (v: string) => v ? fmtDate(v) : '—' },
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
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1360, mx: 'auto' }} spacing={3}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Đơn hàng của tôi
              </Typography>
              <Typography color="text.secondary">Theo dõi trạng thái các đơn đã đặt</Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.5}>
            <Button variant="outlined" color="inherit" startIcon={<LibraryBooksRounded />} onClick={() => navigate('/models')} sx={{ color: 'text.primary', borderColor: 'divider' }}>
              Thư viện model
            </Button>
          </Stack>
        </Stack>

        {isError ? (
          <Alert severity="error">Không tải được danh sách đơn hàng.</Alert>
        ) : (
          <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper', boxShadow: '0 10px 30px rgba(35, 29, 85, 0.06)' }}>
            <DataGrid
              rows={rows}
              columns={columns}
              loading={isLoading}
              autoHeight
              rowHeight={72}
              columnHeaderHeight={56}
              disableRowSelectionOnClick
              initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
              pageSizeOptions={[10, 25, 50]}
              slots={{ loadingOverlay: () => (
                <Box sx={{ display: 'grid', placeItems: 'center', height: '100%' }}><CircularProgress size={40} /></Box>
              ) }}
              sx={{
                '&.MuiDataGrid-root': { border: 'none', color: 'text.primary', fontSize: '0.925rem' },
                '& .MuiDataGrid-columnHeaders': {
                  bgcolor: '#F5F3FF',
                  borderBottom: '1px solid',
                  borderColor: 'divider',
                },
                '& .MuiDataGrid-columnHeaderTitle': {
                  color: '#3E3564',
                  fontSize: '0.78rem',
                  fontWeight: 800,
                  letterSpacing: '0.055em',
                },
                '& .MuiDataGrid-cell': {
                  borderColor: '#ECEAF4',
                  display: 'flex',
                  alignItems: 'center',
                  fontWeight: 500,
                },
                '& .MuiDataGrid-row:hover': { bgcolor: '#FAF9FF' },
                '& .MuiDataGrid-footerContainer': {
                  minHeight: 58,
                  borderColor: 'divider',
                  bgcolor: '#FCFBFF',
                  color: 'text.primary',
                },
                '& .MuiTablePagination-root': { color: 'text.primary' },
                '& .MuiTablePagination-selectLabel, & .MuiTablePagination-displayedRows': { fontSize: '0.85rem', fontWeight: 600 },
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
        PaperProps={{ sx: { width: { xs: 320, sm: 420 }, bgcolor: 'background.paper', backgroundImage: 'none' } }}
      >
        {selected && (
          <Stack sx={{ height: '100%' }}>
            <Box sx={{ p: 3, display: 'flex', justifyContent: 'flex-end' }}>
              <Button onClick={() => setSelected(null)} size="small" sx={{ color: 'text.secondary', minWidth: 0 }} aria-label="Đóng"><CloseRounded /></Button>
            </Box>
            <Divider sx={{ borderColor: 'divider' }} />
            <Box sx={{ p: 3, flex: 1, overflowY: 'auto' }}>
              <Stack spacing={1} sx={{ mb: 3 }}>
                <Typography>Ngày mong muốn: {selected.requestedDeliveryDate ? fmtDate(selected.requestedDeliveryDate) : 'Chưa khai báo (đơn cũ)'}</Typography>
                <Typography>Ngày cam kết: {selected.promisedDeliveryDate ? fmtDate(selected.promisedDeliveryDate) : '—'}</Typography>
                <Chip label={selected.dispatchMode === 'Automatic' ? 'Xét tự động sau khi đủ cọc' : 'Ops điều phối'} sx={{ alignSelf: 'flex-start' }} />
              </Stack>
              {selected.status === 'Cancelled' && <Alert severity="info" sx={{ mb: 3 }}>Đơn hàng đã hủy. Đơn sẽ không được tiếp tục giao.</Alert>}
              {selected.status === 'PaymentPending' && (
                <>
                  <OrderPaymentPanel orderId={selected.id} />
                  <Divider sx={{ my: 3, borderColor: 'divider' }} />
                </>
              )}
              {selected.status === 'QualityCheck' && (
                <>
                  <CodConfirmationPanel order={selected} />
                  <Divider sx={{ my: 3, borderColor: 'divider' }} />
                </>
              )}
              <OrderTimelineView orderId={selected.id} />
              <OrderProductionProgress orderId={selected.id} />

              <Divider sx={{ my: 3, borderColor: 'divider' }} />
              <ReprintRequestPanel order={selected} />
              
              <Box sx={{ mt: 4 }}>
                <Button
                  variant="contained"
                  color="primary"
                  fullWidth
                  onClick={() => { setSelected(null); navigate('/models') }}
                  startIcon={<LibraryBooksRounded />}
                >
                  Đặt in model mới
                </Button>
              </Box>
            </Box>
          </Stack>
        )}
      </Drawer>
    </Box>
  )
}
