import { StatsSection } from './sections/StatsSection'
import { B2BSection } from './sections/B2BSection'
import { B2CSection } from './sections/B2CSection'
import { MaterialsSection } from './sections/MaterialsSection'
import { PricingSection } from './sections/PricingSection'
import { ContactSection } from './sections/ContactSection'

/**
 * GN3D-style landing sections.
 * Each section uses the existing light metallic theme and scroll-reveal animation.
 */
export function PublicSections() {
  return (
    <>
      <StatsSection />
      <B2BSection />
      <B2CSection />
      <MaterialsSection />
      <PricingSection />
      <ContactSection />
    </>
  )
}
