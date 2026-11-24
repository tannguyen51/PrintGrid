export interface ModelColorGroup {
  id: string
  slot: number
  originalColorHex: string
}

export interface ColorGroupAssignment {
  groupId: string
  colorCode: string
  originalColorHex?: string | null
}

export const groupIdForSlot = (slot: number) => `filament:${String(slot).padStart(8, '0')}`
export const colorGroupNumber = (id: string, fallback: number) => /^filament:\d+$/.test(id) ? Number(id.slice(9)) : fallback

export const PRINT_COLORS = [
  { code: 'BLACK', label: 'Đen', hex: '#000000' },
  { code: 'WHITE', label: 'Trắng', hex: '#FFFFFF' },
  { code: 'SIGNALRED', label: 'Đỏ', hex: '#D02727' },
  { code: 'BLUE', label: 'Xanh dương', hex: '#2563EB' },
  { code: 'GREEN', label: 'Xanh lá', hex: '#16A34A' },
  { code: 'YELLOW', label: 'Vàng', hex: '#FEC600' },
  { code: 'ORANGE', label: 'Cam', hex: '#F97316' },
  { code: 'GREY', label: 'Xám', hex: '#808080' },
  { code: 'TRANSPARENT', label: 'Trong suốt', hex: '#DDE9ED' },
]

export function nearestPrintColor(hex: string) {
  const rgb = (value: string) => [1, 3, 5].map(i => parseInt(value.slice(i, i + 2), 16))
  const original = rgb(hex)
  return PRINT_COLORS.filter(c => c.code !== 'TRANSPARENT').reduce((best, color) => {
    const distance = (value: string) => rgb(value).reduce((sum, v, i) => sum + (v - original[i]) ** 2, 0)
    return distance(color.hex) < distance(best.hex) ? color : best
  }).code
}

export function initializeAssignments(groups: ModelColorGroup[], saved: ColorGroupAssignment[]) {
  return groups.map(group => ({ groupId: group.id, originalColorHex: group.originalColorHex,
    colorCode: saved.find(a => a.groupId === group.id && PRINT_COLORS.some(c => c.code === a.colorCode))?.colorCode
      ?? nearestPrintColor(group.originalColorHex) }))
}

export const assignedColors = (assignments: ColorGroupAssignment[]) => [...new Set(
  [...assignments].sort((a, b) => a.groupId.localeCompare(b.groupId)).map(a => a.colorCode))]
