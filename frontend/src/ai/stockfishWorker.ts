export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  // Rename the files back to their original names in the public/ folder.
  // We use the hash #,worker to force worker mode and let the script find stockfish.wasm by itself.
  const scriptUrl = `${origin}/engine/stockfish.js`;
  const workerUrl = `${scriptUrl}#,worker`;

  console.log("Creating Stockfish worker at:", workerUrl);

  return new Worker(workerUrl);
}
