import { useMemo } from 'react'
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Grid,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import {
  CategoryRounded,
  FactCheckRounded,
  HistoryRounded,
  InsightsRounded,
  PeopleRounded,
  StorefrontRounded,
  TuneRounded,
} from '@mui/icons-material'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAdminUsers } from './useAdminUsers'
import { useCatalogItems, useCommercialConfigurations } from './useAdminConfiguration'
import { useDefectTaxonomy, useInspectionChecklists } from './useQcKnowledge'
import { useLabs } from '../ops/useLabs'

interface OverviewCardProps {
  title: string
  value: ReactNode
  hint: string
  icon: ReactNode
  href: string
  tone?: 'primary' | 'warning' | 'success'
}

function OverviewCard({ title, value, hint, icon, href, tone = 'primary' }: OverviewCardProps) {
  const navigate = useNavigate()
  const color = tone === 'warning' ? 'warning.main' : tone === 'success' ? 'success.main' : 'primary.main'

  return (
    <Paper
      variant="outlined"
      onClick={() => navigate(href)}
      role="link"
      tabIndex={0}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') navigate(href)
      }}
      sx={{
        p: 2.5,
        height: '100%',
        cursor: 'pointer',
        transition: 'border-color .15s, transform .15s',
        '&:hover': { borderColor: color, transform: 'translateY(-2px)' },
      }}
    >
      <Stack direction="row" alignItems="center" spacing={1.25}>
        <Box sx={{ color }}>{icon}</Box>
        <Typography sx={{ fontSize: '0.78rem', fontWeight: 600, color: 'text.secondary' }}>
          {title}
        </Typography>
      </Stack>
      <Box sx={{ mt: 1.5, fontSize: '1.9rem', fontWeight: 800, lineHeight: 1.1 }}>
        {value}
      </Box>
      <Typography sx={{ mt: 0.5, fontSize: '0.8rem', color: 'text.secondary' }}>{hint}</Typography>
    </Paper>
  )
}

/**
 * Admin landing screen: one card per administration surface, each showing the number the
 * backend already exposes so the operator can see whether anything needs attention before
 * opening a section.
 */
export function AdminOverviewPage() {
  const users = useAdminUsers()
  const labs = useLabs(true)
  const commercial = useCommercialConfigurations()
  const catalog = useCatalogItems()
  const checklists = useInspectionChecklists()
  const defects = useDefectTaxonomy()

  const loading =
    users.isLoading || labs.isLoading || commercial.isLoading || catalog.isLoading ||
    checklists.isLoading || defects.isLoading

  const pendingLabs = useMemo(
    () => (labs.data ?? []).filter((lab) => lab.status === 'Pending').length,
    [labs.data],
  )
  const activeUsers = useMemo(() => (users.data ?? []).filter((user) => user.isActive).length, [users.data])
  const activeConfiguration = useMemo(
    () => (commercial.data ?? []).find((config) => config.status === 'Active'),
    [commercial.data],
  )

  return (
    <Box>
      <Stack direction="row" alignItems="flex-end" justifyContent="space-between" spacing={2} sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>
            Tổng quan quản trị
          </Typography>
          <Typography color="text.secondary">
            Cấu hình, xưởng, kho tri thức QC và nhật ký kiểm toán trong một chỗ
          </Typography>
        </Box>
        {loading ? <CircularProgress size={22} /> : null}
      </Stack>

      {users.isError || labs.isError ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Một số số liệu chưa tải được (kiểm tra kết nối API). Các mục bên dưới vẫn mở được.
        </Alert>
      ) : null}

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Người dùng"
            value={users.isLoading ? '…' : String(users.data?.length ?? 0)}
            hint={`${activeUsers} đang hoạt động`}
            icon={<PeopleRounded />}
            href="/admin/users"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Xưởng chờ duyệt"
            value={labs.isLoading ? '…' : String(pendingLabs)}
            hint="Hồ sơ xưởng cần Admin/Ops duyệt"
            icon={<StorefrontRounded />}
            href="/admin/labs"
            tone={pendingLabs > 0 ? 'warning' : 'success'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Cấu hình thương mại"
            value={activeConfiguration ? activeConfiguration.name : '—'}
            hint={activeConfiguration ? `Phiên bản ${activeConfiguration.versionNumber}` : 'Chưa có bản đang áp dụng'}
            icon={<TuneRounded />}
            href="/admin/commercial-configurations"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Catalogue dùng chung"
            value={catalog.isLoading ? '…' : String(catalog.data?.length ?? 0)}
            hint="Vật liệu, màu, định mức đang công bố"
            icon={<CategoryRounded />}
            href="/admin/catalogs"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Bộ checklist QC"
            value={checklists.isLoading ? '…' : String(checklists.data?.length ?? 0)}
            hint="Phiên bản theo hạng chất lượng"
            icon={<FactCheckRounded />}
            href="/admin/qc-knowledge"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Mã lỗi (defect taxonomy)"
            value={defects.isLoading ? '…' : String(defects.data?.length ?? 0)}
            hint="Mã lỗi dùng khi nghiệm thu hub"
            icon={<FactCheckRounded />}
            href="/admin/qc-knowledge"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Trọng số chấm điểm"
            value={<Chip label="Kỳ cấu hình" size="small" color="primary" variant="outlined" />}
            hint="Phiên bản đang dùng cho engine gán việc"
            icon={<InsightsRounded />}
            href="/admin/scoring-configurations"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <OverviewCard
            title="Nhật ký kiểm toán"
            value="Xem"
            hint="Ai đã làm gì, kèm mã yêu cầu"
            icon={<HistoryRounded />}
            href="/admin/audit-logs"
          />
        </Grid>
      </Grid>
    </Box>
  )
}

export default AdminOverviewPage
