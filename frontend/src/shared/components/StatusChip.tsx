import { Chip } from '@mui/material'
import { landing } from '../theme/landing'
import { palette, statusTones } from '../theme/tokens'

/**
 * One chip for every status pill in the app. Backend sends enums as PascalCase
 * strings (DTOs use .ToString(), no JsonStringEnumConverter), so keys here must
 * match the wire values exactly: OrderStatus / GeometryStatus / QuoteStatus.
 * Tones are semantic (good/warn/bad/info) and use the light-theme tint pairs so
 * the label keeps AA contrast on a pale wash.
 */

interface StatusMeta {
  label: string
}

const ORDER: Record<string, StatusMeta> = {
  PaymentPending: { label: 'Chờ thanh toán' },
  Confirmed: { label: 'Đã xác nhận' },
  InProduction: { label: 'Đang sản xuất' },
  QualityCheck: { label: 'Kiểm tra chất lượng' },
  Shipping: { label: 'Đang giao' },
  Delivered: { label: 'Đã giao' },
  Cancelled: { label: 'Đã hủy' },
}

const GEOMETRY: Record<string, StatusMeta> = {
  Pending: { label: 'Đang phân tích…' },
  Ready: { label: 'Đã phân tích' },
  Failed: { label: 'Lỗi phân tích' },
}

const QUOTE: Record<string, StatusMeta> = {
  Pending: { label: 'Đang tính giá…' },
  Ready: { label: 'Sẵn sàng' },
  Expired: { label: 'Hết hạn' },
  Converted: { label: 'Đã đặt hàng' },
  Failed: { label: 'Không báo giá được' },
}

const MAPS = { order: ORDER, geometry: GEOMETRY, quote: QUOTE } as const

export type StatusKind = keyof typeof MAPS

export function StatusChip({ kind, value, size = 'small' }: { kind: StatusKind; value: string; size?: 'small' | 'medium' }) {
  const meta = MAPS[kind][value]
  const tone = statusTones[value as keyof typeof statusTones] ?? {
    fg: palette.status.neutral,
    bg: palette.status.neutralSoft,
    border: palette.border.hairline,
  }
  return (
    <Chip
      size={size}
      label={meta?.label ?? value}
      sx={{
        fontWeight: 600,
        color: tone.fg,
        bgcolor: tone.bg,
        border: `1px solid ${tone.border}`,
        borderRadius: '999px',
      }}
    />
  )
}

/** Exported so pages can order/label statuses without duplicating the maps. */
export { ORDER as ORDER_STATUS, GEOMETRY as GEOMETRY_STATUS, QUOTE as QUOTE_STATUS }

/** Card shell used by public lists that need the light metallic surface. */
export const landingCardSx = {
  bgcolor: landing.card,
  border: `1px solid ${landing.hairline}`,
  borderRadius: `${landing.radius}px`,
  boxShadow: '0 1px 2px rgba(37, 38, 58, 0.04), 0 10px 28px rgba(37, 38, 58, 0.05)',
} as const
