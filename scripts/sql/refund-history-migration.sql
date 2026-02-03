BEGIN TRANSACTION;
GO

CREATE TABLE [RefundRequestHistories] (
    [Id] int NOT NULL IDENTITY,
    [RefundRequestId] int NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [PreviousStatus] nvarchar(50) NULL,
    [NewStatus] nvarchar(50) NULL,
    [Comments] nvarchar(1000) NULL,
    [PerformedBy] nvarchar(450) NOT NULL,
    [UserName] nvarchar(200) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [IpAddress] nvarchar(50) NULL,
    CONSTRAINT [PK_RefundRequestHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefundRequestHistories_RefundRequests_RefundRequestId] FOREIGN KEY ([RefundRequestId]) REFERENCES [RefundRequests] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RefundRequestHistories_RefundRequestId] ON [RefundRequestHistories] ([RefundRequestId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251127053250_AddRefundRequestHistory', N'8.0.6');
GO

COMMIT;
GO

