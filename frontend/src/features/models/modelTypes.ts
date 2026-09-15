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
}

export interface ModelInput {
  name: string
  description?: string | null
  fileName: string
  fileFormat: string
  sizeBytes: number
  tags: string[]
}