  import { Alert, Box, Chip, CircularProgress, Stack, Typography } from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { useOrders } from './useOrders'
import type { Order, OrderStatus } from '../../shared/types/order'

const statusColor: Record<OrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  PaymentPending: 'warning',
  Confirmed: 'info',
  InProduction: 'info',
  QualityCheck: 'warning',
  Shipping: 'info',
  Delivered: 'success',
  Cancelled: 'error',
}

const columns: GridColDef<Order>[] = [
  { field: 'orderNumber', headerName: 'Mã đơn', width: 190 },
  {
    field: 'status',
    headerName: 'Trạng thái',
    width: 160,
    renderCell: (params) => <Chip size="small" label={params.value} color={statusColor[params.value as OrderStatus]} />,
  },
  {
    field: 'totalAmount',
    headerName: 'Tổng tiền',
    width: 150,
    valueFormatter: (value: number) => value?.toLocaleString('vi-VN'),
  },
  { field: 'promisedDeliveryDate', headerName: 'Ngày giao hẹn ', width: 160 },
  { field: 'createdAt', headerName: 'Ngày tạo', width: 200 },
]

export default function OrdersPage() {
  const { data, isLoading, isError } = useOrders()

  if (isLoading) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return <Alert severity="error">Không tải được danh sách đơn hàng</Alert>
  }

  return (
    <Stack spacing={2} sx={{ p: 3 }}>
      <Typography variant="h1">Đơn hàng của tôi</Typography>
      <DataGrid
        rows={data ?? []}
        columns={columns}
        disableRowSelectionOnClick
        initialState={{ pagination: { paginationModel: { pageSize: 25 } } }}
        pageSizeOptions={[25, 50, 100]}
        sx={{ backgroundColor: 'background.paper' }}
      />
    </Stack>
  )
}
