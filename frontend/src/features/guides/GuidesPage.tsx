import { useState } from 'react'
import { Box, Divider, Grid, Stack, Typography } from '@mui/material'
import { ExpandMoreRounded } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { landing, PillButton } from '../../shared/theme/landing'
import { landingCardSx } from '../../shared/components/StatusChip'
import { PageHeading, PublicPage } from '../public/PublicLayout'

const FORMATS = [
  { code: 'STL', name: 'Phổ biến nhất', note: 'Chỉ có bề mặt, không kèm màu. Phù hợp cho hầu hết chi tiết in FDM và SLA.' },
  { code: 'OBJ', name: 'Có nhóm đối tượng', note: 'Giữ được nhiều phần rời nhau trong một file. Tốt cho mô hình lắp ghép.' },
  { code: '3MF', name: 'Kèm đơn vị và màu', note: 'Giữ thông tin đơn vị đo chính xác. Hữu ích khi model xuất từ CAD chuyên nghiệp.' },
  { code: 'GLB', name: 'Nhẹ, dễ xem', note: 'Dung lượng nhỏ, trình xem tải nhanh. Phù hợp khi chia sẻ bản xem trước.' },
]

const FAQ = [
  {
    question: 'File của tôi cần đáp ứng điều kiện gì?',
    answer:
      'Dung lượng tối đa 50 MB, định dạng STL, OBJ, 3MF hoặc GLB. Model nên là khối kín (watertight) và không có cạnh non-manifold để hệ thống phân tích hình học chính xác.',
  },
  {
    question: 'Vì sao hệ thống cần phân tích hình học?',
    answer:
      'Phân tích cho ra kích thước bao, thể tích và thời gian in dự kiến. Đây là đầu vào của bước báo giá và bước xếp lịch máy — không có nó thì không thể chốt giá hay ngày giao.',
  },
  {
    question: 'Tôi có thể đặt in từ mẫu có sẵn không?',
    answer:
      'Có. Vào Kho mẫu 3D, chọn một mẫu rồi bấm "Đặt in mẫu này". Hệ thống tạo một bản sao riêng trong thư viện của bạn, sau đó bạn cấu hình vật liệu, màu và độ mịn như model tự tải lên.',
  },
  {
    question: 'Đơn vị đo trong file có quan trọng không?',
    answer:
      'Rất quan trọng. STL không lưu đơn vị, nên nếu file xuất theo inch mà hệ thống hiểu là mm thì chi tiết sẽ sai kích thước. Hãy kiểm tra lại kích thước bao sau khi tải lên.',
  },
  {
    question: 'Bao lâu thì có báo giá?',
    answer:
      'Ngay sau khi phân tích hình học xong — thường vài giây với file thông thường. Báo giá có hiệu lực 48 giờ và giữ nguyên bộ tham số tại thời điểm tạo.',
  },
  {
    question: 'Tôi cần sửa file thì làm thế nào?',
    answer:
      'Xóa model cũ trong Thư viện model rồi tải bản đã sửa lên. Nếu đơn đã đặt, hãy liên hệ bộ phận hỗ trợ trước khi gửi lại file để tránh trùng đơn.',
  },
]

