import { z } from 'zod'

/** Form-facing schema. `tags` is a comma-separated text field; splitting happens in toModelInput. */
export const modelSchema = z.object({
  name: z.string().min(1, 'Tên model là bắt buộc').max(120, 'Tối đa 120 ký tự'),
  description: z.string().max(2000, 'Tối đa 2000 ký tự').optional().or(z.literal('')),
  fileName: z.string().min(1, 'Tên file là bắt buộc').max(255, 'Tối đa 255 ký tự'),
  fileFormat: z.string().min(1, 'Định dạng là bắt buộc').max(10),
  sizeBytes: z.number().min(0, 'Kích thước không âm'),
  tags: z.string().optional(),
})

export type ModelFormValues = z.infer<typeof modelSchema>

/** Convert the comma-separated tag field into the array the API expects. */
export function toModelInput(values: ModelFormValues): {
  name: string
  description: string | null
  fileName: string
  fileFormat: string
  sizeBytes: number
  tags: string[]
} {
  const tags = (values.tags ?? '')
    .split(',')
    .map((t) => t.trim())
    .filter((t) => t.length > 0)

  return {
    name: values.name.trim(),
    description: values.description?.trim() ? values.description.trim() : null,
    fileName: values.fileName.trim(),
    fileFormat: values.fileFormat.trim().toUpperCase(),
    sizeBytes: Number(values.sizeBytes),
    tags: [...new Set(tags)],
  }
}