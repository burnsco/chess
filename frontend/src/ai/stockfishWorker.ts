export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  // Use a full URL for the script to ensure it's loaded as expected.
  const scriptUrl = `${origin}/engine/engine.js`;

  // Format: #,worker
  // The Stockfish script parses the hash by splitting on ',':
  // (e = self.location.hash.substr(1).split(","))
  // It then looks at e[0] for the WASM URL, or defaults if it's empty:
  // (a = decodeURIComponent(e[0] || location.origin + location.pathname.replace(/\.js$/i, ".wasm")))
  // By using '#,worker', e[0] is "", and it correctly defaults to the .wasm file with the same name.
  const workerUrl = `${scriptUrl}#,worker`;

  console.log("Creating Stockfish worker at:", workerUrl);

  return new Worker(workerUrl);
}
