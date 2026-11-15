import type { ReactNode } from 'react'
import { Box, Paper, Skeleton, Stack, Typography } from '@mui/material'

export function OpsPageHeader({ title, description, actions, eyebrow }: {
  title: string; description: string; actions?: ReactNode; eyebrow?: string
}) {
  return <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} gap={2}>
    <Box sx={{ minWidth: 0 }}>{eyebrow && <div className="ops-eyebrow">{eyebrow}</div>}<Typography variant="h1">{title}</Typography>
      <Typography color="text.secondary" sx={{ mt: 0.75 }}>{description}</Typography></Box>
    {actions && <Box sx={{ flexShrink: 0 }}>{actions}</Box>}
  </Stack>
}

export function OpsStatCard({ label, value, hint, icon, tone = 'primary', loading = false }: {
  label: string; value: string | number; hint: string; icon: ReactNode
  tone?: 'primary' | 'error' | 'success' | 'warning'; loading?: boolean
}) {
  const colors = { primary: ['#F0EDFC', '#6D58BC'], error: ['#FEF0EE', '#B4493C'], success: ['#EAF6F1', '#237856'], warning: ['#FFF5E5', '#9A681E'] }
  return <Paper variant="outlined" className="ops-stat-card">
    <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1}>
      <Typography sx={{ fontSize: 12, fontWeight: 600, color: 'text.secondary' }}>{label}</Typography>
      <Box sx={{ width: 35, height: 35, borderRadius: '9px', display: 'grid', placeItems: 'center', bgcolor: colors[tone][0], color: colors[tone][1], '& svg': { fontSize: 19 } }}>{icon}</Box>
    </Stack>
    <Box className="ops-stat-value">{loading ? <Skeleton width={72} /> : value}</Box>
    <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>{loading ? 'Đang cập nhật dữ liệu…' : hint}</Typography>
  </Paper>
}

export function OpsEmptyState({ icon, title, description, action }: {
  icon: ReactNode; title: string; description: string; action?: ReactNode
}) {
  return <Stack className="ops-empty-state" spacing={1} alignItems="center">
    <Box sx={{ bgcolor: '#F3F1FA', color: '#8C7CAF', width: 45, height: 45, borderRadius: '12px', display: 'grid', placeItems: 'center', mb: 0.5 }}>{icon}</Box>
    <Typography sx={{ fontSize: 13, fontWeight: 650 }}>{title}</Typography>
    <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 380 }}>{description}</Typography>
    {action}
  </Stack>
}
