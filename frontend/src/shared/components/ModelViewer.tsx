import { Component, Suspense, useEffect, useMemo, useState, type ReactNode } from 'react'
import { Canvas, useLoader, useThree } from '@react-three/fiber'
import * as THREE from 'three'
import { Environment, Lightformer, OrbitControls, useGLTF, useProgress } from '@react-three/drei'
import { STLLoader } from 'three/examples/jsm/loaders/STLLoader.js'
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js'
import { ThreeMfPreviewLoader } from '../threeMf/ThreeMfPreviewLoader'
import { groupIdForSlot, type ModelColorGroup } from '../threeMf/colorGroups'
import { Box, Button, CircularProgress, MenuItem, Select, Stack, Typography, type SxProps } from '@mui/material'

/**
 * Reusable 3D viewer, extracted from the login showcase (07/10) and widened to the
 * real customer formats (08/10) so the model library can preview what people upload:
 *   GLB/GLTF → drei useGLTF · STL → STLLoader · OBJ → OBJLoader
 *   3MF → worker-based Bambu painting/filament reader, standard material fallback.
 *
 * CRITICAL: MUI/DOM components must NOT go inside <Canvas> — the WebGL scene graph
 * only accepts R3F primitives. Loading/error UI lives at the DOM layer, as siblings
 * of the Canvas inside the boundary below. Every loader goes through drei's useLoader,
 * so the single useProgress spinner covers all three formats.
 */

/**
 * Centres the object at the origin and scales it into a fixed viewing box, so a
 * 200 mm part and a 5 mm part both frame correctly with one camera. Display-only —
 * real dimensions are shown as numbers beside the viewer.
 */
function fitTransform(object: THREE.Object3D, targetSize = 5) {
  const box = new THREE.Box3().setFromObject(object)
  const size = box.getSize(new THREE.Vector3())
  const center = box.getCenter(new THREE.Vector3())
  const maxDimension = Math.max(size.x, size.y, size.z) || 1
  const scale = targetSize / maxDimension
  return { scale, position: [-center.x * scale, -center.y * scale, -center.z * scale] as [number, number, number] }
}

function Fitted({ object, fit, children }: { object: THREE.Object3D; fit: boolean; children?: ReactNode }) {
  const transform = useMemo(() => (fit ? fitTransform(object) : null), [object, fit])
  if (!transform) return <>{children ?? <primitive object={object} />}</>
  return (
    <group scale={transform.scale} position={transform.position}>
      {children ?? <primitive object={object} />}
    </group>
  )
}

/** Materials belong to a viewer instance; cached models retain their original appearance. */
function usePreviewColor(source: THREE.Object3D, color?: string) {
  const invalidate = useThree(state => state.invalidate)
  const { object, materials } = useMemo(() => {
    const object = source.clone(true)
    const materials: { original: THREE.Material; copy: THREE.Material }[] = []
    object.traverse(child => {
      if (!(child instanceof THREE.Mesh)) return
      const originals = Array.isArray(child.material) ? child.material : [child.material]
      const copies = originals.map(original => {
        const copy = original.clone()
        materials.push({ original, copy })
        return copy
      })
      child.material = Array.isArray(child.material) ? copies : copies[0]
    })
    return { object, materials }
  }, [source])
  useEffect(() => {
    for (const { original, copy } of materials) {
      copy.copy(original)
      if (color && 'color' in copy) {
        const material = copy as THREE.MeshStandardMaterial
        material.color.set(color)
        material.setValues({ map: null, vertexColors: false })
      }
      // Private material clones are mutable GPU resources, not cached loader assets.
      // oxlint-disable-next-line react/immutability
      copy.needsUpdate = true
    }
    invalidate()
  }, [materials, color, invalidate])
  useEffect(() => () => materials.forEach(m => m.copy.dispose()), [materials])
  return object
}

function GltfModel({ url, fit, color }: { url: string; fit: boolean; color?: string }) {
  const { scene } = useGLTF(url)
  // Clone so fitting never mutates drei's cached scene (the same URL can be open
  // in two places at once). Geometries and materials stay shared.
  const object = usePreviewColor(scene, color)
  useEffect(() => {
    console.info('[preview3d] GLTF loaded', { children: object.children.length })
  }, [object])
  return <Fitted object={object} fit={fit} />
}

