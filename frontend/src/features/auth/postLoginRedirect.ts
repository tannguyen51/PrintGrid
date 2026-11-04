/**
 * Resolve the post-login destination from the `state.from` ProtectedRoute attached
 * when it bounced an unauthenticated visitor to /login.
 *
 * Only an internal, absolute path is accepted — a value like "//evil.example" would
 * otherwise be treated as a protocol-relative URL by the browser when navigated to.
 * Auth screens themselves are rejected so a stale state can never cause a redirect loop.
 *
 * The path is also checked against the roles of the account that just signed in: a
 * Customer restoring `/lab/register` from a stale `from` would only reach /forbidden,
 * so the guard returns the guest homepage instead. Public routes stay open to everyone.
 */

const STAFF_ROLES = ['LabManager', 'LabOperator', 'HubQC', 'HubFulfillment', 'OpsManager', 'Admin']

const ADMIN_PATHS = ['/admin']
const STAFF_PATHS = ['/lab', '/hub', '/ops', '/scheduling', '/quote-reviews', '/staff']
const CUSTOMER_PATHS = ['/models', '/orders', '/order', '/account', '/design-request', '/date-change']

/** `/lab` matches `/lab` and `/lab/register`, but never `/labs-public`. */
function underPath(path: string, prefix: string): boolean {
  return path === prefix || path.startsWith(`${prefix}/`)
}

function canOpenWith(path: string, roles: string[]): boolean {
  // No role information (older callers) — keep the previous behaviour.
  if (roles.length === 0) return true

  if (ADMIN_PATHS.some((prefix) => underPath(path, prefix))) return roles.includes('Admin')
  if (STAFF_PATHS.some((prefix) => underPath(path, prefix))) return roles.some((role) => STAFF_ROLES.includes(role))
  if (CUSTOMER_PATHS.some((prefix) => underPath(path, prefix))) return roles.includes('Customer')
  return true
}

export function resolvePostLoginPath(state: unknown, roles: string[] = []): string {
  const from = (state as { from?: unknown } | null | undefined)?.from
  if (typeof from !== 'string') return '/'
  if (!from.startsWith('/') || from.startsWith('//')) return '/'

  const path = from.split(/[?#]/, 1)[0]
  if (path === '/login' || path === '/register') return '/'
  if (!canOpenWith(path, roles)) return '/'

  return from
}
