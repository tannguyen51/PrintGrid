import { Box, Grid } from '@mui/material'
import { SectionShell } from './SectionShell'
import { palette, gradients, shadows } from '../../../../shared/theme/tokens'

const MATERIALS = [
  {
    code: 'PLA',
    name: 'PLA',
    tagline: 'Dễ in, bề mặt đẹp, thân thiện môi trường',
    pros: ['Giá tốt', 'Màu sắc đa dạng', 'Ít co ngót'],
    cons: ['Chịu nhiệt kém', 'Dễ gãy va đập'],
    bestFor: 'Mô hình, decor, quà tặng, prototype thm mỹ',
  },
  {
    code: 'PETG',
    name: 'PETG',
    tagline: 'Bền, chịu nhiệt và ẩm tốt hơn PLA',
    pros: ['Chịu va đập tốt', 'Kháng nước', 'Dễ in hơn ABS'],
    cons: ['Bề mặt hơi bóng', 'Dễ stringing hơn PLA'],
    bestFor: 'Vỏ hộp, linh kiện chịu lực vừa, đồ dùng ngoài trời',
  },
  {
    code: 'ABS',
    name: 'ABS',
    tagline: 'Cứng, chịu nhiệt cao, dễ gia công hậu kỳ',
    pros: ['Chịu nhiệt tới 90°C', 'Cứng và bền', 'Dễ chà nhám/sơn'],
    cons: ['Cần buồng in kín', 'Mùi khi in', 'Co ngót lớn'],
    bestFor: 'Chi tiết máy, đồ gá, vỏ hộp điện tử',
  },
  {
    code: 'TPU',
    name: 'TPU',
    tagline: 'Dẻo dai, đàn hồi, chống mài mòn',
    pros: ['Đàn hồi 95A', 'Chống va đập cc tốt', 'Không vỡ khi uốn'],
    cons: ['Khó in hơn', 'Tốc độ in chậm', 'Dễ stringing'],
    bestFor: 'Ốp điện thoại, khớp nối mềm, bánh xe, seal',
  },
  {
    code: 'RESIN',
    name: 'Resin',
    tagline: 'Chi tiết siêu mịn, bề mặt láng, phù hợp figurine',
    pros: ['Độ chi tiết cao', 'Bề mặt mượt', 'Phù hợp mô hình nhỏ'],
    cons: ['Giá cao hơn FDM', 'Cần xử lý hậu kỳ', 'Giòn hơn'],
    bestFor: 'Figurine, mô hình chi tiết, trang sức, mockup tinh xảo',
  },
]

export function MaterialsSection() {
  return (
    <SectionShell
      id="materials"
      label="Vật liệu"
      heading="So sánh vật liệu in 3D"
      body="Chọn đúng vật liệu sẽ quyết định chất lượng, độ bền và chi phí của sản phẩm. Dưới đây là 5 loại vật liệu chính mà PrintGrid hỗ trợ."
    >
      <Grid container spacing={{ xs: 2, md: 3 }}>
        {MATERIALS.map((m) => (
          <Grid key={m.code} size={{ xs: 12, md: 6, lg: 4 }}>
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
                  display: 'inline-block',
                  px: 1.5,
                  py: 0.5,
                  borderRadius: '8px',
                  bgcolor: palette.purple.soft,
                  color: palette.purple.deep,
                  fontSize: 12,
                  fontWeight: 700,
                  letterSpacing: '0.08em',
                  mb: 2,
                }}
              >
                {m.code}
              </Box>
              <Box sx={{ fontSize: 19, fontWeight: 800, color: 'text.primary', mb: 1 }}>
                {m.name}
              </Box>
              <Box sx={{ fontSize: 14, lineHeight: 1.6, color: 'text.secondary', mb: 2 }}>
                {m.tagline}
              </Box>
              <Box sx={{ mb: 1.5 }}>
                <Box sx={{ fontSize: 12, fontWeight: 700, color: 'text.primary', mb: 0.5 }}>Ưu điểm</Box>
                <Box component="ul" sx={{ m: 0, pl: 2, fontSize: 13, color: 'text.secondary' }}>
                  {m.pros.map((p) => (
                    <Box key={p} component="li" sx={{ mb: 0.25 }}>{p}</Box>
                  ))}
                </Box>
              </Box>
              <Box sx={{ mb: 1.5 }}>
                <Box sx={{ fontSize: 12, fontWeight: 700, color: 'text.primary', mb: 0.5 }}>Nhược điểm</Box>
                <Box component="ul" sx={{ m: 0, pl: 2, fontSize: 13, color: 'text.secondary' }}>
                  {m.cons.map((c) => (
                    <Box key={c} component="li" sx={{ mb: 0.25 }}>{c}</Box>
                  ))}
                </Box>
              </Box>
              <Box sx={{ pt: 1.5, borderTop: `1px solid ${palette.border.hairline}` }}>
                <Box sx={{ fontSize: 12, fontWeight: 700, color: 'text.primary', mb: 0.5 }}>Phù hợp cho</Box>
                <Box sx={{ fontSize: 13, color: 'text.secondary' }}>{m.bestFor}</Box>
              </Box>
            </Box>
          </Grid>
        ))}
      </Grid>
    </SectionShell>
  )
}
