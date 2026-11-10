import { Component, useEffect, useMemo, useRef, type ReactNode } from 'react'
import { Box, Typography, useMediaQuery } from '@mui/material'
import { Canvas, useFrame } from '@react-three/fiber'
import { Environment, Lightformer, OrbitControls, QuadraticBezierLine, RoundedBox, type QuadraticBezierLineRef } from '@react-three/drei'
import * as THREE from 'three'

type Position = [number, number, number]
const finish = { shell: '#f0f0ed', frame: '#989da4', dark: '#292d32', gold: '#b9a779' }

function Part({ position, size, color = finish.shell, radius = 0.035, metalness = 0.35 }: { position: Position; size: Position; color?: string; radius?: number; metalness?: number }) {
  return (
    <RoundedBox position={position} args={size} radius={Math.min(radius, Math.min(...size) / 2)} smoothness={3}>
      <meshPhysicalMaterial color={color} metalness={metalness} roughness={0.32} clearcoat={0.3} />
    </RoundedBox>
  )
}

function Label({ text, position, width, height, screen = false }: { text: string; position: Position; width: number; height: number; screen?: boolean }) {
  const texture = useMemo(() => {
    const canvas = document.createElement('canvas')
    canvas.width = 512
    canvas.height = 160
    const ctx = canvas.getContext('2d')!
    ctx.fillStyle = screen ? '#172023' : '#f0f0ed'
    ctx.fillRect(0, 0, 512, 160)
    ctx.fillStyle = screen ? '#7fe2ad' : '#454a50'
    ctx.font = '500 58px Arial'
    ctx.textAlign = 'center'
    ctx.fillText(text, 256, screen ? 65 : 100)
    if (screen) {
      ctx.fillStyle = '#83989a'
      ctx.font = '22px Arial'
      ctx.fillText('PRINTING   210° / 60°', 256, 105)
      ctx.fillStyle = '#34474a'
      ctx.fillRect(55, 127, 402, 7)
      ctx.fillStyle = '#7fe2ad'
      ctx.fillRect(55, 127, 256, 7)
    }
    const result = new THREE.CanvasTexture(canvas)
    result.colorSpace = THREE.SRGBColorSpace
    return result
  }, [text, screen])
  useEffect(() => () => texture.dispose(), [texture])
  return <mesh position={position}><planeGeometry args={[width, height]} /><meshBasicMaterial map={texture} /></mesh>
}

function FilamentSpool({ color, side, level, moving }: { color: string; side: number; level: number; moving: boolean }) {
  const reel = useRef<THREE.Group>(null)
  useFrame((_, delta) => { if (moving && reel.current) reel.current.rotation.x += delta * 0.15 })
  return (
    <group position={[side * 1.53, level, -0.34]}>
      <Part position={[-side * 0.21, -0.35, 0]} size={[0.55, 0.07, 0.13]} color={finish.frame} />
      <group ref={reel}>
        <mesh rotation={[0, 0, Math.PI / 2]}><cylinderGeometry args={[0.43, 0.43, 0.28, 48]} /><meshStandardMaterial color={color} roughness={0.65} /></mesh>
        {[-0.17, 0.17].map(x => (
          <group key={x} position={[x, 0, 0]} rotation={[0, Math.PI / 2, 0]}>
            <mesh><torusGeometry args={[0.46, 0.023, 8, 48]} /><meshPhysicalMaterial color="#e1ded4" metalness={0.25} roughness={0.3} /></mesh>
            {Array.from({ length: 8 }, (_, i) => (
              <mesh key={i} rotation={[0, 0, i * Math.PI / 4]} position={[Math.cos(i * Math.PI / 4) * 0.24, Math.sin(i * Math.PI / 4) * 0.24, 0]}>
                <boxGeometry args={[0.4, 0.027, 0.022]} /><meshStandardMaterial color="#e1ded4" />
              </mesh>
            ))}
            <mesh rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[0.085, 0.085, 0.09, 20]} /><meshStandardMaterial color="#f0f0ed" /></mesh>
          </group>
        ))}
      </group>
    </group>
  )
}

/** Stylized reconstruction from Creality's SPARKX i7 + CFS Nano photos.
 * Reference: https://www.creality.com/eu/products/sparkx-i7
 * This is illustrative geometry, not manufacturer CAD or a dimensional model. */
