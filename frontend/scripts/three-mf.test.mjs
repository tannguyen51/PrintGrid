import { strict as assert } from 'node:assert'
import { readFile } from 'node:fs/promises'
import { join } from 'node:path'
import { test } from 'node:test'
import { rolldown } from 'rolldown'
import { zipSync, strToU8 } from 'fflate'

async function loadModule(input) {
  const bundle = await rolldown({ input, platform: 'node', logLevel: 'silent' })
  const { output } = await bundle.generate({ format: 'esm' })
  await bundle.close()
  return import(`data:text/javascript;base64,${Buffer.from(output[0].code).toString('base64')}`)
}
const { parseBambu } = await loadModule('src/shared/threeMf/parseBambu.ts')
const { initializeAssignments, assignedColors, groupIdForSlot, colorGroupNumber } = await loadModule('src/shared/threeMf/colorGroups.ts')
const core = 'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'
const vertices = '<vertices><vertex x="0" y="0" z="0"/><vertex x="2" y="0" z="0"/><vertex x="0" y="2" z="0"/></vertices>'
const mesh = (paint = '') => `<mesh>${vertices}<triangles><triangle v1="0" v2="1" v3="2" ${paint ? `paint_color="${paint}"` : ''}/></triangles></mesh>`
function fixture(model, config = '<config/>', extra = {}) {
  return zipSync(Object.fromEntries(Object.entries({
    '3D/3dmodel.model': model,
    'Metadata/project_settings.config': JSON.stringify({ filament_colour: ['#FFFFFF', '#D02727'] }),
    'Metadata/model_settings.config': config, ...extra,
  }).map(([name, content]) => [name, strToU8(content)])))
}

test('painting splits the original triangle into the exact two colors and midpoints', () => {
  const preview = parseBambu(fixture(`<model xmlns="${core}"><resources><object id="1">${mesh('481')}</object></resources><build><item objectid="1"/></build></model>`))
  assert.equal(preview.triangleCount, 2)
  assert.deepEqual(preview.meshes.map(m => m.color).sort(), ['#D02727', '#FFFFFF'])
  const red = preview.meshes.find(m => m.slot === 2)
  assert.deepEqual([...red.positions], [1, 1, 0, 0, 2, 0, 0, 0, 0])
  assert.deepEqual([...red.normals], [0, 0, 1, 0, 0, 1, 0, 0, 1])
})

test('object and part colors, internal references, mixed units and plates survive import', () => {
  const preview = parseBambu(fixture(
    `<model xmlns="${core}" xmlns:p="http://schemas.microsoft.com/3dmanufacturing/production/2015/06" unit="centimeter"><resources>
      <object id="1"><components><component objectid="2" p:path="Objects/part.model" transform="1 0 0 0 1 0 0 0 1 3 0 0"/></components></object>
      <object id="3">${mesh()}</object></resources><build><item objectid="1"/><item objectid="3"/></build></model>`,
    '<config><object id="1"><metadata key="extruder" value="1"/><part id="2"><metadata key="extruder" value="2"/></part></object><plate><metadata key="plater_id" value="1"/><model_instance><metadata key="object_id" value="1"/><metadata key="instance_id" value="0"/></model_instance></plate><plate><metadata key="plater_id" value="2"/><model_instance><metadata key="object_id" value="3"/><metadata key="instance_id" value="0"/></model_instance></plate></config>',
    { '3D/Objects/part.model': `<model xmlns="${core}" unit="millimeter"><resources><object id="2">${mesh()}</object></resources></model>` },
  ))
  assert.deepEqual(preview.plates, ['1', '2'])
  const part = preview.meshes.find(m => m.plate === '1')
  assert.equal(part.color, '#D02727')
  assert.deepEqual([...part.positions].slice(0, 6), [30, 0, 0, 32, 0, 0])
  assert.equal(preview.meshes.find(m => m.plate === '2').color, '#FFFFFF')
})

test('truncated paint, absent vertices and component cycles fail instead of displaying misleading colors', () => {
  for (const paint of ['1', 'C', '44', 'Z']) {
    assert.throws(() => parseBambu(fixture(`<model xmlns="${core}"><resources><object id="1">${mesh(paint)}</object></resources><build><item objectid="1"/></build></model>`)))
  }
  assert.throws(() => parseBambu(fixture(`<model xmlns="${core}"><resources><object id="1"><components><component objectid="1"/></components></object></resources><build><item objectid="1"/></build></model>`)))
  assert.throws(() => parseBambu(fixture(`<model xmlns="${core}"><resources><object id="1">${mesh().replace('v3="2"', 'v3="99"')}</object></resources><build><item objectid="1"/></build></model>`)))
})

test('standard 3MF continues through the existing material and texture loader', () => {
  assert.equal(parseBambu(zipSync({ '3D/3dmodel.model': strToU8(`<model xmlns="${core}"/>`) })), null)
})

test('color choices follow stable sparse source IDs and merge physical colors without merging original groups', () => {
  const groups = [{ id: groupIdForSlot(1), slot: 1, originalColorHex: '#FFFFFF' }, { id: groupIdForSlot(3), slot: 3, originalColorHex: '#D02727' }]
  const original = initializeAssignments(groups, [])
  assert.deepEqual(original.map(a => a.colorCode), ['WHITE', 'RED'])
  const chosen = initializeAssignments(groups, [{ groupId: groups[1].id, colorCode: 'BLUE' }, { groupId: groups[0].id, colorCode: 'BLUE' }])
  assert.equal(chosen.length, 2)
  assert.deepEqual(assignedColors(chosen), ['BLUE'])
  assert.equal(chosen[1].groupId, 'filament:00000003')
  assert.equal(colorGroupNumber(chosen[1].groupId, 2), 3)
  assert.equal(original[1].colorCode, 'RED')
})

test('saved legacy color names restore canonical inventory codes without losing source groups', () => {
  const groups = [{ id: groupIdForSlot(1), slot: 1, originalColorHex: '#FFFFFF' },
    { id: groupIdForSlot(2), slot: 2, originalColorHex: '#FFFFFF' }]
  const saved = [{ groupId: groups[0].id, colorCode: 'SIGNALRED' }, { groupId: groups[1].id, colorCode: 'GREY' }]
  assert.deepEqual(initializeAssignments(groups, saved).map(a => a.colorCode), ['RED', 'GRAY'])
  assert.deepEqual(assignedColors([{ groupId: groups[0].id, colorCode: 'SIGNALRED' },
    { groupId: groups[1].id, colorCode: 'RED' }]), ['RED'])
})

const directory = process.env.PRINTGRID_3MF_TEST_DIRECTORY
for (const [name, expectedColors, expectedPlates] of [
  ['Peppermint Express.3mf', ['#D02727', '#FFFFFF'], ['1']],
  ['gravity-phone-stand.3mf', ['#000000', '#FEC600'], ['1', '2', '3']],
]) {
  test(`real sample: ${name}`, { skip: !directory }, async () => {
    const started = performance.now()
    const preview = parseBambu(new Uint8Array(await readFile(join(directory, name))))
    assert.deepEqual([...new Set(preview.meshes.map(m => m.color))].sort(), expectedColors)
    assert.deepEqual(preview.plates, expectedPlates)
    for (const mesh of preview.meshes) assert.equal(mesh.positions.length, mesh.normals.length)
    console.log(JSON.stringify({ file: name, triangles: preview.triangleCount, colors: expectedColors, plates: preview.plates,
      meshes: preview.meshes.map(m => ({ slot: m.slot, plate: m.plate, faces: m.positions.length / 9 })), milliseconds: Math.round(performance.now() - started) }))
  })
}
