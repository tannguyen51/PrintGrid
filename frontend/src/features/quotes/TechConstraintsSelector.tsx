import { useState } from 'react'
import {
  Box,
  FormControl,
  FormLabel,
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  Stack,
  TextField,
  Typography,
  Collapse,
} from '@mui/material'
import ExpandMoreRounded from '@mui/icons-material/ExpandMoreRounded'

/** Output shape matching doc §3 tech constraints (hard filters, no surcharge). */
export interface TechConstraintsValue {
  maxColors?: number
  enclosedChamber?: boolean
  nozzleDiameterMm?: number
  maxToleranceMm?: string
  minLayerHeightMm?: string
}

interface Props {
  defaultQualityLayerMm: number
  onValueChange: (value: TechConstraintsValue) => void
}

const NOZZLE_OPTIONS = [0.2, 0.4, 0.6] as const
const COLOR_OPTIONS = [1, 2, 3, 4] as const

/**
 * Section 3 — "Ràng buộc kỹ thuật" from the machine selection doc.
 * Hard constraints that filter machines at quote time — free, no surcharge.
 */
export default function TechConstraintsSelector({ defaultQualityLayerMm, onValueChange }: Props) {
  const [open, setOpen] = useState(false)
  const [maxColors, setMaxColors] = useState<number | null>(null)
  const [enclosedChamber, setEnclosedChamber] = useState<boolean | null>(null)
  const [nozzleDiameter, setNozzleDiameter] = useState<number | null>(null)
  const [maxTolerance, setMaxTolerance] = useState<string>('')
  const [minLayerHeight, setMinLayerHeight] = useState<string>('')

  const hasAny = maxColors !== null || enclosedChamber !== null || nozzleDiameter !== null
    || maxTolerance.trim() !== '' || minLayerHeight.trim() !== ''

  const value: TechConstraintsValue = {}
  if (maxColors !== null) value.maxColors = maxColors
  if (enclosedChamber !== null) value.enclosedChamber = !!enclosedChamber
  if (nozzleDiameter !== null) value.nozzleDiameterMm = nozzleDiameter
  if (maxTolerance.trim()) value.maxToleranceMm = maxTolerance
  if (minLayerHeight.trim()) value.minLayerHeightMm = minLayerHeight

  // Debounced emit
  setTimeout(() => onValueChange(value), 300)

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
        <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>
          Yêu cầu kỹ thuật <Typography component="span" sx={{ fontWeight: 400, fontSize: '0.8rem', color: 'text.secondary' }}>(tùy chọn)</Typography>
        </Typography>
        <ExpandMoreRounded sx={{
          transform: open ? 'rotate(180deg)' : 'none',
          transition: 'transform 0.2s',
        }} />
      </Box>

      <Collapse in={open}>
        <Stack spacing={2.5} sx={{
          p: 2.5,
          borderRadius: 2,
          border: hasAny ? '1px solid rgba(111,85,255,0.32)' : '1px solid #E8EBF2',
          bgcolor: hasAny ? 'rgba(111,85,255,0.04)' : 'transparent',
        }}>
          <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.82rem' }}>
            Ràng buộc cứng — máy không đáp ứng sẽ bị loại ở bước lọc khả năng. Tập máy nhỏ hơn có thể làm ngày giao muộn hơn.
          </Typography>

          {/* Max colors */}
          <FormControl fullWidth>
            <InputLabel>Số màu trong mô hình</InputLabel>
            <Select
              value={maxColors ?? ''}
              label="Số màu trong mô hình"
              onChange={(e) => {
                const raw = e.target.value as number | string
                const v = raw === '' ? null : Number(raw)
                setMaxColors(v)
              }}
            >
              <MenuItem value="">Mặc định</MenuItem>
              {COLOR_OPTIONS.map(c => (
                <MenuItem key={c} value={c}>{c} màu</MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Enclosed chamber */}
          <Box>
            <FormLabel sx={{ fontWeight: 600, color: 'text.primary', mb: 0.5 }}>Buồng kín</FormLabel>
            <RadioGroup
              row
              value={enclosedChamber === null ? '' : String(enclosedChamber)}
              onChange={(e) => setEnclosedChamber(e.target.value === 'true')}
              sx={{ gap: 2 }}
            >
              <Radio value="true" /> Cần buồng kín (ABS/PETG/Nylon…)
              <Radio value="false" /> Không cần
            </RadioGroup>
          </Box>

          {/* Nozzle diameter */}
          <FormControl fullWidth>
            <InputLabel>Đường kính đầu phun</InputLabel>
            <Select
              value={nozzleDiameter ?? ''}
              label="Đường kính đầu phun"
              onChange={(e) => {
                const raw = e.target.value as number | string
                const v = raw === '' ? null : Number(raw)
                setNozzleDiameter(v)
              }}
            >
              <MenuItem value="">Mặc định</MenuItem>
              {NOZZLE_OPTIONS.map(n => (
                <MenuItem key={n} value={n}>{n} mm</MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Max tolerance */}
          <TextField
            label="Sai số kích thước tối đa (±mm)"
            placeholder={`Mặc định ≈ ${defaultQualityLayerMm.toFixed(2)} mm`}
            size="small"
            value={maxTolerance}
            onChange={(e) => setMaxTolerance(e.target.value)}
            fullWidth
          />

          {/* Min layer height */}
          <TextField
            label="Chiều cao lớp tối thiểu (mm)"
            placeholder={`Mặc định ≥ ${defaultQualityLayerMm} mm`}
            size="small"
            value={minLayerHeight}
            onChange={(e) => setMinLayerHeight(e.target.value)}
            fullWidth
          />
        </Stack>
      </Collapse>
    </Box>
  )
}
