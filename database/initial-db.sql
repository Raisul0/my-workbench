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

IF OBJECT_ID(N'dbo.CustomerProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerProfiles (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerAddress NVARCHAR(MAX) NOT NULL,
        CustomerEmail NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(50) NOT NULL,
        CustomerRegistrationNumber NVARCHAR(200) NOT NULL,
        CustomerTaxNumber NVARCHAR(200) NOT NULL,
        TaxLabel NVARCHAR(50) NOT NULL,
        DefaultCurrencyCode NVARCHAR(8) NOT NULL,
        InvoicePrefix NVARCHAR(32) NOT NULL,
        NextSequence INT NOT NULL,
        DefaultTaxRate DECIMAL(18,4) NOT NULL,
        DefaultTerms NVARCHAR(4000) NOT NULL,
        LineItemColumns NVARCHAR(256) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET NOT NULL,
        UpdatedAtUtc DATETIMEOFFSET NOT NULL
    );
END
GO

IF COL_LENGTH(N'dbo.CustomerProfiles', N'LineItemColumns') IS NULL
BEGIN
    ALTER TABLE dbo.CustomerProfiles
        ADD LineItemColumns NVARCHAR(256) NOT NULL
            CONSTRAINT DF_CustomerProfiles_LineItemColumns DEFAULT N'description:text,total:number';
END
GO

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        CustomerProfileId UNIQUEIDENTIFIER NOT NULL,
        InvoiceNumber NVARCHAR(64) NOT NULL UNIQUE,
        Status INT NOT NULL,
        TemplateVersionId UNIQUEIDENTIFIER NOT NULL,
        IssueDate DATE NOT NULL,
        DueDate DATE NULL,
        CurrencyCode NVARCHAR(8) NOT NULL,
        Notes NVARCHAR(MAX) NOT NULL,
        Terms NVARCHAR(MAX) NOT NULL,
        LineItemColumns NVARCHAR(256) NOT NULL,
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

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerProfiles)
BEGIN
    INSERT INTO dbo.CustomerProfiles (
        Id, CustomerName, CustomerAddress, CustomerEmail, CustomerPhone,
        CustomerRegistrationNumber, CustomerTaxNumber, TaxLabel,
        DefaultCurrencyCode, InvoicePrefix, NextSequence, DefaultTaxRate,
        DefaultTerms, LineItemColumns, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        NEWID(),
        N'Customer',
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
        N'description:text,total:number',
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'CustomerProfileId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
        ADD CustomerProfileId UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'LineItemColumns') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
        ADD LineItemColumns NVARCHAR(256) NOT NULL
            CONSTRAINT DF_Invoices_LineItemColumns DEFAULT N'description:text,total:number';
END
GO

IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE CustomerProfileId IS NULL)
BEGIN
    DECLARE @DefaultCustomerProfileId UNIQUEIDENTIFIER;
    SELECT TOP (1) @DefaultCustomerProfileId = Id
    FROM dbo.CustomerProfiles
    ORDER BY CreatedAtUtc, Id;

    UPDATE dbo.Invoices
    SET CustomerProfileId = @DefaultCustomerProfileId
    WHERE CustomerProfileId IS NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Invoices')
      AND name = N'CustomerProfileId'
      AND is_nullable = 1
)
AND NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE CustomerProfileId IS NULL)
BEGIN
    ALTER TABLE dbo.Invoices
        ALTER COLUMN CustomerProfileId UNIQUEIDENTIFIER NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Invoices')
      AND name = N'IX_Invoices_CustomerProfileId'
)
BEGIN
    CREATE INDEX IX_Invoices_CustomerProfileId ON dbo.Invoices (CustomerProfileId);
END
GO

IF OBJECT_ID(N'dbo.InvoiceLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLineItems (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        Total DECIMAL(18,4) NOT NULL,
        OtherColumns NVARCHAR(4000) NOT NULL,
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

EXEC('CREATE OR ALTER PROCEDURE dbo.usp_UpsertCustomerProfile
    @Id UNIQUEIDENTIFIER,
    @CustomerName NVARCHAR(200),
    @CustomerAddress NVARCHAR(MAX),
    @CustomerEmail NVARCHAR(200),
    @CustomerPhone NVARCHAR(50),
    @CustomerRegistrationNumber NVARCHAR(200),
    @CustomerTaxNumber NVARCHAR(200),
    @TaxLabel NVARCHAR(50),
    @DefaultCurrencyCode NVARCHAR(8),
    @InvoicePrefix NVARCHAR(32),
    @NextSequence INT,
    @DefaultTaxRate DECIMAL(18,4),
    @DefaultTerms NVARCHAR(4000),
    @LineItemColumns NVARCHAR(256),
    @CreatedAtUtc DATETIMEOFFSET,
    @UpdatedAtUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CustomerProfiles WHERE Id = @Id)
    BEGIN
        UPDATE dbo.CustomerProfiles
        SET
            CustomerName = @CustomerName,
            CustomerAddress = @CustomerAddress,
            CustomerEmail = @CustomerEmail,
            CustomerPhone = @CustomerPhone,
            CustomerRegistrationNumber = @CustomerRegistrationNumber,
            CustomerTaxNumber = @CustomerTaxNumber,
            TaxLabel = @TaxLabel,
            DefaultCurrencyCode = @DefaultCurrencyCode,
            InvoicePrefix = @InvoicePrefix,
            NextSequence = @NextSequence,
            DefaultTaxRate = @DefaultTaxRate,
            DefaultTerms = @DefaultTerms,
            LineItemColumns = @LineItemColumns,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CustomerProfiles (
            Id, CustomerName, CustomerAddress, CustomerEmail, CustomerPhone,
            CustomerRegistrationNumber, CustomerTaxNumber, TaxLabel,
            DefaultCurrencyCode, InvoicePrefix, NextSequence, DefaultTaxRate,
            DefaultTerms, LineItemColumns, CreatedAtUtc, UpdatedAtUtc
        )
        VALUES (
            @Id, @CustomerName, @CustomerAddress, @CustomerEmail, @CustomerPhone,
            @CustomerRegistrationNumber, @CustomerTaxNumber, @TaxLabel,
            @DefaultCurrencyCode, @InvoicePrefix, @NextSequence, @DefaultTaxRate,
            @DefaultTerms, @LineItemColumns, @CreatedAtUtc, @UpdatedAtUtc
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

SELECT 'Database and schema initialized successfully.' AS Status;
GO
