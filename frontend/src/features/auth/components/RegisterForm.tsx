import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate, Link as RouterLink } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Divider,
  IconButton,
  InputAdornment,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { Visibility, VisibilityOff, ArrowForward, PersonRounded, PhoneRounded } from '@mui/icons-material'
import { FaGoogle } from 'react-icons/fa6'
import { useAuth } from '../../../app/AuthContext'

const registerSchema = z
  .object({
    fullName: z.string().min(2, 'Họ tên ít nhất 2 ký tự'),
    email: z.string().min(1, 'Email là bắt buộc').email('Email không hợp lệ'),
    phoneNumber: z
      .string()
      .optional()
      .refine((v) => !v || /^[0-9+\-\s()]{7,15}$/.test(v), 'Số điện thoại không hợp lệ'),
    password: z.string().min(6, 'Mật khẩu ít nhất 6 ký tự'),
    confirmPassword: z.string().min(1, 'Xác nhận mật khẩu là bắt buộc'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
  })

type RegisterFormValues = z.infer<typeof registerSchema>

interface RegisterFormProps {
  /** When set, the footer "Đăng nhập" link switches a dialog instead of navigating. */
  onSwitchToLogin?: () => void
  /** When set (used inside a dialog with no showcase), the panel takes full width. */
  fullBleed?: boolean
  /** Called after a successful registration (lets a dialog close itself). When unset the page navigates home. */
  onSuccess?: () => void
}

/**
 * Register card on the unified dark background.
 * Mirrors LoginForm's premium-tech layout. Creates the account and navigates home (or closes a dialog).
 */
export function RegisterForm({ onSwitchToLogin, fullBleed, onSuccess }: RegisterFormProps = {}) {
  const { register: registerAccount } = useAuth()
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  async function onSubmit(values: RegisterFormValues) {
    setSubmitting(true)
    setServerError(null)
    try {
      await registerAccount({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        phoneNumber: values.phoneNumber?.trim() || undefined,
      })
      if (onSuccess) onSuccess()
      else navigate('/', { replace: true })
    } catch {
      setServerError('Không thể tạo tài khoản. Vui lòng thử lại.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box
      sx={{
        flex: { xs: '1 1 auto', md: fullBleed ? '1 1 100%' : '1 1 48%' },
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
            Tạo tài khoản
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 1 }}>
            Tham gia PrintGrid để theo dõi & đặt in 3D.
          </Typography>

          {serverError && <Alert severity="error">{serverError}</Alert>}

          {/* ── Họ tên ── */}
          <TextField
            label="Họ và tên"
            autoComplete="name"
            placeholder="Nguyễn Văn A"
            fullWidth
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <PersonRounded fontSize="small" color="action" />
                </InputAdornment>
              ),
            }}
            {...register('fullName')}
            error={Boolean(errors.fullName)}
            helperText={errors.fullName?.message}
          />

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

          {/* ── Số điện thoại ── */}
          <TextField
            label="Số điện thoại"
            type="tel"
            autoComplete="tel"
            placeholder="0901 234 567"
            fullWidth
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <PhoneRounded fontSize="small" color="action" />
                </InputAdornment>
              ),
            }}
            {...register('phoneNumber')}
            error={Boolean(errors.phoneNumber)}
            helperText={errors.phoneNumber?.message}
          />

          {/* ── Mật khẩu ── */}
          <TextField
            label="Mật khẩu"
            placeholder="••••••••"
            type={showPassword ? 'text' : 'password'}
            autoComplete="new-password"
            fullWidth
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                    onClick={() => setShowPassword((v) => !v)}
                    edge="end"
                    size="small"
                  >
                    {showPassword ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
            {...register('password')}
            error={Boolean(errors.password)}
            helperText={errors.password?.message}
          />

          {/* ── Xác nhận mật khẩu ── */}
          <TextField
            label="Xác nhận mật khẩu"
            placeholder="••••••••"
            type={showConfirm ? 'text' : 'password'}
            autoComplete="new-password"
            fullWidth
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label={showConfirm ? 'Ẩn xác nhận mật khẩu' : 'Hiện xác nhận mật khẩu'}
                    onClick={() => setShowConfirm((v) => !v)}
                    edge="end"
                    size="small"
                  >
                    {showConfirm ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
            {...register('confirmPassword')}
            error={Boolean(errors.confirmPassword)}
            helperText={errors.confirmPassword?.message}
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
            {submitting ? 'ĐANG TẠO TÀI KHOẢN…' : 'TẠO TÀI KHOẢN'}
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
            Đăng ký với Google
          </Button>

          {/* ── Login footer ── */}
          <Stack direction="row" justifyContent="center" spacing={0.5} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              Đã có tài khoản?
            </Typography>
            {onSwitchToLogin ? (
              <Link component="button" type="button" variant="body2" underline="hover" onClick={onSwitchToLogin} sx={{ color: 'text.primary', fontWeight: 600 }}>
                Đăng nhập
              </Link>
            ) : (
              <Link component={RouterLink} to="/login" variant="body2" underline="hover" sx={{ color: 'text.primary', fontWeight: 600 }}>
                Đăng nhập
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