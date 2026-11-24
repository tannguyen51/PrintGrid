import { useQuery } from '@tanstack/react-query'
import axios from 'axios'

export interface Notification {
  id: string
  title: string
  message: string
  type: string // 'info' | 'warning' | 'success' | 'error'
  link?: string
  isRead: boolean
  createdAtUtc: string
}

export interface NotificationsResponse {
  items: Notification[]
  totalCount: number
  unreadCount: number
}

export function useNotifications() {
  return useQuery({
    queryKey: ['notifications'],
    queryFn: async () => {
      const response = await axios.get('/api/v1/notifications')
      return response.data as NotificationsResponse
    },
    refetchInterval: 30000, // Refetch every 30 seconds
  })
}

export function useMarkNotificationRead() {
  return async (notificationId: string) => {
    await axios.post(`/api/v1/notifications/${notificationId}/mark-read`)
  }
}

export function useMarkAllNotificationsRead() {
  return async () => {
    await axios.post('/api/v1/notifications/mark-all-read')
  }
}
