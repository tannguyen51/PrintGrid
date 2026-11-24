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
  enclosedChamber?: boolean
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
  MissingProfile: 'Chưa có đủ profile',
  PendingReview: 'Đã tìm thấy profile · Chờ duyệt máy',
  Approved: 'Đã duyệt báo giá & gán việc',
  Rejected: 'Máy bị từ chối duyệt',
}

export const PROFILE_APPROVAL_STATUS_LABELS: Record<string, string> = {
  PendingReview: 'Chờ Admin duyệt',
  Approved: 'Đã duyệt',
  Rejected: 'Bị từ chối',
}

export const LAB_STATUS_LABELS: Record<LabApprovalStatus, string> = {
  Pending: 'Chờ duyệt',
  Active: 'Đã kích hoạt',
  Rejected: 'Đã từ chối',
  Inactive: 'Tạm dừng',
}

export interface LabOption {
  id: string
  name: string
  city: string
  status: LabApprovalStatus
  isActive: boolean
}

export interface MachineMatchedProfileSummary {
  id: string
  name: string
  materialCode: string
  version: string
  approvalStatus: string
  isActive: boolean
}

export interface MachineListItem {
  id: string
  labId: string
  labName: string
  labCity: string
  labStatus: LabApprovalStatus
  labIsActive: boolean
  labIsInGoodStanding: boolean
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
  curaProfileIds: string[]
  matchedProfiles: MachineMatchedProfileSummary[]
  maintenanceFromUtc?: string | null
  maintenanceUntilUtc?: string | null
  maintenanceReason?: string | null
  printerConfiguration?: PrinterConfiguration | null
  profileStatus: string
  profileReviewReason?: string | null
  profileReviewedAtUtc?: string | null
  isEligibleForQuotingAndScheduling: boolean
}

export interface CuraProfileListItem {
  id: string
  name: string
  manufacturer: string
  machineModel: string
  materialCode: string
  version: string
  engineVersion: string
  nozzleDiameterMm: number
  filamentDiameterMm: number
  extruderCount: number
  maximumColorSlots: number
  widthMm: number
  depthMm: number
  heightMm: number
  heatedBed: boolean
  requiredNozzleTemperatureC: number
  requiredBedTemperatureC: number
  isActive: boolean
  isVerified: boolean
  approvalStatus: 'PendingReview' | 'Approved' | 'Rejected' | string
  submittedByLabId?: string | null
  submittedByLabName?: string | null
  targetMachineId?: string | null
  targetMachineName?: string | null
  submissionNotes?: string | null
  sourceFileName?: string | null
  reviewReason?: string | null
  reviewedAtUtc?: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CuraProfileDetail extends CuraProfileListItem {
  settingsJson: string
}

export interface PagedLabsFilter {
  search?: string
  approvalStatus?: string
  isActive?: '' | 'true' | 'false'
  isInGoodStanding?: '' | 'true' | 'false'
  manufacturerOrModel?: string
  material?: string
  includeInactive?: boolean
  sortBy?: 'pendingFirst' | 'name' | 'newest'
  page: number
  pageSize: number
}

export interface PagedMachinesFilter {
  search?: string
  labId?: string
  profileStatus?: string
  operationalStatus?: string
  labStatus?: string
  manufacturerOrModel?: string
  material?: string
  technology?: string
  sortBy?: 'pendingFirst' | 'name'
  page: number
  pageSize: number
}

export interface PagedProfilesFilter {
  scope?: 'library' | 'proposals' | 'all'
  search?: string
  labId?: string
  approvalStatus?: string
  isActive?: '' | 'true' | 'false'
  manufacturerOrModel?: string
  material?: string
  page: number
  pageSize: number
}

export interface MyProposalsFilter {
  search?: string
  approvalStatus?: string
  material?: string
  page: number
  pageSize: number
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
  profileSyncErrors?: string[] | null
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
