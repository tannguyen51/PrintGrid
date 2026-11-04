import type { ReactNode } from 'react'
import { Box } from '@mui/material'
import { landing, useReveal } from './landingTheme'
import { alpha, gradients, palette, shadows } from '../../../shared/theme/tokens'
import './landing.css'

/* ─────────────────────────────── section shell ─────────────────────────────── */

interface LandingSectionProps {
  id: string
  label: string
  heading: string
  body: string
  children?: ReactNode
}

/** Content section with a purple eyebrow, display heading and one-line intro. */
function LandingSection({ id, label, heading, body, children }: LandingSectionProps) {
  const { ref, className } = useReveal<HTMLDivElement>()
  return (
    <Box
      component="section"
      id={id}
      ref={ref}
      className={className}
      sx={{
        px: { xs: '20px', md: '40px', lg: '104px' },
        py: { xs: '64px', lg: '104px' },
        display: 'flex',
        flexDirection: 'column',
        gap: '32px',
        borderBottom: `1px solid ${landing.hairlineSoft}`,
      }}
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
        <Box sx={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.16em', textTransform: 'uppercase', color: 'primary.main' }}>
          {label}
        </Box>
        <Box
          component="h2"
          sx={{
            m: 0,
            fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
            fontWeight: 800,
            fontSize: { xs: 'clamp(30px, 5vw, 38px)', lg: 50 },
            lineHeight: 1.12,
            letterSpacing: '-0.03em',
            color: landing.text,
          }}
        >
          {heading}
        </Box>
        <Box component="p" sx={{ m: 0, fontSize: 17, lineHeight: 1.65, color: landing.textMuted, maxWidth: 720 }}>
          {body}
        </Box>
      </Box>
      {children}
    </Box>
  )
}

// White metallic cards floating on the page wash — consistent with every
// dashboard surface in the app.
const cardSx = {
  p: { xs: 2.5, md: 4 },
  bgcolor: landing.card,
  backgroundImage: gradients.metal,
  border: `1px solid ${landing.hairline}`,
  borderRadius: '18px',
  boxShadow: shadows.card,
} as const

const labelSx = { fontSize: 13.5, color: landing.textMuted } as const
const valueSx = { fontSize: 14, fontWeight: 600, color: landing.text } as const

/* ─────────────────────── 01 · mock: uploaded file being analyzed ─────────────────────── */

/** Stylized low-poly preview of an uploaded model (SVG, no assets). */
function ModelPreview() {
  return (
    <Box
      sx={{
        position: 'relative',
        overflow: 'hidden',
        borderRadius: '14px',
        flex: { md: 1 },
        minWidth: 240,
        background: gradients.lavender,
        border: `1px solid ${landing.hairline}`,
      }}
    >
      <Box component="svg" viewBox="0 0 300 220" sx={{ display: 'block', width: '100%' }}>
        <g transform="translate(150 110)">
          {[-1, 1].map((dir) => (
            <g key={dir} transform={`skewX(${dir * 18})`}>
              <rect x={-55} y={-78} width={110} height={96} fill="none" stroke={palette.silver.dark} strokeWidth={1} transform="rotate(8)" />
              <rect x={-38} y={-54} width={76} height={66} fill="none" stroke={palette.silver.metallic} strokeWidth={1} transform="rotate(8)" />
            </g>
          ))}
          <circle cx={-70} cy={-84} r={3} fill={palette.gold.main} />
          <circle cx={70} cy={-68} r={3} fill={palette.purple.main} />
          <circle cx={0} cy={36} r={3} fill={palette.purple.light} />
          <path d="M -70 -84 L 70 -68 L 0 36 Z" fill="none" stroke={palette.purple.main} strokeDasharray="3 5" strokeWidth={1.2} opacity={0.55} />
        </g>
      </Box>
      {/* mesh scan line, animates top→bottom */}
      <div className="pg-scanline" />
    </Box>
  )
}

function ModelScanCard() {
  return (
    <Box sx={{ ...cardSx }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, pb: 2.5, borderBottom: `1px solid ${landing.hairlineSoft}`, flexWrap: 'wrap' }}>
        <Box component="svg" width={20} height={20} viewBox="0 0 20 20" sx={{ flexShrink: 0 }}>
          <path d="M10 2 L17 6 L17 14 L10 18 L3 14 L3 6 Z" fill="none" stroke={palette.purple.main} strokeWidth={1.4} />
          <path d="M10 2 L10 10 M3 6 L10 10 L17 6" fill="none" stroke={palette.silver.metallic} strokeWidth={1.2} />
        </Box>
        <Box sx={{ ...valueSx }}>gear_assembly_v3.stl</Box>
        <Box sx={{ ...labelSx, ml: 'auto' }}>42 MB · manifold ✓</Box>
      </Box>
      <Box sx={{ display: 'flex', gap: 4, pt: 3, flexWrap: 'wrap' }}>
        <ModelPreview />
        {/* extracted analysis metrics — demo values */}
        <Box sx={{ flex: 1, minWidth: 240, display: 'flex', flexDirection: 'column' }}>
          {[
            ['Volume', '128.4 cm³'],
            ['Bounding box', '96 × 54 × 96 mm'],
            ['Thinnest wall', '1.2 mm'],
            ['Topology', 'Closed · printable'],
          ].map(([k, v], i) => (
            <Box
              key={k}
              sx={{
                display: 'flex',
                justifyContent: 'space-between',
                py: 1.5,
                borderBottom: i < 3 ? `1px solid ${landing.hairlineSoft}` : 'none',
              }}
            >
              <Box sx={labelSx}>{k}</Box>
              <Box sx={valueSx}>{v}</Box>
            </Box>
          ))}
        </Box>
      </Box>
    </Box>
  )
}

