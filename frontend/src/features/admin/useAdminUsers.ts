import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/** The eight roles the backend recognises (Roles.cs). Keep in sync. */
export const ROLES = [
  'Customer',
  'LabManager',
  'LabOperator',
  'HubQC',
  'HubFulfillment',
  'OrderStaff',
  'OpsManager',
  'Admin',
] as const

export type Role = (typeof ROLES)[number]

export const ROLE_LABELS: Record<Role, string> = {
  Customer: 'Khách hàng',
  LabManager: 'Quản lý xưởng',
  LabOperator: 'Vận hành máy',
  HubQC: 'Kiểm định hub',
  HubFulfillment: 'Đóng gói & giao hàng',
  OrderStaff: 'Nhân viên duyệt đơn',
  OpsManager: 'Điều phối mạng lưới',
  Admin: 'Quản trị hệ thống',
}

export interface AdminUser {
  id: string
  email: string
  fullName: string
  phoneNumber?: string | null
  roles: string[]
  isActive: boolean
  isEmailVerified: boolean
  createdAt: string
  lastLoginAt?: string | null
}

export interface CreateUserInput {
  email: string
  password: string
  fullName: string
  phoneNumber?: string
  role: Role
}

const USERS_KEY = ['admin', 'users'] as const

export function useAdminUsers() {
  return useQuery({
    queryKey: USERS_KEY,
    queryFn: () => apiClient.get<AdminUser[]>('/users').then((r) => r.data),
  })
}

function useInvalidateUsers() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: USERS_KEY })
}

export function useCreateUser() {
  const invalidate = useInvalidateUsers()
  return useMutation({
    mutationFn: (input: CreateUserInput) => apiClient.post('/users', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useAssignRole() {
  const invalidate = useInvalidateUsers()
  return useMutation({
    mutationFn: ({ id, role }: { id: string; role: Role }) =>
      apiClient.put(`/users/${id}/role`, { role }),
    onSuccess: invalidate,
  })
}

export function useDeactivateUser() {
  const invalidate = useInvalidateUsers()
  return useMutation({
    mutationFn: (id: string) => apiClient.put(`/users/${id}/deactivate`),
    onSuccess: invalidate,
  })
}