import { useEffect, useState } from 'react'
import { apiClient } from '../../shared/api/apiClient'

interface FileUrlState {
  /** Which model this result belongs to — a mismatch means "still loading". */
  modelId: string | null
  url: string | null
  failed: boolean
}

/**
 * Fetches a model's stored file as an object URL the 3D viewer can load.
 *
 * Deliberately hand-rolled instead of TanStack Query: an object URL has to be
 * revoked exactly when the viewer goes away, and a query cache would keep handing
 * out dead URLs to the next component that mounts. The file is streamed with the
 * auth header, which a plain loader fetch could not do.
 */
export function useModelFileUrl(modelId: string | null, enabled: boolean) {
  const [result, setResult] = useState<FileUrlState>({ modelId: null, url: null, failed: false })

  useEffect(() => {
    if (!enabled || !modelId) return

    let cancelled = false
    let objectUrl: string | null = null

    apiClient
      .get(`/models/${modelId}/file`, { responseType: 'blob' })
      .then((response) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(response.data as Blob)
        setResult({ modelId, url: objectUrl, failed: false })
      })
      .catch(() => {
        if (!cancelled) setResult({ modelId, url: null, failed: true })
      })

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [modelId, enabled])

  // Derived during render: a result that belongs to a previous model reads as
  // "loading", so switching models never flashes the last one's preview.
  const current = result.modelId === modelId ? result : { url: null, failed: false }
  return { url: current.url, failed: current.failed }
}