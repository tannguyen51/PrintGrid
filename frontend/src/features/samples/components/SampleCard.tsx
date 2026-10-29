import { Box, Stack, Typography } from '@mui/material'
import { ArrowForwardRounded } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { landing } from '../../../shared/theme/landing'
import { landingCardSx } from '../../../shared/components/StatusChip'
import { formatDims, formatPrintTime, formatVnd, type SampleSummary } from '../sampleTypes'

/** Wireframe cube used when a sample has no admin-supplied cover image. */
function SampleGlyph() {
  return (
    <Box component="svg" viewBox="0 0 200 160" sx={{ width: '100%', height: '100%', opacity: 0.55 }}>
      <g transform="translate(100 82)" fill="none" stroke="#4A4A52" strokeWidth={1.2}>
        <path d="M0 -46 L46 -20 L46 26 L0 52 L-46 26 L-46 -20 Z" />
        <path d="M0 -46 L0 52 M-46 -20 L0 6 L46 -20" />
        <path d="M-30 -34 L16 -8" strokeDasharray="3 5" />
      </g>
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
        '&:hover': { borderColor: '#3A3A42', transform: 'translateY(-2px)' },
      }}
    >
      <Box
        sx={{
          position: 'relative',
          aspectRatio: '4 / 3',
          bgcolor: '#08080A',
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
          <Box sx={{ width: '72%', height: '72%' }}>
            <SampleGlyph />
          </Box>
        )}
        <Box
          sx={{
            position: 'absolute',
            top: 12,
            left: 12,
            px: 1.25,
            py: '3px',
            borderRadius: '999px',
            bgcolor: 'rgba(0,0,0,0.62)',
            border: `1px solid ${landing.hairline}`,
            fontSize: 11,
            letterSpacing: '0.04em',
            color: landing.textMuted,
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
