import { useState, type ReactNode } from 'react'
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useAuth } from '../../app/AuthContext'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { landing, PillButton } from '../../shared/theme/landing'
import { landingCardSx } from '../../shared/components/StatusChip'
import { PageHeading, PublicPage } from '../public/PublicLayout'
import { formatDate, formatVnd } from './pricingTypes'
import { usePricingDetail, usePricingPreview } from './usePricing'

export default function PricingPage() {
  const { isAuthenticated, user } = useAuth()
  const isCustomer = isAuthenticated && (user?.roles?.includes('Customer') ?? false)
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)

  const preview = usePricingPreview()
  const detail = usePricingDetail(isCustomer)

  const loading = preview.isLoading || (isCustomer && detail.isLoading)
  const version = detail.data?.version ?? preview.data?.version
  const effectiveAt = detail.data?.effectiveAtUtc ?? preview.data?.effectiveAtUtc

  return (
    <PublicPage>
      <PageHeading
        eyebrow="Bảng giá & vật liệu"
        title="Một công thức giá cho toàn mạng lưới."
        description="Mỗi báo giá được tính từ cùng một bộ tham số đang hoạt động: khối lượng vật liệu cộng thời gian máy. Không có bảng giá riêng cho từng xưởng."
        actions={
          !isCustomer ? (
            <PillButton onClick={() => setAuthMode('login')}>Đăng nhập để xem đơn giá</PillButton>
          ) : undefined
        }
      />

      {version && (
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 3.5, flexWrap: 'wrap' }} useFlexGap>
          <Chip
            label={`Phiên bản giá ${version}`}
            sx={{ bgcolor: landing.surface, color: landing.text, border: `1px solid ${landing.hairline}` }}
          />
          <Typography sx={{ color: landing.textMuted, fontSize: '0.82rem' }}>
            Áp dụng từ {formatDate(effectiveAt)} · giá cũ vẫn giữ nguyên trên báo giá đã tạo
          </Typography>
        </Stack>
      )}

      {preview.isError && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: `${landing.radius}px` }}>
          Không tải được bảng giá. Vui lòng thử lại sau.
        </Alert>
      )}

      {loading ? (
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 240 }}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* ── Materials ── */}
          <SectionTitle
            title="Vật liệu"
            note={
              isCustomer
                ? 'Đơn giá chính xác đang áp dụng cho báo giá mới.'
                : `Khoảng giá cho một chi tiết tham chiếu ${preview.data?.referenceVolumeCm3 ?? 50} cm³.`
            }
          />

          <Grid container spacing={2.5}>
            {isCustomer
              ? (detail.data?.materials ?? []).map((material) => (
                  <Grid key={material.code} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <MaterialCard
                      code={material.code}
                      name={material.name}
                      description={material.description}
                      isPremium={material.isPremium}
                      price={`${formatVnd(material.unitRatePerGram)} / g`}
                      secondary={`Mật độ ${material.densityGramsPerCm3} g/cm³`}
                    />
                  </Grid>
                ))
              : (preview.data?.materials ?? []).map((material) => (
                  <Grid key={material.code} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <MaterialCard
                      code={material.code}
                      name={material.name}
                      description={material.description}
                      isPremium={material.isPremium}
                      price={`${formatVnd(material.priceRange.from)} – ${formatVnd(material.priceRange.to)}`}
                      secondary={`Cho chi tiết ${preview.data?.referenceVolumeCm3 ?? 50} cm³`}
                    />
                  </Grid>
                ))}
          </Grid>

          {!isCustomer && (
            <Box
              sx={{
                ...landingCardSx,
                mt: 3,
                p: { xs: 2.5, md: 3 },
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                alignItems: { sm: 'center' },
                gap: 2,
                justifyContent: 'space-between',
              }}
            >
              <Box>
                <Typography sx={{ color: landing.text, fontWeight: 600 }}>
                  Đơn giá theo gram và hệ số độ mịn chỉ hiện sau khi đăng nhập
                </Typography>
                <Typography sx={{ color: landing.textMuted, fontSize: '0.85rem', mt: 0.5 }}>
                  Khoảng giá bên trên do máy chủ tính từ bộ tham số đang hoạt động — không phải số nhập tay.
                </Typography>
              </Box>
              <PillButton onClick={() => setAuthMode('login')}>Xem đơn giá chính xác</PillButton>
            </Box>
          )}

          {/* ── How it is computed ── */}
          <Box sx={{ mt: { xs: 7, md: 9 } }}>
            <SectionTitle title="Giá được tính thế nào" note="Ba thành phần, không có phụ phí ẩn." />
            <Grid container spacing={2.5}>
              <Grid size={{ xs: 12, md: 4 }}>
                <StepCard
                  step="01"
                  title="Khối lượng vật liệu"
                  body="Thể tích hình học × khối lượng riêng × tỷ lệ đặc. Bạn chọn vật liệu và mức đặc (infill) ở bước cấu hình."
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <StepCard
                  step="02"
                  title="Thời gian máy"
                  body="Thời gian in ước tính theo chiều cao lớp × đơn giá giờ máy của mạng lưới — một mức chung cho mọi xưởng."
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <StepCard
                  step="03"
                  title="Chốt giá khi đặt"
                  body="Báo giá giữ nguyên bộ tham số tại thời điểm tạo. Thay đổi giá sau đó không làm thay đổi đơn đã đặt."
                />
              </Grid>
            </Grid>
          </Box>

          {/* ── Exact parameter table (customers only) ── */}
          {isCustomer && detail.data && (
            <Box sx={{ mt: { xs: 7, md: 9 } }}>
              <SectionTitle
                title="Thông số đang áp dụng"
                note="Dữ liệu từ bộ tham số thương mại đang hoạt động. Chỉ tài khoản khách hàng thấy được."
              />

              <Grid container spacing={2.5}>
                <Grid size={{ xs: 12, md: 6 }}>
                  <Box sx={{ ...landingCardSx, p: 2.5 }}>
                    <Typography sx={{ color: landing.text, fontWeight: 600, mb: 1.5 }}>Độ mịn in</Typography>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <StyledCell head>Chiều cao lớp</StyledCell>
                          <StyledCell head align="right">
                            Hệ số thời gian
                          </StyledCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {detail.data.layerHeights.map((layer) => (
                          <TableRow key={layer.layerHeightMm}>
                            <StyledCell>{layer.layerHeightMm} mm</StyledCell>
                            <StyledCell align="right">×{layer.timeMultiplier}</StyledCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                    <Divider sx={{ my: 2, borderColor: landing.hairlineSoft }} />
                    <Stack direction="row" justifyContent="space-between">
                      <Typography sx={{ color: landing.textMuted, fontSize: '0.86rem' }}>Đơn giá giờ máy</Typography>
                      <Typography sx={{ color: landing.text, fontWeight: 700 }}>
                        {formatVnd(detail.data.pricePerPrintHour)} / giờ
                      </Typography>
                    </Stack>
                  </Box>
                </Grid>

                <Grid size={{ xs: 12, md: 6 }}>
                  <Box sx={{ ...landingCardSx, p: 2.5 }}>
                    <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.5 }}>Ví dụ tính giá</Typography>
                    <Typography sx={{ color: landing.textMuted, fontSize: '0.82rem', mb: 2 }}>
                      {detail.data.referenceExample?.materialCode ?? 'PLA'} · {detail.data.referenceVolumeCm3} cm³ ·{' '}
                      {detail.data.referenceInfillPercent}% infill · lớp {detail.data.referenceExample?.layerHeightMm ?? 0.2} mm
                    </Typography>

                    {detail.data.referenceExample ? (
                      <Stack spacing={1.2}>
                        <ExampleRow
                          label={`Vật liệu (${Math.round(detail.data.referenceExample.grams)} g)`}
                          value={formatVnd(detail.data.referenceExample.materialCost)}
                        />
                        <ExampleRow
                          label={`Máy in (${detail.data.referenceExample.effectiveMinutes} phút)`}
                          value={formatVnd(detail.data.referenceExample.machineCost)}
                        />
                        <Divider sx={{ borderColor: landing.hairlineSoft }} />
                        <ExampleRow label="Tổng" value={formatVnd(detail.data.referenceExample.total)} strong />
                      </Stack>
                    ) : (
                      <Typography sx={{ color: landing.textMuted }}>Chưa có dữ liệu ví dụ.</Typography>
                    )}
                  </Box>
                </Grid>
              </Grid>
            </Box>
          )}

          {/* ── FAQ ── */}
          <Box sx={{ mt: { xs: 7, md: 9 } }}>
            <SectionTitle title="Câu hỏi thường gặp" />
            <Stack spacing={1.5}>
              <Faq
                question="Vì sao mỗi xưởng không có bảng giá riêng?"
                answer="PrintGrid dùng một công thức và một bộ tham số chung cho toàn mạng lưới. Xưởng cạnh tranh bằng thời gian và chất lượng, không bằng cách tự đặt giá."
              />
              <Faq
                question="Giá có thay đổi sau khi tôi đặt không?"
                answer="Không. Báo giá chốt bộ tham số tại thời điểm tạo và được giữ nguyên trên đơn hàng, kể cả khi bảng giá chung thay đổi sau đó."
              />
              <Faq
                question="Vì sao khách vãng lai chỉ thấy khoảng giá?"
                answer="Đơn giá chính xác là thông tin vận hành. Khách vãng lai thấy khoảng giá do máy chủ tính để tham khảo; đăng nhập để xem bảng đơn giá đầy đủ."
              />
            </Stack>
          </Box>
        </>
      )}

      <AuthDialog
        open={authMode !== null}
        mode={authMode ?? 'login'}
        onClose={() => setAuthMode(null)}
        onSwitchMode={setAuthMode}
      />
    </PublicPage>
  )
}

