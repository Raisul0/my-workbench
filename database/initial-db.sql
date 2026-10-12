-- MyWorkbench initial SQL Server bootstrap
-- Run with:
--   sqlcmd -S localhost\MSSQLSERVER01 -U sa -P "%MY_WORKBENCH_SQL_PASSWORD%" -i database\initial-db.sql
-- This script creates the database, tables, and stored procedures required for the invoice module.

-- ==========================================================
-- DROP EXISTING DATABASE
-- WARNING: This permanently deletes the database and its data.
-- ==========================================================

USE master;
GO

IF DB_ID(N'MyWorkbench') IS NOT NULL
BEGIN
    ALTER DATABASE [MyWorkbench]
        SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE [MyWorkbench];
END
GO

IF DB_ID(N'MyWorkbench') IS NULL
BEGIN
    CREATE DATABASE [MyWorkbench];
END
GO

USE [MyWorkbench];
GO

IF OBJECT_ID(N'dbo.BusinessProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessProfiles (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        IssuerName NVARCHAR(200) NOT NULL,
        IssuerAddress NVARCHAR(MAX) NOT NULL,
        IssuerEmail NVARCHAR(200) NOT NULL,
        IssuerPhone NVARCHAR(50) NOT NULL,
        IssuerRegistrationNumber NVARCHAR(200) NOT NULL,
        IssuerTaxNumber NVARCHAR(200) NOT NULL,
        TaxLabel NVARCHAR(50) NOT NULL,
        DefaultCurrencyCode NVARCHAR(8) NOT NULL,
        InvoicePrefix NVARCHAR(32) NOT NULL,
        NextSequence INT NOT NULL,
        DefaultTaxRate DECIMAL(18,4) NOT NULL,
        DefaultTerms NVARCHAR(4000) NOT NULL,
        LineItemColumnsCsv NVARCHAR(256) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.BusinessProfiles', N'LineItemColumnsCsv') IS NULL
BEGIN
    ALTER TABLE dbo.BusinessProfiles
        ADD LineItemColumnsCsv NVARCHAR(256) NOT NULL
            CONSTRAINT DF_BusinessProfiles_LineItemColumnsCsv DEFAULT N'itemNo,date,client,project,description,hours,rate,total';
END
GO

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        BusinessProfileId UNIQUEIDENTIFIER NOT NULL,
        InvoiceNumber NVARCHAR(64) NOT NULL UNIQUE,
        Status INT NOT NULL,
        TemplateVersionId UNIQUEIDENTIFIER NOT NULL,
        IssueDate DATE NOT NULL,
        DueDate DATE NULL,
        CurrencyCode NVARCHAR(8) NOT NULL,
        IssuerName NVARCHAR(200) NOT NULL,
        IssuerAddress NVARCHAR(MAX) NOT NULL,
        IssuerEmail NVARCHAR(200) NOT NULL,
        IssuerPhone NVARCHAR(50) NOT NULL,
        IssuerRegistrationNumber NVARCHAR(200) NOT NULL,
        IssuerTaxNumber NVARCHAR(200) NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerAddress NVARCHAR(MAX) NOT NULL,
        CustomerEmail NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(50) NOT NULL,
        CustomerRegistrationNumber NVARCHAR(200) NOT NULL,
        CustomerTaxNumber NVARCHAR(200) NOT NULL,
        Notes NVARCHAR(MAX) NOT NULL,
        Terms NVARCHAR(MAX) NOT NULL,
        LineItemColumnsCsv NVARCHAR(256) NOT NULL,
        Subtotal DECIMAL(18,4) NOT NULL,
        DiscountTotal DECIMAL(18,4) NOT NULL,
        TaxTotal DECIMAL(18,4) NOT NULL,
        GrandTotal DECIMAL(18,4) NOT NULL,
        PdfStorageKey NVARCHAR(512) NULL,
        PdfSha256 NVARCHAR(128) NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL,
        GeneratedAtUtc DATETIMEOFFSET NULL,
        RowVersion ROWVERSION NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.BusinessProfiles)
