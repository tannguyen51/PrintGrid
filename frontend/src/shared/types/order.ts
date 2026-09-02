export type OrderStatus =
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
  createdAt: string
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
