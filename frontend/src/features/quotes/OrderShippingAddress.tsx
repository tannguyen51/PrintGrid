import { useEffect, useState } from 'react'
import { AddRounded, EditOutlined } from '@mui/icons-material'
import { Alert, Box, Button, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Grid, Radio, RadioGroup, Stack, TextField, Typography } from '@mui/material'
import type { AddressInput, CustomerAddress } from '../account/accountTypes'
import { useAddresses, useProfile, useSaveAddress } from '../account/useAccount'
import { getApiErrorMessage } from '../../shared/api/apiError'

const fields = [
  { key: 'label', label: 'Nhãn địa chỉ', maxLength: 50 },
  { key: 'recipientName', label: 'Người nhận', maxLength: 200 },
  { key: 'phoneNumber', label: 'Số điện thoại', maxLength: 32 },
  { key: 'street', label: 'Số nhà, đường', maxLength: 200 },
  { key: 'ward', label: 'Phường/Xã', maxLength: 100 },
  { key: 'district', label: 'Quận/Huyện', maxLength: 100 },
  { key: 'city', label: 'Tỉnh/Thành phố', maxLength: 100 },
  { key: 'postalCode', label: 'Mã bưu điện', maxLength: 20 },
] as const

export function OrderShippingAddress({ value, onChange }: {
  value: CustomerAddress | null
  onChange: (address: CustomerAddress | null) => void
}) {
  const addresses = useAddresses()
  const profile = useProfile()
  const save = useSaveAddress()
  const [draft, setDraft] = useState<AddressInput | null>(null)
  const [editingId, setEditingId] = useState<string>()

  useEffect(() => {
    if (!addresses.data) return
    // Preserve the customer's choice across refetches and quote changes.
    const selected = addresses.data.find(address => address.id === value?.id)
      ?? addresses.data.find(address => address.isDefault) ?? addresses.data[0] ?? null
    if (selected !== value) onChange(selected)
  }, [addresses.data, value, onChange])

  function openForm(address?: CustomerAddress) {
    save.reset()
    setEditingId(address?.id)
    setDraft(address ? { ...address } : {
      label: 'Nhà riêng', recipientName: profile.data?.fullName ?? '', phoneNumber: profile.data?.phoneNumber ?? '',
      street: '', ward: '', district: '', city: '', postalCode: '', country: 'VN', isDefault: !addresses.data?.length,
    })
  }

  async function submit() {
    if (!draft) return
    const input = { ...draft }
    for (const { key } of fields) input[key] = input[key].trim()
    try {
      const saved = await save.mutateAsync({ id: editingId, input })
      onChange(saved)
      setDraft(null)
    } catch { /* The dialog keeps the draft and displays the API error. */ }
  }

  const valid = draft && fields.every(({ key, maxLength }) =>
    (key === 'postalCode' || !!draft[key].trim()) && draft[key].trim().length <= maxLength)

  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} gap={1}>
        <Typography fontWeight={700} id="shipping-address-label">Địa chỉ giao hàng</Typography>
        <Button startIcon={<AddRounded />} onClick={() => openForm()} disabled={addresses.isLoading}>Thêm địa chỉ</Button>
      </Stack>
      {addresses.isLoading && <Typography role="status" color="text.secondary">Đang tải địa chỉ từ hồ sơ…</Typography>}
      {addresses.isError && <Alert severity="error">Không tải được địa chỉ giao hàng. <Button onClick={() => void addresses.refetch()}>Thử lại</Button></Alert>}
      {addresses.data?.length === 0 && <Alert severity="info">Bạn chưa lưu địa chỉ. Thêm địa chỉ để nhận hàng và sử dụng cho các lần đặt sau.</Alert>}
      <RadioGroup aria-labelledby="shipping-address-label" value={value?.id ?? ''} onChange={(_, id) => {
        const selected = addresses.data?.find(address => address.id === id)
        if (selected) onChange(selected)
      }}>
        <Stack spacing={1}>
          {addresses.data?.map(address => (
            <Box key={address.id} sx={{ p: 1.5, border: '1px solid', borderColor: value?.id === address.id ? 'primary.main' : 'divider', borderRadius: 2, bgcolor: value?.id === address.id ? '#F5F2FF' : 'transparent' }}>
              <FormControlLabel value={address.id} sx={{ m: 0, alignItems: 'flex-start', width: '100%' }} control={<Radio size="small" />} label={
                <Stack spacing={0.5} sx={{ py: 0.75, overflowWrap: 'anywhere' }}>
                  <Stack direction="row" alignItems="center" gap={1} flexWrap="wrap"><Typography fontWeight={700}>{address.label}</Typography>{address.isDefault && <Chip label="Mặc định" color="primary" size="small" />}</Stack>
                  <Typography variant="body2">{address.recipientName} · {address.phoneNumber}</Typography>
                  <Typography variant="body2" color="text.secondary">{[address.street, address.ward, address.district, address.city, address.postalCode].filter(Boolean).join(', ')}</Typography>
                </Stack>
              } />
              {value?.id === address.id && <Button size="small" startIcon={<EditOutlined />} onClick={() => openForm(address)}>Sửa địa chỉ</Button>}
            </Box>
          ))}
        </Stack>
      </RadioGroup>
      <Dialog open={!!draft} onClose={() => { if (!save.isPending) setDraft(null) }} fullWidth maxWidth="sm">
        <Box component="form" onSubmit={event => { event.preventDefault(); void submit() }}>
          <DialogTitle>{editingId ? 'Sửa địa chỉ' : 'Thêm địa chỉ'}</DialogTitle>
          <DialogContent>
            <Grid container spacing={2} sx={{ mt: 0.5 }}>
              {draft && fields.map(({ key, label, maxLength }) => <Grid key={key} size={{ xs: 12, sm: key === 'street' ? 12 : 6 }}>
                <TextField fullWidth required={key !== 'postalCode'} label={label} value={draft[key]} type={key === 'phoneNumber' ? 'tel' : 'text'} disabled={save.isPending}
                  inputProps={{ maxLength }} onChange={event => setDraft(current => current && { ...current, [key]: event.target.value })} />
              </Grid>)}
              <Grid size={12}><FormControlLabel control={<Checkbox checked={draft?.isDefault ?? false} disabled={save.isPending} onChange={event => setDraft(current => current && { ...current, isDefault: event.target.checked })} />} label="Đặt làm địa chỉ mặc định" /></Grid>
            </Grid>
            {save.isError && <Alert severity="error" sx={{ mt: 2 }}>{getApiErrorMessage(save.error, 'Không thể lưu địa chỉ. Vui lòng thử lại.')}</Alert>}
          </DialogContent>
          <DialogActions><Button disabled={save.isPending} onClick={() => setDraft(null)}>Hủy</Button><Button type="submit" variant="contained" disabled={!valid || save.isPending}>{save.isPending ? 'Đang lưu…' : 'Lưu và sử dụng'}</Button></DialogActions>
        </Box>
      </Dialog>
    </Stack>
  )
}
