export interface QuoteItem {
  id: string
  modelId: string
  quantity: number
  materialCode: string
  colorCode: string
  layerHeightMm: number
  infillPercent: number
  unitPrice: number
  estimatedPrintMinutes: number
  estimatedMaterialGrams: number
}

export type QuoteStatus = 'Pending' | 'Ready' | 'Expired' | 'Converted' | 'Failed'

export interface Quote {
  id: string
  customerId: string
  status: QuoteStatus
  totalPrice: number
  currency: string
  promisedDeliveryDate: string
  createdAt: string
  expiresAt: string
  failureReason?: string | null
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

export const MATERIALS = [
  { code: 'PLA', label: 'PLA', desc: 'Rẻ, dễ in, tốt cho trang trí', rate: '450 đ/g' },
  { code: 'PETG', label: 'PETG', desc: 'Bền, chịu nhiệt tốt hơn', rate: '550 đ/g' },
  { code: 'ABS', label: 'ABS', desc: 'Cứng, chịu mài mòn', rate: '500 đ/g' },
  { code: 'TPU', label: 'TPU', desc: 'Dẻo, cao su', rate: '650 đ/g' },
  { code: 'RESIN', label: 'Resin', desc: 'Chi tiết mịn, chất lượng cao', rate: '1.500 đ/g' },
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