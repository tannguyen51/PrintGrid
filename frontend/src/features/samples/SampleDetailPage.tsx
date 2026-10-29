import { useState } from 'react'
import {
  Alert,
  Box,
  CircularProgress,
  Divider,
  Grid,
  Stack,
  Typography,
} from '@mui/material'
import { useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { ModelViewer } from '../../shared/components/ModelViewer'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { landing, PillButton } from '../../shared/theme/landing'
import { landingCardSx } from '../../shared/components/StatusChip'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { PageHeading, PublicPage } from '../public/PublicLayout'
import { SampleCard } from './components/SampleCard'
import { formatDims, formatPrintTime, formatVnd } from './sampleTypes'
import { useCopySample, useSample, useSamples } from './useSamples'

export default function SampleDetailPage() {
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)
  const [actionError, setActionError] = useState('')

  const { data: sample, isLoading, isError } = useSample(slug)
  const copy = useCopySample()
  const relatedQuery = useSamples('', sample?.category ?? '')

  const canOrder = isAuthenticated && (user?.roles?.includes('Customer') ?? false)
  const related = (relatedQuery.data ?? []).filter((item) => item.slug !== slug).slice(0, 3)

  const handleOrder = async () => {
    if (!sample) return
    setActionError('')
    if (!isAuthenticated) {
      setAuthMode('login')
      return
    }
    if (!canOrder) return
    try {
      const model = await copy.mutateAsync(sample.slug)
      navigate(`/models/${model.id}/order`)
    } catch (error) {
      setActionError(getApiErrorMessage(error, 'Không thể tạo bản sao của mẫu. Vui lòng thử lại.'))
    }
  }

  if (isLoading) {
    return (
      <PublicPage>
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '50vh' }}>
          <CircularProgress />
        </Box>
      </PublicPage>
    )
  }

  if (isError || !sample) {
    return (
      <PublicPage>
        <PageBackButton to="/samples" title="Về kho mẫu" />
        <Alert severity="warning" sx={{ mt: 2, borderRadius: `${landing.radius}px` }}>
          Không tìm thấy mẫu này. Có thể mẫu đã được ẩn khỏi kho.
        </Alert>
        <Box sx={{ mt: 2 }}>
          <PillButton onClick={() => navigate('/samples')}>Về kho mẫu</PillButton>
        </Box>
      </PublicPage>
    )
  }

  return (
    <PublicPage>
      <Box sx={{ mb: 2 }}>
        <PageBackButton to="/samples" title="Về kho mẫu" />
      </Box>

      <PageHeading
        eyebrow={`${sample.category} · ${sample.fileFormat}`}
        title={sample.name}
        description={sample.description ?? 'Mẫu PrintGrid original, sẵn sàng đặt in.'}
      />

      <Grid container spacing={{ xs: 3, md: 4 }}>
        {/* ── 3D preview ── */}
        <Grid size={{ xs: 12, md: 7 }}>
          <Box
            sx={{
              position: 'relative',
              height: { xs: 320, sm: 420, md: 560 },
              borderRadius: `${landing.radius}px`,
              overflow: 'hidden',
              border: `1px solid ${landing.hairline}`,
              bgcolor: '#050505',
            }}
          >
            <ModelViewer
              url={`/api/v1/samples/${encodeURIComponent(sample.slug)}/file`}
              format={sample.fileFormat}
              fit
              height="100%"
              background="#050505"
            />
            <Box
              sx={{
                position: 'absolute',
                left: 14,
                bottom: 12,
                px: 1.25,
                py: '4px',
                borderRadius: '8px',
                bgcolor: 'rgba(0,0,0,0.6)',
                border: `1px solid ${landing.hairline}`,
                fontSize: 11,
                color: landing.textMuted,
              }}
            >
              Kéo để xoay · cuộn để phóng
            </Box>
          </Box>
          {sample.tags.length > 0 && (
            <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: 'wrap' }} useFlexGap>
              {sample.tags.map((tag) => (
                <Box
                  key={tag}
                  sx={{
                    px: 1.25,
                    py: '3px',
                    borderRadius: '999px',
                    border: `1px solid ${landing.hairline}`,
                    color: landing.textMuted,
                    fontSize: 12,
                  }}
                >
                  {tag}
                </Box>
              ))}
            </Stack>
          )}
        </Grid>

        {/* ── Specs + CTA ── */}
        <Grid size={{ xs: 12, md: 5 }}>
          <Box sx={{ ...landingCardSx, p: 3 }}>
            <Typography sx={{ color: landing.textMuted, fontSize: 11, letterSpacing: '0.12em', mb: 0.75 }}>
              GIÁ THAM KHẢO
            </Typography>
            <Typography sx={{ color: landing.text, fontWeight: 700, fontSize: '1.6rem', lineHeight: 1.2 }}>
              {sample.priceRange
                ? `${formatVnd(sample.priceRange.from)} – ${formatVnd(sample.priceRange.to)}`
                : 'Liên hệ'}
            </Typography>
            <Typography sx={{ color: landing.textMuted, fontSize: '0.8rem', mt: 0.75 }}>
              Vật liệu, màu và độ mịn thay đổi giá. Báo giá chính xác hiện ngay ở bước cấu hình.
            </Typography>

            <Divider sx={{ my: 2.5, borderColor: landing.hairlineSoft }} />

            <Stack spacing={0}>
              <SpecRow label="Kích thước" value={formatDims(sample)} />
              <SpecRow
                label="Thể tích"
                value={sample.volumeCm3 != null ? `${sample.volumeCm3.toFixed(1)} cm³` : '–'}
              />
              <SpecRow label="Thời gian in (mốc)" value={formatPrintTime(sample.estimatedPrintMinutes)} />
              <SpecRow
                label="Vật liệu ước tính"
                value={sample.estimatedMaterialGrams != null ? `${Math.round(sample.estimatedMaterialGrams)} g` : '–'}
              />
              <SpecRow label="Định dạng" value={sample.fileFormat} last />
            </Stack>

            {actionError && (
              <Alert severity="error" sx={{ mt: 2, borderRadius: 2 }}>
                {actionError}
              </Alert>
            )}

            <Box sx={{ mt: 3 }}>
              <PillButton
                fullWidth
                onClick={handleOrder}
                disabled={copy.isPending || (isAuthenticated && !canOrder)}
                sx={{
                  bgcolor: landing.text,
                  color: '#0A0A0A',
                  borderColor: landing.text,
                  fontWeight: 700,
                  '&:hover': { bgcolor: '#FFFFFF', borderColor: '#FFFFFF' },
                }}
              >
                {copy.isPending
                  ? 'Đang chuẩn bị bản sao…'
                  : !isAuthenticated
                    ? 'Đăng nhập để đặt in'
                    : canOrder
                      ? 'Đặt in mẫu này'
                      : 'Chỉ tài khoản khách hàng đặt in được'}
              </PillButton>
              <Typography sx={{ color: landing.textMuted, fontSize: '0.74rem', mt: 1.25, textAlign: 'center' }}>
                Mẫu gốc thuộc PrintGrid. Bạn nhận một bản sao riêng trong thư viện của mình.
              </Typography>
            </Box>
          </Box>
        </Grid>
      </Grid>

      {related.length > 0 && (
        <Box sx={{ mt: { xs: 6, md: 8 } }}>
          <Typography sx={{ color: landing.text, fontWeight: 600, fontSize: '1.15rem', mb: 2.5 }}>
            Mẫu cùng danh mục
          </Typography>
          <Grid container spacing={2.5}>
            {related.map((item) => (
              <Grid key={item.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                <SampleCard sample={item} />
              </Grid>
            ))}
          </Grid>
        </Box>
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

function SpecRow({ label, value, last = false }: { label: string; value: string; last?: boolean }) {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'space-between',
        gap: 2,
        py: 1.4,
        borderBottom: last ? 'none' : `1px solid ${landing.hairlineSoft}`,
      }}
    >
      <Typography sx={{ color: landing.textMuted, fontSize: '0.86rem' }}>{label}</Typography>
      <Typography sx={{ color: landing.text, fontSize: '0.86rem', fontWeight: 600, textAlign: 'right' }}>{value}</Typography>
    </Box>
  )
}
