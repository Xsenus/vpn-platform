import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')

const servers = [
  {
    name: 'public-web',
    appDir: 'apps/public-web',
    port: '5293'
  },
  {
    name: 'cabinet',
    appDir: 'apps/cabinet',
    port: '5294'
  }
]

const children = servers.map((server) => {
  const viteCli = resolve(root, server.appDir, 'node_modules/vite/bin/vite.js')
  const child = spawn(
    process.execPath,
    [viteCli, server.appDir, '--host', '127.0.0.1', '--port', server.port, '--strictPort'],
    {
      cwd: root,
      env: {
        ...process.env,
        VITE_API_BASE_URL: 'http://127.0.0.1:19080',
        VITE_PUBLIC_WEB_URL: 'http://127.0.0.1:5293'
      },
      stdio: ['ignore', 'pipe', 'pipe']
    }
  )

  child.stdout.on('data', (chunk) => process.stdout.write(`[${server.name}] ${chunk}`))
  child.stderr.on('data', (chunk) => process.stderr.write(`[${server.name}] ${chunk}`))
  child.on('exit', (code, signal) => {
    if (shuttingDown) return
    console.error(`[${server.name}] exited with code=${code ?? 'null'} signal=${signal ?? 'null'}`)
    shutdown(code ?? 1)
  })

  return child
})

let shuttingDown = false

function shutdown(exitCode = 0) {
  if (shuttingDown) return
  shuttingDown = true

  for (const child of children) {
    if (!child.killed) child.kill('SIGTERM')
  }

  setTimeout(() => process.exit(exitCode), 250).unref()
}

process.on('SIGINT', () => shutdown(0))
process.on('SIGTERM', () => shutdown(0))
