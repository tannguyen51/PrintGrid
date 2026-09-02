import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { getAccessToken } from './tokenStore'

const hubBaseUrl = import.meta.env.VITE_HUB_BASE_URL ?? ''

const connections = new Map<string, HubConnection>()

export function getHubConnection(path: string): HubConnection {
  const existing = connections.get(path)
  if (existing) return existing

  const connection = new HubConnectionBuilder()
    .withUrl(`${hubBaseUrl}${path}`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connections.set(path, connection)
  return connection
}

export async function ensureStarted(connection: HubConnection): Promise<void> {
  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start()
  }
}
