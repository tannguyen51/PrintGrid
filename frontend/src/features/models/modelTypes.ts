export type GeometryStatus = 'Pending' | 'Ready' | 'Failed'
export type ModelReviewStatus = 'Pending' | 'Approved' | 'Rejected'

export interface ThreeDModel {
  id: string
  name: string
  description?: string | null
  fileName: string
  fileFormat: string
  sizeBytes: number
  tags: string[]
  createdAt: string
  updatedAt: string
  geometryStatus: GeometryStatus
  boundingWidthMm?: number | null
  boundingDepthMm?: number | null
  boundingHeightMm?: number | null
  volumeCm3?: number | null
  estimatedPrintMinutes?: number | null
  estimatedMaterialGrams?: number | null
  isWatertight?: boolean | null
  isManifold?: boolean | null
  isPrintable?: boolean | null
  geometryMessage?: string | null
  /** The storage key is intentionally no longer exposed; only this flag is returned. */
  hasFile: boolean
  sha256?: string | null
  reviewStatus: ModelReviewStatus
  reviewReason?: string | null
  reviewedAtUtc?: string | null
}

export interface ModelInput {
  name: string
  description?: string | null
  fileName: string
  fileFormat: string
  sizeBytes: number
  tags: string[]
}

export interface ModelQuota {
  usedModels: number
  maxModels: number
  usedBytes: number
  maxBytes: number
}

/** Model fields as returned for list/detail rows (may be absent for legacy rows). */
export type ModelRow = Partial<ThreeDModel> & { id: string }
