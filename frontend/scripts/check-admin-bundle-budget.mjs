import { readdir, readFile } from 'node:fs/promises'
import { gzipSync } from 'node:zlib'
import { resolve } from 'node:path'
import { pathToFileURL } from 'node:url'

export const ADMIN_BUNDLE_BUDGET = Object.freeze({
  maxJavaScriptFiles: 5,
  maxChunkRawBytes: 360 * 1024,
  maxTotalRawBytes: 553 * 1024,
  maxTotalGzipBytes: 147 * 1024
})

export async function collectJavaScriptBundleStats(distDirectory) {
  const assetsDirectory = resolve(distDirectory, 'assets')
  const names = (await readdir(assetsDirectory))
    .filter((name) => name.endsWith('.js'))
    .sort()
  if (names.length === 0) throw new Error(`No JavaScript assets found in ${assetsDirectory}`)

  const files = await Promise.all(names.map(async (name) => {
    const content = await readFile(resolve(assetsDirectory, name))
    return { name, rawBytes: content.byteLength, gzipBytes: gzipSync(content).byteLength }
  }))

  return {
    files,
    totalRawBytes: files.reduce((total, file) => total + file.rawBytes, 0),
    totalGzipBytes: files.reduce((total, file) => total + file.gzipBytes, 0),
    maxChunkRawBytes: Math.max(...files.map((file) => file.rawBytes))
  }
}

export function assertAdminBundleBudget(stats, budget = ADMIN_BUNDLE_BUDGET) {
  const failures = []
  if (stats.files.length > budget.maxJavaScriptFiles) failures.push(`JS files ${stats.files.length} > ${budget.maxJavaScriptFiles}`)
  if (stats.maxChunkRawBytes > budget.maxChunkRawBytes) failures.push(`max chunk ${stats.maxChunkRawBytes} > ${budget.maxChunkRawBytes} bytes`)
  if (stats.totalRawBytes > budget.maxTotalRawBytes) failures.push(`total raw ${stats.totalRawBytes} > ${budget.maxTotalRawBytes} bytes`)
  if (stats.totalGzipBytes > budget.maxTotalGzipBytes) failures.push(`total gzip ${stats.totalGzipBytes} > ${budget.maxTotalGzipBytes} bytes`)
  if (failures.length > 0) throw new Error(`Admin bundle budget exceeded: ${failures.join('; ')}`)
  return stats
}

async function main() {
  const stats = assertAdminBundleBudget(await collectJavaScriptBundleStats(resolve(process.cwd(), 'dist')))
  const files = stats.files.map((file) => `${file.name}:${file.rawBytes}/${file.gzipBytes}`).join(', ')
  console.log(`admin bundle budget ok files=${stats.files.length} raw=${stats.totalRawBytes} gzip=${stats.totalGzipBytes} max=${stats.maxChunkRawBytes} [${files}]`)
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exitCode = 1
  })
}
