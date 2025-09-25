-- BestLogViewer SQLite schema (normalized)
PRAGMA foreign_keys = ON;

-- Single-row application settings
CREATE TABLE IF NOT EXISTS Settings (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    WholeWordOnly INTEGER NOT NULL DEFAULT 0,   -- 0/1
    IgnoreCaseLegacy INTEGER NOT NULL DEFAULT 1, -- legacy global flag
    BackgroundColor TEXT NOT NULL,             -- e.g., #111111
    DefaultTextColor TEXT NOT NULL             -- e.g., #DDDDDD
);

INSERT INTO Settings(Id, WholeWordOnly, IgnoreCaseLegacy, BackgroundColor, DefaultTextColor)
    SELECT 1, 0, 1, '#111111', '#DDDDDD'
    WHERE NOT EXISTS(SELECT 1 FROM Settings WHERE Id = 1);

-- Per-keyword highlighting rules
CREATE TABLE IF NOT EXISTS KeywordRule (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Keyword TEXT NOT NULL,
    ColorHex TEXT NOT NULL,
    Scope INTEGER NOT NULL,                    -- 0=Word, 1=Line
    IgnoreCase INTEGER NOT NULL DEFAULT 1,     -- 0/1
    DisplayOrder INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_KeywordRule_Keyword ON KeywordRule(Keyword);

-- Palette of colors shown in UI
CREATE TABLE IF NOT EXISTS PaletteColor (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ColorHex TEXT NOT NULL,
    Position INTEGER NOT NULL DEFAULT 0
);

-- Converted files history
CREATE TABLE IF NOT EXISTS ConversionRecord (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OriginalPath TEXT NOT NULL,
    OutputPath TEXT NOT NULL,
    ConvertedAt TEXT NOT NULL                   -- ISO-8601 string
);
CREATE INDEX IF NOT EXISTS IX_ConversionRecord_ConvertedAt ON ConversionRecord(ConvertedAt);
