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

export type ConfigurationGroup = 'machine' | 'extrusion' | 'multicolor' | 'pricing'

export function useCuraProfiles() {
  return useQuery({ queryKey: ['cura-profiles'], queryFn: () => apiClient.get<CuraProfileSummary[]>('/cura-profiles').then(r => r.data) })
}

export function PrinterConfigurationFields({ value, onChange, groups }: {
  value: PrinterConfiguration; onChange: (value: PrinterConfiguration) => void
  groups?: ConfigurationGroup[]
}) {
  const profiles = useCuraProfiles()
  const visibleGroups = groups ?? ['machine', 'extrusion', 'multicolor', 'pricing']
  const update = (key: keyof PrinterConfiguration, next: string | number | boolean) => onChange({ ...value, [key]: next })
  return <>
    {visibleGroups.includes('machine') && <Grid size={{ xs: 12 }}>
    <Autocomplete freeSolo options={[...new Set(profiles.data?.map(p => p.manufacturer) ?? [])]}
      inputValue={value.manufacturer} onInputChange={(_, next) => update('manufacturer', next)}
      renderInput={params => <TextField {...params} required label="Hãng sản xuất"
        error={profiles.isError} helperText={profiles.isError ? 'Không tải được danh sách hãng từ thư viện CuraEngine.' : undefined} />} />
    </Grid>}
    {([
    ['extruderCount', 'Số đầu đùn vật lý', 1], ['maximumColorSlots', 'Số màu tối đa / mô hình', 1],
    ['nozzleDiameterMm', 'Đường kính đầu phun (mm)', 0.1], ['filamentDiameterMm', 'Đường kính sợi nhựa (mm)', 0.01],
    ] as const).filter(([key]) => visibleGroups.includes(key === 'extruderCount' || key === 'maximumColorSlots' ? 'multicolor' : 'extrusion'))
    .map(([key, label, step]) => <Grid key={key} size={{ xs: 12, sm: 6 }}>
    <TextField fullWidth required type="number" label={label}
      value={value[key]}
      slotProps={{ htmlInput: { min: 0.01, step } }}
      onChange={e => update(key, Number(e.target.value))} />
    </Grid>)}
    {visibleGroups.includes('extrusion') && <>
    <Grid size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label="Nhiệt độ đầu phun tối đa (°C)"
        value={value.maxNozzleTemperatureC} slotProps={{ htmlInput: { min: 1, step: 1 } }}
        onChange={e => update('maxNozzleTemperatureC', Number(e.target.value))} />
    </Grid>
    <Grid size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label="Nhiệt độ bàn tối đa (°C)"
        disabled={!value.heatedBed} value={value.maxBedTemperatureC}
        slotProps={{ htmlInput: { min: 0, step: 1 } }}
        onChange={e => update('maxBedTemperatureC', Number(e.target.value))} />
    </Grid>
    <Grid size={{ xs: 12 }}>
      <FormControlLabel label="Máy có bàn nhiệt" control={<Checkbox checked={value.heatedBed}
        onChange={(_, checked) => onChange({ ...value, heatedBed: checked, maxBedTemperatureC: checked ? 100 : 0 })} />} />
    </Grid>
    </>}
    {visibleGroups.includes('pricing') && <>
    <Grid size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label="Đơn giá chạy máy (đ/giờ)"
        value={value.pricePerHour} slotProps={{ htmlInput: { min: 1, step: 1000 } }}
        onChange={e => update('pricePerHour', Number(e.target.value))} />
    </Grid>
    <Grid size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label="Tỷ lệ lỗi dự phòng (%)"
        value={Number((value.failureRate * 100).toFixed(4))}
        slotProps={{ htmlInput: { min: 0, max: 99.99, step: 0.1 } }}
        onChange={e => update('failureRate', Number(e.target.value) / 100)} />
    </Grid>
    </>}
    {groups === undefined && <Grid size={{ xs: 12 }}>
    <Alert severity="info">Profile được tìm theo hãng, model, khổ in, đầu phun, đầu đùn, số màu và vật liệu. Máy chỉ tham gia báo giá sau khi quản trị viên duyệt. Tồn kho và màu vật liệu được quản lý trong kho của xưởng.</Alert>
    </Grid>}
  </>
}

export function ProfilePresetField({ value, onSelect }: {
  value: string
  onSelect: (profile: CuraProfileSummary) => void
}) {
  const profiles = useCuraProfiles()
  const selectedProfile = profiles.data?.find(p => p.id === value)
  return <>
    {profiles.isError && <Grid size={{ xs: 12 }}><Alert severity="error">Không tải được thư viện CuraEngine. Thử tải lại trước khi chọn profile.</Alert></Grid>}
    <Grid size={{ xs: 12 }}>
    <TextField select fullWidth label="Profile model / vật liệu tham chiếu" value={value}
      disabled={profiles.isLoading || profiles.isError || !profiles.data?.length}
      onChange={e => { const p = profiles.data?.find(p => p.id === e.target.value); if (p) onSelect(p) }}>
    {(profiles.data ?? []).map(p => <MenuItem key={p.id} value={p.id}>{p.manufacturer} · {p.machineModel} · {p.materialCode} · {p.nozzleDiameterMm} mm · {p.name}</MenuItem>)}
    {!profiles.isLoading && !profiles.isError && !profiles.data?.length && <MenuItem disabled value="">Chưa có profile — cần quản trị viên nhập thư viện CuraEngine</MenuItem>}
    </TextField>
    </Grid>
    {selectedProfile && <Grid size={{ xs: 12 }}>
    <Alert severity="info">
      Profile tham chiếu: {selectedProfile.name} · CuraEngine {selectedProfile.engineVersion} · vật liệu {selectedProfile.materialCode} ·
      đầu phun {selectedProfile.nozzleDiameterMm} mm · khổ in {selectedProfile.widthMm} × {selectedProfile.depthMm} × {selectedProfile.heightMm} mm.
      Nhiệt độ tối thiểu theo vật liệu: đầu phun {selectedProfile.requiredNozzleTemperatureC} °C, bàn {selectedProfile.requiredBedTemperatureC} °C.
      Hãy xác nhận giới hạn nhiệt thực tế của máy ở mục đầu đùn. Tốc độ, gia tốc, rút nhựa, support và bám bàn lấy từ profile, không nhập thủ công tại đây.
    </Alert>
    </Grid>}
  </>
}
