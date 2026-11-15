import type { ReactNode } from 'react'
import { Alert, Avatar, Box, Button, Chip, Grid, Paper, Skeleton, Stack, Typography } from '@mui/material'
import {
  ArrowForwardRounded, CategoryRounded, CheckCircleOutlineRounded, ChevronRightRounded,
  FactCheckRounded, HistoryRounded, InsightsRounded, PeopleOutlineRounded,
  RefreshRounded, ScheduleRounded, StorefrontOutlined, TuneRounded, ViewInArRounded,
} from '@mui/icons-material'
import { Link } from 'react-router-dom'
import { useAdminUsers } from './useAdminUsers'
import { useCatalogItems, useCommercialConfigurations } from './useAdminConfiguration'
import { useDefectTaxonomy, useInspectionChecklists } from './useQcKnowledge'
import { useAdminSamples } from './useAdminSamples'
import { useAuditLogs } from './useAuditLogs'
import { useLabs } from '../ops/useLabs'

const number = (value: number) => value.toLocaleString('vi-VN')
const date = (value: string) => new Date(value).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })

function Metric({ title, value, hint, icon, href, loading, unavailable, warning = false }: {
  title: string; value: number; hint: string; icon: ReactNode; href: string
  loading: boolean; unavailable: boolean; warning?: boolean
}) {
  return <Link to={href} className="admin-metric">
    <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
      <Typography sx={{ fontSize: 12, color: 'text.secondary', fontWeight: 600 }}>{title}</Typography>
      <Box sx={{ width: 34, height: 34, borderRadius: '9px', bgcolor: warning ? '#FFF5E6' : '#F3F1FC', color: warning ? '#996014' : '#7562BE', display: 'grid', placeItems: 'center', '& svg': { fontSize: 19 } }}>{icon}</Box>
    </Stack>
    <Box className="admin-metric-value">{loading ? <Skeleton width={70} /> : unavailable ? '—' : number(value)}</Box>
    <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
      <Typography sx={{ fontSize: 11, color: warning ? '#996014' : 'text.secondary' }}>{unavailable ? 'Chưa tải được dữ liệu' : loading ? 'Đang tải dữ liệu…' : hint}</Typography>
      <ArrowForwardRounded sx={{ fontSize: 15, color: '#9299AB' }} />
    </Stack>
  </Link>
}

function PanelHeader({ title, subtitle, href, action = 'Xem tất cả' }: { title: string; subtitle: string; href: string; action?: string }) {
  return <Box className="admin-panel-header"><Box><h2>{title}</h2><p>{subtitle}</p></Box>
    <Button component={Link} to={href} size="small" endIcon={<ArrowForwardRounded sx={{ fontSize: '14px !important' }} />}>{action}</Button>
  </Box>
}

function EmptyPanel({ icon, title, description }: { icon: ReactNode; title: string; description: string }) {
  return <Stack spacing={1} alignItems="center" justifyContent="center" sx={{ px: 3, py: 4, textAlign: 'center', minHeight: 190 }}>
    <Box sx={{ color: '#8D83B0', '& svg': { fontSize: 30 } }}>{icon}</Box>
    <Typography sx={{ fontSize: 13, fontWeight: 600 }}>{title}</Typography>
    <Typography variant="body2" color="text.secondary">{description}</Typography>
  </Stack>
}