function StlModel({ url, fit, color }: { url: string; fit: boolean; color?: string }) {
  const geometry = useLoader(STLLoader, url)
  useEffect(() => {
    console.info('[preview3d] STL parsed', {
      vertices: geometry.attributes.position?.count ?? 0,
      indexed: Boolean(geometry.index),
    })
  }, [geometry])
  const mesh = useMemo(
    () =>
      new THREE.Mesh(
        geometry,
        new THREE.MeshStandardMaterial({ color: color ?? '#E9E9EC', metalness: 0.2, roughness: 0.45 }),
      ),
    [geometry, color],
  )
  useEffect(() => () => mesh.material.dispose(), [mesh])
  return <Fitted object={mesh} fit={fit} />
}

function ObjModel({ url, fit, color }: { url: string; fit: boolean; color?: string }) {
  const source = useLoader(OBJLoader, url)
  const object = usePreviewColor(source, color)
  useEffect(() => {
    console.info('[preview3d] OBJ loaded', { children: object.children.length })
  }, [object])
  return <Fitted object={object} fit={fit} />
}

interface ThreeMfInfo { palette: string[]; plates: string[]; slots: number[]; surfacePainting: boolean }

interface ColorInteraction {
  colorOverrides?: Record<string, string>
  selectedColorGroup?: string | null
  onColorGroupSelect?: (id: string | null) => void
}

function ThreeMfModel({ url, fit, plate, onInfo, colorOverrides, selectedColorGroup, onColorGroupSelect }:
  { url: string; fit: boolean; plate: string; onInfo: (url: string, info: ThreeMfInfo) => void } & ColorInteraction) {
  const source = useLoader(ThreeMfPreviewLoader, url)
  const fallback = usePreviewColor(source, colorOverrides?.all)
  const invalidate = useThree(state => state.invalidate)
  const object = useMemo(() => {
    if (!source.userData.plates) return fallback
    const selected = source.clone(false)
    for (const child of source.children) {
      if (plate === 'ALL' || child.userData.plate === plate) {
        const clone = child.clone() as THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial>
        clone.material = clone.material.clone()
        selected.add(clone)
      }
    }
    return selected
  }, [source, plate, fallback])
  useEffect(() => {
    if (!source.userData.plates) return
    object.children.forEach(child => {
      const mesh = child as THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial>
      const id = groupIdForSlot(mesh.userData.slot)
      const dimmed = !!selectedColorGroup && selectedColorGroup !== id
      mesh.material.color.set(colorOverrides?.[id] ?? source.userData.palette[mesh.userData.slot - 1])
      mesh.material.transparent = dimmed
      mesh.material.opacity = dimmed ? .16 : 1
      mesh.material.depthWrite = !dimmed
      mesh.material.emissive.set(selectedColorGroup === id ? mesh.material.color : '#000000')
      mesh.material.emissiveIntensity = selectedColorGroup === id ? .12 : 0
      mesh.material.needsUpdate = true
      mesh.renderOrder = dimmed ? 1 : 0
    })
    invalidate()
  }, [object, source, colorOverrides, selectedColorGroup, invalidate])
  useEffect(() => () => {
    if (source.userData.plates) object.children.forEach(child => (child as THREE.Mesh<THREE.BufferGeometry, THREE.Material>).material.dispose())
  }, [object, source])
  useEffect(() => {
    onInfo(url, { palette: source.userData.palette ?? [], plates: source.userData.plates ?? [], surfacePainting: !!source.userData.surfacePainting,
      slots: [...new Set(source.children.map(c => c.userData.slot as number).filter(Boolean))].sort((a, b) => a - b) })
  }, [source, url, onInfo])
  return <Fitted object={object} fit={fit}><primitive object={object} onClick={onColorGroupSelect ? (event: { stopPropagation: () => void; object: THREE.Object3D }) => {
    const slot = event.object.userData.slot
    if (!slot) return
    event.stopPropagation()
    const id = groupIdForSlot(slot)
    onColorGroupSelect(selectedColorGroup === id ? null : id)
  } : undefined} /></Fitted>
}

