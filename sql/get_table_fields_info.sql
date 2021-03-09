SELECT
	ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,
	ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,
	ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,
	ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,
	CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE
FROM
	INFORMATION_SCHEMA.COLUMNS
WHERE
	TABLE_NAME = '_Reference60'
ORDER BY
	ORDINAL_POSITION ASC;