import { unzipSync } from 'fflate'
import { SaxesParser } from 'saxes'
import { Matrix4, Vector3 } from 'three'
import { decodePaint, type Point, type Triangle } from './paint'

const MAX_BYTES = 512 * 1024 * 1024
const MAX_FACES = 2_097_150
type Attributes = Record<string, string>
type Ref = { id: string; path?: string; transform: Matrix4 }
type Part = { id: string; vertices: number[]; faces: number[]; paint: string[]; components: Ref[] }
type Model = { unit: number; parts: Map<string, Part>; build: Ref[] }
export type PreviewMesh = { positions: Float32Array; normals: Float32Array; slot: number; plate: string; color: string }
export type BambuPreview = { meshes: PreviewMesh[]; palette: string[]; plates: string[]; triangleCount: number; surfacePainting: boolean }

// No DOM in this reader: XML parsing, decompression, painting and normals run in a worker.
function xml(bytes: Uint8Array, open: (name: string, attrs: Attributes) => void, close: (name: string) => void = () => {}) {
  const parser = new SaxesParser()
  parser.on('doctype', () => { throw new Error('3MF không được chứa DTD.') })
  parser.on('opentag', tag => open(tag.name.split(':').at(-1)!, tag.attributes as Attributes))
  parser.on('closetag', tag => close(tag.name.split(':').at(-1)!))
  const decoder = new TextDecoder()
  for (let i = 0; i < bytes.length; i += 65536) parser.write(decoder.decode(bytes.subarray(i, i + 65536), { stream: true }))
  parser.write(decoder.decode()).close()
}
function number(value: string | undefined) {
  if (value == null || value.trim() === '' || !Number.isFinite(Number(value))) throw new Error('Tọa độ hoặc chỉ số 3MF không hợp lệ.')
  return Number(value)
}
function index(value: string | undefined) {
  const n = number(value)
  if (!Number.isSafeInteger(n) || n < 0) throw new Error('Chỉ số đỉnh 3MF không hợp lệ.')
  return n
}
function transform(raw?: string) {
  if (raw == null) return new Matrix4()
  const v = raw.trim().split(/\s+/).map(number)
  if (v.length !== 12) throw new Error('Transform 3MF không hợp lệ.')
  return new Matrix4().set(v[0], v[3], v[6], v[9], v[1], v[4], v[7], v[10], v[2], v[5], v[8], v[11], 0, 0, 0, 1)
}
function resolve(source: string, raw: string) {
  const target = decodeURIComponent(raw.replaceAll('\\', '/'))
  if (/[:?#]/.test(target)) throw new Error('3MF chỉ được tham chiếu tệp trong gói.')
  const path = target.startsWith('/') ? target.slice(1) : source.slice(0, source.lastIndexOf('/') + 1) + target
  const parts: string[] = []
  for (const part of path.split('/')) {
    if (!part || part === '.') continue
    if (part === '..') { if (!parts.length) throw new Error('Đường dẫn 3MF vượt ra ngoài gói.'); parts.pop() }
    else parts.push(part)
  }
  return parts.join('/')
}
function parseModel(bytes: Uint8Array): Model {
  const model: Model = { unit: 1, parts: new Map(), build: [] }
  let part: Part | undefined
  let faces = 0
  xml(bytes, (tag, a) => {
    switch (tag) {
      case 'model': {
        const unit = ({ millimeter: 1, micron: .001, centimeter: 10, meter: 1000, inch: 25.4, foot: 304.8 } as Record<string, number>)[a.unit ?? 'millimeter']
        if (!unit) throw new Error('Đơn vị 3MF không hỗ trợ.')
        model.unit = unit; break
      }
      case 'object':
        if (!a.id || model.parts.has(a.id)) throw new Error('Object 3MF thiếu ID hoặc trùng ID.')
        part = { id: a.id, vertices: [], faces: [], paint: [], components: [] }
        model.parts.set(a.id, part); break
      case 'vertex':
        if (!part) break
        if (part.vertices.length >= MAX_FACES * 9) throw new Error('3MF có quá nhiều đỉnh.')
        part.vertices.push(number(a.x), number(a.y), number(a.z)); break
      case 'triangle':
        if (!part) break
        if (++faces > MAX_FACES) throw new Error('3MF có quá nhiều mặt.')
        part.faces.push(index(a.v1), index(a.v2), index(a.v3))
        part.paint.push(a.paint_color ?? a['slic3rpe:mmu_segmentation'] ?? ''); break
      case 'component':
        if (part) part.components.push({ id: a.objectid, path: a['p:path'], transform: transform(a.transform) })
        break
      case 'item':
        if (a.printable !== '0' && a.printable !== 'false') model.build.push({ id: a.objectid, transform: transform(a.transform) })
        break
    }
  }, tag => { if (tag === 'object') part = undefined })
  return model
}

class FloatBuffer {
  data = new Float32Array(4096)
  length = 0
  push(n: number) {
    if (this.length === this.data.length) { const next = new Float32Array(this.data.length * 2); next.set(this.data); this.data = next }
    this.data[this.length++] = n
  }
  finish() { return this.data.slice(0, this.length) }
}

/** Returns null for standard 3MFs, which keep the existing materials/texture loader. */
export function parseBambu(bytes: Uint8Array): BambuPreview | null {
  let expanded = 0, count = 0
  const files = unzipSync(bytes, { filter: file => {
    if (++count > 4096 || (expanded += file.originalSize) > MAX_BYTES || file.originalSize > 256 * 1024 * 1024)
      throw new Error('3MF vượt giới hạn dữ liệu giải nén.')
    return /\.model$/i.test(file.name) || ['Metadata/project_settings.config', 'Metadata/model_settings.config'].includes(file.name)
  } })
  const decoder = new TextDecoder()
  if (!files['Metadata/project_settings.config'] || !files['Metadata/model_settings.config']) return null
  if (files['Metadata/project_settings.config'].length > 4 * 1024 * 1024 || files['Metadata/model_settings.config'].length > 4 * 1024 * 1024)
    throw new Error('Cấu hình 3MF quá lớn.')
  const settings = JSON.parse(decoder.decode(files['Metadata/project_settings.config'])) as { filament_colour?: string[] }
  if (!Array.isArray(settings.filament_colour)) return null
  const palette = settings.filament_colour.map(c => {
    if (typeof c !== 'string' || !/^#[\da-f]{6}([\da-f]{2})?$/i.test(c)) throw new Error('Bảng màu filament 3MF không hợp lệ.')
    return c.slice(0, 7)
  })
  const objectSlots = new Map<string, number>(), partSlots = new Map<string, number>()
  const ignored = new Set<string>(), plateByInstance = new Map<string, string>()
  let objectId = '', partId = '', plate = '', instanceObject = '', instanceId = '0'
  xml(files['Metadata/model_settings.config'], (tag, a) => {
    if (tag === 'object') objectId = a.id
    if (tag === 'part') { partId = a.id; if (a.subtype && a.subtype !== 'normal_part') ignored.add(`${objectId}:${partId}`) }
    if (tag === 'plate') plate = ''
    if (tag === 'model_instance') { instanceObject = ''; instanceId = '0' }
    if (tag !== 'metadata') return
    if (a.key === 'extruder' && number(a.value) > 0) {
      if (partId) partSlots.set(`${objectId}:${partId}`, index(a.value))
      else if (objectId) objectSlots.set(objectId, index(a.value))
    }
    if (a.key === 'plater_id') plate = a.value
    if (a.key === 'object_id') instanceObject = a.value
    if (a.key === 'instance_id') instanceId = a.value
  }, tag => {
    if (tag === 'part') partId = ''
    if (tag === 'object') objectId = ''
    if (tag === 'model_instance' && instanceObject && plate) plateByInstance.set(`${instanceObject}:${instanceId}`, plate)
  })
  const models = new Map<string, Model>()
  function load(path: string) {
    if (!models.has(path)) {
      if (!files[path]) throw new Error(`3MF thiếu tệp hình học: ${path}`)
      models.set(path, parseModel(files[path])); delete files[path]
    }
    return models.get(path)!
  }
  const mainPath = files['3D/3dmodel.model'] ? '3D/3dmodel.model' : Object.keys(files).find(p => /\.model$/i.test(p))
  if (!mainPath) throw new Error('3MF thiếu model.')
  const main = load(mainPath)
  const groups = new Map<string, { positions: FloatBuffer; normals: FloatBuffer; slot: number; plate: string }>()
  let triangleCount = 0
  let surfacePainting = false
  const vector = new Vector3()
  function emit(t: Triangle, slot: number, plate: string) {
    if (!Number.isInteger(slot) || slot < 1 || slot > palette.length) throw new Error('Vùng màu tham chiếu filament không tồn tại.')
    if (++triangleCount > MAX_FACES) throw new Error('3MF có quá nhiều mặt sau phân vùng màu.')
    const key = `${plate}:${slot}`
    if (!groups.has(key)) groups.set(key, { positions: new FloatBuffer(), normals: new FloatBuffer(), slot, plate })
    const group = groups.get(key)!
    const [a, b, c] = t
    const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2]
    const vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2]
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx
    const length = Math.hypot(nx, ny, nz) || 1
    nx /= length; ny /= length; nz /= length
    for (const p of t) {
      for (const value of p) group.positions.push(value)
      group.normals.push(nx); group.normals.push(ny); group.normals.push(nz)
    }
  }
  function visit(file: string, id: string, matrix: Matrix4, slot: number, rootId: string, plate: string, path: Set<string>) {
    const identity = `${file}#${id}`
    if (path.size >= 64 || path.has(identity)) throw new Error('3MF tham chiếu component vòng lặp hoặc quá sâu.')
    path = new Set(path); path.add(identity)
    const model = load(file), part = model.parts.get(id)
    if (!part) throw new Error('3MF tham chiếu object không tồn tại.')
    if (ignored.has(`${rootId}:${id}`)) return
    slot = partSlots.get(`${rootId}:${id}`) ?? slot
    const point = (i: number): Point => {
      const offset = i * 3
      if (offset + 2 >= part.vertices.length) throw new Error('3MF tham chiếu đỉnh không tồn tại.')
      vector.set(part.vertices[offset], part.vertices[offset + 1], part.vertices[offset + 2]).applyMatrix4(matrix)
      if (![vector.x, vector.y, vector.z].every(Number.isFinite)) throw new Error('Tọa độ 3MF không hợp lệ.')
      return [vector.x, vector.y, vector.z]
    }
    for (let i = 0; i < part.faces.length; i += 3) {
      const triangle: Triangle = [point(part.faces[i]), point(part.faces[i + 1]), point(part.faces[i + 2])]
      const paint = part.paint[i / 3]
      if (paint) { surfacePainting = true; decodePaint(paint, triangle, slot, (t, s) => emit(t, s, plate)) }
      else emit(triangle, slot, plate)
    }
    for (const component of part.components) {
      const target = component.path ? resolve(file, component.path) : file
      const scale = load(target).unit / model.unit
      const child = matrix.clone().multiply(component.transform).multiply(new Matrix4().makeScale(scale, scale, scale))
      visit(target, component.id, child, slot, rootId, plate, path)
    }
  }
  const instances = new Map<string, number>()
  for (const item of main.build) {
    const n = instances.get(item.id) ?? 0; instances.set(item.id, n + 1)
    const plate = plateByInstance.get(`${item.id}:${n}`) ?? '1'
    visit(mainPath, item.id, new Matrix4().makeScale(main.unit, main.unit, main.unit).multiply(item.transform),
      objectSlots.get(item.id) ?? 1, item.id, plate, new Set())
  }
  if (!triangleCount) throw new Error('3MF không có hình học có thể in.')
  const meshes = [...groups.values()].map(g => ({ positions: g.positions.finish(), normals: g.normals.finish(), slot: g.slot, plate: g.plate, color: palette[g.slot - 1] }))
  return { meshes, palette, plates: [...new Set(meshes.map(m => m.plate))].sort((a, b) => Number(a) - Number(b)), triangleCount, surfacePainting }
}