function SectionTitle({ title, note }: { title: string; note?: string }) {
  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'baseline' }} justifyContent="space-between" gap={1} sx={{ mb: 2.5 }}>
      <Typography component="h2" sx={{ color: landing.text, fontSize: '1.3rem', fontWeight: 600 }}>
        {title}
      </Typography>
      {note && <Typography sx={{ color: landing.textMuted, fontSize: '0.84rem' }}>{note}</Typography>}
    </Stack>
  )
}

function MaterialCard({
  code,
  name,
  description,
  isPremium,
  price,
  secondary,
}: {
  code: string
  name: string
  description?: string | null
  isPremium: boolean
  price: string
  secondary: string
}) {
  return (
    <Box sx={{ ...landingCardSx, p: 2.5, height: '100%', display: 'flex', flexDirection: 'column', gap: 1.25 }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <Typography sx={{ color: landing.text, fontWeight: 700, fontSize: '1.05rem' }}>{name}</Typography>
        <Chip size="small" label={code} variant="outlined" sx={{ color: landing.textMuted, borderColor: landing.hairline }} />
        {isPremium && (
          <Chip size="small" label="Cao cấp" sx={{ color: '#C4B5FD', bgcolor: 'rgba(139,92,246,0.16)', border: '1px solid rgba(139,92,246,0.4)' }} />
        )}
      </Stack>
      <Typography sx={{ color: landing.textMuted, fontSize: '0.85rem', flex: 1 }}>
        {description ?? 'Vật liệu tiêu chuẩn của mạng lưới PrintGrid.'}
      </Typography>
      <Box sx={{ pt: 0.5 }}>
        <Typography sx={{ color: landing.text, fontWeight: 700, fontSize: '1.05rem' }}>{price}</Typography>
        <Typography sx={{ color: landing.textMuted, fontSize: '0.76rem', mt: 0.25 }}>{secondary}</Typography>
      </Box>
    </Box>
  )
}

function StepCard({ step, title, body }: { step: string; title: string; body: string }) {
  return (
    <Box sx={{ ...landingCardSx, p: 2.75, height: '100%' }}>
      <Typography sx={{ color: landing.textMuted, fontSize: 12, letterSpacing: '0.12em', mb: 1.25 }}>{step}</Typography>
      <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.75 }}>{title}</Typography>
      <Typography sx={{ color: landing.textMuted, fontSize: '0.87rem', lineHeight: 1.6 }}>{body}</Typography>
    </Box>
  )
}

