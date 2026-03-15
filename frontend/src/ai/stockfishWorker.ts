import engineScriptUrl from "stockfish/bin/stockfish-18-lite-single.js?url";
import engineWasmUrl from "stockfish/bin/stockfish-18-lite-single.wasm?url";

export function createStockfishWorker(): Worker {
  const origin = globalThis.location.origin;
  const engineScriptHref = new URL(engineScriptUrl, origin).href;
  const engineWasmHref = new URL(engineWasmUrl, origin).href;
  const workerUrl = `${engineScriptHref}#${encodeURIComponent(engineWasmHref)},worker`;
  return new Worker(workerUrl, { name: "stockfish-engine" });
}
