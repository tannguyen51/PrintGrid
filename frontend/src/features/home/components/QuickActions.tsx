import { Box, Card, CardActionArea, Container, Grid, Typography, Skeleton } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../shared/api/apiClient';

export function QuickActions() {
  const navigate = useNavigate();

  const { data: orders, isLoading: loadingOrders, isError: errOrders } = useQuery({
    queryKey: ['recent-orders'],
    queryFn: async () => {
      const res = await apiClient.get('/orders');
      return res.data;
    },
    retry: false
  });

  const { data: models, isLoading: loadingModels, isError: errModels } = useQuery({
    queryKey: ['recent-models'],
    queryFn: async () => {
      const res = await apiClient.get('/models');
      return res.data;
    },
    retry: false
  });

  const { data: quotes, isError: errQuotes } = useQuery({
    queryKey: ['active-quotes'],
    queryFn: async () => {
      const res = await apiClient.get('/quotes');
      return res.data;
    },
    retry: false
  });

  const hasOrdersEndpoint = !errOrders;
  const hasModelsEndpoint = !errModels;
  const hasQuotesEndpoint = !errQuotes;

  return (
    <Box sx={{ py: 6 }}>
      <Container maxWidth="lg">
        <Typography variant="h5" sx={{ mb: 3, fontWeight: 700 }}>Hoạt động của bạn</Typography>
        <Grid container spacing={3}>
          {hasOrdersEndpoint && (
            <Grid size={{ xs: 12, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', bgcolor: 'background.paper' }}>
                <CardActionArea 
                  onClick={() => navigate(orders?.length > 0 ? `/orders` : '/order/new')} 
                  sx={{ height: '100%', p: 2 }}
                >
                  <Typography variant="subtitle1" fontWeight={600} mb={1}>Đơn gần đây</Typography>
                  {loadingOrders ? <Skeleton /> : orders?.length > 0 ? (
                    <Box>
                      {orders.slice(0, 2).map((o: any) => (
                        <Typography key={o.id} variant="body2" color="text.secondary">
                          Đơn hàng {o.id.substring(0,8)} - {o.status}
                        </Typography>
                      ))}
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary">Chưa có đơn nào. Đặt in đơn đầu tiên!</Typography>
                  )}
                </CardActionArea>
              </Card>
            </Grid>
          )}

          {hasModelsEndpoint && (
            <Grid size={{ xs: 12, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', bgcolor: 'background.paper' }}>
                <CardActionArea 
                  onClick={() => navigate(models?.length > 0 ? `/order/new?modelId=${models[0].id}` : '/models')} 
                  sx={{ height: '100%', p: 2 }}
                >
                  <Typography variant="subtitle1" fontWeight={600} mb={1}>Đặt lại từ thư viện</Typography>
                  {loadingModels ? <Skeleton /> : models?.length > 0 ? (
                    <Typography variant="body2" color="text.secondary">Model: {models[0].name}</Typography>
                  ) : (
                    <Typography variant="body2" color="text.secondary">Thư viện trống. Tải model lên ngay!</Typography>
                  )}
                </CardActionArea>
              </Card>
            </Grid>
          )}

          {hasQuotesEndpoint && quotes?.length > 0 && (
            <Grid size={{ xs: 12, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', bgcolor: 'background.paper' }}>
                <CardActionArea 
                  onClick={() => navigate('/order/new')} 
                  sx={{ height: '100%', p: 2 }}
                >
                  <Typography variant="subtitle1" fontWeight={600} mb={1}>Tiếp tục báo giá</Typography>
                  <Typography variant="body2" color="text.secondary">Báo giá của bạn còn hạn (dưới 48 giờ)</Typography>
                </CardActionArea>
              </Card>
            </Grid>
          )}
        </Grid>
      </Container>
    </Box>
  );
}
