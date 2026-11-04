import { useQuery } from '@tanstack/react-query'
import { Alert, Autocomplete, Checkbox, FormControlLabel, Grid, MenuItem, TextField } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import type { PrinterConfiguration } from '../ops/labTypes'

export const defaultPrinterConfiguration: PrinterConfiguration = {
  manufacturer: '', extruderCount: 1, maximumColorSlots: 1, nozzleDiameterMm: 0.4,
  filamentDiameterMm: 1.75, heatedBed: true, maxNozzleTemperatureC: 260,
  maxBedTemperatureC: 100, pricePerHour: 6000, failureRate: 0.08,
}

export interface CuraProfileSummary {
  id: string; name: string; manufacturer: string; machineModel: string; materialCode: string
  version: string; engineVersion: string; nozzleDiameterMm: number; filamentDiameterMm: number
  extruderCount: number; maximumColorSlots: number; widthMm: number; depthMm: number; heightMm: number
  heatedBed: boolean; requiredNozzleTemperatureC: number; requiredBedTemperatureC: number
}

export function useCuraProfiles() {
  return useQuery({ queryKey: ['cura-profiles'], queryFn: () => apiClient.get<CuraProfileSummary[]>('/cura-profiles').then(r => r.data) })
}

export function PrinterConfigurationFields({ value, onChange }: {
  value: PrinterConfiguration; onChange: (value: PrinterConfiguration) => void
}) {
  const profiles = useCuraProfiles()
  const update = (key: keyof PrinterConfiguration, next: string | number | boolean) => onChange({ ...value, [key]: next })
  return <>
    <Grid size={{ xs: 12 }}>
      <Autocomplete freeSolo options={[...new Set(profiles.data?.map(p => p.manufacturer) ?? [])]}
        inputValue={value.manufacturer} onInputChange={(_, next) => update('manufacturer', next)}
        renderInput={params => <TextField {...params} required label="Hãng sản xuất" />} />
    </Grid>
    {([
      ['extruderCount', 'Số đầu đùn vật lý', 1], ['maximumColorSlots', 'Số màu tối đa / mô hình', 1],
      ['nozzleDiameterMm', 'Đường kính đầu phun (mm)', 0.1], ['filamentDiameterMm', 'Đường kính sợi nhựa (mm)', 0.01],
      ['maxNozzleTemperatureC', 'Nhiệt độ đầu phun tối đa (°C)', 1], ['maxBedTemperatureC', 'Nhiệt độ bàn tối đa (°C)', 1],
      ['pricePerHour', 'Đơn giá máy (đ/giờ)', 1000], ['failureRate', 'Tỷ lệ lỗi (%)', 0.1],
    ] as const).map(([key, label, step]) => <Grid key={key} size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label={label}
        disabled={key === 'maxBedTemperatureC' && !value.heatedBed}
        value={key === 'failureRate' ? Number((value.failureRate * 100).toFixed(4)) : value[key]}
        slotProps={{ htmlInput: { min: 0, step } }}
        onChange={e => update(key, Number(e.target.value) / (key === 'failureRate' ? 100 : 1))} />
    </Grid>)}
    <Grid size={{ xs: 12 }}>
      <FormControlLabel label="Máy có bàn nhiệt" control={<Checkbox checked={value.heatedBed}
        onChange={(_, checked) => onChange({ ...value, heatedBed: checked, maxBedTemperatureC: checked ? 100 : 0 })} />} />
      <Alert severity="info">Profile được tìm theo hãng, model, khổ in, đầu phun, đầu đùn, số màu và vật liệu. Máy chỉ tham gia báo giá sau khi quản trị viên duyệt. Tồn kho và màu vật liệu được quản lý trong kho của xưởng.</Alert>
    </Grid>
  </>
}

export function ProfilePresetField({ onSelect }: { onSelect: (profile: CuraProfileSummary) => void }) {
  const profiles = useCuraProfiles()
  return <TextField select fullWidth label="Điền cấu hình từ thư viện (tùy chọn)" value=""
    onChange={e => { const p = profiles.data?.find(p => p.id === e.target.value); if (p) onSelect(p) }}>
    {(profiles.data ?? []).map(p => <MenuItem key={p.id} value={p.id}>{p.manufacturer} · {p.machineModel} · {p.materialCode} · {p.nozzleDiameterMm} mm · {p.name}</MenuItem>)}
    {!profiles.data?.length && <MenuItem disabled value="empty">Chưa có profile — có thể khai báo máy thủ công</MenuItem>}
  </TextField>
}
