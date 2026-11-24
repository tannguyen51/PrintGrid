export function normalizeMaterialColor(code: string): string {
  const normalized = code.trim().toUpperCase()
  return normalized === 'SIGNALRED' ? 'RED' : normalized === 'GREY' ? 'GRAY' : normalized
}

// Shared by customer configuration and lab inventory. Hex values are preview colors;
// codes identify stock and never depend on the original model's filament slot.
export const MATERIAL_COLORS = [
  { code: 'BLACK', label: 'Đen', hex: '#000000' },
  { code: 'WHITE', label: 'Trắng', hex: '#FFFFFF' },
  { code: 'RED', label: 'Đỏ', hex: '#D02727' },
  { code: 'BLUE', label: 'Xanh dương', hex: '#2563EB' },
  { code: 'GREEN', label: 'Xanh lá', hex: '#16A34A' },
  { code: 'YELLOW', label: 'Vàng', hex: '#FEC600' },
  { code: 'ORANGE', label: 'Cam', hex: '#F97316' },
  { code: 'GRAY', label: 'Xám', hex: '#808080' },
  { code: 'SILVER', label: 'Bạc', hex: '#CBD5E1' },
  { code: 'NAVY', label: 'Xanh navy', hex: '#1E3A8A' },
  { code: 'PURPLE', label: 'Tím', hex: '#8B5CF6' },
  { code: 'PINK', label: 'Hồng', hex: '#EC4899' },
  { code: 'BROWN', label: 'Nâu', hex: '#92400E' },
  { code: 'BEIGE', label: 'Be', hex: '#D6C6A5' },
  { code: 'GOLD', label: 'Vàng kim', hex: '#D4A72C' },
  { code: 'COPPER', label: 'Đồng', hex: '#B87333' },
  { code: 'TRANSPARENT', label: 'Trong suốt', hex: '#DDE9ED' },
  { code: 'NATURAL', label: 'Tự nhiên', hex: '#F1E7D0' },
] as const
