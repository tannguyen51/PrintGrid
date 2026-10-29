export interface PriceRange {
  from: number
  to: number
  currency: string
}

export interface PublicMaterialPrice {
  code: string
  name: string
  description?: string | null
  isPremium: boolean
  priceRange: PriceRange
}

export interface PublicPricing {
  version: string
  effectiveAtUtc?: string | null
  referenceVolumeCm3: number
  referenceHeightMm: number
  referenceInfillPercent: number
  referenceBaseMinutes: number
  materials: PublicMaterialPrice[]
}

export interface PricingLayer {
  layerHeightMm: number
  timeMultiplier: number
}

export interface PricingMaterial {
  code: string
  name: string
  description?: string | null
  unitRatePerGram: number
  densityGramsPerCm3: number
  isPremium: boolean
}

export interface PricingExample {
  materialCode: string
  volumeCm3: number
  baseMinutes: number
  effectiveMinutes: number
  layerHeightMm: number
  infillPercent: number
  grams: number
  materialCost: number
  machineCost: number
  total: number
}

export interface PricingDetail {
  version: string
  effectiveAtUtc?: string | null
  pricePerPrintHour: number
  referenceVolumeCm3: number
  referenceHeightMm: number
  referenceInfillPercent: number
  referenceBaseMinutes: number
  materialRates: Record<string, number>
  materialDensities: Record<string, number>
  layerHeights: PricingLayer[]
  materials: PricingMaterial[]
  referenceExample?: PricingExample | null
}

export function formatVnd(value: number | null | undefined): string {
  if (value === null || value === undefined) return '–'
  return `${Math.round(value).toLocaleString('vi-VN')} đ`
}

export function formatDate(value?: string | null): string {
  if (!value) return '–'
  return new Date(value).toLocaleDateString('vi-VN')
}
