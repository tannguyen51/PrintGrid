import { useEffect, useState } from 'react'
import {
  Alert,
  Badge,
  Box,
  Button,
  CircularProgress,
  Divider,
  IconButton,
  List,
  ListItemButton,
  Popover,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material'
import { CheckRounded, NotificationsNoneRounded } from '@mui/icons-material'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { apiClient } from '../api/apiClient'
import { getAccessToken } from '../api/tokenStore'

interface NotificationItem {
  id: string
  title: string
  message: string
  type: string
  link: string | null
  createdAtUtc: string
  readAtUtc: string | null
}

interface NotificationList {
  items: NotificationItem[]
  unreadCount: number
}

function notificationHubUrl(): string {
  const apiBase = apiClient.defaults.baseURL ?? '/api/v1'
  const hubPath = `${apiBase.replace(/\/api\/v1\/?$/, '').replace(/\/$/, '')}/hubs/notifications`
  return /^https?:\/\//i.test(hubPath) ? hubPath : `${window.location.origin}${hubPath}`
}

function formatCreatedAt(value: string): string {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function NotificationBell() {
  const { isAuthenticated, user } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const userId = user?.id
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)

  const notificationsQuery = useQuery({
    queryKey: ['notifications', userId],
    queryFn: async () => {
      const { data } = await apiClient.get<NotificationList>('/notifications')
      return data
    },
    enabled: isAuthenticated,
    staleTime: 15_000,
  })

  const markReadMutation = useMutation({
    mutationFn: (id: string) => apiClient.patch(`/notifications/${id}/read`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications', userId] }),
  })

  const markAllReadMutation = useMutation({
    mutationFn: () => apiClient.post('/notifications/read-all'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications', userId] }),
  })

  useEffect(() => {
    if (!isAuthenticated || !userId) return

    const connection = new HubConnectionBuilder()
      .withUrl(notificationHubUrl(), {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('NotificationsUpdated', () => {
      void queryClient.invalidateQueries({ queryKey: ['notifications', userId] })
    })

    let disposed = false
    void connection.start()
      .then(() => {
        if (!disposed) {
          void queryClient.invalidateQueries({ queryKey: ['notifications', userId] })
        }
      })
      .catch((error: unknown) => {
        if (!disposed) console.error('Unable to connect to the notifications hub.', error)
      })

    return () => {
      disposed = true
      void connection.stop()
    }
  }, [isAuthenticated, queryClient, userId])

  if (!isAuthenticated) return null

  const notifications = notificationsQuery.data?.items ?? []
  const unreadCount = notificationsQuery.data?.unreadCount ?? 0

  const openNotification = (notification: NotificationItem) => {
    if (!notification.readAtUtc) markReadMutation.mutate(notification.id)
    setAnchorEl(null)
    if (notification.link) navigate(notification.link)
  }

  return (
    <>
      <IconButton
        aria-label={`Thông báo${unreadCount > 0 ? `, ${unreadCount} chưa đọc` : ''}`}
        aria-haspopup="dialog"
        aria-expanded={Boolean(anchorEl)}
        onClick={(event) => setAnchorEl(event.currentTarget)}
        sx={{ border: '1px solid', borderColor: 'divider', bgcolor: '#FFFFFF' }}
      >
        <Badge badgeContent={unreadCount} color="error" max={99}>
          <NotificationsNoneRounded fontSize="small" />
        </Badge>
      </IconButton>

      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: { xs: 'calc(100vw - 24px)', sm: 380 }, maxWidth: 380, mt: 1 } } }}
      >
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ px: 2, py: 1.5 }}>
          <Box>
            <Typography fontWeight={700}>Thông báo</Typography>
            <Typography variant="caption" color="text.secondary">
              {unreadCount > 0 ? `${unreadCount} thông báo chưa đọc` : 'Bạn đã đọc hết thông báo'}
            </Typography>
          </Box>
          <Button
            size="small"
            startIcon={<CheckRounded />}
            disabled={unreadCount === 0 || markAllReadMutation.isPending}
            onClick={() => markAllReadMutation.mutate()}
          >
            Đọc tất cả
          </Button>
        </Stack>
        <Divider />

        {notificationsQuery.isPending ? (
          <Box sx={{ display: 'grid', placeItems: 'center', p: 4 }}>
            <CircularProgress size={24} />
          </Box>
        ) : notificationsQuery.isError ? (
          <Typography role="alert" color="error" sx={{ p: 2 }}>
            Không thể tải thông báo. Vui lòng thử lại.
          </Typography>
        ) : notifications.length === 0 ? (
          <Typography color="text.secondary" textAlign="center" sx={{ p: 4 }}>
            Chưa có thông báo nào.
          </Typography>
        ) : (
          <List disablePadding sx={{ maxHeight: 420, overflowY: 'auto' }}>
            {notifications.map((notification) => (
              <ListItemButton
                key={notification.id}
                onClick={() => openNotification(notification)}
                sx={{
                  alignItems: 'flex-start',
                  gap: 1.25,
                  px: 2,
                  py: 1.5,
                  bgcolor: notification.readAtUtc ? 'background.paper' : 'action.hover',
                  borderBottom: '1px solid',
                  borderColor: 'divider',
                }}
              >
                <Box
                  aria-hidden
                  sx={{
                    width: 8,
                    height: 8,
                    mt: 0.8,
                    borderRadius: '50%',
                    flexShrink: 0,
                    bgcolor: notification.readAtUtc ? 'transparent' : 'primary.main',
                  }}
                />
                <Box sx={{ minWidth: 0 }}>
                  <Typography fontSize="0.9rem" fontWeight={notification.readAtUtc ? 500 : 700}>
                    {notification.title}
                  </Typography>
                  <Typography fontSize="0.82rem" color="text.secondary" sx={{ mt: 0.25 }}>
                    {notification.message}
                  </Typography>
                  <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.75 }}>
                    {formatCreatedAt(notification.createdAtUtc)}
                  </Typography>
                </Box>
              </ListItemButton>
            ))}
          </List>
        )}
      </Popover>
      <Snackbar
        open={markReadMutation.isError || markAllReadMutation.isError}
        autoHideDuration={5000}
        onClose={() => {
          markReadMutation.reset()
          markAllReadMutation.reset()
        }}
      >
        <Alert
          severity="error"
          onClose={() => {
            markReadMutation.reset()
            markAllReadMutation.reset()
          }}
        >
          Không thể cập nhật trạng thái thông báo. Vui lòng thử lại.
        </Alert>
      </Snackbar>
    </>
  )
}
