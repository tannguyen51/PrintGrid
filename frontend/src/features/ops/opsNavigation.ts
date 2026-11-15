export const OPS_GROUPS = [
  { label: 'Điều hành mạng lưới', items: [
    { label: 'Trung tâm điều phối', href: '/scheduling', icon: 'dashboard' },
    { label: 'Cảnh báo & can thiệp', href: '/ops/escalations', icon: 'warning' },
    { label: 'Xưởng & máy in', href: '/ops/labs', icon: 'factory' },
  ] },
  { label: 'Phê duyệt & chất lượng', items: [
    { label: 'Duyệt báo giá', href: '/quote-reviews', icon: 'quote' },
    { label: 'Kiểm duyệt model', href: '/model-reviews', icon: 'model' },
    { label: 'Nghiệm thu QC', href: '/staff/qc-proofs', icon: 'qc' },
    { label: 'Yêu cầu thiết kế', href: '/ops/design-requests', icon: 'design' },
  ] },
  { label: 'Đối soát & báo cáo', items: [
    { label: 'Thanh toán & đặt cọc', href: '/ops/payments', icon: 'payment' },
    { label: 'Xử lý hoàn tiền', href: '/ops/refunds', icon: 'refund' },
    { label: 'Hiệu suất & SLA', href: '/ops/sla', icon: 'chart' },
    { label: 'Nhật ký quyết định', href: '/ops/decisions', icon: 'history' },
  ] },
] as const

export const OPS_SECTIONS = OPS_GROUPS.flatMap((group) => [...group.items])
export const isOpsWorkspacePath = (pathname: string) => OPS_SECTIONS.some((section) =>
  pathname === section.href || pathname.startsWith(`${section.href}/`),
)
