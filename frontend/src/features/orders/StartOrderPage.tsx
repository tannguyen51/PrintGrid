import { useMemo, useState } from 'react'
import { Box, Button, CircularProgress, Grid, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { AddPhotoAlternateRounded, LibraryBooksRounded } from '@mui/icons-material'
import { CustomerNavbar } from '../home/components/CustomerNavbar'
import { landing, PillButton } from '../../shared/theme/landing'
import { StatusChip, landingCardSx } from '../../shared/components/StatusChip'
import { EmptyState } from '../../shared/components/EmptyState'
import { useModels, useUploadModel } from '../models/useModels'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { ModelFormDialog } from '../models/components/ModelFormDialog'
import type { ModelFormValues } from '../models/modelSchema'
import type { ThreeDModel } from '../models/modelTypes'

/**
 * /order/new — the "Đặt in" entry point. Two doors, same destination:
 * pick a Ready model from the library, or upload a new file (which lands
 * straight in the config wizard). Sample library will be a third door once
 * GET /samples exists (FR-CUST-014, plan Part A §A7).
 */

function formatSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${bytes} B`
}

function geometrySummary(m: ThreeDModel): string | null {
  const dims = [m.boundingWidthMm, m.boundingDepthMm, m.boundingHeightMm]
    .filter((v): v is number => v != null)
    .map((v) => v.toFixed(0))
    .join(' × ')
  if (!dims) return null
  const vol = m.volumeCm3 != null ? ` · ${m.volumeCm3.toFixed(1)} cm³` : ''
  const mins = m.estimatedPrintMinutes != null ? ` · ~${m.estimatedPrintMinutes} phút` : ''
  return `${dims} mm${vol}${mins}`
}

function ModelPickCard({ model, onPick }: { model: ThreeDModel; onPick: (m: ThreeDModel) => void }) {
  const ready = model.geometryStatus === 'Ready'
  const summary = geometrySummary(model)
  return (
    <Box sx={{ ...landingCardSx, p: 2, height: '100%', display: 'flex', flexDirection: 'column', gap: 1.25, transition: 'border-color .18s ease', '&:hover': { borderColor: '#303036' } }}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1} sx={{ minWidth: 0 }}>
        <Typography sx={{ fontWeight: 600, color: landing.text, minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {model.name}
        </Typography>
        <StatusChip kind="geometry" value={model.geometryStatus} />
      </Stack>
      <Typography sx={{ fontSize: '0.82rem', color: landing.textMuted }}>
        {model.fileFormat} · {formatSize(model.sizeBytes)}
        {summary ? ` · ${summary}` : ''}
      </Typography>
      <Box sx={{ mt: 'auto' }}>
        <PillButton size="small" disabled={!ready} onClick={() => onPick(model)} sx={{ height: 40, px: 2 }}>
          {ready ? 'Cấu hình in →' : 'Chờ phân tích'}
        </PillButton>
      </Box>
    </Box>
  )
}

export default function StartOrderPage() {
  const navigate = useNavigate()
  const [uploadOpen, setUploadOpen] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

  const { data, isLoading, isError } = useModels('')
  const uploadMutation = useUploadModel()

  const models = useMemo(() => data ?? [], [data])
  // Ready first, then the rest by recency — the ones you can actually order.
  const ordered = useMemo(
    () => [...models].sort((a, b) => (a.geometryStatus === 'Ready' ? -1 : 0) - (b.geometryStatus === 'Ready' ? -1 : 0)),
    [models],
  )

  const pick = (m: ThreeDModel) => navigate(`/models/${m.id}/order`)

  async function handleUploadSubmit(values: ModelFormValues, file?: File) {
    setUploadError(null)
    if (!file) {
      setUploadError('Chọn file .stl / .obj / .3mf / .glb để tải lên.')
      return
    }
    try {
      const created = await uploadMutation.mutateAsync({
        name: values.name,
        description: values.description?.trim() || undefined,
        tags: (values.tags ?? '').split(',').map((t) => t.trim()).filter(Boolean),
        file,
      })
      setUploadOpen(false)
      navigate(`/models/${created.id}/order`)
    } catch (error) {
      setUploadError(getApiErrorMessage(error, 'Không tải được file lên. Kiểm tra kết nối rồi thử lại.'))
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: landing.pageBg }}>
      <CustomerNavbar />

      {/* Hero band — same gradient language as the landing hero */}
      <Box sx={{ background: landing.heroGradient, borderBottom: `1px solid ${landing.hairline}` }}>
        <Stack sx={{ maxWidth: 1200, mx: 'auto', px: { xs: 2.5, md: 4 }, py: { xs: 5, md: 7 } }} spacing={1.25}>
          <Typography sx={{ fontSize: '0.72rem', fontWeight: 600, letterSpacing: '0.16em', textTransform: 'uppercase', color: landing.textMuted }}>
            PrintGrid · Đặt in
          </Typography>
          <Typography variant="h1" sx={{ fontWeight: 800 }}>
            Bắt đầu một đơn in
          </Typography>
          <Typography sx={{ color: landing.textMuted, maxWidth: '62ch' }}>
            Chọn model có sẵn trong thư viện hoặc tải file 3D mới lên. Sau bước này bạn sẽ chọn vật liệu,
            nhận báo giá và ngày giao cam kết.
          </Typography>
        </Stack>
      </Box>

      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }} spacing={4}>
        {/* Two doors */}
        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 7 }}>
            <Box sx={{ ...landingCardSx, p: 3, height: '100%' }}>
              <Stack direction="row" alignItems="center" spacing={1.25} sx={{ mb: 2.5 }}>
                <LibraryBooksRounded sx={{ color: landing.textMuted }} />
                <Box sx={{ minWidth: 0 }}>
                  <Typography sx={{ fontWeight: 700, color: landing.text }}>Chọn từ thư viện</Typography>
                  <Typography sx={{ fontSize: '0.85rem', color: landing.textMuted }}>
                    Model đã phân tích xong thì đặt được ngay
                  </Typography>
                </Box>
              </Stack>

              {isLoading ? (
                <Box sx={{ display: 'grid', placeItems: 'center', py: 5 }}>
                  <CircularProgress size={36} />
                </Box>
              ) : isError ? (
                <Typography sx={{ color: 'error.main', py: 2 }}>Không tải được danh sách model.</Typography>
              ) : models.length === 0 ? (
                <EmptyState
                  icon={<LibraryBooksRounded />}
                  title="Thư viện đang trống"
                  description="Tải một file 3D lên để bắt đầu — hoặc dùng cửa bên cạnh."
                  action={
                    <PillButton onClick={() => setUploadOpen(true)} startIcon={<AddPhotoAlternateRounded />}>
                      Tải file lên
                    </PillButton>
                  }
                />
              ) : (
                <>
                  <Grid container spacing={2}>
                    {ordered.slice(0, 6).map((m) => (
                      <Grid key={m.id} size={{ xs: 12, sm: 6 }}>
                        <ModelPickCard model={m} onPick={pick} />
                      </Grid>
                    ))}
                  </Grid>
                  {models.length > 6 && (
                    <Button onClick={() => navigate('/models')} sx={{ mt: 2, color: landing.textMuted, textTransform: 'none', '&:hover': { color: landing.text } }}>
                      Xem tất cả {models.length} model →
                    </Button>
                  )}
                </>
              )}
            </Box>
          </Grid>

          <Grid size={{ xs: 12, md: 5 }}>
            <Box sx={{ ...landingCardSx, p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Stack direction="row" alignItems="center" spacing={1.25} sx={{ mb: 2.5 }}>
                <AddPhotoAlternateRounded sx={{ color: landing.textMuted }} />
                <Box sx={{ minWidth: 0 }}>
                  <Typography sx={{ fontWeight: 700, color: landing.text }}>Tải file 3D mới</Typography>
                  <Typography sx={{ fontSize: '0.85rem', color: landing.textMuted }}>
                    STL · OBJ · 3MF · GLB — tối đa 50 MB
                  </Typography>
                </Box>
              </Stack>
              <Typography sx={{ fontSize: '0.88rem', color: landing.textMuted, mb: 3 }}>
                File được kiểm tra tính toàn vẹn của lưới và đo kích thước thật trong vài phút. Bạn nhận được
                email xác nhận kèm mã băm SHA-256 của file.
              </Typography>
              <Box sx={{ mt: 'auto' }}>
                <Button
                  variant="contained"
                  color="primary"
                  fullWidth
                  sx={{ height: 48, borderRadius: `${landing.radius}px`, textTransform: 'none', fontWeight: 600 }}
                  startIcon={<AddPhotoAlternateRounded />}
                  onClick={() => { setUploadError(null); setUploadOpen(true) }}
                >
                  Chọn file để tải lên
                </Button>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </Stack>

      <ModelFormDialog
        key={uploadOpen ? 'new' : 'closed'}
        open={uploadOpen}
        onClose={() => setUploadOpen(false)}
        editing={null}
        onSubmit={handleUploadSubmit}
        submitting={uploadMutation.isPending}
        error={uploadError}
      />
    </Box>
  )
}