interface SceneProps extends ColorInteraction {
  url: string
  format: string
  fit: boolean
  cameraPosition: [number, number, number]
  autoRotate: boolean
  interactive: boolean
  background: string
  plate: string
  onThreeMfInfo: (url: string, info: ThreeMfInfo) => void
  onContextLost: () => void
  onContextRestored: () => void
}

function Scene({ url, format, fit, cameraPosition, autoRotate, interactive, background, plate, onThreeMfInfo, colorOverrides,
  selectedColorGroup, onColorGroupSelect, onContextLost, onContextRestored }: SceneProps) {
  const normalized = (format ?? '').trim().toUpperCase()

  useEffect(() => {
    console.info('[preview3d] scene mount', {
      format: normalized,
      fit,
      autoRotate,
      urlPrefix: url.slice(0, 32),
    })
    return () => console.info('[preview3d] scene unmount')
  }, [normalized, fit, autoRotate, url])

  return (
    <Canvas
      frameloop={autoRotate ? 'always' : 'demand'}
      camera={{ position: cameraPosition, fov: 40 }}
      dpr={[1, 1.5]}
      style={{ background }}
      gl={{ toneMapping: THREE.ACESFilmicToneMapping, toneMappingExposure: normalized === '3MF' ? 1 : 1.5 }}
      onCreated={({ gl }) => {
        const ctx = gl.getContext()
        const debug = ctx.getExtension('WEBGL_debug_renderer_info')
        console.info('[preview3d] webgl created', {
          version: ctx.getParameter(ctx.VERSION),
          renderer: debug ? ctx.getParameter(debug.UNMASKED_RENDERER_WEBGL) : 'unknown',
          maxTextureSize: ctx.getParameter(ctx.MAX_TEXTURE_SIZE),
        })
        gl.domElement.addEventListener('webglcontextlost', () => {
          console.warn('[preview3d] WEBGL CONTEXT LOST — GPU reset/out of memory. ' +
            'Close other 3D tabs, retry, or check chrome://gpu')
          onContextLost()
        })
        gl.domElement.addEventListener('webglcontextrestored', () => {
          console.info('[preview3d] WEBGL CONTEXT RESTORED')
          onContextRestored()
        })
      }}
    >
      <color attach="background" args={[background]} />

      {/* ── Lighting: bright + rim lights to pick out every edge/line ── */}
      <ambientLight intensity={0.6} />
      <directionalLight position={[5, 8, 5]} intensity={2.6} color="#ffffff" />
      <directionalLight position={[-4, -2, -8]} intensity={2.4} color="#ffffff" />
      <directionalLight position={[-8, 1, 1]} intensity={1.8} color="#6ea8ff" />
      <directionalLight position={[8, 1, 1]} intensity={1.8} color="#A78BFA" />
      <pointLight position={[-5, 2, -3]} intensity={0.8} color="#3b82f6" />
      <pointLight position={[3, 0, 4]} intensity={1} color="#8B5CF6" />

      {/* Procedural studio environment (no external HDR fetch) — gives clearcoat/
          transmission materials the reflections that make them look rich, not dark */}
      <Environment resolution={128} frames={1}>
        <Lightformer intensity={3.5} position={[0, 5, 0]} rotation={[-Math.PI / 2, 0, 0]} scale={[8, 8, 1]} />
        <Lightformer intensity={3} color="#ffffff" position={[0, 2, -6]} rotation={[0, 0, 0]} scale={[10, 2, 1]} />
        <Lightformer intensity={2.5} color="#3b82f6" position={[-5, 2, 3]} rotation={[0, Math.PI / 2, 0]} scale={[6, 2, 1]} />
        <Lightformer intensity={2.5} color="#8B5CF6" position={[5, 2, 3]} rotation={[0, -Math.PI / 2, 0]} scale={[6, 2, 1]} />
      </Environment>

      <Suspense fallback={null}>
        {normalized === 'STL' ? (
          <StlModel url={url} fit={fit} color={colorOverrides?.all} />
        ) : normalized === 'OBJ' ? (
          <ObjModel url={url} fit={fit} color={colorOverrides?.all} />
        ) : normalized === '3MF' ? (
          <ThreeMfModel url={url} fit={fit} plate={plate} onInfo={onThreeMfInfo} colorOverrides={colorOverrides}
            selectedColorGroup={selectedColorGroup} onColorGroupSelect={onColorGroupSelect} />
        ) : (
          <GltfModel url={url} fit={fit} color={colorOverrides?.all} />
        )}
      </Suspense>

      {/* Full mouse interaction: drag to rotate, scroll to zoom, right-drag to pan.
          Auto-rotate is gentle and pauses the moment the user grabs the model. */}
      {interactive && <OrbitControls
        enableZoom
        enablePan
        enableDamping
        dampingFactor={0.08}
        autoRotate={autoRotate}
        autoRotateSpeed={0.5}
        minDistance={3}
        maxDistance={14}
      />}
    </Canvas>
  )
}

