import { Component, Suspense, type ReactNode } from 'react'
import { Canvas } from '@react-three/fiber'
import * as THREE from 'three'
import { Environment, Lightformer, OrbitControls, useGLTF, useProgress } from '@react-three/drei'
import { Stack, Typography, Box, CircularProgress } from '@mui/material'
import { useTheme } from '@mui/material/styles'

/**
 * MODEL_URL
 * File lives at frontend/public/models/placeholder-cube_5.glb.
 * Vite serves `public/` at root, so the fetch URL is `/models/placeholder-cube_5.glb`.
 */
const MODEL_URL = '/models/placeholder-cube_5.glb'

/* ── GLB loader — inside the R3F Canvas: only R3F primitives here ── */
function ModelView() {
  const { scene } = useGLTF(MODEL_URL)
  // Render the scene as-is; the model's own materials/background are preserved.
  return <primitive object={scene} />
}

/* ── 3D Canvas ──
 * CRITICAL: MUI/DOM components (Stack, Typography, CircularProgress) must NOT go
 * inside <Canvas>. The WebGL scene graph only accepts R3F primitives. Loading and
 * error UI therefore live at the DOM layer (LoginShowcase), never inside <Suspense>
 * within the Canvas tree.
 */
function Scene() {
  return (
    <Canvas
      // Default camera pulled back + angled to the cube's corner (front/side/top
      // faces all visible), so it clearly reads as a 3D object on load.
      camera={{ position: [6, 4.2, 8], fov: 40 }}
      dpr={[1, 1.75]}
      // Pure black background — matches the login area's background.default (#050505).
      style={{ background: '#050505' }}
      gl={{ toneMapping: THREE.ACESFilmicToneMapping, toneMappingExposure: 1.5 }}
    >
      {/* Fills the scene to #050505 so the 3D view is a seamless black, identical
          to the login panel background. */}
      <color attach="background" args={['#050505']} />

      {/* ── Lighting: bright + rim lights to pick out every edge/line ── */}
      <ambientLight intensity={0.6} />
      {/* Key: bright white from the front-top-left */}
      <directionalLight position={[5, 8, 5]} intensity={2.6} color="#ffffff" />
      {/* Rim/back light: white from behind-bottom — silhouettes every edge */}
      <directionalLight position={[-4, -2, -8]} intensity={2.4} color="#ffffff" />
      {/* Cool rim from left */}
      <directionalLight position={[-8, 1, 1]} intensity={1.8} color="#6ea8ff" />
      {/* Purple rim from right */}
      <directionalLight position={[8, 1, 1]} intensity={1.8} color="#A78BFA" />
      {/* Small fills */}
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
        <ModelView />
      </Suspense>

      {/* Full mouse interaction: drag to rotate, scroll to zoom, right-drag to pan.
          Auto-rotate is gentle and pauses the moment the user grabs the model. */}
      <OrbitControls
        enableZoom
        enablePan
        enableDamping
        dampingFactor={0.08}
        autoRotate
        autoRotateSpeed={0.5}
        minDistance={3}
        maxDistance={14}
      />
    </Canvas>
  )
}

/* ── DOM-layer ErrorBoundary around <Canvas> (catches 3D crashes) ── */
class SceneBoundary extends Component<{ children: ReactNode }, { error: boolean }> {
  state = { error: false }
  static getDerivedStateFromError() {
    return { error: true }
  }
  render() {
    return this.state.error ? (
      <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center', p: 4 }}>
        <Typography color="rgba(255,255,255,0.55)" align="center">
          Không thể hiển thị cảnh 3D.
        </Typography>
      </Stack>
    ) : (
      this.props.children
    )
  }
}

/* ── Spinner shown at the DOM layer while the .glb streams in ── */
function ModelLoader() {
  const { active } = useProgress()
  if (!active) return null
  return (
    <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }}>
      <CircularProgress color="secondary" size={40} />
    </Stack>
  )
}

