import { Box, Stack, Typography } from '@mui/material'
import { ArrowForwardRounded } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { lazy, Suspense, useEffect, useRef, useState } from 'react'
import { apiClient } from '../../../shared/api/apiClient'
import { landing } from '../../../shared/theme/landing'
import { landingCardSx } from '../../../shared/components/StatusChip'
import { formatDims, formatPrintTime, formatVnd, type SampleSummary } from '../sampleTypes'

const ModelViewer = lazy(() => import('../../../shared/components/ModelViewer').then(module => ({ default: module.ModelViewer })))

/** Load actual geometry only while the card is near the viewport. */
function SamplePreview({ sample }: { sample: SampleSummary }) {
  const container = useRef<HTMLDivElement>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const element = container.current
    if (!element) return
    const observer = new IntersectionObserver(([entry]) => setVisible(entry.isIntersecting), { rootMargin: '100px' })
    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  return (
    <Box ref={container} role="img" aria-label={`Mô hình 3D ${sample.name}`} sx={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
      {visible && (
        <Suspense fallback={null}>
          <ModelViewer
            url={apiClient.getUri({ url: `/samples/${encodeURIComponent(sample.slug)}/file` })}
            format={sample.fileFormat}
            fit
            autoRotate={false}
            interactive={false}
            background="#F5F2FF"
            sx={{ minHeight: 0 }}
          />
        </Suspense>
      )}
    </Box>
  )
}

export function SampleCard({ sample }: { sample: SampleSummary }) {
  const navigate = useNavigate()
  const to = `/samples/${sample.slug}`

  return (
    <Box
      onClick={() => navigate(to)}
      sx={{
        ...landingCardSx,
        overflow: 'hidden',
        cursor: 'pointer',
        display: 'flex',
        flexDirection: 'column',
        transition: 'border-color .18s ease, transform .18s ease',
        '&:hover': { borderColor: '#6F55FF', transform: 'translateY(-2px)' },
      }}
    >
      <Box
        sx={{
          position: 'relative',
          aspectRatio: '4 / 3',
          bgcolor: 'background.secondary',
          borderBottom: `1px solid ${landing.hairlineSoft}`,
          display: 'grid',
          placeItems: 'center',
          overflow: 'hidden',
        }}
      >
        {sample.coverImageUrl ? (
          <Box
            component="img"
            src={sample.coverImageUrl}
            alt={sample.name}
            loading="lazy"
            sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        ) : (
          <SamplePreview sample={sample} />
        )}
        <Box
          sx={{
            position: 'absolute',
            zIndex: 2,
            top: 12,
            left: 12,
            px: 1.25,
            py: '3px',
            borderRadius: '999px',
            bgcolor: 'rgba(255,255,255,0.9)',
            backdropFilter: 'blur(6px)',
            border: `1px solid ${landing.hairline}`,
            fontSize: 11,
            letterSpacing: '0.04em',
            color: landing.text,
          }}
        >
          {sample.category}
        </Box>
      </Box>

      <Stack spacing={1.25} sx={{ p: 2.25, flex: 1 }}>
        <Box>
          <Typography sx={{ color: landing.text, fontWeight: 600, fontSize: '1rem', lineHeight: 1.3 }}>
            {sample.name}
          </Typography>
          <Typography
            sx={{
              color: landing.textMuted,
              fontSize: '0.82rem',
              mt: 0.5,
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden',
              minHeight: 34,
            }}
          >
            {sample.description ?? 'Mẫu PrintGrid original.'}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 0.75 }}>
          <Meta label={sample.fileFormat} />
          <Meta label={formatDims(sample)} />
          <Meta label={formatPrintTime(sample.estimatedPrintMinutes)} />
        </Stack>

        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ pt: 0.5, mt: 'auto' }}>
          <Box>
            <Typography sx={{ fontSize: 11, color: landing.textMuted, letterSpacing: '0.06em' }}>GIÁ THAM KHẢO</Typography>
            <Typography sx={{ color: landing.text, fontWeight: 700, fontSize: '0.95rem' }}>
              {sample.priceRange
                ? `${formatVnd(sample.priceRange.from)} – ${formatVnd(sample.priceRange.to)}`
                : 'Liên hệ'}
            </Typography>
          </Box>
          <ArrowForwardRounded sx={{ color: landing.textMuted, fontSize: 18 }} />
        </Stack>
      </Stack>
    </Box>
  )
}

function Meta({ label }: { label: string }) {
  return (
    <Box
      sx={{
        px: 1,
        py: '2px',
        borderRadius: '8px',
        border: `1px solid ${landing.hairlineSoft}`,
        color: landing.textMuted,
        fontSize: 11,
        whiteSpace: 'nowrap',
      }}
    >
      {label}
    </Box>
  )
}
