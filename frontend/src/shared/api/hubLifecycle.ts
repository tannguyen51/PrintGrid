import type { HubConnection } from '@microsoft/signalr'

/** Starts an effect-owned connection and returns its cleanup function. */
export function startHubConnection(
  connection: Pick<HubConnection, 'start' | 'stop'>,
  onConnected: () => void,
  onError: (error: unknown) => void,
): () => void {
  let disposed = false
  let startAttempted = false

  // StrictMode runs setup/cleanup/setup synchronously. Skip its discarded setup
  // before it opens a request, and let an in-flight negotiation settle before stop.
  const started = Promise.resolve().then(async () => {
    if (disposed) return
    startAttempted = true
    try {
      await connection.start()
      if (!disposed) onConnected()
    } catch (error) {
      if (!disposed) onError(error)
    }
  })

  return () => {
    if (disposed) return
    disposed = true
    void started
      .then(() => startAttempted ? connection.stop() : undefined)
      .catch((error: unknown) => console.error('Unable to stop the hub connection.', error))
  }
}
