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

export interface PrinterConfiguration {
  manufacturer: string
  extruderCount: number
  maximumColorSlots: number
  nozzleDiameterMm: number
  filamentDiameterMm: number
  heatedBed: boolean
  maxNozzleTemperatureC: number
  maxBedTemperatureC: number
  pricePerHour: number
  failureRate: number
  curaDefinitionId: string
  curaExtruderDefinitionId: string
  curaVariantId: string
  curaQualityId: string
}

export const PROFILE_STATUS_LABELS: Record<string, string> = {
  MissingProfile: 'Chưa có đủ profile', PendingReview: 'Đã tìm thấy profile · Chờ duyệt',
  Approved: 'Đã duyệt báo giá', Rejected: 'Bị từ chối',
}

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
  printerConfiguration?: PrinterConfiguration | null
  profileStatus: string
  curaProfileIds: string[]
  profileReviewReason?: string | null
}

/**
 * Mirrors `LabDto` from the Scheduling module (backend source of truth). Every field the
 * API returns is present here: keeping this type a superset is what lets the admin
 * approval screens and the lab/ops consoles share one model.
 */
export interface Lab {
  id: string
  name: string
  city: string
  ownerUserId: string | null
  isActive: boolean
  isInGoodStanding: boolean
  maxConcurrentJobs: number | null
  onTimeDeliveryRate: number
  firstPassYield: number
  transitDaysToHub: number
  workingDays: string
  openingTime: string
  closingTime: string
  materialStockCount: number
  createdAt: string
  machines: Machine[]
  status: LabApprovalStatus
  operatingHours: string
  contactPhone: string | null
  address: string | null
  district: string | null
  rejectionReason: string | null
  isDeclarationComplete: boolean
}

export interface RegisterLabInput {
  name: string
  city: string
  transitDaysToHub: number
  workingDays?: string
  openingTime?: string
  closingTime?: string
  district?: string
}

export interface SelfRegisterLabInput {
  name: string
  city: string
  transitDaysToHub: number
  operatingHours: string
  contactPhone?: string
  address?: string
  district?: string
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
  printerConfiguration?: PrinterConfiguration
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
