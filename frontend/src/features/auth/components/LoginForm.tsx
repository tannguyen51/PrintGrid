import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate, Link as RouterLink } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Divider,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { Visibility, VisibilityOff, ArrowForward } from '@mui/icons-material'
import { FaGoogle } from 'react-icons/fa6'
import { useAuth } from '../../../app/AuthContext'

const loginSchema = z.object({
  email: z.string().min(1, 'Email là bắt buộc').email('Email không hợp lệ'),
  password: z.string().min(1, 'Mật khẩu là bắt buộc'),
})

type LoginFormValues = z.infer<typeof loginSchema>

interface LoginFormProps {
  /** When set, the footer "Đăng ký" link switches a dialog instead of navigating. */
  onSwitchToRegister?: () => void
}

/**
 * Right-hand login card on the unified dark background.
 * Keeps `useAuth().login()` flow; UI follows the premium-tech layout.
 */
export function LoginForm({ onSwitchToRegister }: LoginFormProps = {}) {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  async function onSubmit(values: LoginFormValues) {
    setSubmitting(true)
    setServerError(null)
    try {
      await login(values.email, values.password)
      navigate('/', { replace: true })
    } catch {
      setServerError('Email hoặc mật khẩu không đúng')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box
      sx={{
        flex: { xs: '1 1 auto', md: '1 1 48%' },
        minWidth: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default', // #050505
        p: { xs: 4, md: 6 },
      }}
    >
      <Box
        component="form"
        onSubmit={handleSubmit(onSubmit)}
        noValidate
        sx={{
          width: '100%',
          maxWidth: 420,
          bgcolor: 'background.paper', // #0A0A0A
          border: '1px solid',
          borderColor: 'rgba(255,255,255,0.12)',
          borderRadius: 4,
          boxShadow: (t) => t.custom.loginCardShadow,
          px: { xs: 4, sm: 5 },
          py: { xs: 5, sm: 6 },
        }}
      >
        <Stack spacing={2}>
          {/* ── Heading ── */}
          <Typography sx={{ fontWeight: 700, fontSize: '1.75rem', color: 'text.primary' }}>
            Đăng nhập
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 1 }}>
            Chào mừng bạn quay lại. Đăng nhập để tiếp tục.
          </Typography>

          {serverError && <Alert severity="error">{serverError}</Alert>}

          {/* ── Email ── */}
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            placeholder="email@example.com"
            fullWidth
            {...register('email')}
            error={Boolean(errors.email)}
            helperText={errors.email?.message}
          />

          {/* ── Password + Quên mật khẩu? ── */}
          <Box>
            <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 0.5 }}>
              <Typography variant="body2" sx={{ fontWeight: 500, color: 'text.primary' }}>
                Mật khẩu
              </Typography>
              <Link
                component="button"
                type="button"
                variant="body2"
                underline="hover"
                onClick={() => {}}
                sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}
              >
                Quên mật khẩu?
              </Link>
            </Stack>
            <TextField
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              fullWidth
              InputProps={{
                endAdornment: showPassword ? (
                  <InputAdornment position="end">
                    <IconButton
                      aria-label="Ẩn mật khẩu"
                      onClick={() => setShowPassword(false)}
                      edge="end"
                      size="small"
                    >
                      <VisibilityOff fontSize="small" />
                    </IconButton>
                  </InputAdornment>
                ) : (
                  <InputAdornment position="end">
                    <IconButton
                      aria-label="Hiện mật khẩu"
                      onClick={() => setShowPassword(true)}
                      edge="end"
                      size="small"
                    >
                      <Visibility fontSize="small" />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
              {...register('password')}
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
            />
          </Box>

          {/* ── Remember me ── */}
          <FormControlLabel
            control={<Checkbox size="small" defaultChecked />}
            label={<Typography variant="body2" color="text.secondary">Ghi nhớ đăng nhập</Typography>}
          />

          {/* ── Primary CTA ── */}
          <Button
            type="submit"
            fullWidth
            size="large"
            variant="contained"
            color="primary"
            disabled={submitting}
            sx={{ mt: 1, py: 1.5, fontSize: '0.95rem', fontWeight: 700, letterSpacing: '0.02em' }}
            endIcon={<ArrowForward />}
          >
            {submitting ? 'ĐANG ĐĂNG NHẬP…' : 'ĐĂNG NHẬP'}
          </Button>

          {/* ── Divider ── */}
          <Stack direction="row" alignItems="center" spacing={2} sx={{ my: 0.5 }}>
            <Divider sx={{ flex: 1 }} />
            <Typography variant="body2" color="text.secondary">
              hoặc
            </Typography>
            <Divider sx={{ flex: 1 }} />
          </Stack>

          {/* ── Google ── */}
          <Button variant="outlined" color="inherit" fullWidth disabled startIcon={<GoogleMark />} sx={{ py: 1.2, color: 'text.primary', borderColor: 'rgba(255,255,255,0.2)', justifyContent: 'flex-start', pl: 3 }}>
            Tiếp tục với Google
          </Button>

          {/* ── Signup footer ── */}
          <Stack direction="row" justifyContent="center" spacing={0.5} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              Chưa có tài khoản?
            </Typography>
            {onSwitchToRegister ? (
              <Link component="button" type="button" variant="body2" underline="hover" onClick={onSwitchToRegister} sx={{ color: 'text.primary', fontWeight: 600 }}>
                Đăng ký
              </Link>
            ) : (
              <Link component={RouterLink} to="/register" variant="body2" underline="hover" sx={{ color: 'text.primary', fontWeight: 600 }}>
                Đăng ký
              </Link>
            )}
          </Stack>
        </Stack>
      </Box>
    </Box>
  )
}

/**
 * Google brand mark via react-icons (MIT-licensed SVG).
 * Neutral single-glyph brand icon, consistent with the muted social button.
 */
function GoogleMark() {
  return (
    <FaGoogle
      aria-hidden
      style={{ display: 'inline-block', width: 18, height: 18, flexShrink: 0, color: '#8A8A8A' }}
    />
  )
}