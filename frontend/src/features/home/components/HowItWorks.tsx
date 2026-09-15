import { Box, Container, Stack, Typography } from '@mui/material'

const steps = [
  {
    step: '01',
    title: 'Tải model',
    desc: 'Đăng tải file STL/OBJ/3MF. Hệ thống xác thực lưới, tính kích thước và kiểm tra tính khả thi với toàn mạng lưới trong thời gian thực.',
  },
  {
    step: '02',
    title: 'Nhận báo giá chính xác',
    desc: 'Cấu hình vật liệu & chất lượng, sau đó nhận giá chi tiết cùng ngày giao được tính từ lịch trình thật — không phải ước đoán.',
  },
  {
    step: '03',
    title: 'Theo dõi đến tay',
    desc: 'Xác nhận đơn hàng chỉ với một nút bấm. Mạng lưới phân công tự động cho lab phù hợp nhất và bạn theo dõi tiến độ từng bước.',
  },
]

export function HowItWorks() {
  return (
    <Box component="section" id="cach-hoat-dong" sx={{ py: { xs: 7, md: 10 }, bgcolor: 'background.paper', scrollMarginTop: 90 }}>
      <Container maxWidth="lg">
        <Stack spacing={2} alignItems="center" textAlign="center" sx={{ mb: { xs: 5, md: 7 } }}>
          <Typography
            component="span"
            sx={{ fontSize: '0.78rem', fontWeight: 700, letterSpacing: '0.16em', textTransform: 'uppercase', color: '#A78BFA' }}
          >
            Cách hoạt động
          </Typography>
          <Typography variant="h2" sx={{ fontSize: { xs: '1.7rem', md: '2.4rem' }, fontWeight: 800, color: 'common.white' }}>
            Từ ý tưởng đến thành phẩm trong 3 bước
          </Typography>
        </Stack>

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={3} alignItems="stretch">
          {steps.map((s, i) => (
            <Box key={s.step} sx={{ flex: 1, position: 'relative', p: { xs: 3, md: 4 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography sx={{ fontSize: '2.4rem', fontWeight: 800, color: 'rgba(139,92,246,0.35)', lineHeight: 1 }}>
                {s.step}
              </Typography>
              <Typography sx={{ mt: 2, fontSize: '1.1rem', fontWeight: 700, color: 'common.white' }}>{s.title}</Typography>
              <Typography sx={{ mt: 1, fontSize: '0.9rem', color: 'text.secondary', lineHeight: 1.7 }}>{s.desc}</Typography>
              {/* connector */}
              {i < steps.length - 1 && (
                <Box
                  sx={{
                    display: { xs: 'none', md: 'block' },
                    position: 'absolute',
                    right: '-1.75rem',
                    top: '50%',
                    width: '1.25rem',
                    height: 2,
                    bgcolor: 'rgba(139,92,246,0.4)',
                  }}
                />
              )}
            </Box>
          ))}
        </Stack>
      </Container>
    </Box>
  )
}