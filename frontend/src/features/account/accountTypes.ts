export interface CustomerProfile {
  id: string
  email: string
  fullName: string
  phoneNumber?: string | null
  isEmailVerified: boolean
}

export interface CustomerAddress {
  id: string
  label: string
  recipientName: string
  phoneNumber: string
  street: string
  ward: string
  district: string
  city: string
  postalCode: string
  country: string
  isDefault: boolean
}

export type AddressInput = Omit<CustomerAddress, 'id'>
