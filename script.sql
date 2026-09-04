CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `migration_id` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `product_version` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `pk___ef_migrations_history` PRIMARY KEY (`migration_id`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `roles` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `normalized_name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `concurrency_stamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `pk_roles` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `users` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `user_name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `normalized_user_name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `normalized_email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `email_confirmed` tinyint(1) NOT NULL,
    `password_hash` longtext CHARACTER SET utf8mb4 NULL,
    `security_stamp` longtext CHARACTER SET utf8mb4 NULL,
    `concurrency_stamp` longtext CHARACTER SET utf8mb4 NULL,
    `phone_number` longtext CHARACTER SET utf8mb4 NULL,
    `phone_number_confirmed` tinyint(1) NOT NULL,
    `two_factor_enabled` tinyint(1) NOT NULL,
    `lockout_end` datetime(6) NULL,
    `lockout_enabled` tinyint(1) NOT NULL,
    `access_failed_count` int NOT NULL,
    CONSTRAINT `pk_users` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `role_claims` (
    `id` int NOT NULL AUTO_INCREMENT,
    `role_id` bigint NOT NULL,
    `claim_type` longtext CHARACTER SET utf8mb4 NULL,
    `claim_value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `pk_role_claims` PRIMARY KEY (`id`),
    CONSTRAINT `fk_role_claims_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `user_claims` (
    `id` int NOT NULL AUTO_INCREMENT,
    `user_id` bigint NOT NULL,
    `claim_type` longtext CHARACTER SET utf8mb4 NULL,
    `claim_value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `pk_user_claims` PRIMARY KEY (`id`),
    CONSTRAINT `fk_user_claims_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `user_logins` (
    `login_provider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `provider_key` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `provider_display_name` longtext CHARACTER SET utf8mb4 NULL,
    `user_id` bigint NOT NULL,
    CONSTRAINT `pk_user_logins` PRIMARY KEY (`login_provider`, `provider_key`),
    CONSTRAINT `fk_user_logins_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `user_roles` (
    `user_id` bigint NOT NULL,
    `role_id` bigint NOT NULL,
    CONSTRAINT `pk_user_roles` PRIMARY KEY (`user_id`, `role_id`),
    CONSTRAINT `fk_user_roles_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_user_roles_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `user_tokens` (
    `user_id` bigint NOT NULL,
    `login_provider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `pk_user_tokens` PRIMARY KEY (`user_id`, `login_provider`, `name`),
    CONSTRAINT `fk_user_tokens_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

INSERT INTO `roles` (`id`, `concurrency_stamp`, `name`, `normalized_name`)
VALUES (1, NULL, 'Super Administrator', 'SUPER ADMINISTRATOR');

INSERT INTO `users` (`id`, `access_failed_count`, `concurrency_stamp`, `email`, `email_confirmed`, `lockout_enabled`, `lockout_end`, `normalized_email`, `normalized_user_name`, `password_hash`, `phone_number`, `phone_number_confirmed`, `security_stamp`, `two_factor_enabled`, `user_name`)
VALUES (1, 0, 'ab5d6049-e401-4159-9c04-da30825ecef3', 'admin@urmarry.com', TRUE, FALSE, NULL, 'ADMIN@URMARRY.COM', 'ADMIN', 'AQAAAAIAAYagAAAAEOI0pa3uVXNPNDgo3DTBOugdjvX3gVenseja/DditneRbK7VPFdxmevvxD0jwBEgvA==', NULL, FALSE, 'F3A1877B-B990-4F13-8E87-C90DF070B2C9', FALSE, 'admin');

INSERT INTO `user_roles` (`role_id`, `user_id`)
VALUES (1, 1);

CREATE INDEX `ix_role_claims_role_id` ON `role_claims` (`role_id`);

CREATE UNIQUE INDEX `RoleNameIndex` ON `roles` (`normalized_name`);

CREATE INDEX `ix_user_claims_user_id` ON `user_claims` (`user_id`);

CREATE INDEX `ix_user_logins_user_id` ON `user_logins` (`user_id`);

CREATE INDEX `ix_user_roles_role_id` ON `user_roles` (`role_id`);

CREATE INDEX `EmailIndex` ON `users` (`normalized_email`);

CREATE UNIQUE INDEX `UserNameIndex` ON `users` (`normalized_user_name`);

INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
VALUES ('20240219021551_userdb', '7.0.5');

COMMIT;

START TRANSACTION;

INSERT INTO `roles` (`id`, `concurrency_stamp`, `name`, `normalized_name`)
VALUES (2, NULL, 'Staff', 'STAFF');

UPDATE `users` SET `concurrency_stamp` = '6b35b46a-9c09-415d-a5ae-f68d60ae7ee8', `password_hash` = 'AQAAAAIAAYagAAAAEFQ8Ol8fZ5yeDHh6LfI6w7PjpTvI+O3eHokq4qEm17NUK239iMGxifhvSsOq8Sat9g=='
WHERE `id` = 1;
SELECT ROW_COUNT();


INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
VALUES ('20260613074330_AddStaffRole', '7.0.5');

COMMIT;

