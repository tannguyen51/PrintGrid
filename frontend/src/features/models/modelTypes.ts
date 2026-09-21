export type GeometryStatus = 'Pending' | 'Ready' | 'Failed'

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
  geometryMessage?: string | null
  storageKey?: string | null
}

export interface ModelInput {
  name: string
  description?: string | null
  fileName: string
  fileFormat: string
  sizeBytes: number
  tags: string[]
}

/** Model fields as returned for list/detail rows (may be absent for legacy rows). */
export type ModelRow = Partial<ThreeDModel> & { id: string }