BEGIN
    INSERT INTO dbo.BusinessProfiles (
        Id, IssuerName, IssuerAddress, IssuerEmail, IssuerPhone,
        IssuerRegistrationNumber, IssuerTaxNumber, TaxLabel,
        DefaultCurrencyCode, InvoicePrefix, NextSequence, DefaultTaxRate,
        DefaultTerms, LineItemColumnsCsv, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        NEWID(),
        N'My Business',
        N'',
        N'',
        N'',
        N'',
        N'',
        N'Tax',
        N'USD',
        N'INV',
        100,
        0,
        N'Payment due within 14 days.',
        N'itemNo,date,client,project,description,hours,rate,total',
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'BusinessProfileId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
        ADD BusinessProfileId UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'LineItemColumnsCsv') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
        ADD LineItemColumnsCsv NVARCHAR(256) NOT NULL
            CONSTRAINT DF_Invoices_LineItemColumnsCsv DEFAULT N'itemNo,date,client,project,description,hours,rate,total';
END
GO

IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE BusinessProfileId IS NULL)
BEGIN
    DECLARE @DefaultBusinessProfileId UNIQUEIDENTIFIER;
    SELECT TOP (1) @DefaultBusinessProfileId = Id
    FROM dbo.BusinessProfiles
    ORDER BY CreatedAtUtc, Id;

    UPDATE dbo.Invoices
    SET BusinessProfileId = @DefaultBusinessProfileId
    WHERE BusinessProfileId IS NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Invoices')
      AND name = N'BusinessProfileId'
      AND is_nullable = 1
)
AND NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE BusinessProfileId IS NULL)
BEGIN
    ALTER TABLE dbo.Invoices
        ALTER COLUMN BusinessProfileId UNIQUEIDENTIFIER NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Invoices')
      AND name = N'IX_Invoices_BusinessProfileId'
)
BEGIN
    CREATE INDEX IX_Invoices_BusinessProfileId ON dbo.Invoices (BusinessProfileId);
END
GO