function ExampleRow({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <Stack direction="row" justifyContent="space-between" alignItems="baseline">
      <Typography sx={{ color: strong ? landing.text : landing.textMuted, fontSize: '0.88rem', fontWeight: strong ? 600 : 400 }}>
        {label}
      </Typography>
      <Typography sx={{ color: landing.text, fontWeight: strong ? 800 : 600, fontSize: strong ? '1.05rem' : '0.9rem' }}>
        {value}
      </Typography>
    </Stack>
  )
}

function StyledCell({
  children,
  head = false,
  align = 'left',
}: {
  children: ReactNode
  head?: boolean
  align?: 'left' | 'right'
}) {
  return (
    <TableCell
      align={align}
      sx={{
        borderColor: landing.hairlineSoft,
        color: head ? landing.textMuted : landing.text,
        fontSize: head ? '0.76rem' : '0.86rem',
        fontWeight: head ? 600 : 500,
        letterSpacing: head ? '0.04em' : 0,
        py: 1,
      }}
    >
      {children}
    </TableCell>
  )
}

function Faq({ question, answer }: { question: string; answer: string }) {
  return (
    <Box sx={{ ...landingCardSx, p: 2.5 }}>
      <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.75 }}>{question}</Typography>
      <Typography sx={{ color: landing.textMuted, fontSize: '0.88rem', lineHeight: 1.65 }}>{answer}</Typography>
    </Box>
  )
}
