import { Box, Grid } from '@mui/material'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'
import PrecisionManufacturingRounded from '@mui/icons-material/PrecisionManufacturingRounded'
import SpeedRounded from '@mui/icons-material/SpeedRounded'
import VerifiedRounded from '@mui/icons-material/VerifiedRounded'
import BusinessCenterRounded from '@mui/icons-material/BusinessCenterRounded'

const FEATURES = [
  {
    icon: PrecisionManufacturingRounded,
    title: 'Linh kiện cơ khí chức năng',
    desc: 'Sản xuất đồ gá, vỏ hộp, bracket và chi tiết máy với độ chính xác cao.',
  },
  {
    icon: SpeedRounded,
    title: 'Rapid prototyping',
    desc: 'Tạo mẫu nhanh từ 24-48h, giúp rút ngắn vòng lặp thiết kế của bạn.',
  },
  {
    icon: VerifiedRounded,
    title: 'Độ chính xác ±0.1mm',
    desc: 'Hệ thống kiểm soát chất lượng và lựa chọn máy phù hợp với yêu cầu kỹ thuật.',
  },
  {
    icon: BusinessCenterRounded,
    title: 'Sản xuất theo đơn hàng',
    desc: 'Không cần đầu tư máy móc, in t 1 đến hàng trăm chi tiết theo nhu cầu.',
  },
]

export function B2BSection() {
  return (
    <SectionShell
      id="b2b"
      label="Dành cho doanh nghiệp"
      heading="In 3D kỹ thuật, rapid prototyping & linh kiện cơ khí"
      body="Giải pháp in 3D FDM/Resin cho doanh nghiệp: từ mẫu thử nghiệm đến chi tiết chức năng, với độ chính xác ±0.1mm và thời gian giao hàng 24-48h."
    >
      <Grid container spacing={{ xs: 2, md: 3 }}>
        {FEATURES.map(({ icon: Icon, title, desc }) => (
          <Grid key={title} size={{ xs: 12, md: 6, lg: 3 }}>
            <Box
              sx={{
                p: { xs: 2.5, md: 3.5 },
                height: '100%',
                borderRadius: '18px',
                background: gradients.metal,
                border: `1px solid ${palette.border.hairline}`,
                boxShadow: shadows.card,
                transition: 'box-shadow 200ms ease, transform 200ms ease',
                '&:hover': {
                  boxShadow: shadows.cardHover,
                  transform: 'translateY(-4px)',
                },
              }}
            >
              <Box
                sx={{
                  width: 48,
                  height: 48,
                  borderRadius: '14px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: palette.purple.soft,
                  color: palette.purple.deep,
                  mb: 2,
                }}
              >
                <Icon sx={{ fontSize: 26 }} />
              </Box>
              <Box sx={{ fontSize: 17, fontWeight: 700, color: 'text.primary', mb: 1 }}>
                {title}
              </Box>
              <Box sx={{ fontSize: 14, lineHeight: 1.6, color: 'text.secondary' }}>
                {desc}
              </Box>
            </Box>
          </Grid>
        ))}
      </Grid>
    </SectionShell>
  )
}