/* ──────────────────── 02 · mock: printers filtered by constraints ──────────────────── */

const FILTER_CHIPS = ['FDM · SLA · SLS', 'PETG · ABS · Resin', 'Build ≥ 96 mm', 'Delivery: thứ 5 tuần này']

function PrinterRow({ name, tech, volume, slot, match, hot = false }: {
  name: string
  tech: string
  volume: string
  slot: string
  match: string
  hot?: boolean
}) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr auto', md: '1.4fr .9fr .9fr .5fr' },
        gap: { xs: 1, md: 2 },
        alignItems: 'center',
        py: 1.75,
        px: 1,
        mx: -1,
        borderRadius: '10px',
        borderBottom: `1px solid ${landing.hairlineSoft}`,
        transition: 'background-color .15s ease',
        '&:hover': { bgcolor: alpha(palette.purple.main, 0.035) },
      }}
    >
      <Box>
        <Box sx={{ ...valueSx }}>{name}</Box>
        <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' } }}>{tech}</Box>
      </Box>
      <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' } }}>{volume}</Box>
      <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' }, color: hot ? palette.purple.deep : landing.textMuted }}>{slot}</Box>
      <Box sx={{ ...valueSx, justifySelf: 'end', color: hot ? palette.purple.main : landing.textMuted }}>{match}</Box>
    </Box>
  )
}

function PrinterMatchList() {
  return (
    <Box sx={{ ...cardSx }}>
      <Box sx={{ display: 'flex', gap: 1.25, flexWrap: 'wrap', pb: 2.5, borderBottom: `1px solid ${landing.hairlineSoft}` }}>
        {FILTER_CHIPS.map((chip) => (
          <Box
            key={chip}
            sx={{
              px: '14px',
              py: '8px',
              borderRadius: '10px',
              bgcolor: palette.purple.soft,
              border: `1px solid ${alpha(palette.purple.main, 0.18)}`,
              fontSize: 13,
              fontWeight: 600,
              color: palette.purple.deep,
            }}
          >
            {chip}
          </Box>
        ))}
      </Box>
      <Box sx={{ pt: 0.5 }}>
        <PrinterRow name="Xưởng Sài Gòn · UltiMaker S5" tech="FDM · PETG" volume="240 × 210 × 300 mm" slot="Còn trống · T4" match="96%" hot />
        <PrinterRow name="Xưởng Hà Nội · Prusa XL" tech="FDM · ABS" volume="360 × 360 × 360 mm" slot="Còn trống · T5" match="91%" hot />
        <PrinterRow name="Xưởng Đà Nẵng · Form 4" tech="SLA · Resin" volume="200 × 200 × 400 mm" slot="Hết lịch T4–T6" match="— " />
      </Box>
    </Box>
  )
}

/* ───────────────────── 03 · network diagram (order → labs → time) ───────────────────── */

