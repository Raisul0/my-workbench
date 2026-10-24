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

-- ==========================================================
-- CREATE TABLES
-- ==========================================================

-- Customer Profiles Table
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
    LineItemColumns NVARCHAR(256) NOT NULL DEFAULT N'description:text,total:number',
    CreatedAtUtc DATETIMEOFFSET NOT NULL,
    UpdatedAtUtc DATETIMEOFFSET NOT NULL
);
GO

-- Invoice Templates Table
CREATE TABLE dbo.InvoiceTemplates (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    ActiveVersionId UNIQUEIDENTIFIER NULL,
    IsArchived BIT NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIMEOFFSET NOT NULL,
    UpdatedAtUtc DATETIMEOFFSET NOT NULL
);
GO

-- Invoice Template Versions Table
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
GO

-- Invoices Table
CREATE TABLE dbo.Invoices (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    CustomerProfileId UNIQUEIDENTIFIER NOT NULL,
    TemplateId UNIQUEIDENTIFIER NOT NULL,
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
    RowVersion ROWVERSION NULL,
    CONSTRAINT FK_Invoices_CustomerProfiles FOREIGN KEY (CustomerProfileId) REFERENCES dbo.CustomerProfiles (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Invoices_Templates FOREIGN KEY (TemplateId) REFERENCES dbo.InvoiceTemplates (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Invoices_TemplateVersions FOREIGN KEY (TemplateVersionId) REFERENCES dbo.InvoiceTemplateVersions (Id) ON DELETE NO ACTION
);
GO

-- Invoice Line Items Table
CREATE TABLE dbo.InvoiceLineItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InvoiceId UNIQUEIDENTIFIER NOT NULL,
    SortOrder INT NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    Total DECIMAL(18,4) NOT NULL,
    OtherColumns NVARCHAR(4000) NOT NULL,
    CONSTRAINT FK_InvoiceLineItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices (Id) ON DELETE CASCADE
);
GO

-- ==========================================================
-- CREATE INDEXES
-- ==========================================================

CREATE INDEX IX_Invoices_CustomerProfileId ON dbo.Invoices (CustomerProfileId);
GO

CREATE INDEX IX_Invoices_TemplateId ON dbo.Invoices (TemplateId);
GO

CREATE INDEX IX_InvoiceLineItems_InvoiceId ON dbo.InvoiceLineItems (InvoiceId);
GO

-- ==========================================================
-- ADD FOREIGN KEY FOR ACTIVE VERSION
-- ==========================================================

ALTER TABLE dbo.InvoiceTemplates
    ADD CONSTRAINT FK_InvoiceTemplates_ActiveVersion
    FOREIGN KEY (ActiveVersionId) REFERENCES dbo.InvoiceTemplateVersions (Id) ON DELETE NO ACTION;
GO

-- ==========================================================
-- SEED DEFAULT DATA
-- ==========================================================

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

-- ==========================================================
-- CREATE STORED PROCEDURES
-- ==========================================================

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

SELECT 'Database and schema initialized successfully.' AS Status;
GO
