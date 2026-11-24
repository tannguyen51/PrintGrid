import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Alert, Box, Button, CircularProgress, FormControl, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material'
import { ModelViewer } from '../../shared/components/ModelViewer'
import { assignedColors, initializeAssignments, nearestPrintColor, PRINT_COLORS, type ColorGroupAssignment, type ModelColorGroup } from '../../shared/threeMf/colorGroups'

interface Props {
  url: string | null
  format: string
  failed: boolean
  assignments: ColorGroupAssignment[]
  singleColor: string
  fallbackColors: string[]
  onFallbackColorsChange: (colors: string[]) => void
  onChange: (assignments: ColorGroupAssignment[]) => void
  onSingleColorChange: (code: string) => void
  onValidityChange: (valid: boolean) => void
}

export function OrderColorConfigurator({ url, format, failed, assignments, singleColor, fallbackColors, onFallbackColorsChange, onChange, onSingleColorChange, onValidityChange }: Props) {
  const [groups, setGroups] = useState<ModelColorGroup[] | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [showOriginal, setShowOriginal] = useState(false)
  const [previewFailed, setPreviewFailed] = useState(false)
  const current = useRef({ assignments, onChange })
  // Initialization runs only when model metadata changes; editing a selection must not reload the file.
  // oxlint-disable-next-line react/refs
  current.current = { assignments, onChange }
  const handleGroups = useCallback((next: ModelColorGroup[]) => {
    setGroups(next)
    const normalized = initializeAssignments(next, current.current.assignments)
    if (normalized.length !== current.current.assignments.length || normalized.some(a =>
      !current.current.assignments.some(saved => saved.groupId === a.groupId && saved.colorCode === a.colorCode)))
      current.current.onChange(normalized)
  }, [])
  const ready = format !== '3MF' || groups !== null
  const count = assignments.length ? assignedColors(assignments).length : fallbackColors.length
  const valid = !!url && ready && !failed && !previewFailed && count <= 4
  useEffect(() => onValidityChange(valid), [valid, onValidityChange])
  const overrides = useMemo(() => showOriginal || !assignments.length && fallbackColors.length > 1 ? undefined : assignments.length ? Object.fromEntries(assignments.map(a => [a.groupId,
    PRINT_COLORS.find(c => c.code === a.colorCode)?.hex ?? a.originalColorHex ?? '#FFFFFF']))
    : { all: PRINT_COLORS.find(c => c.code === singleColor)?.hex ?? '#000000' }, [assignments, showOriginal, singleColor, fallbackColors.length])

  function changeGroup(group: ModelColorGroup, code: string) {
    const next = initializeAssignments(groups ?? [], assignments).map(a => a.groupId === group.id ? { ...a, colorCode: code } : a)
    onChange(next)
    setShowOriginal(false)
    setSelected(group.id)
  }
  const swatch = (hex: string) => <Box component="span" sx={{ display: 'inline-block', flexShrink: 0, width: 18, height: 18,
    borderRadius: '50%', bgcolor: hex, border: '1px solid #B8B8C3' }} />

  return <Stack spacing={1.5} component="section" aria-label="Nhóm màu và mô hình đặt in">
    <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1} flexWrap="wrap">
      <Typography fontWeight={700}>Màu sắc trên mô hình</Typography>
      {!!groups?.length && <Stack direction="row" gap={1}>
        <Button size="small" variant={showOriginal ? 'contained' : 'outlined'} onClick={() => setShowOriginal(v => !v)}>
          {showOriginal ? 'Đang xem màu gốc' : 'Xem màu gốc'}
        </Button>
        {selected && <Button size="small" onClick={() => setSelected(null)}>Hiện tất cả nhóm</Button>}
      </Stack>}
    </Stack>
    {url ? <ModelViewer url={url} format={format} fit height={340} autoRotate={false}
      onColorGroupsChange={handleGroups} onPreviewError={() => setPreviewFailed(true)} showColorLegend={false}
      colorOverrides={overrides} selectedColorGroup={selected} onColorGroupSelect={setSelected}
      sx={{ borderRadius: 2, border: '1px solid #E8EBF2' }} />
      : <Stack alignItems="center" justifyContent="center" sx={{ height: 250, bgcolor: '#F5F2FF', borderRadius: 2 }}>
        {!failed && <CircularProgress size={28} />}<Typography sx={{ mt: 1 }}>{failed ? 'Không tải được mô hình.' : 'Đang tải mô hình…'}</Typography>
      </Stack>}
    {(failed || previewFailed) && <Alert severity="error">Chưa đọc được nhóm màu. Hãy tải lại trang hoặc kiểm tra file trước khi lấy báo giá.</Alert>}
    {!ready && !failed && !previewFailed && <Typography variant="body2" color="text.secondary">Đang nhận diện nhóm màu gốc…</Typography>}
    {!!groups?.length && <>
      <Typography variant="body2" color="text.secondary">Bấm nhóm màu hoặc bấm lên mô hình để xem vùng tương ứng. Chọn màu in thay thế cho từng nhóm.</Typography>
      {groups.map(group => {
        const assignment = assignments.find(a => a.groupId === group.id)
        const originalName = PRINT_COLORS.find(c => c.code === nearestPrintColor(group.originalColorHex))?.label
        return <Stack key={group.id} direction={{ xs: 'column', sm: 'row' }} gap={1.5} alignItems={{ sm: 'center' }}
          sx={{ p: 1.5, borderRadius: 2, border: '1px solid', borderColor: selected === group.id ? 'primary.main' : 'divider',
            bgcolor: selected === group.id ? 'rgba(111,85,255,.05)' : 'transparent' }}>
          <Button aria-pressed={selected === group.id} onClick={() => setSelected(selected === group.id ? null : group.id)}
            sx={{ flex: 1, justifyContent: 'flex-start', textTransform: 'none', color: 'text.primary', gap: 1 }}>
            {swatch(group.originalColorHex)}
            <Box sx={{ textAlign: 'left' }}><Typography fontSize={13} fontWeight={650}>Nhóm {group.slot} · {originalName}</Typography>
              <Typography variant="caption" color="text.secondary">Màu gốc {group.originalColorHex}</Typography></Box>
          </Button>
          <FormControl size="small" sx={{ width: { xs: '100%', sm: 180 } }}>
            <InputLabel id={`print-color-${group.slot}`}>Màu in nhóm {group.slot}</InputLabel>
            <Select labelId={`print-color-${group.slot}`} label={`Màu in nhóm ${group.slot}`}
              value={assignment?.colorCode ?? nearestPrintColor(group.originalColorHex)}
              onChange={event => changeGroup(group, event.target.value)}>
              {PRINT_COLORS.map(color => <MenuItem key={color.code} value={color.code}>
                <Stack direction="row" alignItems="center" gap={1}>{swatch(color.hex)}{color.label}</Stack>
              </MenuItem>)}
            </Select>
          </FormControl>
        </Stack>
      })}
      <Typography variant="caption" color="text.secondary">{count} màu in · Các nhóm có thể dùng chung một màu. Màu xem trước mang tính mô phỏng.</Typography>
      {count > 4 && <Alert severity="warning">Hệ thống hiện hỗ trợ tối đa 4 màu in khác nhau. Hãy cho một số nhóm dùng chung màu.</Alert>}
    </>}
    {ready && !groups?.length && !failed && !previewFailed && <FormControl fullWidth>
      <InputLabel>Màu in</InputLabel><Select label="Màu in" multiple={format === '3MF'} value={format === '3MF' ? fallbackColors : singleColor}
        onChange={event => {
          if (format !== '3MF') { onSingleColorChange(event.target.value as string); return }
          const values = typeof event.target.value === 'string' ? event.target.value.split(',') : event.target.value
          if (values.length) onFallbackColorsChange(values)
        }}>
        {PRINT_COLORS.map(color => <MenuItem key={color.code} value={color.code}>{color.label}</MenuItem>)}
      </Select>
      {format === '3MF' && <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
        File này chưa hỗ trợ chọn nhóm trực tiếp trên mô hình. Các màu in được gán theo thứ tự vùng vật liệu trong file; phần xem trước giữ màu gốc khi chọn nhiều màu.
      </Typography>}
    </FormControl>}
  </Stack>
}
