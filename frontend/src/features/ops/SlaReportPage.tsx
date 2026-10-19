import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { DownloadRounded, InsightsRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useExportSlaReport, useSlaReport, type SlaLabRow } from './useSlaReport'

const pct = (value: number) => `${(value * 100).toFixed(1)}%`

const rateColor = (value: number) =>
  value >= 0.9 ? 'success.main' : value >= 0.75 ? 'warning.main' : 'error.main'

function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Box sx={{ p: 2, borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography sx={{ fontSize: '1.5rem', fontWeight: 800, color: 'text.primary' }}>{value}</Typography>
      {hint && <Typography variant="caption" color="text.disabled">{hint}</Typography>}
    </Box>
  )
}

/**
 * FR-ANAL-002/003 — network SLA report over the BR-PERF-001 90-day window.
 * The network row is an aggregate of the underlying jobs (not an average of lab rates).
 */
export default function SlaReportPage() {
  const { data, isLoading, isError, error } = useSlaReport()
  const exportCsv = useExportSlaReport()

  const network = data?.network
  const labs = data?.labs ?? []

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Báo cáo SLA mạng lưới
              </Typography>
              <Typography color="text.secondary">
                Cửa sổ trượt {data?.windowDays ?? 90} ngày — đúng hạn, đạt lần đầu, tỷ lệ chấp nhận và sử dụng công suất của từng xưởng.
              </Typography>
            </Box>
          </Stack>
          <Button
            variant="contained"
            startIcon={<DownloadRounded />}
            disabled={exportCsv.isPending || isLoading}
            onClick={() => exportCsv.mutate(undefined)}
          >
            {exportCsv.isPending ? 'Đang xuất…' : 'Xuất CSV'}
          </Button>
        </Stack>

        {isError && (
          <Alert severity="error">
            Không tải được báo cáo SLA. Vui lòng thử lại sau.
            {error instanceof Error ? ` (${error.message})` : ''}
          </Alert>
        )}
        {exportCsv.isError && <Alert severity="error">Không xuất được tệp CSV.</Alert>}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center' }} />
        ) : !data ? (
          <Alert severity="info">Không có dữ liệu báo cáo.</Alert>
        ) : (
          <>
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.25 }}>
                <InsightsRounded sx={{ color: 'primary.main' }} />
                <Typography sx={{ fontWeight: 800, color: 'text.primary' }}>
                  Toàn mạng lưới
                </Typography>
                <Typography variant="caption" color="text.disabled">
                  {new Date(data.windowStartUtc).toLocaleDateString('vi-VN')} – {new Date(data.windowEndUtc).toLocaleDateString('vi-VN')}
                </Typography>
              </Stack>
              <Grid container spacing={1.5}>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Đúng hạn" value={pct(network!.onTimeDeliveryRate)} hint={`${network!.lateJobs} job trễ`} />
                </Grid>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Đạt lần đầu (FPY)" value={pct(network!.firstPassYield)} />
                </Grid>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Tỷ lệ chấp nhận" value={pct(network!.acceptanceRate)} />
                </Grid>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Sử dụng công suất" value={pct(network!.utilizationRate)} hint="7 ngày gần nhất" />
                </Grid>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Job hoàn thành" value={network!.completedJobs.toString()} />
                </Grid>
                <Grid size={{ xs: 6, md: 2 }}>
                  <StatCard label="Job được giao" value={network!.assignedJobs.toString()} />
                </Grid>
              </Grid>
            </Box>

            {labs.length === 0 ? (
              <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
                <Typography color="text.secondary">
                  Không có xưởng nào trong mạng lưới — tệp CSV sẽ ghi “no data”.
                </Typography>
              </Box>
            ) : (
              <TableContainer sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Xưởng</TableCell>
                      <TableCell align="right">Đúng hạn</TableCell>
                      <TableCell align="right">FPY</TableCell>
                      <TableCell align="right">Chấp nhận</TableCell>
                      <TableCell align="right">Công suất</TableCell>
                      <TableCell align="right">Hoàn thành</TableCell>
                      <TableCell align="right">Được giao</TableCell>
                      <TableCell align="right">Trễ</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {labs.map((row) => (
                      <LabRow key={row.labId ?? row.labName} row={row} />
                    ))}
                    <TableRow sx={{ bgcolor: 'rgba(139,92,246,0.08)' }}>
                      <TableCell sx={{ fontWeight: 800 }}>
                        {data.network.labName}
                        <Typography component="span" variant="caption" color="text.disabled" sx={{ ml: 1 }}>
                          tổng hợp
                        </Typography>
                      </TableCell>
                      <RateCell value={data.network.onTimeDeliveryRate} />
                      <RateCell value={data.network.firstPassYield} />
                      <RateCell value={data.network.acceptanceRate} />
                      <RateCell value={data.network.utilizationRate} />
                      <TableCell align="right">{data.network.completedJobs}</TableCell>
                      <TableCell align="right">{data.network.assignedJobs}</TableCell>
                      <TableCell align="right">{data.network.lateJobs}</TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </>
        )}
      </Stack>
    </Box>
  )
}

function LabRow({ row }: { row: SlaLabRow }) {
  return (
    <TableRow hover>
      <TableCell>
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography sx={{ fontWeight: 600 }}>{row.labName}</Typography>
          {row.isProvisional && (
            <Chip size="small" variant="outlined" label="Tạm tính (<10 job)" />
          )}
        </Stack>
      </TableCell>
      <RateCell value={row.onTimeDeliveryRate} />
      <RateCell value={row.firstPassYield} />
      <RateCell value={row.acceptanceRate} />
      <RateCell value={row.utilizationRate} />
      <TableCell align="right">{row.completedJobs}</TableCell>
      <TableCell align="right">{row.assignedJobs}</TableCell>
      <TableCell align="right" sx={{ color: row.lateJobs > 0 ? 'error.main' : 'text.primary' }}>
        {row.lateJobs}
      </TableCell>
    </TableRow>
  )
}

function RateCell({ value }: { value: number }) {
  return (
    <TableCell align="right" sx={{ color: rateColor(value), fontWeight: 700 }}>
      {pct(value)}
    </TableCell>
  )
}
