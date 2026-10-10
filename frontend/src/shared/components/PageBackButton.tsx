import { ArrowBackRounded } from '@mui/icons-material'
import { IconButton, Tooltip } from '@mui/material'
import { useNavigate } from 'react-router-dom'

export function PageBackButton() {
  const navigate = useNavigate()

  return (
    <Tooltip title="Về trang chủ">
      <IconButton
        aria-label="Về trang chủ"
        onClick={() => navigate('/')}
        sx={{ mt: 0.25, color: 'text.secondary', border: '1px solid', borderColor: 'divider', flexShrink: 0 }}
      >
        <ArrowBackRounded />
      </IconButton>
    </Tooltip>
  )
}
