import { Component, Suspense, useMemo, useRef, type ReactNode } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { Environment, Lightformer } from '@react-three/drei'
import * as THREE from 'three'
import { Stack, Typography } from '@mui/material'

/*
 * Geometric Network — hero 3D scene.
 *
 * A 3×3×3 Rubik-style cube floating on the hero gradient (transparent canvas,
 * no panel background, so there is no visible frame edge). The cube spins
 * gently like a slow sun, and on a CYCLE loop it performs Rubik LAYER TURNS:
 * slices of 9 cells rotate 90° one at a time — xoay cạnh → giữ → trả về —
 * like an invisible hand solving and re-scrambling it. The active slice's
 * hairline edges glow while it moves.
 *
 * Monochrome white-on-black palette per the landing design system.
 * Pure primitives — no external model.
 */

const CYCLE = 10 // s for the full move sequence (fast solve)
const QUARTER = Math.PI / 2

const CELL_SIZE = 0.8 // nearly flush — reads as one solid Rubik block
const CELL_SPACING = 0.82
// Polished silver cells with a faint self-glow — chrome catches the studio
// environment so bright reflections sweep across the faces as the block rolls.
const CORE_COLOR = '#C9CDD5'
const CORE_EMISSIVE = '#9FA7B8'
const EDGE_COLOR = '#E8EBF0'

type Axis = 'x' | 'y' | 'z'

/** One slice move: rotate the layer at `index` around `axis` by ±90°, then return. */
interface Move {
  axis: Axis
  index: number
  dir: 1 | -1
}

// Hand-picked choreography — alternating axes so the motion reads as a solve.
const MOVES: Move[] = [
  { axis: 'y', index: 1, dir: 1 },
  { axis: 'x', index: 0, dir: -1 },
  { axis: 'z', index: -1, dir: 1 },
  { axis: 'y', index: -1, dir: -1 },
  { axis: 'x', index: 1, dir: 1 },
  { axis: 'z', index: 1, dir: -1 },
]

const AXIS_VECTORS: Record<Axis, THREE.Vector3> = {
  x: new THREE.Vector3(1, 0, 0),
  y: new THREE.Vector3(0, 1, 0),
  z: new THREE.Vector3(0, 0, 1),
}

const smooth = (x: number) => x * x * (3 - 2 * x)

/** Angle profile for one move slot: turn (0–40%), hold (40–60%), return (60–100%). */
function moveAngle(u: number, dir: number) {
  if (u < 0.4) return dir * QUARTER * smooth(u / 0.4)
  if (u < 0.6) return dir * QUARTER
  return dir * QUARTER * (1 - smooth((u - 0.6) / 0.4))
}

function RubikCube() {
  const root = useRef<THREE.Group>(null!)
  const cellRefs = useRef<(THREE.Group | null)[]>([])
  const edgeRefs = useRef<(THREE.LineBasicMaterial | null)[]>([])

  // scratch objects reused every frame (no per-frame allocation)
  const scratch = useMemo(
    () => ({
      q: new THREE.Quaternion(),
      v: new THREE.Vector3(),
    }),
    [],
  )

  const cells = useMemo(() => {
    const list: { pos: THREE.Vector3; sliceIndex: Record<Axis, number> }[] = []
    for (let x = -1; x <= 1; x++)
      for (let y = -1; y <= 1; y++)
        for (let z = -1; z <= 1; z++) {
          const pos = new THREE.Vector3(x * CELL_SPACING, y * CELL_SPACING, z * CELL_SPACING)
          list.push({ pos, sliceIndex: { x, y, z } })
        }
    return list
  }, [])

  const { boxGeo, edgeGeo } = useMemo(() => {
    const box = new THREE.BoxGeometry(CELL_SIZE, CELL_SIZE, CELL_SIZE)
    return { boxGeo: box, edgeGeo: new THREE.EdgesGeometry(box) }
  }, [])

  useFrame((state) => {
    const t = state.clock.elapsedTime

    // Which move is active and how far through it we are.
    const slot = Math.floor(((t % CYCLE) / CYCLE) * MOVES.length)
    const u = (((t % CYCLE) / CYCLE) * MOVES.length) % 1
    const move = MOVES[slot]
    const angle = moveAngle(u, move.dir)

    scratch.q.setFromAxisAngle(AXIS_VECTORS[move.axis], angle)

    cells.forEach((c, i) => {
      const g = cellRefs.current[i]
      if (!g) return
      const active = c.sliceIndex[move.axis] === move.index
      if (active) {
        g.position.copy(scratch.v.copy(c.pos).applyQuaternion(scratch.q))
        g.quaternion.copy(scratch.q)
      } else {
        g.position.copy(c.pos)
        g.quaternion.identity()
      }
      const em = edgeRefs.current[i]
      if (em) {
        // damp toward the target so slices fade in/out of "active" smoothly
        const target = active ? 1 : 0.6
        em.opacity += (target - em.opacity) * 0.12
      }
    })

    // whole block rolls top→bottom (primary X spin) with a gentle yaw sway
    root.current.rotation.x = t * 0.95
    root.current.rotation.y = Math.sin(t * 0.24) * 0.32
  })

  return (
    <group ref={root}>
      {cells.map((c, i) => (
        <group key={i} ref={(el) => { cellRefs.current[i] = el }} position={c.pos.toArray()}>
          <mesh geometry={boxGeo}>
            <meshPhysicalMaterial
              color={CORE_COLOR}
              emissive={CORE_EMISSIVE}
              emissiveIntensity={0.16}
              roughness={0.3}
              metalness={0.8}
              clearcoat={0.7}
              clearcoatRoughness={0.22}
              envMapIntensity={1.0}
            />
          </mesh>
          <lineSegments geometry={edgeGeo}>
            <lineBasicMaterial
              ref={(el) => { edgeRefs.current[i] = el }}
              color={EDGE_COLOR}
              transparent
              opacity={0.6}
            />
          </lineSegments>
        </group>
      ))}
    </group>
  )
}

