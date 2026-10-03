import { Box, Container, Typography, Button } from '@mui/material';
import { useNavigate } from 'react-router-dom';

export function DesignRequestBanner() {
  const navigate = useNavigate();
  return (
    <Box sx={{ py: 8, bgcolor: 'primary.main', color: 'primary.contrastText', textAlign: 'center' }}>
      <Container maxWidth="md">
        <Typography variant="h4" fontWeight={700} mb={2}>Chưa có file 3D?</Typography>
        <Typography variant="body1" mb={4} sx={{ fontSize: '1.1rem' }}>Gửi ý tưởng, bản vẽ hoặc ảnh chụp, đội ngũ của chúng tôi sẽ hỗ trợ thiết kế cho bạn.</Typography>
        <Button variant="contained" color="inherit" size="large" sx={{ color: 'primary.main', fontWeight: 600, px: 4 }} onClick={() => navigate('/design-request')}>
          Gửi yêu cầu thiết kế
        </Button>
      </Container>
    </Box>
  );
}
