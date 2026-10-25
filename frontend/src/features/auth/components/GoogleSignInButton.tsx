import { useEffect, useRef, useState } from 'react'
import { Alert, Box, CircularProgress, Typography } from '@mui/material'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/AuthContext'
import { getApiErrorMessage } from '../../../shared/api/apiError'
import { resolvePostLoginPath } from '../postLoginRedirect'

const GOOGLE_SCRIPT_ID = 'google-identity-services'

interface GoogleSignInButtonProps {
  onSuccess?: () => void
  text?: 'signin_with' | 'signup_with' | 'continue_with'
}

export function GoogleSignInButton({ onSuccess, text = 'continue_with' }: GoogleSignInButtonProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const { googleLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim()

  useEffect(() => {
    if (!clientId) {
      setLoading(false)
      return
    }

    let cancelled = false

    const render = () => {
      if (cancelled || !window.google || !containerRef.current) return
      containerRef.current.replaceChildren()
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: async ({ credential }) => {
          if (!credential) {
            setError('Google không trả về thông tin đăng nhập.')
            return
          }
          setError(null)
          setLoading(true)
          try {
            await googleLogin(credential, true)
            if (onSuccess) {
              // Homepage dialog: close itself and leave the visitor on the current page.
              onSuccess()
              return
            }
            // Same deep-link restore as the password form (ProtectedRoute stored it in state).
            navigate(resolvePostLoginPath(location.state), { replace: true })
          } catch (reason) {
            setError(getApiErrorMessage(reason, 'Không thể đăng nhập bằng Google.'))
          } finally {
            setLoading(false)
          }
        },
      })
      window.google.accounts.id.renderButton(containerRef.current, {
        type: 'standard',
        theme: 'filled_black',
        size: 'large',
        text,
        shape: 'rectangular',
        logo_alignment: 'left',
        width: Math.min(containerRef.current.clientWidth || 320, 400),
      })
      setLoading(false)
    }

    const existing = document.getElementById(GOOGLE_SCRIPT_ID) as HTMLScriptElement | null
    if (window.google) {
      render()
    } else if (existing) {
      existing.addEventListener('load', render, { once: true })
    } else {
      const script = document.createElement('script')
      script.id = GOOGLE_SCRIPT_ID
      script.src = 'https://accounts.google.com/gsi/client'
      script.async = true
      script.defer = true
      script.addEventListener('load', render, { once: true })
      script.addEventListener('error', () => {
        if (!cancelled) {
          setLoading(false)
          setError('Không tải được dịch vụ đăng nhập Google.')
        }
      }, { once: true })
      document.head.appendChild(script)
    }

    return () => {
      cancelled = true
      existing?.removeEventListener('load', render)
    }
  }, [clientId, googleLogin, navigate, onSuccess, text])

  if (!clientId) {
    return <Alert severity="warning">Google Login chưa được cấu hình.</Alert>
  }

  return (
    <Box>
      <Box sx={{ minHeight: 44, display: 'grid', placeItems: 'center', position: 'relative' }}>
        {loading && <CircularProgress size={22} />}
        <Box ref={containerRef} sx={{ width: '100%', display: loading ? 'none' : 'flex', justifyContent: 'center' }} />
      </Box>
      {error && <Typography color="error" variant="caption" sx={{ display: 'block', mt: 1 }}>{error}</Typography>}
    </Box>
  )
}