/** No glow/halo meshes: any full-canvas quad clips square edges against the
 *  canvas frame on top of the hero gradient. Only the cube is rendered. */
function RubikScene() {
  const outer = useRef<THREE.Group>(null!)

  useFrame((state) => {
    // Pointer parallax — the whole cube tilts gently toward the cursor.
    outer.current.rotation.y += (state.pointer.x * 0.3 - outer.current.rotation.y) * 0.05
    outer.current.rotation.x += (-state.pointer.y * 0.18 - outer.current.rotation.x) * 0.05
  })

  return (
    <group ref={outer}>
      <RubikCube />
    </group>
  )
}

/* ── DOM-layer boundary, same pattern as LoginShowcase ── */
class CanvasBoundary extends Component<{ children: ReactNode }, { error: boolean }> {
  state = { error: false }
  static getDerivedStateFromError() {
    return { error: true }
  }
  render() {
    return this.state.error ? (
      <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }}>
        <Typography color="rgba(241,241,241,0.4)" sx={{ fontSize: 14 }}>
          (WebGL không khả dụng — đã bỏ qua hình 3D)
        </Typography>
      </Stack>
    ) : (
      this.props.children
    )
  }
}

const prefersReducedMotion =
  typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches

export function GeometricNetwork() {
  return (
    <CanvasBoundary>
      <Canvas
        camera={{ position: [0, 0.4, 6.4], fov: 40 }}
        dpr={[1, 1.75]}
        // alpha canvas, no <color> background → the hero gradient shows straight
        // through the scene and there is no visible panel edge.
        gl={{
          antialias: true,
          alpha: true,
          toneMapping: THREE.ACESFilmicToneMapping,
          toneMappingExposure: 1.35,
        }}
        style={{ background: 'transparent' }}
        frameloop={prefersReducedMotion ? 'demand' : 'always'}
      >
        {/* ── Studio lighting: key + cool rim + low fill, reflections from a
               procedural environment (baked once — no external HDR fetch) ── */}
        <ambientLight intensity={0.25} />
        <directionalLight position={[5, 7, 4]} intensity={2.3} color="#ffffff" />
        <directionalLight position={[-4, 3, -6]} intensity={1.9} color="#DCE3FF" />
        <directionalLight position={[-6, -1, 3]} intensity={0.55} color="#9A9AA2" />
        <Environment resolution={128} frames={1}>
          <Lightformer intensity={4} position={[0, 4, 0]} rotation={[-Math.PI / 2, 0, 0]} scale={[6, 6, 1]} color="#ffffff" />
          <Lightformer intensity={2.4} position={[-4, 1, 2]} rotation={[0, Math.PI / 2, 0]} scale={[3, 4, 1]} color="#CFD6EE" />
          <Lightformer intensity={2.6} position={[4, -1, -2]} rotation={[0, -Math.PI / 2, 0]} scale={[3, 4, 1]} color="#ffffff" />
        </Environment>
        <Suspense fallback={null}>
          <RubikScene />
        </Suspense>
      </Canvas>
    </CanvasBoundary>
  )
}
