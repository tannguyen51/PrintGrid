import { useState } from 'react'
import {
  Box,
  Chip,
  Stack,
  Typography,
  Collapse,
  Alert,
} from '@mui/material'
import ExpandMoreRounded from '@mui/icons-material/ExpandMoreRounded'

/** Output matching doc Section 4 — "Chỉ định model máy" with surcharge. */
export interface PinModelValue {
  bindingType: 1          // ByModel
  pinnedModelId: string
}

/**
 * Section 4 — "Nâng cao → Chỉ định model máy" from the machine selection doc.
 * Note: This feature requires backend implementation with access to Scheduling.Domain
 * Machine entities. Currently disabled as a stub per BR-04 documentation.
 */
export default function MachinePinningSelector() {
  const [open, setOpen] = useState(false)
  const [selectedId] = useState<string | null>(null)

  return (
    <Box>
      {/* Toggle header */}
      <Box
        onClick={() => setOpen(!open)}
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          cursor: 'pointer',
          pb: 1,
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>
            Nâng cao → Chỉ định model máy <Typography component="span" sx={{ fontWeight: 400, fontSize: '0.8rem', color: 'text.secondary' }}>(tùy chọn)</Typography>
          </Typography>
          {selectedId && (
            <Chip label={`Đã chọn`} size="small" color="primary" variant="outlined" />
          )}
        </Stack>
        <ExpandMoreRounded sx={{
          transform: open ? 'rotate(180deg)' : 'none',
          transition: 'transform 0.2s',
        }} />
      </Box>

      <Collapse in={open}>
        <Box sx={{
          p: 2.5,
          borderRadius: 2,
          border: '1px solid rgba(111,85,255,0.32)',
          bgcolor: 'rgba(111,85,255,0.04)',
        }}>
          <Alert severity="info">
            Tính năng chỉ định model máy đang trong quá trình phát triển. Khi hoàn thiện, phần này sẽ hiển thị danh sách model có đủ điều kiện (≥3 máy từ ≥2 lab) để khách chọn. Model được chọn sẽ được slice chính xác hơn và áp dụng hệ số phụ phí ~15%.
          </Alert>
        </Box>
      </Collapse>
    </Box>
  )
}
