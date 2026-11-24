import { parseBambu } from './parseBambu'

self.onmessage = (event: MessageEvent<ArrayBuffer>) => {
  try {
    console.info('[preview3d] 3MF worker parsing', { bytes: event.data.byteLength })
    const preview = parseBambu(new Uint8Array(event.data))
    console.info('[preview3d] 3MF worker complete', { triangles: preview?.triangleCount, plates: preview?.plates })
    // Standard materials/textures continue through ThreeMFLoader without refetching.
    if (!preview) self.postMessage({ standard: event.data }, { transfer: [event.data] })
    else self.postMessage({ preview }, { transfer: preview.meshes.flatMap(m => [m.positions.buffer, m.normals.buffer]) })
  } catch (error) {
    self.postMessage({ error: error instanceof Error ? error.message : 'Không thể đọc file 3MF.' })
  }
}
