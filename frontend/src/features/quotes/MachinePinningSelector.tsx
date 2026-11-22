import { useEffect, useState } from 'react'
import { Accordion, AccordionDetails, AccordionSummary, Alert, Button, FormControlLabel, MenuItem,
  Stack, Switch, TextField, Typography } from '@mui/material'
import ExpandMoreRounded from '@mui/icons-material/ExpandMoreRounded'
import TechConstraintsSelector from './TechConstraintsSelector'
import type { TechRequirements } from './quoteTypes'
import { useEligibleModels } from './useQuotes'

interface Props {
  modelId: string; materialCode: string; colorCodes: string[]; sizeCode: string; layerHeightMm: number
  constraints: TechRequirements; onConstraintsChange: (value: TechRequirements) => void
  pinnedModelId: string | null; onPinnedModelChange: (id: string | null) => void
  onValidityChange: (valid: boolean) => void
}

export default function MachinePinningSelector(props: Props) {
  const [expanded, setExpanded] = useState(false)
  const [pinning, setPinning] = useState(false)
  const options = useEligibleModels({ modelId: props.modelId, materialCode: props.materialCode,
    colorCodes: props.colorCodes, sizeCode: props.sizeCode, layerHeightMm: props.layerHeightMm,
    toleranceMm: 0.2, ...props.constraints }, expanded || !!props.pinnedModelId)
  const models = options.data?.models ?? []
  const selectedValid = !props.pinnedModelId || models.some(m => m.id === props.pinnedModelId)
  const { maxToleranceMm, minLayerHeightMm } = props.constraints
  const constraintsValid = (maxToleranceMm == null || maxToleranceMm > 0 && maxToleranceMm <= 10)
    && (minLayerHeightMm == null || minLayerHeightMm > 0 && minLayerHeightMm <= 1)
  const valid = constraintsValid && (!(pinning || props.pinnedModelId)
    || !!props.pinnedModelId && selectedValid && !options.isFetching && !options.isError)
  useEffect(() => { props.onValidityChange(valid) }, [valid, props.onValidityChange])
  const coefficient = options.data?.pinSurchargeCoefficient
  return <Accordion expanded={expanded} onChange={(_, value) => setExpanded(value)} disableGutters elevation={0}
    sx={{ border: '1px solid', borderColor: 'divider', borderRadius: '12px !important', '&::before': { display: 'none' } }}>
    <AccordionSummary expandIcon={<ExpandMoreRounded />}>
      <Typography fontWeight={700}>Yêu cầu in nâng cao</Typography>
    </AccordionSummary>
    <AccordionDetails><Stack spacing={2.5}>
      <TechConstraintsSelector value={props.constraints} onValueChange={props.onConstraintsChange} />
      {!constraintsValid && <Alert severity="error">Sai số phải lớn hơn 0 và không quá 10 mm; chiều cao lớp phải lớn hơn 0 và không quá 1 mm.</Alert>}
      {options.isFetching && <Typography role="status" variant="body2">Đang tìm máy phù hợp…</Typography>}
      {options.isError && <Alert severity="error" action={<Button onClick={() => void options.refetch()}>Thử lại</Button>}>
        Không tải được danh sách model phù hợp. Vui lòng kiểm tra yêu cầu kỹ thuật.
      </Alert>}
      {!options.isFetching && !options.isError && options.data && options.data.matchingMachineCount < 3 && <Alert severity="warning">
        Chỉ có {options.data.matchingMachineCount} máy phù hợp. Thời gian chờ có thể dài; bạn có thể nới yêu cầu kỹ thuật.
      </Alert>}
      <FormControlLabel control={<Switch checked={pinning || !!props.pinnedModelId}
        sx={{ mr: 1 }}
        onChange={(_, checked) => { setPinning(checked); if (!checked) props.onPinnedModelChange(null) }} />}
        label="Bắt buộc model / dòng máy" />
      {pinning || props.pinnedModelId ? <Stack spacing={1.5}>
        <Typography variant="body2" color="text.secondary">
          {coefficient != null ? `Giá theo profile của model, nhân hệ số ${coefficient.toLocaleString('vi-VN')} (phụ phí ${((coefficient - 1) * 100).toLocaleString('vi-VN')}%). ` : ''}
          Hệ thống tự chọn máy và nơi sản xuất. Bạn sẽ được xem thêm phương án không chỉ định để so sánh.
        </Typography>
        <TextField select fullWidth label="Model máy bắt buộc" value={props.pinnedModelId ?? ''}
          slotProps={{ select: { renderValue: value => models.find(m => m.id === value)?.name ?? 'Model không còn phù hợp' } }}
          helperText={models.find(m => m.id === props.pinnedModelId) ? `${models.find(m => m.id === props.pinnedModelId)!.approvedMachineCount} máy đang hoạt động` : undefined}
          disabled={options.isFetching || options.isError} error={!options.isFetching && !selectedValid}
          onChange={e => props.onPinnedModelChange(e.target.value || null)}>
          <MenuItem value="">Hệ thống tự chọn máy phù hợp</MenuItem>
          {!selectedValid && <MenuItem value={props.pinnedModelId!}>Model đã chọn không còn phù hợp</MenuItem>}
          {models.map(m => <MenuItem key={m.id} value={m.id}>{m.name} · {m.approvedMachineCount} máy đang hoạt động</MenuItem>)}
        </TextField>
        {!props.pinnedModelId && <Typography variant="body2" color="text.secondary">Chọn một model để lấy báo giá chỉ định, hoặc tắt tùy chọn để hệ thống tự chọn.</Typography>}
        {!options.isFetching && !selectedValid && <Alert severity="warning">Hãy chọn lại model hoặc tắt chỉ định để tiếp tục. Hệ thống sẽ không tự đổi model.</Alert>}
        {!options.isFetching && !options.isError && !models.length && <Typography variant="body2" color="text.secondary">
          Chưa có model đủ điều kiện cho cấu hình này. Bạn vẫn có thể để hệ thống tự chọn máy.
        </Typography>}
      </Stack> : <Typography variant="body2" color="text.secondary">Hệ thống tự chọn máy đáp ứng yêu cầu, không có phụ phí chỉ định.</Typography>}
    </Stack></AccordionDetails>
  </Accordion>
}
