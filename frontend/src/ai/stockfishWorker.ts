export function createStockfishWorker(): Worker {
  // Use a relative URL which is often more reliable for finding assets in the same folder.
  // We add #,worker to ensure it starts in worker mode and finds stockfish.wasm.
  const workerUrl = new URL("/engine/stockfish.js#,worker", globalThis.location.origin);

  console.log("Creating Stockfish worker at:", workerUrl.href);

  return new Worker(workerUrl);
}
