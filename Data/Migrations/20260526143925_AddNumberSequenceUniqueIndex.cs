using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberSequenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NumberSequences_TenantId",
                table: "NumberSequences");

            migrationBuilder.Sql(@"
CREATE TABLE #NumberSequenceBackfill
(
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Prefix] nvarchar(30) NOT NULL,
    [CurrentNumber] int NOT NULL,
    [PaddingLength] int NOT NULL,
    [Year] int NOT NULL
);

INSERT INTO #NumberSequenceBackfill ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT
    src.[TenantId],
    N'OP_REQ',
    CONVERT(nvarchar(30), CONCAT(N'OPR-', src.[SequenceYear], N'-')),
    src.[CurrentNumber],
    3,
    src.[SequenceYear]
FROM
(
    SELECT
        [TenantId],
        TRY_CONVERT(int, PARSENAME(REPLACE([RequestNo], N'-', N'.'), 2)) AS [SequenceYear],
        MAX(TRY_CONVERT(int, PARSENAME(REPLACE([RequestNo], N'-', N'.'), 1))) AS [CurrentNumber]
    FROM [OperationRequests]
    WHERE [RequestNo] LIKE N'OPR-[0-9][0-9][0-9][0-9]-%'
      AND TRY_CONVERT(int, PARSENAME(REPLACE([RequestNo], N'-', N'.'), 2)) IS NOT NULL
      AND TRY_CONVERT(int, PARSENAME(REPLACE([RequestNo], N'-', N'.'), 1)) IS NOT NULL
    GROUP BY [TenantId], TRY_CONVERT(int, PARSENAME(REPLACE([RequestNo], N'-', N'.'), 2))
) src;

INSERT INTO #NumberSequenceBackfill ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT [TenantId], N'OP_PLAN', N'OPP-', MAX(TRY_CONVERT(int, SUBSTRING([Code], 5, 50))), 4, 0
FROM [OperationPlans]
WHERE [Code] LIKE N'OPP-%'
  AND TRY_CONVERT(int, SUBSTRING([Code], 5, 50)) IS NOT NULL
GROUP BY [TenantId];

INSERT INTO #NumberSequenceBackfill ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT [TenantId], N'EQUIP', N'EQ-', MAX(TRY_CONVERT(int, SUBSTRING([Code], 4, 50))), 4, 0
FROM [Equipments]
WHERE [Code] LIKE N'EQ-%'
  AND TRY_CONVERT(int, SUBSTRING([Code], 4, 50)) IS NOT NULL
GROUP BY [TenantId];

INSERT INTO #NumberSequenceBackfill ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT [TenantId], N'WSPACE', N'WS-', MAX(TRY_CONVERT(int, SUBSTRING([Code], 4, 50))), 3, 0
FROM [Workspaces]
WHERE [Code] LIKE N'WS-%'
  AND TRY_CONVERT(int, SUBSTRING([Code], 4, 50)) IS NOT NULL
GROUP BY [TenantId];

INSERT INTO #NumberSequenceBackfill ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT [TenantId], N'SP_PART', N'SP-', MAX(TRY_CONVERT(int, SUBSTRING([Code], 4, 50))), 4, 0
FROM [SpareParts]
WHERE [Code] LIKE N'SP-%'
  AND TRY_CONVERT(int, SUBSTRING([Code], 4, 50)) IS NOT NULL
GROUP BY [TenantId];

CREATE TABLE #NumberSequenceBackfillAgg
(
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Prefix] nvarchar(30) NOT NULL,
    [CurrentNumber] int NOT NULL,
    [PaddingLength] int NOT NULL,
    [Year] int NOT NULL
);

INSERT INTO #NumberSequenceBackfillAgg ([TenantId], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year])
SELECT [TenantId], [Code], [Prefix], MAX([CurrentNumber]), [PaddingLength], [Year]
FROM #NumberSequenceBackfill
WHERE [CurrentNumber] > 0
GROUP BY [TenantId], [Code], [Prefix], [PaddingLength], [Year];

UPDATE ns
SET
    [Prefix] = src.[Prefix],
    [PaddingLength] = src.[PaddingLength],
    [CurrentNumber] = CASE WHEN ns.[CurrentNumber] < src.[CurrentNumber] THEN src.[CurrentNumber] ELSE ns.[CurrentNumber] END,
    [UpdatedAt] = SYSDATETIMEOFFSET()
FROM [NumberSequences] ns
INNER JOIN #NumberSequenceBackfillAgg src
    ON src.[TenantId] = ns.[TenantId]
   AND src.[Code] = ns.[Code]
   AND src.[Year] = ns.[Year]
WHERE ns.[IsDeleted] = CAST(0 AS bit);

INSERT INTO [NumberSequences]
    ([Id], [Code], [Prefix], [CurrentNumber], [PaddingLength], [Year], [CreatedAt], [CreatedByUserId], [UpdatedAt], [UpdatedByUserId], [IsDeleted], [TenantId])
SELECT
    NEWID(),
    src.[Code],
    src.[Prefix],
    src.[CurrentNumber],
    src.[PaddingLength],
    src.[Year],
    SYSDATETIMEOFFSET(),
    NULL,
    NULL,
    NULL,
    CAST(0 AS bit),
    src.[TenantId]
FROM #NumberSequenceBackfillAgg src
WHERE NOT EXISTS
(
    SELECT 1
    FROM [NumberSequences] ns
    WHERE ns.[TenantId] = src.[TenantId]
      AND ns.[Code] = src.[Code]
      AND ns.[Year] = src.[Year]
      AND ns.[IsDeleted] = CAST(0 AS bit)
);

WITH Duplicates AS
(
    SELECT
        [Id],
        ROW_NUMBER() OVER
        (
            PARTITION BY [TenantId], [Code], [Year]
            ORDER BY [CurrentNumber] DESC, [CreatedAt] DESC, [Id] DESC
        ) AS [RowNumber]
    FROM [NumberSequences]
    WHERE [IsDeleted] = CAST(0 AS bit)
)
UPDATE ns
SET [IsDeleted] = CAST(1 AS bit), [UpdatedAt] = SYSDATETIMEOFFSET()
FROM [NumberSequences] ns
INNER JOIN Duplicates d ON d.[Id] = ns.[Id]
WHERE d.[RowNumber] > 1;

DROP TABLE #NumberSequenceBackfillAgg;
DROP TABLE #NumberSequenceBackfill;
");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId_Code_Year",
                table: "NumberSequences",
                columns: new[] { "TenantId", "Code", "Year" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NumberSequences_TenantId_Code_Year",
                table: "NumberSequences");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_TenantId",
                table: "NumberSequences",
                column: "TenantId");
        }
    }
}