function NetworkDiagram() {
  type Lane = { y: number; selected: boolean; barX: number; barW: number; barLabel: string }
  const lanes: Lane[] = [
    { y: 30, selected: false, barX: 0, barW: 0, barLabel: '' },
    { y: 116, selected: true, barX: 700, barW: 170, barLabel: '8h · T4' },
    { y: 202, selected: false, barX: 0, barW: 0, barLabel: '' },
    { y: 288, selected: true, barX: 850, barW: 190, barLabel: '9h · T5' },
  ]
  return (
    <Box sx={{ ...cardSx, p: { xs: 2, md: 4 } }}>
      <Box component="svg" viewBox="0 0 1200 380" sx={{ display: 'block', width: '100%', height: 'auto' }}>
        {/* order node */}
        <rect x={24} y={160} width={180} height={60} rx={14} fill={palette.purple.soft} stroke={alpha(palette.purple.main, 0.25)} />
        <text x={114} y={185} textAnchor="middle" fill={palette.purple.deep} fontSize={14} fontWeight={700}>
          1 đơn hàng
        </text>
        <text x={114} y={205} textAnchor="middle" fill={landing.textMuted} fontSize={12}>
          gear_assembly_v3
        </text>

        {/* machine nodes + edges */}
        {lanes.map((lane, i) => {
          const cy = lane.y + 26
          const d = `M 204 190 C 330 190, 330 ${cy}, 470 ${cy}`
          const labels = ['Lab A · FDM', 'Lab B · FDM', 'Lab C · SLA', 'Lab D · FDM']
          return (
            <g key={lane.y}>
              <path d={d} className={lane.selected ? 'pg-edge pg-edge-live' : 'pg-edge'} />
              <rect
                x={470}
                y={lane.y}
                width={180}
                height={52}
                rx={12}
                fill={lane.selected ? palette.purple.soft : palette.background.secondary}
                stroke={lane.selected ? alpha(palette.purple.main, 0.35) : landing.hairline}
              />
              <text x={560} y={cy + 4} textAnchor="middle" fill={lane.selected ? palette.purple.deep : landing.textMuted} fontSize={13} fontWeight={lane.selected ? 600 : 400}>
                {labels[i]}
              </text>
              <path d={`M 650 ${cy} L 680 ${cy}`} className={lane.selected ? 'pg-edge pg-edge-live' : 'pg-edge'} />
              {/* schedule lane */}
              <rect x={680} y={cy - 5} width={480} height={10} rx={5} fill={palette.background.secondary} />
              {lane.selected && (
                <g>
                  <rect x={lane.barX} y={cy - 6} width={lane.barW} height={12} rx={6} fill="url(#pgScheduleGradient)" />
                  <text x={lane.barX + lane.barW + 10} y={cy + 4} fill={landing.textMuted} fontSize={12}>
                    {lane.barLabel}
                  </text>
                </g>
              )}
            </g>
          )
        })}

        {/* gradient used by the schedule bars */}
        <defs>
          <linearGradient id="pgScheduleGradient" x1="0" x2="1" y1="0" y2="0">
            <stop offset="0%" stopColor={palette.purple.main} />
            <stop offset="70%" stopColor={palette.purple.light} />
            <stop offset="100%" stopColor={palette.gold.main} />
          </linearGradient>
        </defs>

        {/* time axis */}
        <path d="M 680 340 L 1160 340" stroke={landing.hairline} strokeWidth={1} />
        {['T3', 'T4', 'T5', 'T6'].map((t, i) => (
          <g key={t}>
            <circle cx={680 + i * 160} cy={340} r={3} fill={palette.silver.metallic} />
            <text x={680 + i * 160} y={362} textAnchor="middle" fill={landing.textMuted} fontSize={12}>
              {t}
            </text>
          </g>
        ))}
        <text x={920} y={18} textAnchor="middle" fill={landing.textMuted} fontSize={12} fontWeight={600}>
          Lịch máy · cửa giao hàng
        </text>
      </Box>
    </Box>
  )
}

/* ─────────────────────────────── assembled sections ─────────────────────────────── */

export function PublicSections() {
  return (
    <>
      <LandingSection
        id="sec-01"
        label="01 / YOUR MODEL"
        heading="It starts with a single file."
        body="Upload an STL, OBJ, 3MF or GLB. PrintGrid turns your model into structured information that can be matched against a distributed printer network."
      >
        <ModelScanCard />
      </LandingSection>

      <LandingSection
        id="sec-02"
        label="02 / NETWORK MATCHING"
        heading="Then we find the right machines."
        body="Compatible printers are filtered by technology, material, build volume, availability and other constraints."
      >
        <PrinterMatchList />
      </LandingSection>

      <LandingSection
        id="sec-03"
        label="03 / SCHEDULING"
        heading="Then we find the right time."
        body="The selected jobs are placed across the available machines to find a feasible schedule and promised delivery date."
      >
        <NetworkDiagram />
      </LandingSection>
    </>
  )
}
