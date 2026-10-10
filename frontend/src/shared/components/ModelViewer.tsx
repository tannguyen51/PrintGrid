import { Component, Suspense, useMemo, type ReactNode } from 'react'
import { Canvas, useLoader } from '@react-three/fiber'
import * as THREE from 'three'
import { Environment, Lightformer, OrbitControls, useGLTF, useProgress } from '@react-three/drei'
import { STLLoader } from 'three/examples/jsm/loaders/STLLoader.js'
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js'
import { Box, CircularProgress, Stack, Typography, type SxProps } from '@mui/material'

/**
 * Reusable 3D viewer, extracted from the login showcase (07/10) and widened to the
 * real customer formats (08/10) so the model library can preview what people upload:
 *   GLB/GLTF → drei useGLTF · STL → STLLoader · OBJ → OBJLoader
 * 3MF is a ZIP package and stays unsupported here — the drawer says so instead of
 * showing an empty frame.
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

function GltfModel({ url, fit }: { url: string; fit: boolean }) {
  const { scene } = useGLTF(url)
  // Clone so fitting never mutates drei's cached scene (the same URL can be open
  // in two places at once). Geometries and materials stay shared.
  const object = useMemo(() => scene.clone(true), [scene])
  return <Fitted object={object} fit={fit} />
}

function StlModel({ url, fit }: { url: string; fit: boolean }) {
  const geometry = useLoader(STLLoader, url)
  const mesh = useMemo(
    () =>
      new THREE.Mesh(
        geometry,
        new THREE.MeshStandardMaterial({ color: '#E9E9EC', metalness: 0.2, roughness: 0.45 }),
      ),
    [geometry],
  )
  return <Fitted object={mesh} fit={fit} />
}

function ObjModel({ url, fit }: { url: string; fit: boolean }) {
  const object = useLoader(OBJLoader, url)
  return <Fitted object={object} fit={fit} />
}

interface SceneProps {
  url: string
  format: string
  fit: boolean
  cameraPosition: [number, number, number]
  autoRotate: boolean
  background: string
}

function Scene({ url, format, fit, cameraPosition, autoRotate, background }: SceneProps) {
  const normalized = (format ?? '').trim().toUpperCase()

  return (
    <Canvas
      camera={{ position: cameraPosition, fov: 40 }}
      dpr={[1, 1.75]}
      style={{ background }}
      gl={{ toneMapping: THREE.ACESFilmicToneMapping, toneMappingExposure: 1.5 }}
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
      <Environment resolution={256} frames={1}>
        <Lightformer intensity={3.5} position={[0, 5, 0]} rotation={[-Math.PI / 2, 0, 0]} scale={[8, 8, 1]} />
        <Lightformer intensity={3} color="#ffffff" position={[0, 2, -6]} rotation={[0, 0, 0]} scale={[10, 2, 1]} />
        <Lightformer intensity={2.5} color="#3b82f6" position={[-5, 2, 3]} rotation={[0, Math.PI / 2, 0]} scale={[6, 2, 1]} />
        <Lightformer intensity={2.5} color="#8B5CF6" position={[5, 2, 3]} rotation={[0, -Math.PI / 2, 0]} scale={[6, 2, 1]} />
      </Environment>

      <Suspense fallback={null}>
        {normalized === 'STL' ? (
          <StlModel url={url} fit={fit} />
        ) : normalized === 'OBJ' ? (
          <ObjModel url={url} fit={fit} />
        ) : (
          <GltfModel url={url} fit={fit} />
        )}
      </Suspense>

      {/* Full mouse interaction: drag to rotate, scroll to zoom, right-drag to pan.
          Auto-rotate is gentle and pauses the moment the user grabs the model. */}
      <OrbitControls
        enableZoom
        enablePan
        enableDamping
        dampingFactor={0.08}
        autoRotate={autoRotate}
        autoRotateSpeed={0.5}
        minDistance={3}
        maxDistance={14}
      />
    </Canvas>
  )
}

/* ── DOM-layer ErrorBoundary around <Canvas> (catches 3D crashes) ── */
class SceneBoundary extends Component<{ children: ReactNode; fallback?: ReactNode }, { error: boolean }> {
  state = { error: false }
  static getDerivedStateFromError() {
    return { error: true }
  }
  render() {
    if (this.state.error) {
      return (
        this.props.fallback ?? (
          <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center', p: 4 }}>
            <Typography color="rgba(255,255,255,0.55)" align="center">
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

export interface ModelViewerProps {
  url: string
  /** Wire format of the file behind `url`: GLB (default), GLTF, STL or OBJ. */
  format?: string
  /** Centre and scale the model to fit the camera — use for uploaded parts. */
  fit?: boolean
  /** Any valid height; pass '100%' to fill a positioned parent. */
  height?: number | string
  autoRotate?: boolean
  cameraPosition?: [number, number, number]
  background?: string
  sx?: SxProps
}

export function ModelViewer({
  url,
  format = 'GLB',
  fit = false,
  height = '100%',
  autoRotate = true,
  cameraPosition = [6, 4.2, 8],
  background = '#000000',
  sx,
}: ModelViewerProps) {
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
        <SceneBoundary>
          <Scene
            url={url}
            format={format}
            fit={fit}
            cameraPosition={cameraPosition}
            autoRotate={autoRotate}
            background={background}
          />
          <ModelLoader />
        </SceneBoundary>
      </Box>
    </Box>
  )
}