/* ── DOM-layer ErrorBoundary around <Canvas> (catches 3D crashes) ── */
class SceneBoundary extends Component<{ children: ReactNode; fallback?: ReactNode; onError?: () => void }, { error: boolean }> {
  state = { error: false }
  static getDerivedStateFromError() {
    return { error: true }
  }
  componentDidCatch(error: Error) {
    console.error('[preview3d] viewer CRASHED', error)
    this.props.onError?.()
  }
  render() {
    if (this.state.error) {
      return (
        this.props.fallback ?? (
          <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center', p: 4 }}>
            <Typography color="text.secondary" align="center">
              Không thể hiển thị cảnh 3D.
            </Typography>
          </Stack>
        )
      )
    }
    return this.props.children
  }
}

/* ── Spinner shown at the DOM layer while the model streams in ── */
function ModelLoader() {
  const { active } = useProgress()
  if (!active) return null
  return (
    <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }}>
      <CircularProgress color="secondary" size={40} />
    </Stack>
  )
}

export interface ModelViewerProps extends ColorInteraction {
  url: string
  /** Wire format of the file behind `url`: GLB (default), GLTF, STL, OBJ or 3MF. */
  format?: string
  /** Centre and scale the model to fit the camera — use for uploaded parts. */
  fit?: boolean
  /** Any valid height; pass '100%' to fill a positioned parent. */
  height?: number | string
  autoRotate?: boolean
  /** Disable mouse controls for decorative previews inside clickable cards. */
  interactive?: boolean
  cameraPosition?: [number, number, number]
  background?: string
  sx?: SxProps
  onColorGroupsChange?: (groups: ModelColorGroup[], details: { surfacePainting: boolean }) => void
  onPreviewError?: () => void
  showColorLegend?: boolean
}

