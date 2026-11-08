import { useEffect, useState } from 'react'
import { apiClient } from '../../shared/api/apiClient'

interface FileUrlState {
  modelId: string | null
  url: string | null
  failed: boolean
}

/**
 * Fetches a 3D model's stored file as an object URL the viewer can load.
 * Default path prefix is '/models', but can be overridden (e.g. '/model-reviews').
 */
export function useModelFileUrl(modelId: string | null, enabled: boolean, filePathBase: string = '/models') {
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
      .get(`${filePathBase}/${modelId}/file`, { responseType: 'blob' })
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
  }, [modelId, enabled, filePathBase])

  const current = result.modelId === modelId ? result : { url: null, failed: false }
  return { url: current.url, failed: current.failed }
}
