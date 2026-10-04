import { Box, Container, Grid, Typography, Skeleton } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../shared/api/apiClient';

export function NetworkStatsSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['network-stats'],
    queryFn: async () => {
      const res = await apiClient.get('/network/stats');
      return res.data;
    },
    retry: false
  });

  if (isError) return null;

  return (
    <Box sx={{ py: 6, bgcolor: 'rgba(255,255,255,0.02)', borderTop: '1px solid rgba(255,255,255,0.05)', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
      <Container maxWidth="lg">
        <Grid container spacing={4} textAlign="center">
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="h3" fontWeight={700} color="primary.main">
              {isLoading ? <Skeleton width={80} sx={{ mx: 'auto' }} /> : data?.activeLabs || 0}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">Xưởng in đối tác</Typography>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="h3" fontWeight={700} color="primary.main">
              {isLoading ? <Skeleton width={80} sx={{ mx: 'auto' }} /> : data?.totalMachines || 0}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">Máy in sẵn sàng</Typography>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="h3" fontWeight={700} color="primary.main">
              {isLoading ? <Skeleton width={80} sx={{ mx: 'auto' }} /> : `${((data?.averageOnTimeDeliveryRate || 1) * 100).toFixed(0)}%`}
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">Giao đúng cam kết</Typography>
          </Grid>
        </Grid>
      </Container>
    </Box>
  );
}
