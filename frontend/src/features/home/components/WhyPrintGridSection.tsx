import { Box, Container, Grid, Typography, Paper } from '@mui/material';

const BENEFITS = [
  { title: 'Một giá cho mọi xưởng', desc: 'Cùng model và cùng cấu hình luôn ra cùng giá, bất kể xưởng nào sản xuất.' },
  { title: 'Ngày giao tính từ năng lực thật', desc: 'Ngày giao lấy từ lịch xếp máy thực tế, không dùng bảng thời gian cố định.' },
  { title: 'Kiểm tra chất lượng tại Hub', desc: 'Mỗi món được kiểm tra theo checklist trước khi giao cho khách.' },
  { title: 'Bảo hành 30 ngày', desc: 'Hỗ trợ gửi yêu cầu in lại miễn phí nếu sản phẩm có lỗi sản xuất.' },
];

export function WhyPrintGridSection() {
  return (
    <Box sx={{ py: 8 }}>
      <Container maxWidth="lg">
        <Typography variant="h4" textAlign="center" fontWeight={700} mb={6}>Tại sao chọn PrintGrid?</Typography>
        <Grid container spacing={4}>
          {BENEFITS.map((item, idx) => (
            <Grid size={{ xs: 12, sm: 6 }} key={idx}>
              <Paper sx={{ p: 4, height: '100%', bgcolor: 'background.paper', borderColor: 'rgba(255,255,255,0.08)' }} variant="outlined">
                <Typography variant="h6" fontWeight={600} mb={1}>{item.title}</Typography>
                <Typography variant="body2" color="text.secondary">{item.desc}</Typography>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  );
}