IF OBJECT_ID(N'dbo.InvoiceLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLineItems (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        Quantity DECIMAL(18,4) NOT NULL,
        UnitPrice DECIMAL(18,4) NOT NULL,
        DiscountPercent DECIMAL(18,4) NOT NULL,
        TaxRate DECIMAL(18,4) NOT NULL,
        LineSubtotal DECIMAL(18,4) NOT NULL,
        DiscountAmount DECIMAL(18,4) NOT NULL,
        TaxAmount DECIMAL(18,4) NOT NULL,
        LineTotal DECIMAL(18,4) NOT NULL,
        CONSTRAINT FK_InvoiceLineItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices (Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.InvoiceTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceTemplates (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NOT NULL,
        ActiveVersionId UNIQUEIDENTIFIER NULL,
        IsArchived BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.InvoiceTemplateVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceTemplateVersions (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TemplateId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NOT NULL,
        SourceType INT NOT NULL,
        SourceStorageKey NVARCHAR(512) NOT NULL,
        SelectedPdfPageNumber INT NULL,
        PageWidthPoints DECIMAL(18,4) NOT NULL,
        PageHeightPoints DECIMAL(18,4) NOT NULL,
        LayoutSchemaVersion INT NOT NULL,
        LayoutJson NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_InvoiceTemplateVersions_Templates FOREIGN KEY (TemplateId) REFERENCES dbo.InvoiceTemplates (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_InvoiceTemplateVersions_TemplateVersion UNIQUE (TemplateId, VersionNumber)
    );
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InvoiceTemplates_ActiveVersion')
BEGIN
    ALTER TABLE dbo.InvoiceTemplates DROP CONSTRAINT FK_InvoiceTemplates_ActiveVersion;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_InvoiceTemplates_ActiveVersion'
)
BEGIN
    ALTER TABLE dbo.InvoiceTemplates
        ADD CONSTRAINT FK_InvoiceTemplates_ActiveVersion
        FOREIGN KEY (ActiveVersionId) REFERENCES dbo.InvoiceTemplateVersions (Id) ON DELETE NO ACTION;
END
GO

EXEC('CREATE OR ALTER PROCEDURE dbo.usp_UpsertBusinessProfile
    @Id UNIQUEIDENTIFIER,
    @IssuerName NVARCHAR(200),
    @IssuerAddress NVARCHAR(MAX),
    @IssuerEmail NVARCHAR(200),
    @IssuerPhone NVARCHAR(50),
    @IssuerRegistrationNumber NVARCHAR(200),
    @IssuerTaxNumber NVARCHAR(200),
    @TaxLabel NVARCHAR(50),
    @DefaultCurrencyCode NVARCHAR(8),
    @InvoicePrefix NVARCHAR(32),
    @NextSequence INT,
    @DefaultTaxRate DECIMAL(18,4),
    @DefaultTerms NVARCHAR(4000),
    @LineItemColumnsCsv NVARCHAR(256),
    @CreatedAtUtc DATETIMEOFFSET,
    @UpdatedAtUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.BusinessProfiles WHERE Id = @Id)
    BEGIN
        UPDATE dbo.BusinessProfiles
        SET
            IssuerName = @IssuerName,
            IssuerAddress = @IssuerAddress,
            IssuerEmail = @IssuerEmail,
            IssuerPhone = @IssuerPhone,
            IssuerRegistrationNumber = @IssuerRegistrationNumber,
            IssuerTaxNumber = @IssuerTaxNumber,
            TaxLabel = @TaxLabel,
            DefaultCurrencyCode = @DefaultCurrencyCode,
            InvoicePrefix = @InvoicePrefix,
            NextSequence = @NextSequence,
            DefaultTaxRate = @DefaultTaxRate,
            DefaultTerms = @DefaultTerms,
            LineItemColumnsCsv = @LineItemColumnsCsv,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.BusinessProfiles (
            Id, IssuerName, IssuerAddress, IssuerEmail, IssuerPhone,
            IssuerRegistrationNumber, IssuerTaxNumber, TaxLabel,
            DefaultCurrencyCode, InvoicePrefix, NextSequence, DefaultTaxRate,
            DefaultTerms, LineItemColumnsCsv, CreatedAtUtc, UpdatedAtUtc
        )
        VALUES (
            @Id, @IssuerName, @IssuerAddress, @IssuerEmail, @IssuerPhone,
            @IssuerRegistrationNumber, @IssuerTaxNumber, @TaxLabel,
            @DefaultCurrencyCode, @InvoicePrefix, @NextSequence, @DefaultTaxRate,
            @DefaultTerms, @LineItemColumnsCsv, @CreatedAtUtc, @UpdatedAtUtc
        );
    END
END');
GO

IF OBJECT_ID(N'dbo.usp_ListInvoices', N'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.usp_ListInvoices
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT i.*
        FROM dbo.Invoices i
        ORDER BY i.CreatedAtUtc DESC;
    END');
END
GO

IF OBJECT_ID(N'dbo.usp_CreateInvoiceTemplate', N'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.usp_CreateInvoiceTemplate
        @Id UNIQUEIDENTIFIER,
        @Name NVARCHAR(200),
        @Description NVARCHAR(2000),
        @CreatedAtUtc DATETIMEOFFSET,
        @UpdatedAtUtc DATETIMEOFFSET
    AS
    BEGIN
        SET NOCOUNT ON;

        INSERT INTO dbo.InvoiceTemplates (Id, Name, Description, ActiveVersionId, IsArchived, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@Id, @Name, @Description, NULL, 0, @CreatedAtUtc, @UpdatedAtUtc);
    END');
END
GO

IF OBJECT_ID(N'dbo.usp_GetTemplateVersionById', N'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.usp_GetTemplateVersionById
        @Id UNIQUEIDENTIFIER
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT *
        FROM dbo.InvoiceTemplateVersions
        WHERE Id = @Id;
    END');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.BusinessProfiles)
BEGIN
    INSERT INTO dbo.BusinessProfiles (
        Id, IssuerName, IssuerAddress, IssuerEmail, IssuerPhone,
        IssuerRegistrationNumber, IssuerTaxNumber, TaxLabel,
        DefaultCurrencyCode, InvoicePrefix, NextSequence, DefaultTaxRate,
        DefaultTerms, LineItemColumnsCsv, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        NEWID(),
        N'My Business',
        N'',
        N'',
        N'',
        N'',
        N'',
        N'Tax',
        N'USD',
        N'INV',
        100,
        0,
        N'Payment due within 14 days.',
        N'itemNo,date,client,project,description,hours,rate,total',
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END
GO

SELECT 'Database and schema initialized successfully.' AS Status;
GO
