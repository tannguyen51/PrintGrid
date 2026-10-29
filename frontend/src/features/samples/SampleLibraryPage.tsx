import { useEffect, useMemo, useState } from 'react'
import { Alert, Button, Chip, Grid, InputAdornment, Skeleton, Stack, TextField, Typography } from '@mui/material'
import { SearchRounded, ViewInArOutlined } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { landing, PillButton } from '../../shared/theme/landing'
import { EmptyState } from '../../shared/components/EmptyState'
import { PageHeading, PublicPage } from '../public/PublicLayout'
import { SampleCard } from './components/SampleCard'
import { SAMPLE_CATEGORIES } from './sampleTypes'
import { useSamples } from './useSamples'

function useDebouncedValue<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])
  return debounced
}

export default function SampleLibraryPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const { data, isLoading, isError, refetch } = useSamples(debouncedSearch, category)

  const { isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const canOrder = isAuthenticated && (user?.roles?.includes('Customer') ?? false)

  // Categories actually present in the catalogue come first; the fixed list keeps the
  // filter usable while the library is still empty.
  const categories = useMemo(() => {
    const present = new Set((data ?? []).map((sample) => sample.category))
    return Array.from(new Set([...SAMPLE_CATEGORIES.filter((c) => present.has(c)), ...SAMPLE_CATEGORIES]))
  }, [data])

  return (
    <PublicPage>
      <PageHeading
        eyebrow="Kho mẫu 3D"
        title="Một thư viện mẫu in sẵn để bắt đầu."
        description="Tất cả mẫu do PrintGrid tự thiết kế và đăng tải. Xem trước bằng 3D, chọn mẫu rồi đặt in — hệ thống sẽ sao chép mẫu vào thư viện riêng của bạn."
        actions={
          <PillButton onClick={() => navigate(canOrder ? '/order/new' : '/register')}>
            {canOrder ? 'Đặt in model của bạn' : 'Tạo tài khoản'}
          </PillButton>
        }
      />

      {/* ── Filters ── */}
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        alignItems={{ xs: 'stretch', md: 'center' }}
        sx={{ mb: 3.5 }}
      >
        <TextField
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Tìm theo tên hoặc mô tả…"
          size="small"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRounded sx={{ color: landing.textMuted, fontSize: 19 }} />
                </InputAdornment>
              ),
            },
          }}
          sx={{
            maxWidth: { md: 340 },
            '& .MuiOutlinedInput-root': { bgcolor: landing.card, borderRadius: `${landing.radius}px` },
          }}
        />
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <Chip
            label="Tất cả"
            onClick={() => setCategory('')}
            sx={chipSx(category === '')}
          />
          {categories.map((item) => (
            <Chip
              key={item}
              label={item}
              onClick={() => setCategory(item === category ? '' : item)}
              sx={chipSx(category === item)}
            />
          ))}
        </Stack>
      </Stack>

      {isError && (
        <Alert
          severity="error"
          action={<Button onClick={() => refetch()}>Thử lại</Button>}
          sx={{ mb: 3, borderRadius: `${landing.radius}px` }}
        >
          Không tải được kho mẫu. Kiểm tra kết nối rồi thử lại.
        </Alert>
      )}

      {isLoading ? (
        <Grid container spacing={2.5}>
          {Array.from({ length: 6 }).map((_, index) => (
            <Grid key={index} size={{ xs: 12, sm: 6, lg: 4 }}>
              <Skeleton variant="rounded" height={320} sx={{ bgcolor: '#111114', borderRadius: `${landing.radius}px` }} />
            </Grid>
          ))}
        </Grid>
      ) : (data ?? []).length === 0 ? (
        <EmptyState
          icon={<ViewInArOutlined />}
          title={search || category ? 'Không tìm thấy mẫu phù hợp' : 'Kho mẫu đang được cập nhật'}
          description={
            search || category
              ? 'Thử bỏ bộ lọc hoặc tìm bằng từ khóa khác.'
              : 'Bạn vẫn có thể đặt in ngay bằng cách tải model của mình lên.'
          }
          action={
            search || category ? (
              <PillButton onClick={() => { setSearch(''); setCategory('') }}>Xóa bộ lọc</PillButton>
            ) : (
              <PillButton onClick={() => navigate(canOrder ? '/order/new' : '/register')}>Tải model của bạn lên</PillButton>
            )
          }
        />
      ) : (
        <>
          <Typography sx={{ color: landing.textMuted, fontSize: '0.84rem', mb: 2 }}>
            {data!.length} mẫu
          </Typography>
          <Grid container spacing={2.5}>
            {(data ?? []).map((sample) => (
              <Grid key={sample.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                <SampleCard sample={sample} />
              </Grid>
            ))}
          </Grid>
        </>
      )}
    </PublicPage>
  )
}

function chipSx(active: boolean) {
  return {
    bgcolor: active ? landing.text : landing.card,
    color: active ? '#0A0A0A' : landing.textMuted,
    border: `1px solid ${active ? landing.text : landing.hairline}`,
    borderRadius: '999px',
    fontWeight: active ? 700 : 500,
    '&:hover': { bgcolor: active ? landing.text : '#16161A' },
  } as const
}
