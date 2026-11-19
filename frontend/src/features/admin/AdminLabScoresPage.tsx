import { useQuery } from '@tanstack/react-query'
import { Alert, Box, Chip, CircularProgress, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'

interface LabScoreExplanation {
  labId: string
  labName: string
  completedJobs: number
  totalAssignedJobs: number
  onTimeRate: number
  firstPassYield: number
  utilizationRate: number
  isProvisional: boolean
  smoothedOnTimeRate: number
  smoothedFirstPassYield: number
  provisionalLabel: string
  provisionalThreshold: number
}

function useLabScoreExplanations() {
  return useQuery({
    queryKey: ['admin', 'lab-scores'],
    queryFn: async () => (await apiClient.get<LabScoreExplanation[]>('/scheduling/lab-scores')).data,
  })
}

export function AdminLabScoresPage() {
  const query = useLabScoreExplanations()

  return (
    <Box>
      <Stack direction="row" spacing={1.5} alignItems="flex-start" sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h1">Điểm và giải thích chọn lab</Typography>
          <Typography color="text.secondary">
            Dữ liệu vận hành dùng để chấm điểm lab, bao gồm cả lab mới (điểm tạm thời)
          </Typography>
        </Box>
      </Stack>

      {query.isError && <Alert severity="error" sx={{ mb: 2 }}>Không tải được dữ liệu điểm lab.</Alert>}

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        {query.isLoading ? (
          <Box sx={{ p: 4, display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Lab</TableCell>
                <TableCell align="right">Hoàn thành</TableCell>
                <TableCell align="right">Đúng hạn (raw)</TableCell>
                <TableCell align="right">Đúng hạn (smoothed)</TableCell>
                <TableCell align="right">Chất lượng lần đầu (raw)</TableCell>
                <TableCell align="right">Chất lượng lần đầu (smoothed)</TableCell>
                <TableCell align="right">Tải máy</TableCell>
                <TableCell>Trạng thái</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(query.data ?? []).length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} sx={{ py: 5, textAlign: 'center', color: 'text.secondary' }}>
                    Chưa có lab nào được duyệt.
                  </TableCell>
                </TableRow>
              ) : (
                (query.data ?? []).map((lab) => (
                  <TableRow key={lab.labId} hover>
                    <TableCell sx={{ fontWeight: 700 }}>{lab.labName}</TableCell>
                    <TableCell align="right">{lab.completedJobs}/{lab.totalAssignedJobs}</TableCell>
                    <TableCell align="right">{(lab.onTimeRate * 100).toFixed(1)}%</TableCell>
                    <TableCell align="right">{(lab.smoothedOnTimeRate * 100).toFixed(1)}%</TableCell>
                    <TableCell align="right">{(lab.firstPassYield * 100).toFixed(1)}%</TableCell>
                    <TableCell align="right">{(lab.smoothedFirstPassYield * 100).toFixed(1)}%</TableCell>
                    <TableCell align="right">{(lab.utilizationRate * 100).toFixed(1)}%</TableCell>
                    <TableCell>
                      {lab.isProvisional ? (
                        <Chip label="Tạm thời" size="small" color="warning" />
                      ) : (
                        <Chip label="Chính thức" size="small" color="success" />
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        )}
      </Paper>
    </Box>
  )
}

export default AdminLabScoresPage
