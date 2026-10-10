import { Chip } from '@mui/material'
import { landing } from '../theme/landing'

/**
 * One chip for every status pill in the app. Backend sends enums as PascalCase
 * strings (DTOs use .ToString(), no JsonStringEnumConverter), so keys here must
 * match the wire values exactly: OrderStatus / GeometryStatus / QuoteStatus.
 * Colors are semantic (good/warn/bad/info) — deliberately separate from the
 * brand accent, and tinted as text-on-wash to read on the landing black.
 */

type Tone = '#34c759' | '#f5a623' | '#ff453a' | '#0a84ff' | '#A78BFA' | '#909096'

interface StatusMeta {
  label: string
  tone: Tone
}

const ORDER: Record<string, StatusMeta> = {
  PaymentPending: { label: 'Chờ thanh toán', tone: '#f5a623' },
  Confirmed: { label: 'Đã xác nhận', tone: '#0a84ff' },
  InProduction: { label: 'Đang sản xuất', tone: '#A78BFA' },
  QualityCheck: { label: 'Kiểm tra chất lượng', tone: '#f5a623' },
  Shipping: { label: 'Đang giao', tone: '#0a84ff' },
  Delivered: { label: 'Đã giao', tone: '#34c759' },
  Cancelled: { label: 'Đã hủy', tone: '#ff453a' },
}

const GEOMETRY: Record<string, StatusMeta> = {
  Pending: { label: 'Đang phân tích…', tone: '#f5a623' },
  Ready: { label: 'Đã phân tích', tone: '#34c759' },
  Failed: { label: 'Lỗi phân tích', tone: '#ff453a' },
}

const QUOTE: Record<string, StatusMeta> = {
  Pending: { label: 'Đang tính giá…', tone: '#f5a623' },
  Ready: { label: 'Sẵn sàng', tone: '#34c759' },
  Expired: { label: 'Hết hạn', tone: '#909096' },
  Converted: { label: 'Đã đặt hàng', tone: '#0a84ff' },
  Failed: { label: 'Không báo giá được', tone: '#ff453a' },
}

const MAPS = { order: ORDER, geometry: GEOMETRY, quote: QUOTE } as const

export type StatusKind = keyof typeof MAPS

export function StatusChip({ kind, value, size = 'small' }: { kind: StatusKind; value: string; size?: 'small' | 'medium' }) {
  const meta = MAPS[kind][value]
  const tone: Tone = meta?.tone ?? '#909096'
  return (
    <Chip
      size={size}
      label={meta?.label ?? value}
      sx={{
        fontWeight: 600,
        color: tone,
        bgcolor: `${tone}1f`,
        border: `1px solid ${tone}55`,
        borderRadius: '999px',
      }}
    />
  )
}

/** Exported so pages can order/label statuses without duplicating the maps. */
export { ORDER as ORDER_STATUS, GEOMETRY as GEOMETRY_STATUS, QUOTE as QUOTE_STATUS }

/** Landing-card wrapper used by lists that need the hairline shell. */
export const landingCardSx = {
  bgcolor: landing.card,
  border: `1px solid ${landing.hairline}`,
  borderRadius: `${landing.radius}px`,
} as const
