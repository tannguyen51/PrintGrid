import { BufferAttribute, BufferGeometry, DoubleSide, FileLoader, Group, Loader, Mesh, MeshStandardMaterial } from 'three'
import { ThreeMFLoader } from 'three/examples/jsm/loaders/3MFLoader.js'
import type { BambuPreview } from './parseBambu'

export class ThreeMfPreviewLoader extends Loader<Group> {
  load(url: string, onLoad: (group: Group) => void, onProgress?: (event: ProgressEvent) => void, onError?: (error: unknown) => void) {
    const loader = new FileLoader(this.manager).setResponseType('arraybuffer').setRequestHeader(this.requestHeader).setWithCredentials(this.withCredentials)
    loader.setPath(this.path)
    loader.load(url, data => {
      const worker = new Worker(new URL('./preview.worker.ts', import.meta.url), { type: 'module' })
      // Keep drei's loading indicator active until parsing finishes, not just downloading.
      const task = `${url}#parse3mf`
      this.manager.itemStart(task)
      const finish = () => { worker.terminate(); this.manager.itemEnd(task) }
      worker.onerror = event => { finish(); this.manager.itemError(task); onError?.(new Error(event.message)) }
      worker.onmessage = (event: MessageEvent<{ preview?: BambuPreview; standard?: ArrayBuffer; error?: string }>) => {
        try {
          if (event.data.error) throw new Error(event.data.error)
          if (event.data.standard) { onLoad(new ThreeMFLoader(this.manager).parse(event.data.standard)); return }
          const preview = event.data.preview!
          const group = new Group()
          for (const part of preview.meshes) {
            const geometry = new BufferGeometry()
            geometry.setAttribute('position', new BufferAttribute(part.positions, 3))
            geometry.setAttribute('normal', new BufferAttribute(part.normals, 3))
            const material = new MeshStandardMaterial({ color: part.color, roughness: .7, metalness: 0, side: DoubleSide })
            const mesh = new Mesh(geometry, material)
            mesh.userData = { slot: part.slot, plate: part.plate }
            group.add(mesh)
          }
          group.rotation.x = -Math.PI / 2
          group.userData = { palette: preview.palette, plates: preview.plates, triangleCount: preview.triangleCount, surfacePainting: preview.surfacePainting }
          onLoad(group)
        } catch (error) { this.manager.itemError(task); onError?.(error) }
        finally { finish() }
      }
      worker.postMessage(data, [data as ArrayBuffer])
    }, onProgress, onError)
  }
}