export function ModelViewer({
  url,
  format = 'GLB',
  fit = false,
  height = '100%',
  autoRotate = true,
  interactive = true,
  cameraPosition = [6, 4.2, 8],
  background = '#F5F2FF',
  sx,
  colorOverrides,
  selectedColorGroup,
  onColorGroupSelect,
  onColorGroupsChange,
  onPreviewError,
  showColorLegend = true,
}: ModelViewerProps) {
  // WebGL can lose its GPU context (driver reset, GPU memory, too many 3D tabs).
  // Keep the canvas mounted but tell the user and offer a clean remount.
  const [contextLost, setContextLost] = useState(false)
  const [canvasKey, setCanvasKey] = useState(0)
  const [threeMfInfo, setThreeMfInfo] = useState<{ url: string; info: ThreeMfInfo } | null>(null)
  const [plateSelection, setPlateSelection] = useState<{ url: string; plate: string } | null>(null)
  const info = threeMfInfo?.url === url ? threeMfInfo.info : null
  const plate = plateSelection?.url === url ? plateSelection.plate : 'ALL'
  const onThreeMfInfo = useMemo(() => (sourceUrl: string, info: ThreeMfInfo) => setThreeMfInfo({ url: sourceUrl, info }), [])
  useEffect(() => {
    if (!info) return
    onColorGroupsChange?.(info.slots.map(slot => ({ id: groupIdForSlot(slot), slot, originalColorHex: info.palette[slot - 1] })),
      { surfacePainting: info.surfacePainting })
  }, [info, onColorGroupsChange])

  return (
    <Box
      sx={{
        position: 'relative',
        height: typeof height === 'number' ? `${height}px` : height,
        minHeight: 200,
        overflow: 'hidden',
        bgcolor: background,
        ...sx,
      }}
    >
      <Box sx={{ position: 'absolute', inset: 0, zIndex: 1 }}>
        <SceneBoundary key={`${url}:${canvasKey}`} onError={onPreviewError}>
          <Scene
            key={canvasKey}
            url={url}
            format={format}
            fit={fit}
            cameraPosition={cameraPosition}
            autoRotate={autoRotate}
            interactive={interactive}
            background={background}
            plate={plate}
            onThreeMfInfo={onThreeMfInfo}
            colorOverrides={colorOverrides}
            selectedColorGroup={selectedColorGroup}
            onColorGroupSelect={onColorGroupSelect}
            onContextLost={() => setContextLost(true)}
            onContextRestored={() => setContextLost(false)}
          />
          <ModelLoader />
        </SceneBoundary>
      </Box>

      {info && ((showColorLegend && info.slots.length > 0) || info.plates.length > 1) && (
        <Stack direction="row" gap={1} flexWrap="wrap" alignItems="center"
          sx={{ position: 'absolute', bottom: 10, left: 10, right: 10, zIndex: 2, pointerEvents: 'none' }}>
          {info.plates.length > 1 && <Select size="small" value={plate} aria-label="Chọn bàn in"
            onChange={event => setPlateSelection({ url, plate: event.target.value })}
            sx={{ bgcolor: 'background.paper', fontSize: 12, pointerEvents: 'auto' }}>
            <MenuItem value="ALL">Tất cả {info.plates.length} bàn in</MenuItem>
            {info.plates.map(id => <MenuItem key={id} value={id}>Bàn in {id}</MenuItem>)}
          </Select>}
          {showColorLegend && info.slots.map(slot => <Stack key={slot} direction="row" gap={.75} alignItems="center"
            sx={{ bgcolor: 'background.paper', borderRadius: 1, px: 1, py: .5 }}>
            <Box sx={{ width: 13, height: 13, borderRadius: '50%', bgcolor: info.palette[slot - 1], border: '1px solid #B6B6C0' }} />
            <Typography sx={{ fontSize: 11 }}>Màu {slot} · {info.palette[slot - 1]}</Typography>
          </Stack>)}
        </Stack>
      )}

      {contextLost && (
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            zIndex: 3,
            display: 'grid',
            placeItems: 'center',
            bgcolor: 'rgba(37, 38, 58, 0.28)',
            backdropFilter: 'blur(6px)',
            p: 3,
            textAlign: 'center',
          }}
        >
          <Stack
            spacing={1.5}
            alignItems="center"
            sx={{
              p: 3,
              bgcolor: 'background.paper',
              border: '1px solid',
              borderColor: 'divider',
              borderRadius: 3,
              boxShadow: (t) => t.shadows[8],
            }}
          >
            <Typography color="text.primary" sx={{ fontWeight: 700 }}>
              GPU đã mất kết nối WebGL
            </Typography>
            <Typography color="text.secondary" sx={{ fontSize: '0.85rem', maxWidth: 320 }}>
              Mô hình quá nặng hoặc GPU bị reset. Thử lại, đóng bớt tab 3D khác, hoặc tải lại trang.
            </Typography>
            <Button
              size="small"
              variant="outlined"
              onClick={() => {
                setContextLost(false)
                setCanvasKey((k) => k + 1)
              }}
            >
              Thử lại
            </Button>
          </Stack>
        </Box>
      )}
    </Box>
  )
}
