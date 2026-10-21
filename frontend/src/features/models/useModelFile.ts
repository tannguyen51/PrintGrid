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
    if (!enabled || !modelId) {
      console.info('[preview3d] file fetch skipped', { modelId, enabled })
      return
    }

    let cancelled = false
    let objectUrl: string | null = null

    console.info('[preview3d] file fetch start', { modelId })
    apiClient
      .get(`/models/${modelId}/file`, { responseType: 'blob' })
      .then((response) => {
        if (cancelled) {
          console.info('[preview3d] file fetch resolved after unmount', { modelId })
          return
        }
        const blob = response.data as Blob
        objectUrl = URL.createObjectURL(blob)
        console.info('[preview3d] file fetch OK', {
          modelId,
          bytes: blob.size,
          mime: blob.type,
        })
        setResult({ modelId, url: objectUrl, failed: false })
      })
      .catch((error) => {
        console.error('[preview3d] file fetch FAILED', { modelId, error })
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