export interface QuoteItem {
  id: string
  modelId: string
  quantity: number
  materialCode: string
  colorCode: string
  layerHeightMm: number
  infillPercent: number
  unitPrice: number
  materialCostAmount: number
  machineTimeCostAmount: number
  estimatedPrintMinutes: number
  estimatedMaterialGrams: number
}

export type QuoteStatus = 'Pending' | 'Draft' | 'Ready' | 'Expired' | 'Converted' | 'Failed'

export interface Quote {
  id: string
  customerId: string
  status: QuoteStatus
  totalPrice: number
  currency: string
  promisedDeliveryDate: string
  createdAt: string
  expiresAt?: string | null
  failureReason?: string | null
  /** Pricing parameter-set version frozen onto this quote (FR-SCHED-010). */
  pricingVersion: string
  /** Why this delivery date — real machine timeline + buffers (FR-SCHED-005). */
  placementBasis?: string | null
  approvedAt?: string | null
  reviewedBy?: string | null
  autoApproved: boolean
  engineTotalAmount?: number | null
  enginePromisedDeliveryDate?: string | null
  adjustmentReason?: string | null
  items: QuoteItem[]
}

export interface QuoteConfigInput {
  modelId: string
  materialCode: string
  colorCode: string
  layerHeightMm: number
  infillPercent: number
  quantity: number
  toleranceMm: number
}

// Selector labels only — actual rates live server-side in the versioned pricing
// parameter set and arrive frozen on each quote (FR-SCHED-010, BR-QUOTE-004).
export const MATERIALS = [
  { code: 'PLA', label: 'PLA', desc: 'Rẻ, dễ in, tốt cho trang trí' },
  { code: 'PETG', label: 'PETG', desc: 'Bền, chịu nhiệt tốt hơn' },
  { code: 'ABS', label: 'ABS', desc: 'Cứng, chịu mài mòn' },
  { code: 'TPU', label: 'TPU', desc: 'Dẻo, cao su' },
  { code: 'RESIN', label: 'Resin', desc: 'Chi tiết mịn, chất lượng cao' },
]

export const COLORS = [
  { code: 'BLACK', label: 'Đen' },
  { code: 'WHITE', label: 'Trắng' },
  { code: 'SIGNALRED', label: 'Đỏ' },
  { code: 'BLUE', label: 'Xanh dương' },
  { code: 'GREY', label: 'Xám' },
  { code: 'TRANSPARENT', label: 'Trong suốt' },
]

export const QUALITY_GRADES = [
  { layerMm: 0.3, label: 'Draft', note: 'In nhanh, kinh tế' },
  { layerMm: 0.2, label: 'Standard', note: 'Cân bằng chất lượng & tốc độ' },
  { layerMm: 0.1, label: 'High', note: 'Chi tiết tốt' },
  { layerMm: 0.05, label: 'Ultra', note: 'Chi tiết tối đa, lâu nhất' },
]

export const INFILL_OPTIONS = [10, 30, 50, 100]
