import { Box, Grid } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'
import { PillButton } from '../../../../shared/theme/landing'
import AttachMoneyRounded from '@mui/icons-material/AttachMoneyRounded'
import AccessTimeRounded from '@mui/icons-material/AccessTimeRounded'
import ScaleRounded from '@mui/icons-material/ScaleRounded'

export function PricingSection() {
  const navigate = useNavigate()

  return (
    <SectionShell
      id="pricing"
      label="Bảng giá"
      heading="Giá minh bạch theo vật liệu & thời gian in"
      body="Báo giá dựa trên thể tích vật liệu thực tế và thời gian máy in. Không phí ẩn, bạn nhận giá chính xác ngay sau khi upload file."
    >
      <Grid container spacing={{ xs: 2, md: 3 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Box
            sx={{
              p: { xs: 2.5, md: 3.5 },
              height: '100%',
              borderRadius: '18px',
              background: gradients.metal,
              border: `1px solid ${palette.border.hairline}`,
              boxShadow: shadows.card,
            }}
          >
            <Box sx={{ color: palette.purple.deep, mb: 2 }}>
              <AttachMoneyRounded sx={{ fontSize: 32 }} />
            </Box>
            <Box sx={{ fontSize: 18, fontWeight: 800, color: 'text.primary', mb: 1 }}>
              Tính theo gram vật liệu
            </Box>
            <Box sx={{ fontSize: 14, lineHeight: 1.6, color: 'text.secondary' }}>
              Giá trực tiếp từ khối lượng nhựa in thực tế, không ước tính sai lệch.
            </Box>
          </Box>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Box
            sx={{
              p: { xs: 2.5, md: 3.5 },
              height: '100%',
              borderRadius: '18px',
              background: gradients.metal,
              border: `1px solid ${palette.border.hairline}`,
              boxShadow: shadows.card,
            }}
          >
            <Box sx={{ color: palette.gold.deep, mb: 2 }}>
              <AccessTimeRounded sx={{ fontSize: 32 }} />
            </Box>
            <Box sx={{ fontSize: 18, fontWeight: 800, color: 'text.primary', mb: 1 }}>
              Tính theo giờ máy in
            </Box>
            <Box sx={{ fontSize: 14, lineHeight: 1.6, color: 'text.secondary' }}>
              Máy in phức tạp hoặc yêu cầu layer mịn sẽ tính thêm theo thời gian sử dụng máy.
            </Box>
          </Box>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Box
            sx={{
              p: { xs: 2.5, md: 3.5 },
              height: '100%',
              borderRadius: '18px',
              background: gradients.metal,
              border: `1px solid ${palette.border.hairline}`,
              boxShadow: shadows.card,
            }}
          >
            <Box sx={{ color: palette.status.info, mb: 2 }}>
              <ScaleRounded sx={{ fontSize: 32 }} />
            </Box>
            <Box sx={{ fontSize: 18, fontWeight: 800, color: 'text.primary', mb: 1 }}>
              Chiết khấu số lượng
            </Box>
            <Box sx={{ fontSize: 14, lineHeight: 1.6, color: 'text.secondary' }}>
              Đơn hàng số lượng lớn hoặc khách hàng doanh nghiệp được chiết khấu hấp dẫn.
            </Box>
          </Box>
        </Grid>
      </Grid>

      <Box sx={{ textAlign: 'center', mt: 2 }}>
        <PillButton onClick={() => navigate('/pricing')}>
          Xem bảng giá chi tiết
        </PillButton>
      </Box>
    </SectionShell>
  )
}
