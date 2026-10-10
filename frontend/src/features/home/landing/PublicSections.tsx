import type { ReactNode } from 'react'
import { Box } from '@mui/material'
import { landing, useReveal } from './landingTheme'
import './landing.css'

/* ─────────────────────────────── section shell ─────────────────────────────── */

interface LandingSectionProps {
  id: string
  label: string
  heading: string
  body: string
  children?: ReactNode
}

/** Content section — Figma #36:44 pattern: padding 96/104, column, gap 32, heading block gap 16. */
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
        py: { xs: '56px', lg: '96px' },
        display: 'flex',
        flexDirection: 'column',
        gap: '32px',
      }}
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        <Box component="p" sx={{ m: 0, fontSize: 18, lineHeight: 1.55, color: landing.textMuted, maxWidth: 760 }}>
          {label}
        </Box>
        <Box
          component="h2"
          sx={{
            m: 0,
            fontWeight: 600,
            fontSize: { xs: 'clamp(30px, 5vw, 40px)', lg: 56 },
            lineHeight: 1.2,
            color: landing.text,
          }}
        >
          {heading}
        </Box>
        <Box component="p" sx={{ m: 0, fontSize: 18, lineHeight: 1.55, color: landing.textMuted, maxWidth: 760 }}>
          {body}
        </Box>
      </Box>
      {children}
    </Box>
  )
}

// Content boxes sit flush on the pure-black page: no border, no fill — only
// internal hairline dividers structure them, so nothing reads as a framed panel.
const cardSx = {
  p: { xs: 2.5, md: 4 },
} as const

const labelSx = { fontSize: 14, color: landing.textMuted } as const
const valueSx = { fontSize: 14, fontWeight: 500, color: landing.text } as const

/* ─────────────────────── 01 · mock: uploaded file being analyzed ─────────────────────── */

/** Stylized low-poly preview of an uploaded model (SVG, no assets). */
function ModelPreview() {
  return (
    <Box sx={{ position: 'relative', overflow: 'hidden', borderRadius: '12px', flex: { md: 1 }, minWidth: 240 }}>
      <Box component="svg" viewBox="0 0 300 220" sx={{ display: 'block', width: '100%' }}>
        <g transform="translate(150 110)">
          {[-1, 1].map((dir) => (
            <g key={dir} transform={`skewX(${dir * 18})`}>
              <rect x={-55} y={-78} width={110} height={96} fill="none" stroke="#333338" strokeWidth={1} transform="rotate(8)" />
              <rect x={-38} y={-54} width={76} height={66} fill="none" stroke="#2a2a2f" strokeWidth={1} transform="rotate(8)" />
            </g>
          ))}
          <circle cx={-70} cy={-84} r={2.5} fill="#909096" />
          <circle cx={70} cy={-68} r={2.5} fill="#909096" />
          <circle cx={0} cy={36} r={2.5} fill="#909096" />
          <path d="M -70 -84 L 70 -68 L 0 36 Z" fill="none" stroke="#2c2c31" strokeDasharray="3 5" strokeWidth={1} />
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
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, pb: 2.5, borderBottom: `1px solid ${landing.hairlineSoft}` }}>
        <Box component="svg" width={20} height={20} viewBox="0 0 20 20" sx={{ flexShrink: 0 }}>
          <path d="M10 2 L17 6 L17 14 L10 18 L3 14 L3 6 Z" fill="none" stroke={landing.textMuted} strokeWidth={1.3} />
          <path d="M10 2 L10 10 M3 6 L10 10 L17 6" fill="none" stroke="#3a3a3e" strokeWidth={1.1} />
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
        borderBottom: `1px solid ${landing.hairlineSoft}`,
        transition: 'background-color .15s ease',
        '&:hover': { bgcolor: 'rgba(255,255,255,0.02)' },
      }}
    >
      <Box>
        <Box sx={{ ...valueSx }}>{name}</Box>
        <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' } }}>{tech}</Box>
      </Box>
      <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' } }}>{volume}</Box>
      <Box sx={{ ...labelSx, display: { xs: 'none', md: 'block' }, color: hot ? '#C8C8CC' : landing.textMuted }}>{slot}</Box>
      <Box sx={{ ...valueSx, justifySelf: 'end', color: hot ? landing.text : landing.textMuted }}>{match}</Box>
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
              bgcolor: landing.surface,
              border: `1px solid ${landing.hairline}`,
              fontSize: 13,
              color: landing.text,
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
        <rect x={24} y={160} width={180} height={60} rx={14} fill={landing.surface} stroke={landing.hairline} />
        <text x={114} y={185} textAnchor="middle" fill={landing.text} fontSize={14} fontWeight={600}>
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
              <rect x={470} y={lane.y} width={180} height={52} rx={12} fill="#0E0E11" stroke={lane.selected ? '#3A3A3E' : landing.hairline} />
              <text x={560} y={cy + 4} textAnchor="middle" fill={lane.selected ? landing.text : landing.textMuted} fontSize={13}>
                {labels[i]}
              </text>
              <path d={`M 650 ${cy} L 680 ${cy}`} className={lane.selected ? 'pg-edge pg-edge-live' : 'pg-edge'} />
              {/* schedule lane */}
              <rect x={680} y={cy - 5} width={480} height={10} rx={5} fill={landing.surface} />
              {lane.selected && (
                <g>
                  <rect x={lane.barX} y={cy - 6} width={lane.barW} height={12} rx={6} fill="rgba(241,241,241,0.85)" />
                  <text x={lane.barX + lane.barW + 10} y={cy + 4} fill={landing.textMuted} fontSize={12}>
                    {lane.barLabel}
                  </text>
                </g>
              )}
            </g>
          )
        })}

        {/* time axis */}
        <path d="M 680 340 L 1160 340" stroke={landing.hairline} strokeWidth={1} />
        {['T3', 'T4', 'T5', 'T6'].map((t, i) => (
          <g key={t}>
            <circle cx={680 + i * 160} cy={340} r={2.5} fill="#3A3A3E" />
            <text x={680 + i * 160} y={362} textAnchor="middle" fill={landing.textMuted} fontSize={12}>
              {t}
            </text>
          </g>
        ))}
        <text x={920} y={18} textAnchor="middle" fill={landing.textMuted} fontSize={12}>
          Lịch máy · cửa giao hàng
        </text>
      </Box>
    </Box>
  )
}

/* ─────────────────────────────── assembled sections ─────────────────────────────── */

/**
 * The three content sections the Figma API returned in full (#36:44, #36:109, #36:166).
 * Section 02's heading text was blank in the design export — filled with "…" matching the
 * established sentence pattern; replace it when the design becomes readable again.
 */
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