function PrinterModel({ reducedMotion }: { reducedMotion: boolean }) {
  const root = useRef<THREE.Group>(null)
  const head = useRef<THREE.Group>(null)
  const bed = useRef<THREE.Group>(null)
  const guide = useRef<QuadraticBezierLineRef>(null)
  const guideEnd = useMemo(() => new THREE.Vector3(0, 0.44, 0.2), [])
  const guideStart = useMemo(() => new THREE.Vector3(0, 1.94, -0.24), [])
  const guideMid = useMemo(() => new THREE.Vector3(-0.12, 1.6, 0.55), [])
  useFrame(({ clock, pointer }, delta) => {
    if (reducedMotion) return
    const t = clock.elapsedTime
    const travel = Math.sin(t * 1.5) * 0.24
    if (head.current) head.current.position.x = travel
    if (bed.current) bed.current.position.z = 0.15 + Math.cos(t * 1.5) * 0.19
    guideEnd.x = travel
    guide.current?.setPoints(guideStart, guideEnd, guideMid)
    if (root.current) {
      root.current.rotation.y = THREE.MathUtils.damp(root.current.rotation.y, Math.sin(t * 0.23) * 0.12 + pointer.x * 0.16, 3, delta)
      root.current.rotation.x = THREE.MathUtils.damp(root.current.rotation.x, -pointer.y * 0.06, 3, delta)
    }
  })
  return (
    <group ref={root} position={[0, -0.22, 0]}>
      {/* White low-profile base and characteristic RGB status strip. */}
      <Part position={[0, -1.37, 0.15]} size={[2.48, 0.33, 2.13]} radius={0.14} />
      <Part position={[0, -1.56, 0.15]} size={[2.22, 0.09, 1.95]} color={finish.dark} />
      <Part position={[0, -1.48, 1.218]} size={[1.95, 0.065, 0.025]} color={finish.dark} />
      {['#b285ff', '#797eff', '#56cfe4', '#63db9d', '#d6dc72', '#ffbf67', '#f48e97'].map((color, i) => (
        <mesh key={color} position={[-0.81 + i * 0.27, -1.472, 1.235]}><boxGeometry args={[0.27, 0.027, 0.01]} /><meshStandardMaterial color={color} emissive={color} emissiveIntensity={1.6} toneMapped={false} /></mesh>
      ))}
      {/* Broad silver portal with rounded top corners and inset white faces. */}
      {[-1.1, 1.1].map(x => (
        <group key={x}>
          <Part position={[x, -0.01, -0.55]} size={[0.25, 2.72, 0.31]} color={finish.frame} radius={0.09} metalness={0.7} />
          <Part position={[x, -0.02, -0.382]} size={[0.16, 2.47, 0.025]} />
          <Part position={[x * 0.86, -0.03, -0.4]} size={[0.035, 2.45, 0.035]} color={finish.dark} />
          <Part position={[x, -1.34, -0.55]} size={[0.32, 0.22, 0.4]} color={finish.dark} />
          <Part position={[x, 0.05, -0.39]} size={[0.34, 0.24, 0.36]} radius={0.07} />
        </group>
      ))}
      <Part position={[0, 1.3, -0.55]} size={[2.45, 0.22, 0.31]} color={finish.frame} radius={0.1} metalness={0.7} />
      <Part position={[0, 1.3, -0.382]} size={[2.16, 0.12, 0.025]} />
      <Part position={[0, 0.05, -0.36]} size={[2.27, 0.16, 0.21]} color={finish.dark} />
      <Part position={[0, 0.1, -0.24]} size={[2.12, 0.025, 0.035]} color={finish.frame} metalness={0.8} />
      <Label text="i7" position={[-1.13, 0.04, -0.196]} width={0.25} height={0.12} />
      {/* Bed rails and gold PEI plate. */}
      {[-0.66, 0.66].map(x => <Part key={x} position={[x, -1.13, 0.05]} size={[0.09, 0.13, 1.9]} color={finish.dark} />)}
      <group ref={bed} position={[0, 0, 0.15]}>
        <Part position={[0, -0.99, 0]} size={[1.92, 0.12, 1.72]} color={finish.frame} metalness={0.7} />
        <Part position={[0, -0.918, 0]} size={[1.87, 0.022, 1.67]} color={finish.gold} metalness={0.55} />
        {Array.from({ length: 22 }, (_, i) => (
          <mesh key={i} position={[0, -0.88 + i * 0.02, 0.18]} rotation={[Math.PI / 2, 0, 0]}>
            <torusGeometry args={[0.2 + 0.04 * Math.cos(i / 21 * Math.PI * 2), 0.012, 6, 36]} />
            <meshStandardMaterial color={i > 14 ? '#e9a3bf' : i > 7 ? '#f0d6a0' : '#a1c9b4'} roughness={0.6} />
          </mesh>
        ))}
      </group>
      {/* Tall white SPARKX toolhead, dark lower shroud, circular green emblem. */}
      <group ref={head}>
        <Part position={[0, 0.02, 0.1]} size={[0.39, 0.72, 0.36]} radius={0.07} />
        <Part position={[0, -0.34, 0.13]} size={[0.32, 0.1, 0.32]} color={finish.dark} />
        <mesh position={[0, -0.425, 0.33]}><cylinderGeometry args={[0.055, 0.015, 0.09, 16]} /><meshStandardMaterial color="#c5a777" metalness={0.7} roughness={0.3} /></mesh>
        <mesh position={[0, 0.13, 0.287]}><circleGeometry args={[0.076, 32]} /><meshStandardMaterial color="#313a30" /></mesh>
        <mesh position={[0, 0.136, 0.291]}><ringGeometry args={[0.04, 0.057, 3]} /><meshStandardMaterial color="#b3e33b" emissive="#91c528" emissiveIntensity={0.3} /></mesh>
        <Label text="SPARKX" position={[0, -0.2, 0.287]} width={0.28} height={0.07} />
        <Part position={[0, 0.43, 0.13]} size={[0.15, 0.12, 0.15]} />
        {[-0.1, 0, 0.1].map(x => <Part key={x} position={[x, -0.04, 0.286]} size={[0.017, 0.11, 0.012]} color="#bcc0c2" />)}
      </group>
      {/* Angled touchscreen mounted at the front right. */}
      <group position={[1.22, -1.15, 0.91]} rotation={[-0.3, 0.1, 0]}>
        <Part position={[0, -0.13, -0.08]} size={[0.19, 0.28, 0.2]} color={finish.frame} />
        <Part position={[0, 0, 0]} size={[0.56, 0.37, 0.09]} radius={0.04} />
        <Label text="SPARKX" position={[0, 0, 0.051]} width={0.48} height={0.28} screen />
      </group>
      {/* CFS Nano feeder sits above the portal; four reels flank the frame. */}
      <Part position={[0.04, 1.62, -0.55]} size={[0.18, 0.48, 0.2]} />
      <Part position={[0.04, 1.96, -0.48]} size={[0.72, 0.43, 0.48]} radius={0.08} />
      <Part position={[0.04, 1.85, -0.222]} size={[0.67, 0.12, 0.04]} color={finish.frame} />
      <Label text="CFS nano" position={[0.04, 2, -0.231]} width={0.5} height={0.12} />
      {[
        { side: -1, level: 0.7, color: '#d891b2' },
        { side: -1, level: -0.6, color: '#e7cd49' },
        { side: 1, level: 0.7, color: '#e7dec3' },
        { side: 1, level: -0.6, color: '#a87c32' },
      ].map(({ side, level, color }, i) => (
        <group key={i}>
          <FilamentSpool side={side} level={level} color={color} moving={!reducedMotion && i === 0} />
          <QuadraticBezierLine start={[side * 1.53, level + 0.42, -0.34]} end={[-0.2 + i * 0.15, 2.19, -0.48]} mid={[side * 1.7, 2.65 + i * 0.05, -0.52]} color={i === 0 ? '#dcb6c9' : '#bcb6a5'} lineWidth={1.5} />
        </group>
      ))}
      <QuadraticBezierLine ref={guide} start={guideStart} end={guideEnd} mid={guideMid} color="#34393f" lineWidth={2.2} />
    </group>
  )
}

