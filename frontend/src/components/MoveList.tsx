import { useEffect, useRef } from "react";
import type { MoveRecord } from "../engine/types";

interface MoveListProps {
  moves: MoveRecord[];
}

export function MoveList({ moves }: MoveListProps) {
  const listRef = useRef<HTMLOListElement>(null);

  // Auto-scroll to the latest move
  useEffect(() => {
    const el = listRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [moves.length]);

  const rows: Array<{ moveNumber: number; white?: MoveRecord; black?: MoveRecord }> = [];
  for (let i = 0; i < moves.length; i += 2) {
    rows.push({ moveNumber: i / 2 + 1, white: moves[i], black: moves[i + 1] });
  }

  const latestRowIndex = rows.length - 1;

  return (
    <div className="card move-list-panel">
      <p className="card-label">Moves</p>
      {rows.length === 0 ? (
        <p className="move-empty-state">No moves yet</p>
      ) : (
        <ol className="move-list" ref={listRef} aria-label="Move history">
          {rows.map((row, idx) => (
            <li
              key={row.moveNumber}
              className={`move-row${idx === latestRowIndex ? " move-row--latest" : ""}`}
            >
              <span className="move-number">{row.moveNumber}.</span>
              <span className="move-cell">{row.white?.san ?? ""}</span>
              <span className="move-cell">{row.black?.san ?? ""}</span>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}
