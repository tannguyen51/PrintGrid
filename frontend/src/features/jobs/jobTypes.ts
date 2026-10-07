export type JobStatus =
  | 'Pending'
  | 'Assigned'
  | 'Accepted'
  | 'InProgress'
  | 'AwaitingInspection'
  | 'Completed'
  | 'Failed'
  | 'Reassigned'
  | 'Cancelled'

export interface Job {
  id: string
  orderItemId: string
  modelId: string
  status: JobStatus
  internalDueDate: string
  estimatedPrintMinutes: number
  labId?: string | null
  machineId?: string | null
  plannedStartUtc?: string | null
  plannedEndUtc?: string | null
  startedAtUtc?: string | null
  completedAtUtc?: string | null
  actualPrintMinutes?: number | null
  failureReason?: string | null
  materialCode: string
  colorCode: string
  layerHeightMm: number
  attemptNumber: number
  assignedAtUtc?: string | null
  acceptanceDeadlineUtc?: string | null
  qcProofStatus: 'None' | 'Pending' | 'Approved' | 'Rejected'
  qcSelfReport?: string | null
  qcProofPhotoKeys: string[]
  qcReviewedBy?: string | null
  qcReviewedAtUtc?: string | null
  qcRejectionReason?: string | null
}

export interface QcProofQueueItem {
  job: Job
  photoUrls: string[]
}

export const JOB_LABELS: Record<JobStatus, string> = {
  Pending: 'Chờ gán việc',
  Assigned: 'Đã gán cho lab',
  Accepted: 'Lab đã nhận',
  InProgress: 'Đang in',
  AwaitingInspection: 'Chờ kiểm tra',
  Completed: 'Hoàn thành',
  Failed: 'Thất bại',
  Reassigned: 'Chờ gán lại',
  Cancelled: 'Đã hủy',
}

export const JOB_STEPS: JobStatus[] = [
  'Pending',
  'Assigned',
  'Accepted',
  'InProgress',
  'AwaitingInspection',
  'Completed',
]
