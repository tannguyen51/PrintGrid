export interface ResolvedProfileTemplateInput {
  machineName: string
  widthMm: number
  depthMm: number
  heightMm: number
  nozzleDiameterMm: number
  filamentDiameterMm: number
  heatedBed: boolean
  extruderCount: number
  maximumColorSlots: number
  materialCode: string
  nozzleTemperatureC?: number
  bedTemperatureC?: number
  layerHeightMm?: number
}

const MATERIAL_DEFAULT_TEMPS: Record<string, { nozzle: number; bed: number }> = {
  PLA: { nozzle: 205, bed: 60 },
  PETG: { nozzle: 235, bed: 75 },
  ABS: { nozzle: 245, bed: 95 },
  ASA: { nozzle: 250, bed: 95 },
  TPU: { nozzle: 225, bed: 50 },
}

export function buildSampleResolvedCuraProfileJson(input: ResolvedProfileTemplateInput): string {
  const mat = (input.materialCode || 'PLA').trim().toUpperCase()
  const defaults = MATERIAL_DEFAULT_TEMPS[mat] ?? { nozzle: 210, bed: 60 }
  const nozzleTemp = input.nozzleTemperatureC ?? defaults.nozzle
  const bedTemp = input.heatedBed ? (input.bedTemperatureC ?? defaults.bed) : 0
  const layerHeight = input.layerHeightMm && input.layerHeightMm > 0 ? input.layerHeightMm : 0.2
  const slots = Math.max(1, Math.min(8, Math.max(input.extruderCount || 1, input.maximumColorSlots || 1)))

  const root: Record<string, unknown> = {
    global: {
      machine_name: input.machineName || 'Custom FDM Printer',
      machine_extruder_count: slots,
      machine_width: input.widthMm > 0 ? input.widthMm : 220,
      machine_depth: input.depthMm > 0 ? input.depthMm : 220,
      machine_height: input.heightMm > 0 ? input.heightMm : 250,
      machine_shape: 'rectangular',
      machine_heated_bed: Boolean(input.heatedBed),
      machine_center_is_zero: false,
      machine_nozzle_size: input.nozzleDiameterMm > 0 ? input.nozzleDiameterMm : 0.4,
      material_diameter: input.filamentDiameterMm > 0 ? input.filamentDiameterMm : 1.75,
      machine_max_feedrate_x: 200,
      machine_max_feedrate_y: 200,
      machine_acceleration: 1000,
      layer_height: layerHeight,
      layer_height_0: layerHeight,
      speed_print: 60,
      infill_sparse_density: 20,
      infill_line_distance: 2.0,
      top_thickness: 0.8,
      bottom_thickness: 0.8,
      machine_start_gcode: 'G28\nG92 E0',
      machine_end_gcode: 'M104 S0\nM140 S0\nM107',
    },
  }

  for (let i = 0; i < slots; i += 1) {
    root[`extruder.${i}`] = {
      extruder_nr: i,
      machine_nozzle_size: input.nozzleDiameterMm > 0 ? input.nozzleDiameterMm : 0.4,
      material_diameter: input.filamentDiameterMm > 0 ? input.filamentDiameterMm : 1.75,
      layer_height: layerHeight,
      layer_height_0: layerHeight,
      speed_print: 60,
      speed_wall: 30,
      speed_travel: 150,
      infill_sparse_density: 20,
      infill_line_distance: 2.0,
      top_thickness: 0.8,
      bottom_thickness: 0.8,
      material_print_temperature: nozzleTemp,
      material_print_temperature_layer_0: nozzleTemp + 5,
      material_bed_temperature: bedTemp,
      material_bed_temperature_layer_0: bedTemp,
    }
  }

  return JSON.stringify(root, null, 2)
}
