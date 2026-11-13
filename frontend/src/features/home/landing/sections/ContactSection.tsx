import { Box } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'
import { PillButton, GhostButton } from '../../../../shared/theme/landing'

export function ContactSection() {
  const navigate = useNavigate()

  return (
    <SectionShell
      id="contact"
      label="Liên hệ"
      heading="Cần tư vấn? Liên hệ ngay"
      body="Dù bạn cần báo giá nhanh, hỗ trợ kỹ thuật hay chỉ muốn tìm hiểu thêm, đội ngũ PrintGrid sẵn sàng giúp bạn."
      centered
    >
      <Box
        sx={{
          p: { xs: 3, md: 5 },
          maxWidth: 720,
          width: '100%',
          mx: 'auto',
          borderRadius: '24px',
          background: gradients.lavender,
          border: `1px solid ${palette.border.hairline}`,
          boxShadow: shadows.card,
          textAlign: 'center',
        }}
      >
        <Box
          sx={{
            fontSize: { xs: 22, md: 28 },
            fontWeight: 800,
            color: 'text.primary',
            mb: 2,
          }}
        >
          Bắt đầu dự án in 3D của bạn hôm nay
        </Box>
        <Box sx={{ fontSize: 15, lineHeight: 1.7, color: 'text.secondary', mb: 3 }}>
          Upload file để nhận báo giá tự động trong vòng vài giây, hoặc gửi yêu cầu tư vấn để đội ngũ kỹ thuật hỗ trợ chi tiết.
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2, flexWrap: 'wrap' }}>
          <PillButton onClick={() => navigate('/models')}>
            Tải model & báo giá
          </PillButton>
          <GhostButton onClick={() => navigate('/design-request')}>
            Gửi yêu cầu tư vấn
          </GhostButton>
        </Box>
      </Box>
    </SectionShell>
  )
}
