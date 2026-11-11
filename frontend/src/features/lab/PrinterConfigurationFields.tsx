import { useQuery } from '@tanstack/react-query'
import { Alert, Autocomplete, Checkbox, CircularProgress, FormControlLabel, Grid, MenuItem, TextField } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import type { PrinterConfiguration } from '../ops/labTypes'

export const defaultPrinterConfiguration: PrinterConfiguration = {
  manufacturer: '', extruderCount: 1, maximumColorSlots: 1, nozzleDiameterMm: 0.4,
  filamentDiameterMm: 1.75, heatedBed: true, maxNozzleTemperatureC: 260,
  maxBedTemperatureC: 100, pricePerHour: 6000, failureRate: 0.08,
  curaDefinitionId: '', curaExtruderDefinitionId: '', curaVariantId: '', curaQualityId: '',
}

export interface CuraMachineDefinition {
  id: string; manufacturer: string; name: string; extruderDefinitionId: string
  preferredMaterialId: string; preferredQualityType: string; widthMm: number; depthMm: number
  heightMm: number; heatedBed: boolean; filamentDiameterMm: number; extruderCount: number
}

interface CuraPresetDefinition {
  id: string; name: string; materialId?: string; variantId?: string; qualityType?: string; nozzleDiameterMm?: number
}
interface CuraMachineOptions { variants: CuraPresetDefinition[]; qualities: CuraPresetDefinition[] }

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
  const options = useQuery({ queryKey: ['cura-machine-options', value.curaDefinitionId],
    queryFn: () => apiClient.get<CuraMachineOptions>(`/cura-library/machines/${value.curaDefinitionId}/options`).then(r => r.data),
    enabled: Boolean(value.curaDefinitionId) })
  const update = (key: keyof PrinterConfiguration, next: string | number | boolean) => onChange({ ...value, [key]: next })
  return <>
    <Grid size={{ xs: 12 }}>
      <Autocomplete freeSolo disabled={Boolean(value.curaDefinitionId)} options={[...new Set(profiles.data?.map(p => p.manufacturer) ?? [])]}
        inputValue={value.manufacturer} onInputChange={(_, next) => update('manufacturer', next)}
        renderInput={params => <TextField {...params} required label="Hãng sản xuất" />} />
    </Grid>
    {value.curaDefinitionId && <>
      <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label="Cura definition ID" value={value.curaDefinitionId} disabled /></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label="Extruder definition ID" value={value.curaExtruderDefinitionId} disabled /></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField select fullWidth label="Nozzle variant (tùy chọn)" value={value.curaVariantId}
        onChange={e => { const id = e.target.value; const selected = options.data?.variants.find(x => x.id === id)
          onChange({ ...value, curaVariantId: id, nozzleDiameterMm: selected?.nozzleDiameterMm ?? value.nozzleDiameterMm,
            curaQualityId: '' }) }}><MenuItem value="">Mặc định</MenuItem>
        {(options.data?.variants ?? []).map(x => <MenuItem key={x.id} value={x.id}>{x.name}</MenuItem>)}</TextField></Grid>
      <Grid size={{ xs: 12, sm: 6 }}><TextField select fullWidth label="Quality profile (tự chọn nếu để trống)" value={value.curaQualityId}
        onChange={e => update('curaQualityId', e.target.value)}><MenuItem value="">Tự động · Standard</MenuItem>
        {(options.data?.qualities ?? []).filter(x => !x.variantId || x.variantId.includes(String(value.nozzleDiameterMm)))
          .map(x => <MenuItem key={x.id} value={x.id}>{x.name}</MenuItem>)}</TextField></Grid>
    </>}
    {([
      ['extruderCount', 'Số đầu đùn vật lý', 1], ['maximumColorSlots', 'Số màu tối đa / mô hình', 1],
      ['nozzleDiameterMm', 'Đường kính đầu phun (mm)', 0.1], ['filamentDiameterMm', 'Đường kính sợi nhựa (mm)', 0.01],
      ['maxNozzleTemperatureC', 'Nhiệt độ đầu phun tối đa (°C)', 1], ['maxBedTemperatureC', 'Nhiệt độ bàn tối đa (°C)', 1],
      ['pricePerHour', 'Đơn giá máy (đ/giờ)', 1000], ['failureRate', 'Tỷ lệ lỗi (%)', 0.1],
    ] as const).map(([key, label, step]) => <Grid key={key} size={{ xs: 12, sm: 6 }}>
      <TextField fullWidth required type="number" label={label}
        disabled={(key === 'maxBedTemperatureC' && !value.heatedBed) || (Boolean(value.curaDefinitionId) &&
          (key === 'extruderCount' || key === 'nozzleDiameterMm' || key === 'filamentDiameterMm'))}
        value={key === 'failureRate' ? Number((value.failureRate * 100).toFixed(4)) : value[key]}
        slotProps={{ htmlInput: { min: 0, step } }}
        onChange={e => update(key, Number(e.target.value) / (key === 'failureRate' ? 100 : 1))} />
    </Grid>)}
    <Grid size={{ xs: 12 }}>
      <FormControlLabel label="Máy có bàn nhiệt" control={<Checkbox checked={value.heatedBed}
        disabled={Boolean(value.curaDefinitionId)}
        onChange={(_, checked) => onChange({ ...value, heatedBed: checked, maxBedTemperatureC: checked ? 100 : 0 })} />} />
      <Alert severity="info">Profile được tìm theo hãng, model, khổ in, đầu phun, đầu đùn, số màu và vật liệu. Máy chỉ tham gia báo giá sau khi quản trị viên duyệt. Tồn kho và màu vật liệu được quản lý trong kho của xưởng.</Alert>
    </Grid>
  </>
}

export function ProfilePresetField({ definitionId, onSelect, onClear }: {
  definitionId?: string; onSelect: (machine: CuraMachineDefinition) => void; onClear?: () => void
}) {
  const machines = useQuery({ queryKey: ['cura-library-machines'],
    queryFn: () => apiClient.get<CuraMachineDefinition[]>('/cura-library/machines').then(r => r.data) })
  const selected = machines.data?.find(x => x.id === definitionId) ?? null
  return <Autocomplete options={machines.data ?? []} value={selected} loading={machines.isLoading}
    groupBy={x => x.manufacturer} getOptionKey={x => x.id}
    getOptionLabel={x => `${x.manufacturer} · ${x.name}`}
    isOptionEqualToValue={(option, value) => option.id === value.id}
    onChange={(_, value) => value ? onSelect(value) : onClear?.()}
    renderInput={params => <TextField {...params} label="Hãng / model từ thư viện Cura 5.11.0" required
      error={machines.isError} helperText={machines.isError ? 'Không tải được danh mục Cura.' : undefined}
      slotProps={{ input: { ...params.InputProps, endAdornment: <>{machines.isLoading && <CircularProgress size={18} />}{params.InputProps.endAdornment}</> } }} />} />
}
