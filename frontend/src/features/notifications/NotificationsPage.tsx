import {
  Alert,
  Badge,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material'
import {
  NotificationsRounded,
  InfoRounded,
  WarningRounded,
  CheckCircleRounded,
  ErrorRounded,
  MarkEmailReadRounded,
} from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useNotifications, useMarkAllNotificationsRead } from './useNotifications'

const fmtDateTime = (d: string) => new Date(d).toLocaleString('vi-VN', {
  hour: '2-digit',
  minute: '2-digit',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

const typeConfig: Record<string, { icon: React.ReactNode; color: 'info' | 'warning' | 'success' | 'error' }> = {
  info: { icon: <InfoRounded />, color: 'info' },
  warning: { icon: <WarningRounded />, color: 'warning' },
  success: { icon: <CheckCircleRounded />, color: 'success' },
  error: { icon: <ErrorRounded />, color: 'error' },
}

export default function NotificationsPage() {
  const { data, isLoading, isError, error } = useNotifications()
  const markAllRead = useMarkAllNotificationsRead()
  const navigate = useNavigate()

  const handleNotificationClick = (notification: { id: string; link?: string }) => {
    // TODO: Call API to mark as read
    if (notification.link) {
      navigate(notification.link)
    }
  }

  const handleMarkAllRead = async () => {
    try {
      await markAllRead()
    } catch (err) {
      console.error('Failed to mark all as read:', err)
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Không tải được thông báo. {error instanceof Error ? error.message : ''}
        </Alert>
      </Box>
    )
  }

  const notifications = data?.items ?? []
  const unreadCount = data?.unreadCount ?? 0

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 800, mx: 'auto' }} spacing={2.5}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsRounded sx={{ fontSize: 32, color: 'text.primary' }} />
            </Badge>
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Thông báo
              </Typography>
              <Typography color="text.secondary">
                {unreadCount > 0 ? `${unreadCount} thông báo chưa đọc` : 'Tất cả thông báo đã đọc'}
              </Typography>
            </Box>
          </Stack>
          {unreadCount > 0 && (
            <Button
              variant="outlined"
              startIcon={<MarkEmailReadRounded />}
              onClick={handleMarkAllRead}
              size="small"
            >
              Đánh dấu đã đọc tất cả
            </Button>
          )}
        </Stack>

        {notifications.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed #E8EBF2' }}>
            <NotificationsRounded sx={{ fontSize: 48, color: 'text.disabled', mb: 2 }} />
            <Typography color="text.secondary">Chưa có thông báo nào</Typography>
          </Box>
        ) : (
          <Stack spacing={1.5}>
            {notifications.map((notification) => {
              const config = typeConfig[notification.type] ?? typeConfig.info
              return (
                <Card
                  key={notification.id}
                  sx={{
                    border: notification.isRead ? '1px solid #E8EBF2' : `2px solid`,
                    borderColor: notification.isRead ? '#E8EBF2' : `${config.color}.main`,
                    bgcolor: notification.isRead ? 'background.paper' : `${config.color}.50`,
                  }}
                >
                  <CardActionArea onClick={() => handleNotificationClick(notification)}>
                    <CardContent sx={{ p: 2.5 }}>
                      <Stack direction="row" spacing={2} alignItems="flex-start">
                        <Box sx={{ color: `${config.color}.main`, mt: 0.5 }}>
                          {config.icon}
                        </Box>
                        <Box sx={{ flex: 1 }}>
                          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 0.5 }}>
                            <Typography sx={{ fontWeight: notification.isRead ? 600 : 800, fontSize: '1rem' }}>
                              {notification.title}
                            </Typography>
                            {!notification.isRead && (
                              <Chip size="small" color={config.color} label="Mới" />
                            )}
                          </Stack>
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 1, lineHeight: 1.6 }}>
                            {notification.message}
                          </Typography>
                          <Typography variant="caption" color="text.disabled">
                            {fmtDateTime(notification.createdAtUtc)}
                          </Typography>
                        </Box>
                      </Stack>
                    </CardContent>
                  </CardActionArea>
                </Card>
              )
            })}
          </Stack>
        )}
      </Stack>
    </Box>
  )
}
