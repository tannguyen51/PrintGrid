/**
 * Tokens moved to shared/theme/landing.tsx (07/10) when the logged-in customer
 * area adopted the homepage visual language. This file keeps existing imports
 * under features/home/ working unchanged — new shared code should import from
 * '../../../shared/theme/landing' (or the shared path) instead.
 */
export { landing, PillButton, scrollToSection, useReveal } from '../../../shared/theme/landing'
