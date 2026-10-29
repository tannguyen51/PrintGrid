export interface PriceRange {
  from: number
  to: number
  currency: string
}

export interface SampleSummary {
  id: string
  slug: string
  name: string
  description?: string | null
  category: string
  tags: string[]
  fileFormat: string
  sizeBytes: number
  coverImageUrl?: string | null
  volumeCm3?: number | null
  boundingWidthMm?: number | null
  boundingDepthMm?: number | null
  boundingHeightMm?: number | null
  estimatedPrintMinutes?: number | null
  priceRange?: PriceRange | null
}

export interface SampleDetail extends SampleSummary {
  fileName: string
  estimatedMaterialGrams?: number | null
  createdAtUtc: string
}

/** Admin (curation) view returned by /admin/samples. */
export interface AdminSample {
  id: string
  slug: string
  name: string
  description?: string | null
  category: string
  tags: string[]
  fileName: string
  fileFormat: string
  sizeBytes: number
  coverImageUrl?: string | null
  storageKey: string
  volumeCm3?: number | null
  boundingWidthMm?: number | null
  boundingDepthMm?: number | null
  boundingHeightMm?: number | null
  estimatedPrintMinutes?: number | null
  estimatedMaterialGrams?: number | null
  isPublished: boolean
  sortOrder: number
  createdAtUtc: string
  updatedAtUtc: string
}

export const SAMPLE_CATEGORIES = ['Trang trí', 'Cơ khí', 'Phụ kiện', 'Mô hình', 'Khác'] as const

export function formatVnd(value: number | null | undefined): string {
  if (value === null || value === undefined) return '–'
  return `${value.toLocaleString('vi-VN')} đ`
}

export function formatDims(sample: {
  boundingWidthMm?: number | null
  boundingDepthMm?: number | null
  boundingHeightMm?: number | null
}): string {
  const { boundingWidthMm: w, boundingDepthMm: d, boundingHeightMm: h } = sample
  if (w == null || d == null || h == null) return '–'
  return `${Math.round(w)} × ${Math.round(d)} × ${Math.round(h)} mm`
}

export function formatPrintTime(minutes: number | null | undefined): string {
  if (minutes == null) return '–'
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  if (hours === 0) return `${rest} phút`
  return rest === 0 ? `${hours} giờ` : `${hours} giờ ${rest} phút`
}
