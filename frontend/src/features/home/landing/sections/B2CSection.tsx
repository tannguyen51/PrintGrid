import { Box, Grid } from '@mui/material'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'
import FilterVintageRounded from '@mui/icons-material/FilterVintageRounded'
import HomeRounded from '@mui/icons-material/HomeRounded'
import CardGiftcardRounded from '@mui/icons-material/CardGiftcardRounded'
import SportsHandballRounded from '@mui/icons-material/SportsHandballRounded'

const FEATURES = [
  {
    icon: FilterVintageRounded,
    title: 'Mô hình & Figurine',
    desc: 'Biến nhân vật yêu thích từ game, anime thành mô hình thực với chi tiết sắc nét.',
  },
  {
    icon: HomeRounded,
    title: 'Decor nội thất',
    desc: 'Lam p đèn, khung tranh, vật trang trí độc bản cho không gian sống của bạn.',
  },
  {
    icon: CardGiftcardRounded,
    title: 'Quà tặng cá nhân hóa',
    desc: 'Móc khóa, standee, mô hình khắc tên — ý tưởng quà tặng độc đáo và ý nghĩa.',
  },
  {
    icon: SportsHandballRounded,
    title: 'Cosplay props & Phụ kiện',
    desc: 'Chế tạo phụ kiện, giáp, vũ khí mô hình cho sở thích và sự kiện của bạn.',
  },
]

export function B2CSection() {
  return (
    <SectionShell
      id="b2c"
      label="Dành cho cá nhân"
      heading="Mô hình, figurine, decor & quà tặng cá nhân hóa"
      body="Dù bạn là maker, cosplayer hay chỉ đơn giản muốn một món quà độc đáo, PrintGrid giúp biến ý tưởng thành sản phẩm thực."
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
                  bgcolor: palette.gold.soft,
                  color: palette.gold.deep,
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
