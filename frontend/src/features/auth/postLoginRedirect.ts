/**
 * Resolve the post-login destination from the `state.from` ProtectedRoute attached
 * when it bounced an unauthenticated visitor to /login.
 *
 * Only an internal, absolute path is accepted — a value like "//evil.example" would
 * otherwise be treated as a protocol-relative URL by the browser when navigated to.
 * Auth screens themselves are rejected so a stale state can never cause a redirect loop.
 */
export function resolvePostLoginPath(state: unknown): string {
  const from = (state as { from?: unknown } | null | undefined)?.from
  if (typeof from !== 'string') return '/'
  if (!from.startsWith('/') || from.startsWith('//')) return '/'

  const path = from.split(/[?#]/, 1)[0]
  if (path === '/login' || path === '/register') return '/'

  return from
}
