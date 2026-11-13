import { Box, Grid } from '@mui/material'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'

const STATS = [
  { value: '±0.1mm', label: 'Độ chính xác kích thước' },
  { value: '24-48h', label: 'Thời gian hoàn thành' },
  { value: '5+', label: 'Loại vật liệu' },
  { value: 'STL/OBJ/3MF/GLB', label: 'Định dạng file hỗ trợ' },
]

export function StatsSection() {
  return (
    <SectionShell
      id="stats"
      label="Tại sao chọn PrintGrid"
      heading="In 3D chuyên nghiệp, nhanh chóng và minh bạch"
      body="Từ báo giá tự động đến giao hàng đúng hẹn, chúng tôi giúp bạn biến ý tưởng thành sản phẩm thực."
    >
      <Grid container spacing={{ xs: 2, md: 3 }}>
        {STATS.map((stat) => (
          <Grid key={stat.label} size={{ xs: 12, sm: 6, lg: 3 }}>
            <Box
              sx={{
                p: { xs: 2.5, md: 4 },
                borderRadius: '18px',
                background: gradients.metal,
                border: `1px solid ${palette.border.hairline}`,
                boxShadow: shadows.card,
                textAlign: 'center',
              }}
            >
              <Box
                sx={{
                  fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
                  fontSize: { xs: 28, md: 36 },
                  fontWeight: 800,
                  letterSpacing: '-0.02em',
                  background: gradients.primary,
                  WebkitBackgroundClip: 'text',
                  backgroundClip: 'text',
                  WebkitTextFillColor: 'transparent',
                  color: 'transparent',
                  mb: 1.5,
                }}
              >
                {stat.value}
              </Box>
              <Box sx={{ fontSize: 15, fontWeight: 600, color: 'text.secondary' }}>
                {stat.label}
              </Box>
            </Box>
          </Grid>
        ))}
      </Grid>
    </SectionShell>
  )
}
