import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  AccessTimeRounded,
  CheckCircleRounded,
  ErrorOutlineRounded,
  FactoryRounded,
  LocalShippingRounded,
  LogoutRounded,
  PrecisionManufacturingRounded,
  ScheduleRounded,
} from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useMyLab, useSelfRegisterLab } from '../ops/useLabs'

/**
 * Lab self-registration & profile declaration screen.
 * Declares operating calendar and transit time to Hub.
 */
export default function LabRegisterPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const { data: myLab, isLoading } = useMyLab(false)
  const selfRegister = useSelfRegisterLab()

  const [name, setName] = useState('')
  const [city, setCity] = useState('')
  const [address, setAddress] = useState('')
  const [contactPhone, setContactPhone] = useState('')
  const [operatingHours, setOperatingHours] = useState('T2-T7 08:00-18:00')
  const [transitDaysToHub, setTransitDaysToHub] = useState(1)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  useEffect(() => {
    if (!myLab) return
    setName(myLab.name)
    setCity(myLab.city)
    setAddress(myLab.address ?? '')
    setContactPhone(myLab.contactPhone ?? '')
    setOperatingHours(myLab.operatingHours ?? 'T2-T7 08:00-18:00')
    setTransitDaysToHub(myLab.transitDaysToHub)
  }, [myLab])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErrorMsg(null)
    setSuccessMsg(null)
    try {
      const saved = await selfRegister.mutateAsync({
        name: name.trim(),
        city: city.trim(),
        transitDaysToHub: Number(transitDaysToHub),
        operatingHours: operatingHours.trim(),
        contactPhone: contactPhone.trim() || undefined,
        address: address.trim() || undefined,
      })
      setSuccessMsg(
        saved.status === 'Active'
          ? 'Đã cập nhật hồ sơ xưởng thành công.'
          : 'Đã gửi hồ sơ đăng ký xưởng. Vui lòng vào mục Khai báo máy in để thêm ít nhất 1 máy in trước khi ban quản trị duyệt kích hoạt.',
      )
    } catch (err) {
      setErrorMsg(getApiErrorMessage(err, 'Không thể lưu hồ sơ đăng ký xưởng.'))
    }
  }

  const machineCount = myLab?.machines?.length ?? 0
  const hasScheduleAndTransit = Boolean(operatingHours.trim()) && transitDaysToHub >= 0
  const status = myLab?.status ?? (myLab?.isActive ? 'Active' : 'Pending')

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 960, mx: 'auto' }} spacing={3}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'center' }}
          spacing={2}
        >
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Đăng ký &amp; Hồ sơ Xưởng in
              </Typography>
              <Typography color="text.secondary">
                Khai báo lịch làm việc &amp; thời gian vận chuyển về Hub
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.25} flexWrap="wrap" useFlexGap>
            <Button
              variant="outlined"
              color="primary"
              startIcon={<PrecisionManufacturingRounded />}
              onClick={() => navigate('/lab/machines')}
            >
              Máy in xưởng ({machineCount})
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ScheduleRounded />}
              onClick={() => navigate('/lab/timeline')}
              sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
            >
              Lịch máy &amp; Điểm xưởng
            </Button>
            <Button
              variant="outlined"
              color="error"
              startIcon={<LogoutRounded />}
              onClick={() => {
                logout()
                navigate('/login', { replace: true })
              }}
            >
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center', my: 4 }} />
        ) : (
          <>
            {/* Status & Checklist Banner */}
            <Box
              sx={{
                p: 3,
                borderRadius: 3,
                border: '1px solid rgba(255,255,255,0.12)',
                bgcolor: 'background.paper',
              }}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'flex-start', md: 'center' }}
                spacing={2}
              >
                <Box>
                  <Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
                    <FactoryRounded color="primary" />
                    <Typography sx={{ fontWeight: 700, fontSize: '1.1rem', color: 'text.primary' }}>
                      {myLab ? myLab.name : 'Xưởng chưa đăng ký hồ sơ'}
                    </Typography>
                    {myLab && (
                      <Chip
                        size="small"
                        label={
                          status === 'Active'
                            ? 'Đang hoạt động (Đã duyệt)'
                            : status === 'Rejected'
                              ? 'Cần bổ sung hồ sơ'
                              : 'Chờ duyệt kích hoạt'
                        }
                        color={status === 'Active' ? 'success' : status === 'Rejected' ? 'error' : 'warning'}
                      />
                    )}
                  </Stack>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
                    Lưu ý: Xưởng ở trạng thái chờ duyệt hoặc chưa hoàn tất hồ sơ sẽ chưa được phân bổ đơn in. Ban quản trị sẽ kích hoạt xưởng sau khi bạn khai báo ít nhất 1 máy in.
                  </Typography>
                  {myLab?.rejectionReason && (
                    <Alert severity="error" sx={{ mt: 1.5 }}>
                      Lý do từ chối từ ban quản trị: <b>{myLab.rejectionReason}</b> — hãy cập nhật hồ sơ bên dưới và gửi lại.
                    </Alert>
                  )}
                </Box>
              </Stack>

              <Divider sx={{ my: 2, borderColor: 'rgba(255,255,255,0.08)' }} />

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, md: 4 }}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    {myLab ? (
                      <CheckCircleRounded color="success" fontSize="small" />
                    ) : (
                      <ErrorOutlineRounded color="warning" fontSize="small" />
                    )}
                    <Typography variant="body2" color="text.primary">
                      1. Hồ sơ xưởng &amp; địa điểm: <b>{myLab ? 'Đã khai báo' : 'Chưa khai báo'}</b>
                    </Typography>
                  </Stack>
                </Grid>
                <Grid size={{ xs: 12, md: 4 }}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    {myLab && hasScheduleAndTransit ? (
                      <CheckCircleRounded color="success" fontSize="small" />
                    ) : (
                      <AccessTimeRounded color="warning" fontSize="small" />
                    )}
                    <Typography variant="body2" color="text.primary">
                      2. Lịch &amp; thời gian chuyển Hub: <b>{myLab ? `${myLab.operatingHours} (${myLab.transitDaysToHub} ngày)` : 'Chưa lưu'}</b>
                    </Typography>
                  </Stack>
                </Grid>
                <Grid size={{ xs: 12, md: 4 }}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    {machineCount > 0 ? (
                      <CheckCircleRounded color="success" fontSize="small" />
                    ) : (
                      <ErrorOutlineRounded color="error" fontSize="small" />
                    )}
                    <Typography variant="body2" color="text.primary">
                      3. Máy in khai báo: <b>{machineCount} máy</b> {machineCount === 0 ? '(Cần ít nhất 1 máy để được duyệt)' : ''}
                    </Typography>
                  </Stack>
                </Grid>
              </Grid>
            </Box>

            {errorMsg && (
              <Alert severity="error" onClose={() => setErrorMsg(null)}>
                {errorMsg}
              </Alert>
            )}
            {successMsg && (
              <Alert severity="success" onClose={() => setSuccessMsg(null)}>
                {successMsg}
              </Alert>
            )}

            {/* Registration / Profile Update Form */}
            <Box
              component="form"
              onSubmit={handleSubmit}
              sx={{
                p: { xs: 2.5, md: 3.5 },
                borderRadius: 3,
                border: '1px solid rgba(255,255,255,0.1)',
                bgcolor: 'background.paper',
              }}
            >
              <Typography variant="h2" sx={{ fontSize: '1.25rem', fontWeight: 700, mb: 0.5, color: 'text.primary' }}>
                {myLab ? 'Cập nhật hồ sơ & lịch hoạt động xưởng' : 'Khai báo đăng ký xưởng mới'}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5 }}>
                Khai báo lịch làm việc của xưởng và thời gian vận chuyển hàng tới Hub để hệ thống điều phối lịch in chính xác.
              </Typography>

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 7 }}>
                  <TextField
                    label="Tên xưởng in"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                    fullWidth
                    placeholder="VD: Xưởng In 3D Tân Bình"
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 5 }}>
                  <TextField
                    label="Tỉnh / Thành phố"
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    required
                    fullWidth
                    placeholder="VD: TP. Hồ Chí Minh"
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 7 }}>
                  <TextField
                    label="Địa chỉ chi tiết xưởng"
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    fullWidth
                    placeholder="VD: 45 Cộng Hòa, Phường 4, Quận Tân Bình"
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 5 }}>
                  <TextField
                    label="Số điện thoại liên hệ"
                    value={contactPhone}
                    onChange={(e) => setContactPhone(e.target.value)}
                    fullWidth
                    placeholder="VD: 0911000001"
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 7 }}>
                  <TextField
                    label="Lịch làm việc của xưởng"
                    value={operatingHours}
                    onChange={(e) => setOperatingHours(e.target.value)}
                    required
                    fullWidth
                    helperText="Khai báo ca/ngày hoạt động (VD: T2-T7 08:00-18:00 hoặc 24/7)"
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 5 }}>
                  <TextField
                    label="Thời gian vận chuyển tới Hub (ngày)"
                    type="number"
                    value={transitDaysToHub}
                    onChange={(e) => setTransitDaysToHub(Math.max(0, Number(e.target.value)))}
                    inputProps={{ min: 0, max: 14 }}
                    required
                    fullWidth
                    InputProps={{
                      startAdornment: <LocalShippingRounded fontSize="small" sx={{ mr: 1, color: 'text.secondary' }} />,
                    }}
                    helperText="Thời gian vận chuyển hàng hoàn thiện từ xưởng tới Hub PrintGrid"
                  />
                </Grid>
              </Grid>

              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'stretch', sm: 'center' }}
                spacing={2}
                sx={{ mt: 3 }}
              >
                <Typography variant="caption" color="text.secondary">
                  Sau khi lưu hồ sơ, hãy vào mục <b>Khai báo máy in</b> để thêm ít nhất 1 máy in trước khi ban quản trị duyệt.
                </Typography>
                <Stack direction="row" spacing={1.5}>
                  {myLab && (
                    <Button
                      variant="outlined"
                      color="primary"
                      startIcon={<PrecisionManufacturingRounded />}
                      onClick={() => navigate('/lab/machines')}
                    >
                      Khai báo máy in ({machineCount})
                    </Button>
                  )}
                  <Button
                    type="submit"
                    variant="contained"
                    color="primary"
                    disabled={selfRegister.isPending || !name.trim() || !city.trim() || !operatingHours.trim()}
                  >
                    {selfRegister.isPending ? 'Đang lưu…' : myLab ? 'Lưu cập nhật hồ sơ' : 'Gửi hồ sơ đăng ký xưởng'}
                  </Button>
                </Stack>
              </Stack>
            </Box>
          </>
        )}
      </Stack>
    </Box>
  )
}
