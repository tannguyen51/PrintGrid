import { Checkbox, FormControlLabel, Grid, MenuItem, TextField, Typography } from '@mui/material'
import type { TechRequirements } from './quoteTypes'

export type TechConstraintsValue = TechRequirements

export default function TechConstraintsSelector({ value, onValueChange }: {
  value: TechRequirements; onValueChange: (next: TechRequirements) => void
}) {
  function number(key: keyof TechRequirements, text: string) {
    onValueChange({ ...value, [key]: text === '' ? undefined : Number(text) })
  }
  return <>
    <Typography variant="body2" color="text.secondary">
      Yêu cầu kỹ thuật không có phụ phí riêng. Ít máy phù hợp hơn có thể làm thời gian chờ dài hơn.
    </Typography>
    <Grid container spacing={2}>
      <Grid size={{ xs: 12, sm: 6 }}><TextField select fullWidth label="Số màu máy cần hỗ trợ"
        value={value.maxColors ?? ''} onChange={e => number('maxColors', e.target.value)}>
        <MenuItem value="">Theo số màu sản phẩm</MenuItem>
        {[1, 2, 3, 4].map(n => <MenuItem key={n} value={n}>{n} màu trở lên</MenuItem>)}
      </TextField></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField select fullWidth label="Đường kính đầu phun"
        value={value.nozzleDiameterMm ?? ''} onChange={e => number('nozzleDiameterMm', e.target.value)}>
        <MenuItem value="">Hệ thống tự chọn</MenuItem>
        {[0.2, 0.4, 0.6].map(n => <MenuItem key={n} value={n}>{n} mm</MenuItem>)}
      </TextField></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth type="number" label="Sai số tối đa (±mm)"
        value={value.maxToleranceMm ?? ''} onChange={e => number('maxToleranceMm', e.target.value)}
        slotProps={{ htmlInput: { min: 0.001, max: 10, step: 0.01 } }} helperText="Để trống để dùng yêu cầu chất lượng của sản phẩm." /></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth type="number" label="Khả năng in lớp mỏng đến (mm)"
        value={value.minLayerHeightMm ?? ''} onChange={e => number('minLayerHeightMm', e.target.value)}
        slotProps={{ htmlInput: { min: 0.001, max: 1, step: 0.01 } }} helperText="Ví dụ 0,1 mm: máy phải in được lớp 0,1 mm hoặc mỏng hơn." /></Grid>
      <Grid size={{ xs: 12 }}><FormControlLabel label="Cần máy có buồng kín"
        control={<Checkbox checked={value.enclosedChamber === true}
          onChange={(_, checked) => onValueChange({ ...value, enclosedChamber: checked ? true : undefined })} />} /></Grid>
    </Grid>
  </>
}