export function AdminOverviewPage() {
  const users = useAdminUsers()
  const labs = useLabs(true)
  const commercial = useCommercialConfigurations()
  const catalog = useCatalogItems()
  const checklists = useInspectionChecklists()
  const defects = useDefectTaxonomy()
  const samples = useAdminSamples()
  const activity = useAuditLogs({ actor: '', actionClass: '', fromUtc: '', toUtc: '', page: 0, pageSize: 4 })
  const queries = [users, labs, commercial, catalog, checklists, defects, samples, activity]
  const fetching = queries.some((query) => query.isFetching)
  const pendingLabs = (labs.data ?? []).filter((lab) => lab.status === 'Pending')
  const activeLabs = (labs.data ?? []).filter((lab) => lab.status === 'Active').length
  const activeUsers = (users.data ?? []).filter((user) => user.isActive).length
  const publishedSamples = (samples.data ?? []).filter((sample) => sample.isPublished).length
  const activeConfiguration = commercial.data?.find((config) => config.status === 'Active')
  const resources = [
    { label: 'Danh mục dùng chung', hint: 'Vật liệu, màu sắc & công nghệ', count: catalog.data?.length, loading: catalog.isLoading, icon: <CategoryRounded />, href: '/admin/catalogs' },
    { label: 'Bộ kiểm chất lượng', hint: 'Tiêu chuẩn nghiệm thu theo cấp', count: checklists.data?.length, loading: checklists.isLoading, icon: <FactCheckRounded />, href: '/admin/qc-knowledge' },
    { label: 'Danh mục lỗi QC', hint: 'Mã lỗi dùng tại các Hub', count: defects.data?.length, loading: defects.isLoading, icon: <FactCheckRounded />, href: '/admin/qc-knowledge' },
    { label: 'Trọng số chấm điểm', hint: 'Cấu hình thuật toán gán việc', count: undefined, loading: false, icon: <InsightsRounded />, href: '/admin/scoring-configurations' },
  ]

  return <Stack spacing={3}>
    <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} gap={2}>
      <Box><div className="admin-eyebrow">Bảng điều khiển</div><Typography variant="h1">Tổng quan hệ thống</Typography>
        <Typography color="text.secondary" sx={{ mt: 0.75 }}>Theo dõi mạng lưới, xử lý phê duyệt và quản lý cấu hình PrintGrid.</Typography></Box>
      <Button variant="outlined" disabled={fetching} onClick={() => { void Promise.all(queries.map((query) => query.refetch())) }}
        startIcon={<RefreshRounded className={fetching ? 'admin-refreshing' : undefined} />}>{fetching ? 'Đang cập nhật…' : 'Làm mới dữ liệu'}</Button>
    </Stack>

    {queries.some((query) => query.isError) && <Alert severity="warning">Một số dữ liệu chưa tải được. Chọn “Làm mới dữ liệu” để thử lại; bạn vẫn có thể truy cập các trang quản trị.</Alert>}

    <Box>
      <Typography sx={{ mb: 1.5, fontSize: 11, color: 'text.secondary' }}>Số liệu hiện tại trên toàn hệ thống</Typography>
      <Grid container spacing={2}>
        <Grid size={{ xs: 6, xl: 3, sm: 6, lg: 3 }}><Metric title="Tổng người dùng" value={users.data?.length ?? 0} hint={`${number(activeUsers)} tài khoản hoạt động`} icon={<PeopleOutlineRounded />} href="/admin/users" loading={users.isLoading} unavailable={!users.data && users.isError} /></Grid>
        <Grid size={{ xs: 6, xl: 3, sm: 6, lg: 3 }}><Metric title="Xưởng hoạt động" value={activeLabs} hint={`Trong ${number(labs.data?.length ?? 0)} xưởng đăng ký`} icon={<StorefrontOutlined />} href="/admin/labs" loading={labs.isLoading} unavailable={!labs.data && labs.isError} /></Grid>
        <Grid size={{ xs: 6, xl: 3, sm: 6, lg: 3 }}><Metric title="Kho mẫu 3D" value={samples.data?.length ?? 0} hint={`${number(publishedSamples)} mẫu đang công khai`} icon={<ViewInArRounded />} href="/admin/samples" loading={samples.isLoading} unavailable={!samples.data && samples.isError} /></Grid>
        <Grid size={{ xs: 6, xl: 3, sm: 6, lg: 3 }}><Metric title="Xưởng chờ duyệt" value={pendingLabs.length} hint="Hồ sơ cần được xem xét" icon={<ScheduleRounded />} href="/admin/labs" loading={labs.isLoading} unavailable={!labs.data && labs.isError} warning={pendingLabs.length > 0} /></Grid>
      </Grid>
    </Box>

    <Box className="admin-dashboard-grid">
      <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
        <PanelHeader title="Cần xử lý" subtitle="Hồ sơ xưởng đang chờ phê duyệt kích hoạt" href="/admin/labs" />
        {labs.isLoading ? <Box sx={{ p: 3 }}><Skeleton height={48} /><Skeleton height={48} /><Skeleton height={48} /></Box>
          : labs.isError && !labs.data ? <EmptyPanel icon={<StorefrontOutlined />} title="Chưa tải được hồ sơ xưởng" description="Thử làm mới dữ liệu để kiểm tra các hồ sơ đang chờ." />
          : pendingLabs.length === 0 ? <EmptyPanel icon={<CheckCircleOutlineRounded />} title="Không có xưởng chờ duyệt" description="Hồ sơ đăng ký mới sẽ xuất hiện tại đây để bạn xem xét." />
          : pendingLabs.slice(0, 3).map((lab) => <Link to="/admin/labs" key={lab.id} className="admin-list-link">
            <Avatar variant="rounded" sx={{ width: 38, height: 38, bgcolor: '#F4F2FA', color: '#7C6C9D' }}><StorefrontOutlined sx={{ fontSize: 20 }} /></Avatar>
            <Box sx={{ flex: 1, minWidth: 0 }}><Typography noWrap sx={{ fontSize: 13, fontWeight: 650 }}>{lab.name}</Typography><Typography sx={{ fontSize: 11, color: 'text.secondary', mt: 0.25 }}>{lab.city} · {lab.machines.length} máy in</Typography></Box>
            <Chip label="Chờ duyệt" color="warning" size="small" /><ChevronRightRounded sx={{ fontSize: 17, color: '#9299AB' }} />
          </Link>)}
        {pendingLabs.length > 3 && <Box sx={{ px: 2.75, py: 1.5 }}><Typography variant="caption" color="text.secondary">Còn {pendingLabs.length - 3} hồ sơ khác trong danh sách duyệt xưởng.</Typography></Box>}
      </Paper>

      <Paper variant="outlined" sx={{ display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Box className="admin-panel-header"><Box><h2>Cấu hình thương mại</h2><p>Phiên bản áp dụng cho báo giá mới</p></Box><TuneRounded sx={{ fontSize: 20, color: '#8A7AAF' }} /></Box>
        <Box sx={{ p: 2.75, flex: 1 }}>
          {commercial.isLoading ? <><Skeleton width="40%" /><Skeleton height={40} /><Skeleton /></>
            : commercial.isError && !commercial.data ? <Typography color="text.secondary">Chưa tải được cấu hình thương mại.</Typography>
            : activeConfiguration ? <>
              <Chip color="success" size="small" label="Đang áp dụng" icon={<CheckCircleOutlineRounded />} />
              <Typography sx={{ mt: 2, fontWeight: 700, fontSize: 18, overflowWrap: 'anywhere' }}>{activeConfiguration.name}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>Phiên bản {activeConfiguration.versionNumber}{activeConfiguration.activatedAtUtc ? ` · Áp dụng từ ${date(activeConfiguration.activatedAtUtc)}` : ''}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>{activeConfiguration.changeSummary}</Typography>
            </> : <><Typography fontWeight={650}>Chưa có cấu hình đang áp dụng</Typography><Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>Tạo và kích hoạt một phiên bản để thiết lập chính sách báo giá.</Typography></>}
        </Box>
        <Box sx={{ px: 2.75, pb: 2.5 }}><Button component={Link} to="/admin/commercial-configurations" variant="outlined" fullWidth endIcon={<ArrowForwardRounded />}>Quản lý cấu hình</Button></Box>
      </Paper>

      <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
        <PanelHeader title="Hoạt động gần đây" subtitle="Các sự kiện mới nhất từ nhật ký kiểm toán" href="/admin/audit-logs" action="Xem nhật ký" />
        {activity.isLoading ? <Box sx={{ p: 3 }}><Skeleton height={45} /><Skeleton height={45} /><Skeleton height={45} /></Box>
          : activity.isError && !activity.data ? <EmptyPanel icon={<HistoryRounded />} title="Chưa tải được nhật ký" description="Mở trang nhật ký hoặc thử làm mới dữ liệu." />
          : !activity.data?.items.length ? <EmptyPanel icon={<HistoryRounded />} title="Chưa có hoạt động được ghi nhận" description="Các thay đổi trong hệ thống sẽ được hiển thị tại đây." />
          : activity.data.items.map((entry) => <Link to="/admin/audit-logs" key={entry.id} className="admin-list-link">
            <Box sx={{ width: 32, height: 32, bgcolor: '#F5F6FA', color: '#7F87A0', borderRadius: '50%', display: 'grid', placeItems: 'center', flexShrink: 0 }}><HistoryRounded sx={{ fontSize: 16 }} /></Box>
            <Box sx={{ flex: 1, minWidth: 0 }}><Typography noWrap sx={{ fontSize: 12, fontWeight: 600 }}>{entry.action}</Typography><Typography noWrap sx={{ fontSize: 11, color: 'text.secondary', mt: 0.25 }}>{entry.actorDisplay} · {entry.entityType}</Typography></Box>
            <Typography component="time" dateTime={entry.createdAtUtc} sx={{ fontSize: 10, color: 'text.secondary', whiteSpace: 'nowrap' }}>{date(entry.createdAtUtc)}</Typography>
          </Link>)}
      </Paper>

      <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
        <Box className="admin-panel-header"><Box><h2>Dữ liệu & tiêu chuẩn</h2><p>Truy cập nhanh các tài nguyên quản trị</p></Box><CategoryRounded sx={{ fontSize: 20, color: '#8A7AAF' }} /></Box>
        {resources.map((item) => <Link to={item.href} key={item.label} className="admin-resource-link">
          <Box sx={{ color: '#8D83B0', display: 'flex', '& svg': { fontSize: 19 } }}>{item.icon}</Box>
          <Box sx={{ flex: 1, minWidth: 0 }}><Typography sx={{ fontSize: 12, fontWeight: 600 }}>{item.label}</Typography><Typography sx={{ fontSize: 10, color: 'text.secondary', mt: 0.25 }}>{item.hint}</Typography></Box>
          {item.loading ? <Skeleton width={24} /> : item.count !== undefined ? <Typography sx={{ fontSize: 12, fontWeight: 650, color: 'text.secondary' }}>{number(item.count)}</Typography> : null}
          <ChevronRightRounded sx={{ fontSize: 17, color: '#9299AB' }} />
        </Link>)}
      </Paper>
    </Box>
  </Stack>
}

export default AdminOverviewPage
