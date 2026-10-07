import { useEffect, useState } from 'react'
import { AddRounded, DeleteOutlineRounded, EditRounded, LocationOnOutlined, SaveRounded, VerifiedRounded } from '@mui/icons-material'
import { Alert, Box, Button, Checkbox, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Grid, IconButton, Paper, Stack, TextField, Typography } from '@mui/material'
import { CustomerNavbar } from '../home/components/CustomerNavbar'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useAddresses, useDeleteAddress, useProfile, useSaveAddress, useUpdateProfile } from './useAccount'
import type { AddressInput, CustomerAddress } from './accountTypes'

const emptyAddress: AddressInput = { label: 'Nhà riêng', recipientName: '', phoneNumber: '', street: '', ward: '', district: '', city: '', postalCode: '', country: 'VN', isDefault: false }

export default function AccountPage() {
  const profile = useProfile()
  const addresses = useAddresses()
  const updateProfile = useUpdateProfile()
  const saveAddress = useSaveAddress()
  const deleteAddress = useDeleteAddress()
  const [fullName, setFullName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [editing, setEditing] = useState<CustomerAddress | null | undefined>(undefined)
  const [address, setAddress] = useState<AddressInput>(emptyAddress)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    // The form is an editable draft and must be reset when the async profile changes.
    // oxlint-disable-next-line react/set-state-in-effect
    if (profile.data) { setFullName(profile.data.fullName); setPhoneNumber(profile.data.phoneNumber ?? '') }
  }, [profile.data])

  const openAddress = (value?: CustomerAddress) => {
    setEditing(value ?? null)
    setAddress(value ? { label: value.label, recipientName: value.recipientName, phoneNumber: value.phoneNumber, street: value.street, ward: value.ward, district: value.district, city: value.city, postalCode: value.postalCode, country: value.country, isDefault: value.isDefault } : emptyAddress)
  }

  const saveProfile = async () => {
    setMessage(null)
    try { await updateProfile.mutateAsync({ fullName, phoneNumber: phoneNumber || null }); setMessage('Đã cập nhật hồ sơ.') }
    catch { setMessage('Không thể cập nhật hồ sơ.') }
  }

  const submitAddress = async () => {
    await saveAddress.mutateAsync({ id: editing?.id, input: address })
    setEditing(undefined)
  }

  if (profile.isLoading || addresses.isLoading) return <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>

  return <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
    <CustomerNavbar />
    <Stack spacing={3} sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1050, mx: 'auto' }}>
      <Stack direction="row" spacing={1.5} alignItems="flex-start"><PageBackButton /><Box>
        <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Hồ sơ & địa chỉ</Typography>
        <Typography color="text.secondary">Quản lý thông tin tài khoản và địa chỉ giao hàng của bạn.</Typography>
      </Box></Stack>

      {(profile.isError || addresses.isError) && <Alert severity="error">Không tải được thông tin tài khoản.</Alert>}
      {message && <Alert severity={message.startsWith('Đã') ? 'success' : 'error'} onClose={() => setMessage(null)}>{message}</Alert>}

      <Paper sx={{ p: 3, borderRadius: 3 }}>
        <Stack spacing={2.25}>
          <Stack direction="row" justifyContent="space-between" alignItems="center"><Typography variant="h6" fontWeight={700}>Thông tin cá nhân</Typography>
            {profile.data?.isEmailVerified && <Chip size="small" icon={<VerifiedRounded />} color="success" label="Email đã xác minh" />}
          </Stack>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label="Họ và tên" value={fullName} onChange={e => setFullName(e.target.value)} required /></Grid>
            <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth label="Số điện thoại" value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} /></Grid>
            <Grid size={{ xs: 12 }}><TextField fullWidth label="Email" value={profile.data?.email ?? ''} disabled /></Grid>
          </Grid>
          <Box><Button variant="contained" startIcon={<SaveRounded />} disabled={!fullName.trim() || updateProfile.isPending} onClick={saveProfile}>Lưu hồ sơ</Button></Box>
        </Stack>
      </Paper>

      <Stack direction="row" justifyContent="space-between" alignItems="center"><Box><Typography variant="h6" fontWeight={700}>Địa chỉ giao hàng</Typography><Typography variant="body2" color="text.secondary">Địa chỉ mặc định sẽ được ưu tiên khi thanh toán.</Typography></Box>
        <Button variant="contained" startIcon={<AddRounded />} onClick={() => openAddress()}>Thêm địa chỉ</Button>
      </Stack>

      {deleteAddress.isError && <Alert severity="error">{(deleteAddress.error as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message ?? 'Không thể xóa địa chỉ.'}</Alert>}
      {(addresses.data?.length ?? 0) === 0 ? <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', borderRadius: 3 }}><LocationOnOutlined color="disabled" sx={{ fontSize: 42 }} /><Typography color="text.secondary">Bạn chưa lưu địa chỉ giao hàng.</Typography></Paper> :
        <Grid container spacing={2}>{addresses.data?.map(item => <Grid key={item.id} size={{ xs: 12, md: 6 }}><Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3, height: '100%' }}><Stack spacing={1}>
          <Stack direction="row" justifyContent="space-between"><Stack direction="row" spacing={1} alignItems="center"><Typography fontWeight={700}>{item.label}</Typography>{item.isDefault && <Chip size="small" color="primary" label="Mặc định" />}</Stack><Box><IconButton aria-label="Sửa địa chỉ" onClick={() => openAddress(item)}><EditRounded fontSize="small" /></IconButton><IconButton aria-label="Xóa địa chỉ" color="error" onClick={() => deleteAddress.mutate(item.id)}><DeleteOutlineRounded fontSize="small" /></IconButton></Box></Stack>
          <Typography>{item.recipientName} · {item.phoneNumber}</Typography><Typography color="text.secondary">{[item.street, item.ward, item.district, item.city].filter(Boolean).join(', ')}</Typography>
        </Stack></Paper></Grid>)}</Grid>}
    </Stack>

    <Dialog open={editing !== undefined} onClose={() => setEditing(undefined)} fullWidth maxWidth="sm">
      <DialogTitle>{editing ? 'Sửa địa chỉ' : 'Thêm địa chỉ'}</DialogTitle><DialogContent><Grid container spacing={2} sx={{ mt: 0.25 }}>
        {(['label','recipientName','phoneNumber','street','ward','district','city','postalCode'] as const).map(key => <Grid key={key} size={{ xs: 12, sm: key === 'street' ? 12 : 6 }}><TextField fullWidth required={['label','recipientName','phoneNumber','street','city'].includes(key)} label={({ label:'Nhãn địa chỉ', recipientName:'Người nhận', phoneNumber:'Số điện thoại', street:'Địa chỉ', ward:'Phường/Xã', district:'Quận/Huyện', city:'Tỉnh/Thành phố', postalCode:'Mã bưu chính' } as const)[key]} value={address[key]} onChange={e => setAddress(v => ({ ...v, [key]: e.target.value }))} /></Grid>)}
        <Grid size={{ xs: 12 }}><FormControlLabel control={<Checkbox checked={address.isDefault} onChange={e => setAddress(v => ({ ...v, isDefault: e.target.checked }))} />} label="Đặt làm địa chỉ mặc định" /></Grid>
      </Grid>{saveAddress.isError && <Alert severity="error" sx={{ mt: 2 }}>Không thể lưu địa chỉ. Hãy kiểm tra các trường bắt buộc.</Alert>}</DialogContent>
      <DialogActions><Button onClick={() => setEditing(undefined)}>Hủy</Button><Button variant="contained" disabled={saveAddress.isPending || !address.recipientName || !address.phoneNumber || !address.street || !address.city} onClick={submitAddress}>Lưu địa chỉ</Button></DialogActions>
    </Dialog>
  </Box>
}
