import { useRef } from 'react'
import { useFrame } from '@react-three/fiber'
import { RoundedBox, Cylinder, Cone } from '@react-three/drei'
import type { Group } from 'three'

const DARK_METAL = '#283548' as const
const MID_METAL = '#334155' as const
const BED_DARK = '#1a2b45' as const
const BLUE_ACCENT = '#3b82f6' as const
const ACCENT = '#8B5CF6' as const

/**
 * Stylized FDM 3D printer built purely from geometric primitives.
 * No external .glb assets — brand-colored meshes with a few gentle animations.
 */
export function PrinterModel() {
  const headRef = useRef<Group>(null)
  const spoolRef = useRef<Group>(null)

  useFrame(({ clock }) => {
    const t = clock.getElapsedTime()
    // Print head slides left–right
    if (headRef.current) headRef.current.position.x = Math.sin(t * 0.7) * 0.32
    // Filament spool rotates
    if (spoolRef.current) spoolRef.current.rotation.z = t * 0.35
  })

  return (
    <group position={[0, -0.55, 0]}>
      {/* ── Base plate ── */}
      <RoundedBox args={[2.3, 0.22, 1.95]} radius={0.05} smoothness={4} position={[0, 0.11, 0]}>
        <meshStandardMaterial color="#1e293b" metalness={0.6} roughness={0.35} />
      </RoundedBox>

      {/* ── Print bed (emits soft orange glow) ── */}
      <RoundedBox args={[1.4, 0.06, 1.22]} radius={0.03} smoothness={4} position={[0, 0.26, 0]}>
        <meshStandardMaterial
          color={BED_DARK}
          emissive={ACCENT}
          emissiveIntensity={0.35}
          metalness={0.4}
          roughness={0.25}
        />
      </RoundedBox>

      {/* ── Four frame pillars ── */}
      <Pillar pos={[-0.92, 0, 0.87]} />
      <Pillar pos={[0.92, 0, 0.87]} />
      <Pillar pos={[-0.92, 0, -0.77]} />
      <Pillar pos={[0.92, 0, -0.77]} />

      {/* ── Top cross rails ── */}
      <RoundedBox args={[2.24, 0.13, 0.11]} radius={0.03} smoothness={3} position={[0, 1.82, 0.87]}>
        <meshStandardMaterial color={DARK_METAL} metalness={0.55} roughness={0.38} />
      </RoundedBox>
      <RoundedBox args={[2.24, 0.13, 0.11]} radius={0.03} smoothness={3} position={[0, 1.82, -0.77]}>
        <meshStandardMaterial color={DARK_METAL} metalness={0.55} roughness={0.38} />
      </RoundedBox>

      {/* ── Blue gantry beam (Y-axis) ── */}
      <RoundedBox args={[1.8, 0.12, 0.1]} radius={0.03} smoothness={3} position={[0, 1.64, 0.05]}>
        <meshStandardMaterial color={BLUE_ACCENT} metalness={0.5} roughness={0.3} />
      </RoundedBox>

      {/* ── Print head (animated X) ── */}
      <group ref={headRef} position={[0, 1.56, 0.05]}>
        <RoundedBox args={[0.32, 0.26, 0.32]} radius={0.04} smoothness={3}>
          <meshStandardMaterial color={MID_METAL} metalness={0.6} roughness={0.3} />
        </RoundedBox>
        {/* Nozzle cone */}
        <Cone args={[0.06, 0.14, 16]} position={[0, -0.2, 0]} rotation={[Math.PI, 0, 0]}>
          <meshStandardMaterial
            color={ACCENT}
            emissive={ACCENT}
            emissiveIntensity={0.55}
            metalness={0.7}
            roughness={0.2}
          />
        </Cone>
      </group>

      {/* ── Filament spool (rotates) ── */}
      <group ref={spoolRef} position={[0.72, 2.1, 0.05]}>
        <Cylinder args={[0.3, 0.3, 0.16, 28]} rotation={[0, 0, Math.PI / 2]}>
          <meshStandardMaterial color={BLUE_ACCENT} metalness={0.3} roughness={0.5} />
        </Cylinder>
        <Cylinder args={[0.13, 0.13, 0.18, 28]} rotation={[0, 0, Math.PI / 2]} position={[0, 0.03, 0]}>
          <meshStandardMaterial color="#1e293b" metalness={0.5} roughness={0.4} />
        </Cylinder>
      </group>
    </group>
  )
}

/* ── Small helper: one frame pillar + horizontal strut ── */
function Pillar({ pos }: { pos: [number, number, number] }) {
  return (
    <group position={[pos[0], 0.26 + 0.8, pos[2]]}>
      {/* Vertical post */}
      <RoundedBox args={[0.13, 1.7, 0.13]} radius={0.02} smoothness={3}>
        <meshStandardMaterial color={DARK_METAL} metalness={0.55} roughness={0.38} />
      </RoundedBox>
    </group>
  )
}
