import { Alert, Box, CircularProgress, Grid, Stack, Typography } from '@mui/material'
import { HubOutlined, PublicOutlined } from '@mui/icons-material'
import { formatNumber, useNetworkStats } from '../home/useNetworkStats'
import { landing } from '../../shared/theme/landing'
import { landingCardSx } from '../../shared/components/StatusChip'
import { EmptyState } from '../../shared/components/EmptyState'
import { PageHeading, PublicPage } from '../public/PublicLayout'

export default function NetworkPage() {
  const { data, isLoading, isError } = useNetworkStats()

  return (
    <PublicPage>
      <PageHeading
        eyebrow="Mạng lưới xưởng"
        title="Không phải một xưởng in. Là cả một mạng lưới."
        description="PrintGrid phân bổ đơn hàng cho các xưởng đối tác đã được duyệt, dựa trên lịch trống, vật liệu và thời gian giao thực tế. Số liệu dưới đây là tổng hợp trực tiếp từ hệ thống."
      />

      {isError && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: `${landing.radius}px` }}>
          Không tải được số liệu mạng lưới. Vui lòng thử lại sau.
        </Alert>
      )}

      {isLoading ? (
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 260 }}>
          <CircularProgress />
        </Box>
      ) : !data || data.totalLabs === 0 ? (
        <EmptyState
          icon={<HubOutlined />}
          title="Mạng lưới đang mở rộng"
          description="Chưa có xưởng đối tác nào được kích hoạt. Số liệu sẽ hiển thị ngay khi xưởng đầu tiên hoàn tất duyệt."
        />
      ) : (
        <>
          {/* ── Aggregate tiles ── */}
          <Grid container spacing={2}>
            <Grid size={{ xs: 6, md: 3 }}>
              <StatTile label="Xưởng hoạt động" value={formatNumber(data.activeLabs)} hint={`${formatNumber(data.totalLabs)} đã đăng ký`} />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <StatTile label="Máy in" value={formatNumber(data.totalMachines)} hint="Trong mạng lưới đang chạy" />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <StatTile label="Giao đúng hạn" value={`${data.averageOnTimeDeliveryRate}%`} hint="Trung bình 30 ngày" />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <StatTile label="Đạt lần đầu" value={`${data.averageFirstPassYield}%`} hint="Tỷ lệ QC đạt" />
            </Grid>
          </Grid>

          {/* ── Technology split ── */}
          <Box sx={{ ...landingCardSx, mt: 2.5, p: { xs: 2.5, md: 3 } }}>
            <Typography sx={{ color: landing.textMuted, fontSize: 12, letterSpacing: '0.12em', mb: 2 }}>
              CÔNG NGHỆ IN
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={{ xs: 2, sm: 4 }}>
              <TechnologyBar label="FDM" value={data.fdmMachines} total={data.totalMachines} />
              <TechnologyBar label="SLA / Resin" value={data.slaMachines} total={data.totalMachines} />
              <TechnologyBar label="SLS" value={data.slsMachines} total={data.totalMachines} />
            </Stack>
          </Box>

          {/* ── Regions (city-level only) ── */}
          {(data.regions ?? []).length > 0 && (
            <Box sx={{ mt: { xs: 6, md: 8 } }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'baseline' }} justifyContent="space-between" gap={1} sx={{ mb: 2.5 }}>
                <Typography component="h2" sx={{ color: landing.text, fontSize: '1.3rem', fontWeight: 600 }}>
                  Khu vực hoạt động
                </Typography>
                <Typography sx={{ color: landing.textMuted, fontSize: '0.84rem' }}>
                  Chỉ hiển thị cấp tỉnh/thành — không công bố tên hay địa chỉ xưởng.
                </Typography>
              </Stack>
              <Grid container spacing={2}>
                {(data.regions ?? []).map((region) => (
                  <Grid key={region.city} size={{ xs: 6, sm: 4, lg: 3 }}>
                    <Box sx={{ ...landingCardSx, p: 2.25 }}>
                      <Typography sx={{ color: landing.text, fontWeight: 600 }}>{region.city}</Typography>
                      <Typography sx={{ color: landing.textMuted, fontSize: '0.82rem', mt: 0.75 }}>
                        {region.labs} xưởng · {region.machines} máy in
                      </Typography>
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </Box>
          )}

          {/* ── Explainers ── */}
          <Box sx={{ mt: { xs: 6, md: 8 } }}>
            <Typography component="h2" sx={{ color: landing.text, fontSize: '1.3rem', fontWeight: 600, mb: 2.5 }}>
              Chúng tôi chọn xưởng thế nào
            </Typography>
            <Grid container spacing={2.5}>
              <Grid size={{ xs: 12, md: 4 }}>
                <ExplainCard
                  step="01"
                  title="Lọc theo khả năng"
                  body="Công nghệ, vật liệu, màu và khổ in phải đáp ứng đúng cấu hình bạn chọn."
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <ExplainCard
                  step="02"
                  title="Xếp theo lịch trống"
                  body="Hệ thống xếp lịch cho từng máy để chọn ra phương án giao sớm nhất mà vẫn đúng hạn."
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <ExplainCard
                  step="03"
                  title="Chấm điểm và chốt"
                  body="Thời gian, tải máy, chất lượng, chi phí và logistics được cân theo trọng số đã công bố."
                />
              </Grid>
            </Grid>

            <Box
              sx={{
                ...landingCardSx,
                mt: 2.5,
                p: { xs: 2.5, md: 3 },
                display: 'flex',
                gap: 2,
                alignItems: 'flex-start',
              }}
            >
              <PublicOutlined sx={{ color: landing.textMuted, mt: 0.25 }} />
              <Box>
                <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.5 }}>Xưởng được giữ ẩn danh</Typography>
                <Typography sx={{ color: landing.textMuted, fontSize: '0.87rem', lineHeight: 1.65 }}>
                  Bạn làm việc với PrintGrid, không phải với từng xưởng. Điều này giữ giá công bằng trên toàn mạng lưới
                  và bảo vệ quan hệ đối tác — kể cả trong bản demo, trang này không bao giờ trả về danh tính một xưởng.
                </Typography>
              </Box>
            </Box>
          </Box>
        </>
      )}
    </PublicPage>
  )
}

