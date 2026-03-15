export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  // Use the generic paths from the public/ folder to bypass ad-blockers and name-based filters.
  const engineScriptHref = `${origin}/engine/engine.js`;
  const engineWasmHref = `${origin}/engine/engine.wasm`;

  // Stockfish 18 uses the hash to find the WASM location in worker mode.
  const workerUrl = `${engineScriptHref}#${encodeURIComponent(engineWasmHref)},worker`;

  return new Worker(workerUrl, { name: "chess-engine" });
}
