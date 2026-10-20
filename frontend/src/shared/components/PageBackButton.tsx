import { ArrowBackRounded } from '@mui/icons-material'
import { IconButton, Tooltip } from '@mui/material'
import { useNavigate } from 'react-router-dom'

interface PageBackButtonProps {
  /** Where the button goes. Defaults to the homepage, which every existing page wants. */
  to?: string
  /** Tooltip + accessible label; defaults to "Về trang chủ". */
  title?: string
}

export function PageBackButton({ to = '/', title = 'Về trang chủ' }: PageBackButtonProps = {}) {
  const navigate = useNavigate()

  return (
    <Tooltip title={title}>
      <IconButton
        aria-label={title}
        onClick={() => navigate(to)}
        sx={{ mt: 0.25, color: 'text.secondary', border: '1px solid', borderColor: 'divider', flexShrink: 0 }}
      >
        <ArrowBackRounded />
      </IconButton>
    </Tooltip>
  )
}
