import { useQuery } from '@tanstack/react-query'
import { ArrowForwardRounded, Inventory2Outlined, LibraryBooksRounded, LocalShippingOutlined, UploadFileRounded } from '@mui/icons-material'
import { Box, Button, Chip, Container, LinearProgress, Paper, Skeleton, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { apiClient } from '../../shared/api/apiClient'
import type { Order } from '../../shared/types/order'
import { useOrders } from '../orders/useOrders'

const statusLabel: Record<string, string> = {
  AwaitingApproval: 'Đang chờ duyệt', PaymentPending: 'Chờ thanh toán', Confirmed: 'Đã xác nhận',
  InProduction: 'Đang sản xuất', QualityCheck: 'Kiểm tra chất lượng', Shipping: 'Đang giao',
  Delivered: 'Đã giao', Cancelled: 'Đã hủy',
}

export function CustomerHomeDashboard() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const orders = useOrders()
  const models = useQuery({
    queryKey: ['customer-home-models'],
    queryFn: () => apiClient.get<Array<{ id: string; name: string }>>('/models').then((response) => response.data),
    staleTime: 60_000,
  })

  const orderList = orders.data ?? []
  const activeOrders = orderList.filter((order) => !['Delivered', 'Cancelled'].includes(order.status))
  const recentOrders = orderList.slice(0, 3)
  const firstName = user?.email?.split('@')[0] || 'bạn'

  return (
    <Box sx={{ minHeight: 'calc(100vh - 70px)', pb: 8 }}>
      <Container maxWidth="lg" sx={{ pt: { xs: 4, md: 6 } }}>
        <Paper sx={{ position: 'relative', overflow: 'hidden', p: { xs: 3, md: 5 }, borderRadius: 4, color: '#fff', background: 'linear-gradient(120deg, #312579 0%, #6F55FF 60%, #9B78FF 100%)', boxShadow: '0 24px 60px rgba(73, 51, 184, 0.24)' }}>
          <Box sx={{ position: 'absolute', width: 360, height: 360, borderRadius: '50%', right: -100, top: -210, bgcolor: 'rgba(255,255,255,.12)' }} />
          <Box sx={{ position: 'absolute', width: 220, height: 220, borderRadius: '50%', right: 120, bottom: -170, bgcolor: 'rgba(255,179,71,.25)' }} />
          <Stack spacing={2.5} sx={{ position: 'relative', maxWidth: 700 }}>
            <Typography sx={{ fontSize: '0.78rem', fontWeight: 800, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'rgba(255,255,255,.72)' }}>Không gian khách hàng</Typography>
            <Box>
              <Typography component="h1" sx={{ fontSize: { xs: '2rem', md: '3rem' }, lineHeight: 1.14, fontWeight: 850, letterSpacing: '-0.04em' }}>Chào {firstName}, bạn muốn in gì hôm nay?</Typography>
              <Typography sx={{ mt: 1.25, fontSize: { xs: '0.95rem', md: '1.05rem' }, color: 'rgba(255,255,255,.78)', maxWidth: 580 }}>Tải thiết kế lên để nhận báo giá, chọn model trong thư viện hoặc tiếp tục theo dõi đơn đang sản xuất.</Typography>
            </Box>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <Button variant="contained" size="large" startIcon={<UploadFileRounded />} onClick={() => navigate('/order/new')} sx={{ bgcolor: '#fff', color: '#4933B8', fontWeight: 800, px: 3, '&:hover': { bgcolor: '#F7F4FF' } }}>Bắt đầu đơn in</Button>
              <Button variant="outlined" size="large" startIcon={<LibraryBooksRounded />} onClick={() => navigate('/models')} sx={{ color: '#fff', borderColor: 'rgba(255,255,255,.45)', fontWeight: 700, '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,.08)' } }}>Chọn từ thư viện</Button>
            </Stack>
          </Stack>
        </Paper>

        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 2, mt: 2.5 }}>
          {[
            { label: 'Đơn đang xử lý', value: activeOrders.length, icon: <Inventory2Outlined />, color: '#6F55FF' },
            { label: 'Model đã lưu', value: models.data?.length ?? 0, icon: <LibraryBooksRounded />, color: '#3D6FEA' },
            { label: 'Đơn đã hoàn tất', value: orderList.filter((order) => order.status === 'Delivered').length, icon: <LocalShippingOutlined />, color: '#1F9D6B' },
          ].map((item) => (
            <Paper key={item.label} variant="outlined" sx={{ p: 2.5, borderRadius: 3, borderColor: '#E8EBF2', boxShadow: '0 8px 24px rgba(37,38,58,.04)' }}>
              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <Box><Typography color="text.secondary" sx={{ fontSize: '0.84rem', fontWeight: 650 }}>{item.label}</Typography><Typography sx={{ mt: 0.25, fontSize: '1.85rem', fontWeight: 850, color: 'text.primary' }}>{orders.isLoading || models.isLoading ? <Skeleton width={38} /> : item.value}</Typography></Box>
                <Box sx={{ width: 44, height: 44, borderRadius: 2.5, display: 'grid', placeItems: 'center', color: item.color, bgcolor: `${item.color}12` }}>{item.icon}</Box>
              </Stack>
            </Paper>
          ))}
        </Box>

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2.5} sx={{ mt: 4 }} alignItems="stretch">
          <Paper variant="outlined" sx={{ p: { xs: 2.5, md: 3 }, borderRadius: 3, flex: 1.5, borderColor: '#E8EBF2' }}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2.5 }}>
              <Box><Typography variant="h6" fontWeight={800}>Đơn hàng gần đây</Typography><Typography variant="body2" color="text.secondary">Cập nhật mới nhất từ quy trình sản xuất</Typography></Box>
              <Button endIcon={<ArrowForwardRounded />} onClick={() => navigate('/orders')} sx={{ fontWeight: 700 }}>Xem tất cả</Button>
            </Stack>
            {orders.isLoading && <LinearProgress />}
            {!orders.isLoading && recentOrders.length === 0 && <Box sx={{ py: 5, textAlign: 'center' }}><Typography fontWeight={700}>Bạn chưa có đơn hàng nào</Typography><Typography variant="body2" color="text.secondary" sx={{ mt: .5 }}>Bắt đầu bằng cách tải lên model 3D đầu tiên.</Typography></Box>}
            <Stack divider={<Box sx={{ borderTop: '1px solid #EFF1F6' }} />}>
              {recentOrders.map((order: Order) => (
                <Stack key={order.id} direction="row" justifyContent="space-between" alignItems="center" spacing={2} sx={{ py: 1.75, cursor: 'pointer', '&:hover': { bgcolor: '#FAF9FF' }, px: 1, mx: -1, borderRadius: 2 }} onClick={() => navigate('/orders')}>
                  <Box sx={{ minWidth: 0 }}><Typography fontWeight={750} noWrap>{order.orderNumber}</Typography><Typography variant="caption" color="text.secondary">{new Date(order.createdAt).toLocaleDateString('vi-VN')}</Typography></Box>
                  <Chip size="small" label={statusLabel[order.status] ?? order.status} sx={{ fontWeight: 650, bgcolor: '#F0ECFF', color: '#4933B8' }} />
                </Stack>
              ))}
            </Stack>
          </Paper>
          <Paper variant="outlined" sx={{ p: { xs: 2.5, md: 3 }, borderRadius: 3, flex: 1, borderColor: '#E8EBF2', background: 'linear-gradient(145deg, #fff 0%, #F7F4FF 100%)' }}>
            <Typography variant="h6" fontWeight={800}>Quy trình thật đơn giản</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: .5, mb: 2.5 }}>Từ model 3D đến sản phẩm hoàn thiện.</Typography>
            <Stack spacing={2}>
              {['Tải model hoặc chọn từ thư viện', 'Cấu hình và nhận báo giá', 'Theo dõi sản xuất, nhận hàng'].map((label, index) => <Stack key={label} direction="row" spacing={1.5} alignItems="center"><Box sx={{ width: 30, height: 30, borderRadius: '50%', display: 'grid', placeItems: 'center', bgcolor: index === 0 ? '#6F55FF' : '#fff', color: index === 0 ? '#fff' : '#6F55FF', border: '1px solid #D8CDFF', fontWeight: 800, fontSize: '.82rem' }}>{index + 1}</Box><Typography sx={{ fontSize: '.9rem', fontWeight: 650 }}>{label}</Typography></Stack>)}
            </Stack>
          </Paper>
        </Stack>
      </Container>
    </Box>
  )
}
