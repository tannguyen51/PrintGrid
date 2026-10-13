/**
 * Lab & machine types for the ops, admin, and lab manager consoles (FR-LAB-001/002/006/007).
 *
 * NOTE the enums travel as NUMBERS for PrintTechnology and MachineStatus, while Lab.status
 * travels as a string ('Pending' | 'Active' | 'Rejected' | 'Inactive').
 */
export const PRINT_TECHNOLOGY_LABELS = ['FDM', 'SLA', 'SLS'] as const
export const MACHINE_STATUS_LABELS = ['Sẵn sàng', 'Đang in', 'Bảo trì', 'Ngoại tuyến'] as const

export type PrintTechnology = 0 | 1 | 2
export type MachineStatus = 0 | 1 | 2 | 3
export type LabApprovalStatus = 'Pending' | 'Active' | 'Rejected' | 'Inactive'

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
  maintenanceFromUtc?: string | null
  maintenanceUntilUtc?: string | null
  maintenanceReason?: string | null
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
  status?: LabApprovalStatus
  operatingHours?: string
  ownerUserId?: string | null
  contactPhone?: string | null
  address?: string | null
  rejectionReason?: string | null
  isDeclarationComplete?: boolean
}

export interface RegisterLabInput {
  name: string
  city: string
  transitDaysToHub: number
}

export interface SelfRegisterLabInput {
  name: string
  city: string
  transitDaysToHub: number
  operatingHours: string
  contactPhone?: string
  address?: string
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

export interface UpdateMachineInput extends RegisterMachineInput {
  machineId: string
}

export interface ScheduleMaintenanceInput {
  labId: string
  machineId: string
  maintenanceFromUtc?: string | null
  maintenanceUntilUtc?: string | null
  reason?: string | null
  clear?: boolean
  setMaintenanceStatusNow?: boolean
}

export interface MachineTimelineSlot {
  jobId: string
  orderNumber?: string | null
  modelFileName?: string | null
  materialCode: string
  colorCode: string
  status: string
  attemptNumber: number
  internalDueDate: string
  estimatedPrintMinutes: number
  plannedStartUtc: string
  plannedEndUtc: string
}

export interface MachineTimeline {
  machineId: string
  machineName: string
  model: string
  technology: PrintTechnology
  status: MachineStatus
  maintenanceFromUtc?: string | null
  maintenanceUntilUtc?: string | null
  maintenanceReason?: string | null
  totalQueueMinutes: number
  slots: MachineTimelineSlot[]
}

export interface LabTimeline {
  labId: string
  labName: string
  operatingHours: string
  transitDaysToHub: number
  generatedAtUtc: string
  machines: MachineTimeline[]
}

export interface LabPerformanceMetrics {
  onTimeDeliveryRate: number
  firstPassYield: number
  acceptanceRate: number
  utilizationRate: number
}

export interface LabPerformance {
  labId: string
  labName: string
  completedJobsCount: number
  totalAssignedJobsCount: number
  isProvisional: boolean
  standingScore: number
  standingLabel: string
  labMetrics: LabPerformanceMetrics
  networkAnonymousAverage: LabPerformanceMetrics
  networkAverageStandingScore: number
  activeLabsInBenchmark: number
}