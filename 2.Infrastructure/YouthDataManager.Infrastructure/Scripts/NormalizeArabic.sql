-- Optional: SQL Server function for ad-hoc Arabic normalization.
-- Use for backfilling or direct DB queries. Application search uses pre-computed NormalizedFullName column.
-- Run this script manually against your database if you need the function.

IF OBJECT_ID('dbo.NormalizeArabic', 'FN') IS NOT NULL
    DROP FUNCTION dbo.NormalizeArabic;
GO

CREATE FUNCTION dbo.NormalizeArabic(@input NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    IF @input IS NULL OR LEN(LTRIM(RTRIM(@input))) = 0
        RETURN N'';

    -- Normalize alef variants to ا (U+0627)
    SET @input = REPLACE(@input, N'آ', N'ا');
    SET @input = REPLACE(@input, N'أ', N'ا');
    SET @input = REPLACE(@input, N'إ', N'ا');
    SET @input = REPLACE(@input, N'ٱ', N'ا');

    -- Normalize ya: ى (alef maksura) and ی (Farsi ye) to ي (U+064A)
    SET @input = REPLACE(@input, N'ى', N'ي');
    SET @input = REPLACE(@input, N'ی', N'ي');

    -- Ta marbuta ة to ه
    SET @input = REPLACE(@input, N'ة', N'ه');

    -- Farsi kaf ک to Arabic kaf ك
    SET @input = REPLACE(@input, N'ک', N'ك');

    -- Remove tatweel (kashida) U+0640
    SET @input = REPLACE(@input, NCHAR(0x0640), N'');

    -- Remove tashkeel (diacritics) U+064B to U+065F and U+0670
    SET @input = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@input,
        NCHAR(0x064B), N''), NCHAR(0x064C), N''), NCHAR(0x064D), N''), NCHAR(0x064E), N''), NCHAR(0x064F), N''),
        NCHAR(0x0650), N''), NCHAR(0x0651), N''), NCHAR(0x0652), N''), NCHAR(0x0653), N''), NCHAR(0x0654), N''),
        NCHAR(0x0655), N''), NCHAR(0x0656), N''), NCHAR(0x0657), N''), NCHAR(0x0658), N''), NCHAR(0x0659), N''),
        NCHAR(0x065A), N''), NCHAR(0x065B), N''), NCHAR(0x065C), N''), NCHAR(0x065D), N''), NCHAR(0x065E), N''),
        NCHAR(0x065F), N''), NCHAR(0x0670), N'');

    RETURN LTRIM(RTRIM(@input));
END;
GO
