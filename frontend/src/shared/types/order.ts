export type OrderStatus =
  | 'AwaitingApproval'
  | 'PaymentPending'
  | 'Confirmed'
  | 'InProduction'
  | 'QualityCheck'
  | 'Shipping'
  | 'Delivered'
  | 'Cancelled'

export interface OrderItem {
  id: string
  modelId: string
  quantity: number
  materialCode: string
  colorCode: string
  layerHeightMm: number
  infillPercent: number
  unitPrice: number
}

export interface Order {
  id: string
  orderNumber: string
  status: OrderStatus
  totalAmount: number
  currency: string
  promisedDeliveryDate: string
  requestedDeliveryDate?: string | null
  dispatchMode?: 'Automatic' | 'Manual'
  createdAt: string
  deliveredAt: string | null
  /**
   * Not part of the current GET /orders payload; optional so the UI can show a deposit
   * and refund hint as soon as the backend starts including them.
   */
  depositAmount?: number
  refundedAmount?: number
  /** Issued when the order is confirmed — one contract per order. */
  contractNumber?: string | null
  termsAcceptedAtUtc?: string | null
  items: OrderItem[]
}

export interface ApiError {
  error: {
    code: string
    message: string
    errors?: Record<string, string[]>
    traceId?: string
  }
}
