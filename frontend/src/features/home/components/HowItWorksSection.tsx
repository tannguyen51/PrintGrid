import { Box, Container, Grid, Typography, Stack, Avatar } from '@mui/material';
import { CloudUpload, Settings, LocalShipping, Timeline } from '@mui/icons-material';

const STEPS = [
  { icon: <CloudUpload />, title: '1. Tải file 3D', desc: 'Hỗ trợ STL, OBJ, 3MF lên đến 50MB' },
  { icon: <Settings />, title: '2. Chọn thông số', desc: 'Chọn vật liệu, màu sắc và độ chính xác' },
  { icon: <LocalShipping />, title: '3. Nhận báo giá', desc: 'Biết trước giá và ngày giao cam kết' },
  { icon: <Timeline />, title: '4. Theo dõi realtime', desc: 'Theo dõi tiến độ từ lúc in đến khi giao hàng' },
];

export function HowItWorksSection() {
  return (
    <Box id="how-it-works" sx={{ py: 8 }}>
      <Container maxWidth="lg">
        <Typography variant="h4" textAlign="center" fontWeight={700} mb={6}>Cách hoạt động</Typography>
        <Grid container spacing={4}>
          {STEPS.map((step, idx) => (
            <Grid size={{ xs: 12, sm: 6, md: 3 }} key={idx}>
              <Stack alignItems="center" textAlign="center" spacing={2}>
                <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>{step.icon}</Avatar>
                <Typography variant="h6" fontWeight={600}>{step.title}</Typography>
                <Typography variant="body2" color="text.secondary">{step.desc}</Typography>
              </Stack>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  );
}
