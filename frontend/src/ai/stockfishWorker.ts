export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  // Use the ASM.js version which doesn't need WASM.
  // This is a diagnostic step to see if pure JS workers function correctly.
  const scriptUrl = `${origin}/engine/stockfish-asm.js`;
  const workerUrl = `${scriptUrl}#,worker`;

  console.log("Creating Stockfish worker (ASM.js) at:", workerUrl);

  return new Worker(workerUrl);
}
