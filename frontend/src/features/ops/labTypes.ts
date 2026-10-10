/**
 * Lab & machine types for the ops console.
 *
 * NOTE the enums travel as NUMBERS: LabDto/MachineDto keep the raw C# enum (no .ToString()
 * in LabMappings, and no JsonStringEnumConverter registered), unlike Order/Job DTOs which
 * send PascalCase strings. Keep the index order in sync with the C# enums.
 */
export const PRINT_TECHNOLOGY_LABELS = ['FDM', 'SLA', 'SLS'] as const
export const MACHINE_STATUS_LABELS = ['Sẵn sàng', 'Đang in', 'Bảo trì', 'Ngoại tuyến'] as const

export type PrintTechnology = 0 | 1 | 2
export type MachineStatus = 0 | 1 | 2 | 3

export interface Machine {
  id: string
  name: string
  model: string
  technology: PrintTechnology
  buildWidthMm: number
  buildDepthMm: number
  buildHeightMm: number
  minLayerHeightMm: number
  achievableToleranceMm: number
  speedFactor: number
  status: MachineStatus
  supportedMaterials: string[]
}

export interface Lab {
  id: string
  name: string
  city: string
  isActive: boolean
  onTimeDeliveryRate: number
  firstPassYield: number
  transitDaysToHub: number
  createdAt: string
  machines: Machine[]
}

export interface RegisterLabInput {
  name: string
  city: string
  transitDaysToHub: number
}

export interface RegisterMachineInput {
  labId: string
  name: string
  model: string
  technology: PrintTechnology
  buildWidthMm: number
  buildDepthMm: number
  buildHeightMm: number
  minLayerHeightMm: number
  achievableToleranceMm: number
  supportedMaterials: string[]
}