export default function GuidesPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const canOrder = isAuthenticated && (user?.roles?.includes('Customer') ?? false)

  return (
    <PublicPage>
      <PageHeading
        eyebrow="Hướng dẫn / FAQ"
        title="Chuẩn bị file đúng ngay từ lần đầu."
        description="Phần lớn lỗi in đến từ file nguồn: hở mặt, sai đơn vị, hoặc quá mỏng. Kiểm tra nhanh theo danh sách dưới đây trước khi tải lên."
        actions={
          <PillButton onClick={() => navigate(canOrder ? '/order/new' : '/register')}>
            {canOrder ? 'Bắt đầu đặt in' : 'Tạo tài khoản'}
          </PillButton>
        }
      />

      {/* ── Checklist ── */}
      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Box sx={{ ...landingCardSx, p: { xs: 2.5, md: 3 }, height: '100%' }}>
            <Typography sx={{ color: landing.text, fontSize: '1.05rem', fontWeight: 600, mb: 0.75 }}>
              Trước khi tải lên
            </Typography>
            <Typography sx={{ color: landing.textMuted, fontSize: '0.87rem', mb: 2 }}>
              Bốn bước kiểm tra nhanh, mất chưa tới một phút.
            </Typography>
            <Stack spacing={1.5}>
              <CheckItem index="01" text="Model là một khối kín — không có mặt hở hoặc lỗ thủng." />
              <CheckItem index="02" text="Không có cạnh non-manifold (mỗi cạnh chỉ thuộc đúng hai mặt)." />
              <CheckItem index="03" text="Đơn vị trong file là milimét; kích thước bao khớp với thực tế." />
              <CheckItem index="04" text="Chi tiết nhỏ nhất dày tối thiểu 0,8 mm để máy in được ổn định." />
            </Stack>
          </Box>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Box sx={{ ...landingCardSx, p: { xs: 2.5, md: 3 }, height: '100%' }}>
            <Typography sx={{ color: landing.text, fontSize: '1.05rem', fontWeight: 600, mb: 0.75 }}>
              Định dạng được nhận
            </Typography>
            <Typography sx={{ color: landing.textMuted, fontSize: '0.87rem', mb: 2 }}>
              Tối đa 50 MB mỗi file. Hệ thống tự nhận diện định dạng từ phần mở rộng.
            </Typography>
            <Stack spacing={1.5} divider={<Divider sx={{ borderColor: landing.hairlineSoft }} />}>
              {FORMATS.map((format) => (
                <Box key={format.code}>
                  <Stack direction="row" spacing={1.25} alignItems="baseline">
                    <Typography sx={{ color: landing.text, fontWeight: 700, fontFamily: 'monospace' }}>{format.code}</Typography>
                    <Typography sx={{ color: landing.textMuted, fontSize: '0.78rem' }}>{format.name}</Typography>
                  </Stack>
                  <Typography sx={{ color: landing.textMuted, fontSize: '0.84rem', mt: 0.4, lineHeight: 1.55 }}>
                    {format.note}
                  </Typography>
                </Box>
              ))}
            </Stack>
          </Box>
        </Grid>
      </Grid>

      {/* ── Flow ── */}
      <Box sx={{ mt: { xs: 6, md: 8 } }}>
        <Typography component="h2" sx={{ color: landing.text, fontSize: '1.3rem', fontWeight: 600, mb: 2.5 }}>
          Một đơn hàng đi qua những bước nào
        </Typography>
        <Grid container spacing={2}>
          {[
            ['01', 'Tải model', 'Chọn file, đặt tên và gắn thẻ để dễ tìm lại.'],
            ['02', 'Phân tích', 'Hệ thống đọc hình học, tính kích thước và thời gian in.'],
            ['03', 'Cấu hình & báo giá', 'Chọn vật liệu, màu, độ mịn và mức đặc. Giá hiện ngay.'],
            ['04', 'Đặt & theo dõi', 'Xác nhận địa chỉ, đặt hàng và theo dõi tiến độ sản xuất.'],
          ].map(([step, title, body]) => (
            <Grid key={step} size={{ xs: 12, sm: 6, lg: 3 }}>
              <Box sx={{ ...landingCardSx, p: 2.5, height: '100%' }}>
                <Typography sx={{ color: landing.textMuted, fontSize: 12, letterSpacing: '0.12em', mb: 1 }}>{step}</Typography>
                <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.5 }}>{title}</Typography>
                <Typography sx={{ color: landing.textMuted, fontSize: '0.85rem', lineHeight: 1.55 }}>{body}</Typography>
              </Box>
            </Grid>
          ))}
        </Grid>
      </Box>

      {/* ── FAQ ── */}
      <Box sx={{ mt: { xs: 6, md: 8 } }}>
        <Typography component="h2" sx={{ color: landing.text, fontSize: '1.3rem', fontWeight: 600, mb: 2.5 }}>
          Câu hỏi thường gặp
        </Typography>
        <Stack spacing={1.25}>
          {FAQ.map((item) => (
            <FaqItem key={item.question} question={item.question} answer={item.answer} />
          ))}
        </Stack>
      </Box>
    </PublicPage>
  )
}

function CheckItem({ index, text }: { index: string; text: string }) {
  return (
    <Stack direction="row" spacing={1.5} alignItems="flex-start">
      <Box
        sx={{
          width: 26,
          height: 26,
          borderRadius: '50%',
          display: 'grid',
          placeItems: 'center',
          flexShrink: 0,
          border: `1px solid ${landing.hairline}`,
          bgcolor: landing.surface,
          color: landing.textMuted,
          fontSize: 11,
          fontWeight: 700,
        }}
      >
        {index}
      </Box>
      <Typography sx={{ color: landing.text, fontSize: '0.89rem', lineHeight: 1.55 }}>{text}</Typography>
    </Stack>
  )
}

function FaqItem({ question, answer }: { question: string; answer: string }) {
  const [open, setOpen] = useState(false)
  return (
    <Box sx={{ ...landingCardSx, overflow: 'hidden' }}>
      <Box
        role="button"
        tabIndex={0}
        onClick={() => setOpen((value) => !value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') setOpen((value) => !value)
        }}
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 2,
          p: 2.25,
          cursor: 'pointer',
          '&:hover': { bgcolor: 'rgba(255,255,255,0.02)' },
        }}
      >
        <Typography sx={{ color: landing.text, fontWeight: 600, fontSize: '0.95rem' }}>{question}</Typography>
        <ExpandMoreRounded
          sx={{
            color: landing.textMuted,
            transition: 'transform .2s ease',
            transform: open ? 'rotate(180deg)' : 'none',
          }}
        />
      </Box>
      {open && (
        <Box sx={{ px: 2.25, pb: 2.25 }}>
          <Typography sx={{ color: landing.textMuted, fontSize: '0.88rem', lineHeight: 1.7 }}>{answer}</Typography>
        </Box>
      )}
    </Box>
  )
}
