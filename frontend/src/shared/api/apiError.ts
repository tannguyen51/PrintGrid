import { isAxiosError } from 'axios'

/**
 * Chuẩn hoá lỗi từ API PrintGrid thành thông tin đọc được được.
 * Backend trả về 2 dạng body:
 *   - envelope riêng:  { error: { code, message, errors?, traceId? } }   (AuthController, ExceptionHandlingMiddleware)
 *   - ProblemDetails:  { type, title, detail, status, errors?, traceId } ([ApiController] tự động, 400 model binding)
 */
export interface ApiErrorInfo {
  status?: number
  code?: string
  message?: string
  traceId?: string
  fieldErrors?: Record<string, string[]>
}

function normalizeFieldErrors(raw: unknown): Record<string, string[]> | undefined {
  if (!raw || typeof raw !== 'object') return undefined
  const out: Record<string, string[]> = {}
  for (const [key, value] of Object.entries(raw as Record<string, unknown>)) {
    const messages = Array.isArray(value)
      ? value.filter((v): v is string => typeof v === 'string')
      : typeof value === 'string'
        ? [value]
        : []
    if (messages.length > 0) out[key] = messages
  }
  return Object.keys(out).length > 0 ? out : undefined
}

export function getApiErrorInfo(error: unknown): ApiErrorInfo {
  if (!isAxiosError(error)) {
    return { message: error instanceof Error ? error.message : String(error) }
  }

  const info: ApiErrorInfo = { status: error.response?.status }

  // Không có response => không tới được backend (server chưa chạy, sai baseURL, DNS, timeout…).
  if (!error.response) {
    info.code = error.code === 'ECONNABORTED' ? 'timeout' : 'network_error'
    info.message = error.message
    return info
  }

  const data = error.response.data
  if (data && typeof data === 'object' && !(data instanceof Blob)) {
    const body = data as Record<string, unknown>
    const envelope = body.error
    if (envelope && typeof envelope === 'object') {
      const e = envelope as Record<string, unknown>
      if (typeof e.code === 'string') info.code = e.code
      if (typeof e.message === 'string') info.message = e.message
      if (typeof e.traceId === 'string') info.traceId = e.traceId
      info.fieldErrors = normalizeFieldErrors(e.errors)
    } else {
      // ProblemDetails: detail là câu mô tả đầy đủ hơn title.
      if (typeof body.detail === 'string') info.message = body.detail
      else if (typeof body.title === 'string') info.message = body.title
      if (typeof body.code === 'string') info.code = body.code
      if (typeof body.traceId === 'string') info.traceId = body.traceId
      info.fieldErrors = normalizeFieldErrors(body.errors)
    }
  } else if (typeof data === 'string' && data) {
    info.message = data
  }

  if (!info.message && error.response.statusText) info.message = error.response.statusText
  if (!info.message) info.message = error.message
  return info
}

/** Mã lỗi backend → thông tiếng Việt gọn, không cần xem body. */
const FRIENDLY: Record<string, string> = {
  email_exists: 'Email này đã được đăng ký. Hãy dùng email khác hoặc đăng nhập.',
  forbidden: 'Bạn không có quyền thực hiện thao tác này.',
  not_found: 'Không tìm thấy dữ liệu yêu cầu.',
  rate_limit_exceeded: 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.',
  cooldown_active: 'Vừa gửi email xác thực, vui lòng đợi vài phút trước khi gửi lại.',
  // Upload model (ModelsController + ExceptionHandlingMiddleware)
  file_too_large: 'File vượt giới hạn 50 MB. Hãy giảm kích thước model rồi thử lại.',
  unsupported_format: 'Định dạng không được hỗ trợ. Chỉ nhận STL, OBJ, 3MF hoặc GLB.',
  invalid_file_content: 'Nội dung file không hợp lệ — có thể file bị hỏng hoặc sai định dạng.',
  quota_exceeded: 'Đã đạt giới hạn lưu trữ. Xoá bớt model không dùng rồi thử lại.',
  file_not_stored: 'Model này chưa có file lưu trữ để tải.',
}

/**
 * Diễn giải lỗi API thành một câu để hiển thị trực tiếp cho người dùng.
 * Câu trả về chỉ nói điều người dùng cần biết — chẩn đoán kỹ thuật (HTTP status,
 * error code, traceId) được ghi ra console để dev đối chiếu, không nhét vào màn hình.
 */
export function getApiErrorMessage(error: unknown, fallback = 'Có lỗi xảy ra, vui lòng thử lại.'): string {
  const info = getApiErrorInfo(error)
  const { status, code } = info

  if (code === 'network_error') {
    return 'Không kết nối được máy chủ. Kiểm tra backend API có đang chạy và địa chỉ VITE_API_BASE_URL.'
  }
  if (code === 'timeout') {
    return 'Máy chủ phản hồi quá chậm (timeout). Vui lòng thử lại.'
  }

  let base = code ? FRIENDLY[code] : undefined
  if (!base) {
    if (code === 'unauthorized' || status === 401) {
      // Login: backend cố ý trả lời chung chung "Invalid email or password".
      base = status === 401 && code === 'unauthorized'
        ? 'Email hoặc mật khẩu không đúng.'
        : 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.'
    } else if (code === 'validation_error' || code === 'invalid_file_content') {
      base = info.fieldErrors
        ? `Dữ liệu chưa hợp lệ: ${Object.values(info.fieldErrors).flat().join(' ')}`
        : (info.message || 'Dữ liệu chưa hợp lệ.')
    } else if (code === 'conflict') {
      base = info.message || 'Dữ liệu bị trùng với tài khoản/hồ sơ đã tồn tại.'
    } else if (status === 403) {
      base = 'Bạn không có quyền truy cập (403).'
    } else if (status === 429) {
      base = 'Bạn gửi yêu cầu quá nhiều lần, thử lại sau ít phút.'
    } else if (status && status >= 500) {
      base = `Máy chủ gặp lỗi (${status}).`
    } else {
      base = info.message || fallback
    }
  }

  if (status || code || info.traceId) {
    console.warn('[api]', { status, code, traceId: info.traceId, message: info.message })
  }
  return base
}

/**
 * Map fieldErrors từ backend (FluentValidation dùng "Email", "PhoneNumber";
 * model binding dùng "$.Email") về tên field react-hook-form chữ thường.
 */
export function normalizeFieldKey(key: string): string {
  const cleaned = key.replace(/^\$?\./, '').replace(/^\w/, (c) => c.toLowerCase())
  return cleaned
}
