import { Box, Container, Grid, Stack, Typography } from '@mui/material'
import { CloudUploadRounded, PriceCheckRounded, InsightsRounded, TrackChangesRounded } from '@mui/icons-material'

const features = [
  {
    icon: <CloudUploadRounded />,
    title: 'Tải & phân tích model',
    desc: 'Xem trước model 3D ngay trong trình duyệt với kiểm tra chất lượng lưới tự động — kích thước, thể tích và tính khả thi trên toàn mạng lưới.',
  },
  {
    icon: <PriceCheckRounded />,
    title: 'Báo giá tức thì',
    desc: 'Ngày giao được tính từ lịch trình thực của các lab, không phải bảng thời gian ước tính. Giá minh bạch theo từng hạng mục, không phụ thuộc lab nào làm.',
  },
  {
    icon: <InsightsRounded />,
    title: 'Cấu hình thông minh',
    desc: 'Chọn vật liệu, màu sắc, độ phân giải và mật độ infill với gợi ý chuyên môn — kèm dự báo tác động lên giá và độ bền.',
  },
  {
    icon: <TrackChangesRounded />,
    title: 'Theo dõi thời gian thực',
    desc: 'Từ xác nhận đơn hàng đến khi giao: cập nhật trạng thái liên tục, thông báo qua email và trong ứng dụng. Không cần biết lab nào in — chỉ cần biết hàng đến đúng hạn.',
  },
]

export function FeatureGrid() {
  return (
    <Box component="section" id="tinh-nang" sx={{ py: { xs: 7, md: 10 }, scrollMarginTop: 90 }}>
      <Container maxWidth="lg">
        <Stack spacing={2} alignItems="center" textAlign="center" sx={{ mb: { xs: 5, md: 7 } }}>
          <Typography
            component="span"
            sx={{ fontSize: '0.78rem', fontWeight: 700, letterSpacing: '0.16em', textTransform: 'uppercase', color: '#A78BFA' }}
          >
            Vì sao PrintGrid
          </Typography>
          <Typography variant="h2" sx={{ fontSize: { xs: '1.7rem', md: '2.4rem' }, fontWeight: 800, color: 'common.white' }}>
            Sức mạnh của một mạng lưới,
            <Box component="span" display="block" sx={{ color: 'text.secondary', fontWeight: 600, mt: 0.5, fontSize: { xs: '1.2rem', md: '1.6rem' } }}>
              trải nghiệm của một dịch vụ duy nhất
            </Box>
          </Typography>
        </Stack>

        <Grid container spacing={{ xs: 2.5, md: 3 }}>
          {features.map((f) => (
            <Grid key={f.title} size={{ xs: 12, sm: 6 }}>
              <Box
                sx={{
                  height: '100%',
                  p: { xs: 3, md: 4 },
                  borderRadius: 3,
                  border: '1px solid rgba(255,255,255,0.1)',
                  bgcolor: 'background.paper',
                  transition: 'border-color .25s, transform .25s',
                  '&:hover': { borderColor: 'rgba(139,92,246,0.45)', transform: 'translateY(-3px)' },
                }}
              >
                <Stack spacing={1.5}>
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: 2.5,
                      display: 'grid',
                      placeItems: 'center',
                      bgcolor: 'rgba(139,92,246,0.12)',
                      color: '#8B5CF6',
                      fontSize: '1.5rem',
                    }}
                  >
                    {f.icon}
                  </Box>
                  <Typography sx={{ fontSize: '1.15rem', fontWeight: 700, color: 'common.white' }}>{f.title}</Typography>
                  <Typography sx={{ fontSize: '0.92rem', color: 'text.secondary', lineHeight: 1.7 }}>{f.desc}</Typography>
                </Stack>
              </Box>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  )
}