function StatTile({ label, value, hint }: { label: string; value: string; hint: string }) {
  return (
    <Box sx={{ ...landingCardSx, p: { xs: 2, md: 2.5 }, height: '100%' }}>
      <Typography sx={{ color: landing.textMuted, fontSize: 12, letterSpacing: '0.08em', textTransform: 'uppercase' }}>
        {label}
      </Typography>
      <Typography sx={{ color: landing.text, fontSize: { xs: '1.6rem', md: '2rem' }, fontWeight: 700, mt: 0.75, lineHeight: 1.1 }}>
        {value}
      </Typography>
      <Typography sx={{ color: landing.textMuted, fontSize: '0.76rem', mt: 0.75 }}>{hint}</Typography>
    </Box>
  )
}

function TechnologyBar({ label, value, total }: { label: string; value: number; total: number }) {
  const percent = total === 0 ? 0 : Math.round((value / total) * 100)
  return (
    <Box sx={{ flex: 1, minWidth: 140 }}>
      <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.75 }}>
        <Typography sx={{ color: landing.text, fontSize: '0.88rem', fontWeight: 600 }}>{label}</Typography>
        <Typography sx={{ color: landing.textMuted, fontSize: '0.82rem' }}>
          {value} máy · {percent}%
        </Typography>
      </Stack>
      <Box sx={{ height: 6, borderRadius: 999, bgcolor: landing.surface, overflow: 'hidden' }}>
        <Box sx={{ height: '100%', width: `${percent}%`, bgcolor: landing.text, borderRadius: 999 }} />
      </Box>
    </Box>
  )
}

function ExplainCard({ step, title, body }: { step: string; title: string; body: string }) {
  return (
    <Box sx={{ ...landingCardSx, p: 2.75, height: '100%' }}>
      <Typography sx={{ color: landing.textMuted, fontSize: 12, letterSpacing: '0.12em', mb: 1.25 }}>{step}</Typography>
      <Typography sx={{ color: landing.text, fontWeight: 600, mb: 0.75 }}>{title}</Typography>
      <Typography sx={{ color: landing.textMuted, fontSize: '0.87rem', lineHeight: 1.6 }}>{body}</Typography>
    </Box>
  )
}
