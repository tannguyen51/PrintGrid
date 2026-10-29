import { useEffect, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { LocalShippingOutlined, ThreeDRotationRounded } from '@mui/icons-material'
import type { ThreeDModel } from './modelTypes'
import { useModels } from './useModels'
import { useModelFileUrl } from './useModelFile'
import { ModelViewer } from '../../shared/components/ModelViewer'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { landing } from '../../shared/theme/landing'

/** Formats the in-browser viewer can render (same list as ModelDetailDrawer). */
const PREVIEWABLE_FORMATS = ['GLB', 'GLTF', 'STL', 'OBJ', '3MF']

/** Same formatting as ModelDetailDrawer so the numbers match the analysis table exactly. */
function formatDimensions(model: ThreeDModel): string {
  return (
    [model.boundingWidthMm, model.boundingDepthMm, model.boundingHeightMm]
      .filter((v): v is number => v != null)
      .map((v) => v.toFixed(0))
      .join(' × ') || '—'
  )
}

/**
 * Customer full-screen 3D preview (FR-CUST-003).
 *
 * Route (Customer only, inside CustomerLayout, next to /models/:modelId/order):
 *   path="/models/:modelId/preview"  — wired in AppRoutes; reached from the library's
 *   detail drawer ("Xem 3D"). Default-exported so the router can lazy-import it.
 *
 * AC01: the bounding-box legend beside the viewer renders the very same
 * boundingWidthMm × boundingDepthMm × boundingHeightMm figures as the analysis list.
 */
export default function ModelPreviewPage() {
  const { modelId } = useParams<{ modelId: string }>()
  const navigate = useNavigate()

  // The library hook already fetches models with the auth header; reuse it and pick
  // the requested row instead of adding a second, parallel fetch path.
  const { data, isLoading, isError } = useModels('')
  const model = useMemo(() => data?.find((m) => m.id === modelId) ?? null, [data, modelId])

  const format = (model?.fileFormat ?? '').toUpperCase()
  const previewable = PREVIEWABLE_FORMATS.includes(format)
  // A model row with no storage key has no mesh on the server yet — analysis still running.
  const hasMesh = Boolean(model?.storageKey)
  const { url, failed } = useModelFileUrl(model?.id ?? null, Boolean(model) && hasMesh && previewable)

  const canOrder = model?.geometryStatus === 'Ready' && model.isPrintable !== false
  const dimensions = model ? formatDimensions(model) : '—'

  useEffect(() => {
    console.info('[preview3d] preview page state', {
      modelId,
      found: Boolean(model),
      format,
      previewable,
      hasMesh,
      urlReady: Boolean(url),
      failed,
      geometryStatus: model?.geometryStatus ?? null,
    })
  }, [modelId, model, format, previewable, hasMesh, url, failed])

  if (isLoading) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 'calc(100vh - 72px)' }}>
        <CircularProgress size={44} />
      </Box>
    )
  }

  if (isError) {
    return (
      <Box sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }}>
        <Alert severity="error">Không tải được thông tin model. Vui lòng thử lại.</Alert>
      </Box>
    )
  }

  if (!model) {
    return (
      <Box sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }}>
        <Alert severity="warning">Không tìm thấy model này trong thư viện của bạn.</Alert>
      </Box>
    )
  }

  return (
    <Box
      sx={{
        minHeight: 'calc(100vh - 72px)',
        height: { lg: 'calc(100vh - 72px)' },
        display: 'flex',
        flexDirection: 'column',
        bgcolor: 'background.default',
      }}
    >
      {/* ── Header ── */}
      <Stack
        direction="row"
        spacing={1.5}
        alignItems="flex-start"
        sx={{ px: { xs: 2.5, md: 4 }, pt: { xs: 2.5, md: 3 }, pb: 1.5, flexShrink: 0 }}
      >
        <PageBackButton to="/library" title="Về thư viện model" />
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="h1" sx={{ fontSize: '1.6rem', fontWeight: 800, color: 'text.primary', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {model.name}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {model.fileFormat} · Xoay để xem · Cuộn để thu phóng · Chuột phải để di chuyển
          </Typography>
        </Box>
      </Stack>

      <Box
        sx={{
          flex: { lg: 1 },
          minHeight: 0,
          display: 'flex',
          flexDirection: { xs: 'column', lg: 'row' },
          gap: 2,
          px: { xs: 2.5, md: 4 },
          pb: { xs: 2.5, md: 4 },
        }}
      >
        {/* ── Full-height 3D viewport ── */}
        <Box
          sx={{
            position: 'relative',
            flex: 1,
            height: { xs: 420, lg: 'auto' },
            minHeight: { xs: 420, lg: 0 },
            borderRadius: 3,
            overflow: 'hidden',
            border: `1px solid ${landing.hairline}`,
            bgcolor: '#050505',
          }}
        >
          {!hasMesh ? (
            <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} spacing={2} px={4}>
              <CircularProgress size={36} />
              <Typography color="text.secondary" align="center">
                Đang phân tích mô hình…
              </Typography>
            </Stack>
          ) : !previewable ? (
            <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} spacing={1} px={4}>
              <ThreeDRotationRounded sx={{ fontSize: 48, color: 'text.disabled' }} />
              <Typography color="text.secondary" align="center">
                Chưa xem trước được định dạng {model.fileFormat}. Kích thước và thể tích vẫn được đo chính xác ở bảng bên cạnh.
              </Typography>
            </Stack>
          ) : failed ? (
            <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} px={4}>
              <Typography color="text.secondary" align="center">
                Không tải được file để xem trước.
              </Typography>
            </Stack>
          ) : url ? (
            <ModelViewer url={url} format={format} fit height="100%" background="#050505" sx={{ bgcolor: '#050505' }} />
          ) : (
            <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} spacing={2}>
              <CircularProgress size={36} />
              <Typography color="text.secondary">Đang tải mô hình…</Typography>
            </Stack>
          )}

          {/* ── AC01: bounding-box legend matches the analysis numbers ── */}
          {model.geometryStatus === 'Ready' && (
          <Box
            sx={{
              position: 'absolute',
              left: 16,
              bottom: 16,
              px: 2,
              py: 1.25,
              borderRadius: 2,
              bgcolor: 'rgba(10,10,10,0.82)',
              border: `1px solid ${landing.hairline}`,
              backdropFilter: 'blur(6px)',
              pointerEvents: 'none',
            }}
          >
            <Typography sx={{ color: landing.text, fontWeight: 700, fontSize: '0.95rem' }}>
              Khổ in: {dimensions} mm
            </Typography>
          </Box>
          )}
        </Box>

        {/* ── Analysis panel ── */}
        <Paper
          sx={{
            width: { lg: 380 },
            flexShrink: 0,
            bgcolor: landing.card,
            borderColor: landing.hairline,
            overflowY: { lg: 'auto' },
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <Box sx={{ p: 3 }}>
            <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'rgba(255,255,255,0.5)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
              Phân tích hình học
            </Typography>

            {model.geometryStatus === 'Pending' && (
              <Typography color="text.secondary" sx={{ mt: 1.5 }}>
                Đang phân tích mô hình…
              </Typography>
            )}

            {model.geometryStatus === 'Failed' && (
              <Typography color="error.main" sx={{ mt: 1.5 }}>
                Không phân tích được: {model.geometryMessage ?? 'lỗi không xác định'}
              </Typography>
            )}

            {model.geometryStatus === 'Ready' && (
              <Box sx={{ mt: 1.5 }}>
                <Stack spacing={1.5}>
                  <Box>
                    <Typography variant="caption" color="text.disabled">Kích thước (X × Y × Z)</Typography>
                    <Typography color="text.primary" sx={{ fontSize: '1.35rem', fontWeight: 800 }}>
                      {dimensions} mm
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={3}>
                    <Box>
                      <Typography variant="caption" color="text.disabled">Thể tích</Typography>
                      <Typography color="text.primary" sx={{ fontSize: '0.95rem' }}>
                        {model.volumeCm3 != null ? `${model.volumeCm3.toFixed(1)} cm³` : '—'}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.disabled">Ước tính in</Typography>
                      <Typography color="text.primary" sx={{ fontSize: '0.95rem' }}>
                        {model.estimatedPrintMinutes != null ? `${model.estimatedPrintMinutes} phút` : '—'}
                        {model.estimatedMaterialGrams != null ? ` · ${model.estimatedMaterialGrams.toFixed(1)} g` : ''}
                      </Typography>
                    </Box>
                  </Stack>
                </Stack>

                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 2 }}>
                  <Chip size="small" color={model.isWatertight ? 'success' : 'error'} label={model.isWatertight ? 'Kín' : 'Không kín'} />
                  <Chip size="small" color={model.isManifold ? 'success' : 'error'} label={model.isManifold ? 'Manifold' : 'Non-manifold'} />
                  <Chip size="small" color={model.isPrintable === false ? 'error' : 'success'} label={model.isPrintable === false ? 'Không thể in' : 'Có thể in'} />
                </Stack>

                {model.isPrintable === false && (
                  <Typography color="error.main" sx={{ mt: 1.5, fontSize: '0.9rem' }}>
                    {model.geometryMessage ?? 'Model không thể in trong mạng lưới hiện tại.'}
                  </Typography>
                )}
              </Box>
            )}

            {model.description?.trim() && (
              <>
                <Divider sx={{ my: 2.5, borderColor: 'rgba(255,255,255,0.08)' }} />
                <Typography variant="caption" color="text.disabled">Mô tả</Typography>
                <Typography color="text.secondary" sx={{ mt: 0.5, whiteSpace: 'pre-wrap', fontSize: '0.9rem' }}>
                  {model.description}
                </Typography>
              </>
            )}
          </Box>

          <Box sx={{ mt: 'auto', p: 3, pt: 0 }}>
            <Button
              variant="contained"
              color="primary"
              fullWidth
              size="large"
              startIcon={<LocalShippingOutlined />}
              disabled={!canOrder}
              onClick={() => navigate(`/models/${model.id}/order`)}
            >
              Đặt in
            </Button>
            {!canOrder && model.geometryStatus === 'Ready' && model.isPrintable === false && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1, textAlign: 'center' }}>
                Model cần được sửa để có thể in.
              </Typography>
            )}
          </Box>
        </Paper>
      </Box>
    </Box>
  )
}
