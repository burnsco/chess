export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  const scriptUrl = `${origin}/engine/stockfish-18-lite-single.js`;
  const wasmUrl = `${origin}/engine/stockfish-18-lite-single.wasm`;

  // Format: script.js#wasm_url,worker
  // Use explicit full URLs to avoid any relative path ambiguity in different environments.
  const workerUrl = `${scriptUrl}#${encodeURIComponent(wasmUrl)},worker`;

  console.log("Creating Stockfish worker at:", workerUrl);

  return new Worker(workerUrl);
}