/**
 * Right showcase panel (on desktop, after the layout flip).
 * Full-height: contains the 3D glb model + brand header + stat chips.
 * On mobile (stacked) it appears below the login form.
 *
 * When `frame` is true (used inside the AuthDialog), the panel gets a rounded
 * border so the 3D area reads as its own framed panel next to the form.
 */
export function LoginShowcase({ frame = false }: { frame?: boolean }) {
  const theme = useTheme()

  return (
    <Box
      sx={{
        flex: { xs: '0 0 320px', md: '1 1 52%' },
        minWidth: 0,
        position: 'relative',
        overflow: 'hidden',
        color: 'common.white',
        background: theme.custom.showcaseBg, // #050505 — seamless with login panel
        ...(frame
          ? {
              flex: '1 1 auto',
              m: { md: '6px 8px 6px 2px' },
              borderRadius: { md: 5 },
              border: '1px solid rgba(255,255,255,0.12)',
              boxShadow: '0 24px 80px rgba(0,0,0,0.5)',
              overflow: 'hidden',
            }
          : {}),
      }}
    >
      {/* ── Brand header: top 32px, left 40px ── */}
      <Stack direction="row" alignItems="center" spacing={1.5} sx={{ position: 'absolute', top: 32, left: 40, zIndex: 2 }}>
        <BrandMark />
        <Box>
          <Typography sx={{ fontSize: '1.15rem', fontWeight: 700, lineHeight: 1, letterSpacing: '0.02em', color: 'common.white' }}>
            PrintGrid
          </Typography>
          <Typography
            sx={{
              fontSize: '0.66rem',
              color: 'rgba(255,255,255,0.55)',
              mt: 0.5,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
            }}
          >
            In 3D · Quản lý thông minh
          </Typography>
        </Box>
      </Stack>

      {/* ── 3D canvas (DOM-boundary + loading overlay outside the WebGL tree) ── */}
      <Box sx={{ position: 'absolute', inset: 0, zIndex: 1 }}>
        <SceneBoundary>
          <Scene />
          <ModelLoader />
        </SceneBoundary>
      </Box>

      {/* ── Stat chips (no boxes) ── */}
      <Stack
        direction="row"
        spacing={{ xs: 5, md: 7 }}
        sx={{ position: 'absolute', bottom: { xs: 20, md: 44 }, left: 40, zIndex: 2 }}
      >
        <StatValue value="128" label="Orders" />
        <StatValue value="64" label="Printing" />
        <StatValue value="12" label="Machines" />
      </Stack>
    </Box>
  )
}

/* ── Brand glyph (grid + dot, original artwork) ── */
function BrandMark() {
  return (
    <Box
      sx={{
        width: 40,
        height: 40,
        borderRadius: 2.5,
        display: 'grid',
        placeItems: 'center',
        background: 'linear-gradient(140deg, #8B5CF6, #7C3AED)',
        boxShadow: '0 8px 22px rgba(139,92,246,0.4)',
      }}
    >
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 10px)', gap: 2.5 }}>
        <Box sx={{ width: 10, height: 10, borderRadius: 1, bgcolor: 'rgba(255,255,255,0.4)' }} />
        <Box sx={{ width: 10, height: 10, borderRadius: 1, bgcolor: 'common.white' }} />
        <Box sx={{ width: 10, height: 10, borderRadius: 1, bgcolor: 'rgba(5,5,5,0.85)' }} />
        <Box sx={{ width: 10, height: 10, borderRadius: 1, bgcolor: 'rgba(255,255,255,0.4)' }} />
      </Box>
    </Box>
  )
}

/* ── Stat: number 100%, label 55% ── */
function StatValue({ value, label }: { value: string; label: string }) {
  return (
    <Box>
      <Typography sx={{ fontSize: '1.5rem', fontWeight: 800, color: 'rgba(255,255,255,1)', lineHeight: 1.1 }}>
        {value}
      </Typography>
      <Typography sx={{ fontSize: '0.78rem', color: 'rgba(255,255,255,0.55)' }}>{label}</Typography>
    </Box>
  )
}