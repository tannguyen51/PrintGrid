import { Box, Button, Container, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../../app/AuthContext';

export function HeroSection() {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  const handleCtaClick = () => {
    if (isAuthenticated) {
      navigate('/order/new');
    } else {
      navigate('/login?returnUrl=/order/new');
    }
  };

  return (
    <Box sx={{ pt: 12, pb: 8, textAlign: 'center' }}>
      <Container maxWidth="md">
        <Typography variant="h2" sx={{ fontWeight: 800, mb: 2, color: 'text.primary' }}>
          In 3D theo mạng lưới xưởng, một giá, ngày giao cam kết
        </Typography>
        <Typography variant="h6" sx={{ color: 'text.secondary', mb: 4, fontWeight: 400 }}>
          Tải file 3D của bạn lên, nhận báo giá ngay lập tức và theo dõi tiến độ sản xuất theo thời gian thực.
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
          <Button variant="contained" size="large" onClick={handleCtaClick} sx={{ px: 4 }}>
            Tải model & nhận báo giá
          </Button>
          <Button variant="outlined" size="large" onClick={() => document.getElementById('how-it-works')?.scrollIntoView({ behavior: 'smooth' })}>
            Cách hoạt động
          </Button>
        </Box>
      </Container>
    </Box>
  );
}
