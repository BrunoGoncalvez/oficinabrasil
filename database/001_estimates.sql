-- Execute once against the existing database before starting the updated API.
-- New databases are still initialized by EnsureCreated.
CREATE TABLE IF NOT EXISTS `Estimates` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `ServiceOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Diagnosis` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Summary` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Total` decimal(12,2) NOT NULL,
    `CustomerEmail` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
    `TokenHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `Approved` tinyint(1) NULL,
    `DecidedAt` datetime(6) NULL,
    `Version` char(36) COLLATE ascii_general_ci NOT NULL,
    `EmailBody` longtext CHARACTER SET utf8mb4 NOT NULL,
    `EmailSentAt` datetime(6) NULL,
    CONSTRAINT `PK_Estimates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Estimates_ServiceOrders_ServiceOrderId` FOREIGN KEY (`ServiceOrderId`)
        REFERENCES `ServiceOrders` (`Id`) ON DELETE CASCADE,
    UNIQUE KEY `IX_Estimates_ServiceOrderId` (`ServiceOrderId`)
) CHARACTER SET=utf8mb4;
