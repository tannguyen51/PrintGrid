import assert from 'node:assert/strict'
import { test } from 'node:test'
import { setImmediate } from 'node:timers/promises'
import { startHubConnection } from '../src/shared/api/hubLifecycle.ts'

test('StrictMode discarded setup never starts a negotiation', async () => {
  const calls = []
  const connection = { start: async () => { calls.push('start') }, stop: async () => { calls.push('stop') } }
  const dispose = startHubConnection(connection, () => calls.push('connected'), () => calls.push('error'))
  dispose()
  await setImmediate()
  assert.deepEqual(calls, [])
})

test('cleanup waits for negotiation and never updates a disposed component', async () => {
  const calls = []
  let finishNegotiation
  const negotiation = new Promise(resolve => { finishNegotiation = resolve })
  const connection = {
    start: async () => { calls.push('start'); await negotiation },
    stop: async () => { calls.push('stop') },
  }
  const dispose = startHubConnection(connection, () => calls.push('connected'), () => calls.push('error'))
  await Promise.resolve()
  dispose()
  dispose()
  assert.deepEqual(calls, ['start'])
  finishNegotiation()
  await setImmediate()
  assert.deepEqual(calls, ['start', 'stop'])
})

test('a live connection reports success and stops once', async () => {
  const calls = []
  const connection = { start: async () => { calls.push('start') }, stop: async () => { calls.push('stop') } }
  const dispose = startHubConnection(connection, () => calls.push('connected'), () => calls.push('error'))
  await setImmediate()
  assert.deepEqual(calls, ['start', 'connected'])
  dispose()
  await setImmediate()
  assert.deepEqual(calls, ['start', 'connected', 'stop'])
})

test('real start failures remain visible to the active component', async () => {
  const failure = new Error('Server unavailable')
  const errors = []
  const connection = { start: async () => { throw failure }, stop: async () => {} }
  const dispose = startHubConnection(connection, () => assert.fail('Connection failed'), error => errors.push(error))
  await setImmediate()
  assert.deepEqual(errors, [failure])
  dispose()
  await setImmediate()
})
