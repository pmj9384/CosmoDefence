using System;
using System.Collections.Generic;

// 웨이브 행 패턴 (순수 C#) — CSV 한 줄 = 필드 한 행, 먼저 나온 행이 먼저(아래에) 스폰된다.
// 칸 코드: '.'=빈칸  '1'..'9'=1×1 타입 인덱스  'D'=세로 블록 앵커(1×2, Drifter — 다음 행 같은 칸이 '-')
//          'B'=가로 블록 앵커(2×1, Bulwark — 같은 행 오른쪽 칸이 '-')  '-'=멀티셀 점유 예약(스폰 없음)
// 원작 관찰(2026-07-07): 빈 행/빈 칸도 패턴의 일부, 매판 동일(결정적).
public readonly struct RowCell
{
    public const int Empty = -1;
    public const int DrifterAnchor = 100;    // 타입 인덱스는 소비자(MonsterManager)가 매핑
    public const int BulwarkAnchor = 101;

    public readonly int code;
    public RowCell(int code) => this.code = code;
    public bool IsUnit => code != Empty;
}

public class StagePattern
{
    public IReadOnlyList<RowCell[]> Rows => rows;
    public int TotalUnits { get; }   // 진행도 분모 — 멀티셀도 1유닛

    private readonly List<RowCell[]> rows;

    public StagePattern(List<RowCell[]> rows)
    {
        this.rows = rows;
        foreach (RowCell[] row in rows)
            foreach (RowCell cell in row)
                if (cell.IsUnit) TotalUnits++;
    }

    // CSV: 행당 "1.2.D.B-." 같은 코드 문자열 (#으로 주석). 엄격 검증 — 멀티셀 점유 정합 깨지면 즉시 예외
    public static StagePattern Parse(string csv, int columns)
    {
        var rows = new List<RowCell[]>();
        var pendingAbove = new HashSet<int>();   // 직전 행 세로 블록(D) 앵커 열 — 이번 행 같은 열은 '-'여야 함

        foreach (string raw in csv.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (line.Length != columns)
                throw new FormatException($"행 길이 {line.Length} ≠ 열 수 {columns}: \"{line}\"");

            var row = new RowCell[columns];
            var deerCols = new HashSet<int>();
            for (int c = 0; c < columns; c++)
            {
                char ch = line[c];
                bool mustBeOccupied = pendingAbove.Contains(c);
                if (mustBeOccupied && ch != '-')
                    throw new FormatException($"D(세로 1×2) 위 칸이 비점유: 열 {c}, \"{line}\"");

                switch (ch)
                {
                    case '.': row[c] = new RowCell(RowCell.Empty); break;
                    case '-':
                        if (!mustBeOccupied && (c == 0 || line[c - 1] != 'B'))
                            throw new FormatException($"고아 점유 칸 '-': 열 {c}, \"{line}\"");
                        row[c] = new RowCell(RowCell.Empty);   // 점유 칸은 스폰 없음
                        break;
                    case 'D':
                        row[c] = new RowCell(RowCell.DrifterAnchor);
                        deerCols.Add(c);
                        break;
                    case 'B':
                        if (c == columns - 1 || line[c + 1] != '-')
                            throw new FormatException($"B(가로 2×1) 오른쪽 칸이 '-'가 아님: 열 {c}, \"{line}\"");
                        row[c] = new RowCell(RowCell.BulwarkAnchor);
                        break;
                    default:
                        if (ch < '1' || ch > '9')
                            throw new FormatException($"알 수 없는 칸 코드 '{ch}': \"{line}\"");
                        row[c] = new RowCell(ch - '1');
                        break;
                }
            }
            rows.Add(row);
            pendingAbove = deerCols;
        }

        if (pendingAbove.Count > 0)
            throw new FormatException("마지막 행의 D(세로 1×2) 위 칸 행이 없음");
        if (rows.Count == 0)
            throw new FormatException("행이 하나도 없음");
        return new StagePattern(rows);
    }
}
