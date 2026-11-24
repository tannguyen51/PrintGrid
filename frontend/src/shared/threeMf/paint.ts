export type Point = readonly [number, number, number]
export type Triangle = readonly [Point, Point, Point]

// Bambu/Orca and Prusa serialize the same subdivision tree, last nibble first.
export function decodePaint(paint: string, triangle: Triangle, fallback: number, emit: (t: Triangle, slot: number) => void) {
  let cursor = paint.length - 1
  let nodes = 0
  function next() {
    if (cursor < 0 || !/[0-9a-f]/i.test(paint[cursor])) throw new Error('Dữ liệu tô màu 3MF không hợp lệ hoặc bị thiếu.')
    return parseInt(paint[cursor--], 16)
  }
  const mid = (a: Point, b: Point): Point => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]
  function visit(t: Triangle, depth: number) {
    if (depth > 64 || ++nodes > 65536) throw new Error('Phân vùng màu 3MF quá phức tạp.')
    const code = next(), split = code & 3
    if (!split) {
      let slot = code >> 2
      if (slot === 3) {
        let extra: number
        do { extra = next(); slot += extra } while (extra === 15 && slot <= 256)
      }
      if (slot > 256) throw new Error('Chỉ số filament 3MF không hợp lệ.')
      emit(t, slot || fallback)
      return
    }
    const side = code >> 2
    if (side > 2 || (split === 3 && side !== 0)) throw new Error('Cạnh phân vùng màu 3MF không hợp lệ.')
    const a = t[side], b = t[(side + 1) % 3], c = t[(side + 2) % 3]
    const ab = mid(a, b), bc = mid(b, c), ca = mid(c, a)
    const children: Triangle[] = split === 1 ? [[a, b, bc], [bc, c, a]]
      : split === 2 ? [[a, ab, ca], [ab, b, ca], [b, c, ca]]
        : [[a, ab, ca], [ab, b, bc], [bc, c, ca], [ab, bc, ca]]
    for (let i = children.length - 1; i >= 0; i--) visit(children[i], depth + 1)
  }
  if (!paint) { emit(triangle, fallback); return }
  visit(triangle, 0)
  if (cursor !== -1) throw new Error('Dữ liệu tô màu 3MF có phần dư.')
}