class PrinterBoundary extends Component<{ children: ReactNode }, { error: boolean }> {
  state = { error: false }
  static getDerivedStateFromError() { return { error: true } }
  render() {
    return this.state.error ? <Typography role="status" color="text.secondary" sx={{ p: 4, textAlign: 'center' }}>Không thể hiển thị mô hình 3D. Vui lòng kiểm tra hỗ trợ WebGL.</Typography> : this.props.children
  }
}

export function PrinterScene() {
  const reducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)')
  return (
    <Box sx={{ width: '100%', height: '100%', position: 'relative' }}>
      <PrinterBoundary>
        <Canvas
          camera={{ position: [3.2, 1.8, 8.9], fov: 36 }}
          dpr={[1, 1.5]}
          frameloop={reducedMotion ? 'demand' : 'always'}
          gl={{ alpha: true, antialias: true, toneMapping: THREE.ACESFilmicToneMapping, toneMappingExposure: 1.1 }}
          fallback={<Typography color="text.secondary">Trình duyệt không hỗ trợ hiển thị 3D.</Typography>}
          style={{ background: 'transparent', touchAction: 'pan-y' }}
        >
          <ambientLight intensity={0.8} />
          <directionalLight position={[5, 7, 4]} intensity={2.6} />
          <directionalLight position={[-4, 3, -6]} intensity={2} color="#c4b5fd" />
          <directionalLight position={[-6, -1, 3]} intensity={1.2} color="#ffdfac" />
          <Environment resolution={128} frames={1}>
            <Lightformer intensity={4} position={[0, 4, 0]} rotation={[-Math.PI / 2, 0, 0]} scale={[6, 6, 1]} />
            <Lightformer intensity={2} position={[-4, 1, 2]} rotation={[0, Math.PI / 2, 0]} scale={[3, 4, 1]} color="#c4b5fd" />
            <Lightformer intensity={2} position={[4, -1, -2]} rotation={[0, -Math.PI / 2, 0]} scale={[3, 4, 1]} color="#ffe6bc" />
          </Environment>
          <PrinterModel reducedMotion={reducedMotion} />
          <OrbitControls makeDefault enableDamping={!reducedMotion} enablePan={false} enableZoom={false} minPolarAngle={Math.PI / 4} maxPolarAngle={Math.PI / 2} />
        </Canvas>
      </PrinterBoundary>
    </Box>
  )
}
