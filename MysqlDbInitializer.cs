﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace FoodEnterpriseIMS.Database
{
    /// <summary>
    /// MySQL数据库初始化器，完全对应原init_database.py
    /// 多人并发InnoDB，自动建库、建表、建索引，幂等可重复执行
    /// </summary>
    public static class MysqlDbInitializer
    {
        #region 配置读取逻辑
        private const string DefaultDbName = "spzhprogram";

        private static List<string> GetConfigCandidatePaths()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), "launcher_config.ini"),
                Path.Combine(baseDir, "launcher_config.ini"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "launcher_config.ini")
            };
            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 加载数据库连接配置（环境变量优先 > ini > 硬编码兜底）
        /// </summary>
        public static MySqlConfig LoadMysqlConfig()
        {
            var envHost = Environment.GetEnvironmentVariable("MYSQL_HOST");
            var envPort = Environment.GetEnvironmentVariable("MYSQL_PORT");
            var envUser = Environment.GetEnvironmentVariable("MYSQL_USER");
            var envPwd = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
            var envDb = Environment.GetEnvironmentVariable("LAUNCHER_DB_NAME");

            if (!string.IsNullOrWhiteSpace(envHost))
            {
                return new MySqlConfig
                {
                    Host = envHost,
                    Port = int.TryParse(envPort, out var p) ? p : 3306,
                    User = envUser ?? string.Empty,
                    Password = envPwd ?? string.Empty,
                    Database = envDb ?? DefaultDbName
                };
            }

            foreach (var cfgPath in GetConfigCandidatePaths())
            {
                if (!File.Exists(cfgPath)) continue;
                var config = new ConfigurationBuilder()
                    .AddIniFile(cfgPath, optional: true, reloadOnChange: true)
                    .Build();
                var section = config.GetSection("Settings");
                return new MySqlConfig
                {
                    Host = section["mysql_host"] ?? "10.57.129.50",
                    Port = int.TryParse(section["mysql_port"], out var port) ? port : 3306,
                    User = section["mysql_user"] ?? "spzh_user",
                    Password = section["mysql_password"] ?? "10241024",
                    Database = section["db_name"] ?? DefaultDbName
                };
            }

            // 兜底固定数据库参数
            return new MySqlConfig
            {
                Host = "10.57.129.50",
                Port = 3306,
                User = "spzh_user",
                Password = "10241024",
                Database = DefaultDbName
            };
        }

        /// <summary>
        /// 无库连接串（用于创建数据库）
        /// </summary>
        public static string GetConnStringWithoutDb(MySqlConfig cfg)
        {
            return $"server={cfg.Host};port={cfg.Port};user={cfg.User};password={cfg.Password};charset=utf8mb4;AllowUserVariables=true";
        }

        /// <summary>
        /// 业务正常连接串（带连接池适配多用户）
        /// </summary>
        public static string GetConnString(MySqlConfig cfg)
        {
            return $"{GetConnStringWithoutDb(cfg)};database={cfg.Database};Pooling=true;Max Pool Size=100;Min Pool Size=10";
        }
        #endregion

        #region 底层SQL执行封装
        private static void ExecuteSql(MySqlConnection conn, string sql)
        {
            using var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 数据库不存在则自动创建
        /// </summary>
        private static void CreateDatabaseIfNotExists(MySqlConfig cfg)
        {
            using var conn = new MySqlConnection(GetConnStringWithoutDb(cfg));
            conn.Open();
            var createDbSql = $"CREATE DATABASE IF NOT EXISTS `{cfg.Database}` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            ExecuteSql(conn, createDbSql);
        }
        #endregion

        #region 全部建表SQL集合
        // 2.1 系统数据表
        private static readonly Dictionary<string, string> SystemTables = new()
        {
            ["user_accounts"] = @"
CREATE TABLE IF NOT EXISTS `user_accounts` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `username` VARCHAR(100) NOT NULL,
  `password_hash` TEXT,
  `role_id` BIGINT DEFAULT 1,
  `nickname` VARCHAR(100),
  `department` VARCHAR(100),
  `is_disabled` TINYINT(1) DEFAULT 0,
  `last_login` DATETIME NULL,
  `is_online` TINYINT(1) DEFAULT 0,
  `last_computer` VARCHAR(100),
  `login_count_month` INT DEFAULT 0,
  `login_count_total` INT DEFAULT 0,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户账户'",
            ["roles"] = @"
CREATE TABLE IF NOT EXISTS `roles` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `description` TEXT,
  `is_enabled` TINYINT(1) DEFAULT 1,
  UNIQUE KEY `uk_role_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='角色'",
            ["permissions"] = @"
CREATE TABLE IF NOT EXISTS `permissions` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `permission_key` VARCHAR(200) NOT NULL,
  `name` VARCHAR(100) NOT NULL,
  `node_type` VARCHAR(20) NOT NULL,
  `description` TEXT,
  `node_code` VARCHAR(100),
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_perm_key` (`permission_key`),
  CONSTRAINT chk_node_type CHECK (`node_type` IN ('menu','button'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='权限'",
            ["role_permissions"] = @"
CREATE TABLE IF NOT EXISTS `role_permissions` (
  `role_id` BIGINT NOT NULL,
  `permission_id` BIGINT NOT NULL,
  PRIMARY KEY (`role_id`,`permission_id`),
  FOREIGN KEY (`role_id`) REFERENCES `roles`(`id`) ON DELETE CASCADE,
  FOREIGN KEY (`permission_id`) REFERENCES `permissions`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='角色权限关联'",
            ["users"] = @"
CREATE TABLE IF NOT EXISTS `users` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `username` VARCHAR(100) NOT NULL,
  `password` TEXT NOT NULL,
  `role_id` BIGINT NULL,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `last_login_time` DATETIME NULL,
  UNIQUE KEY `uk_username` (`username`),
  FOREIGN KEY (`role_id`) REFERENCES `roles`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='备用用户表'",
            ["system_config"] = @"
CREATE TABLE IF NOT EXISTS `system_config` (
  `key` VARCHAR(100) PRIMARY KEY,
  `value` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='系统配置'",
            ["column_settings"] = @"
CREATE TABLE IF NOT EXISTS `column_settings` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `settings_group` VARCHAR(100) NOT NULL,
  `hidden_columns` TEXT,
  `column_order` TEXT,
  `column_width_settings` TEXT,
  `fixed_column_width_settings` TEXT,
  `row_height` INT DEFAULT 20,
  `table_height_enabled` TINYINT(1) DEFAULT 0,
  `table_height_mode` VARCHAR(50) DEFAULT '',
  `table_height` INT DEFAULT 300,
  `left_columns` TEXT,
  `center_columns` TEXT,
  `right_columns` TEXT,
  `sort_specs` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_settings_group` (`settings_group`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='表格列配置'",
            ["version_log"] = @"
CREATE TABLE IF NOT EXISTS `version_log` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `version` VARCHAR(50) NOT NULL,
  `update_date` DATE NOT NULL,
  `description` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='版本更新日志'",
            ["system_tables_registry"] = @"
CREATE TABLE IF NOT EXISTS `system_tables_registry` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `table_name` VARCHAR(100) NOT NULL,
  `display_name` VARCHAR(100),
  `table_type` VARCHAR(50),
  `fields_info` TEXT,
  `description` TEXT,
  `is_sync` TINYINT(1) DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_table_name` (`table_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='系统表注册'",
            ["login_stats"] = @"
CREATE TABLE IF NOT EXISTS `login_stats` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `username` VARCHAR(100) NOT NULL,
  `login_time` DATETIME NOT NULL,
  `ip_address` VARCHAR(50),
  `user_agent` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='登录统计'"
        };

        // 2.2 基础主数据表
        private static readonly Dictionary<string, string> MasterTables = new()
        {
            ["tree_nodes"] = @"
CREATE TABLE IF NOT EXISTS `tree_nodes` (
  `tree_key` VARCHAR(100) NOT NULL,
  `node_code` VARCHAR(100) NOT NULL,
  `parent_code` VARCHAR(100),
  `title` VARCHAR(200) NOT NULL,
  `payload_json` TEXT,
  `sort_order` INT DEFAULT 0,
  PRIMARY KEY (`tree_key`,`node_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='树形节点'",
            ["product_infos"] = @"
CREATE TABLE IF NOT EXISTS `product_infos` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `node_code` VARCHAR(100),
  `product_id` VARCHAR(100),
  `product_name` VARCHAR(200) NOT NULL,
  `product_code` VARCHAR(100),
  `standard_code` VARCHAR(100),
  `food_category` VARCHAR(100),
  `dosage_form` VARCHAR(50),
  `ownership_status` VARCHAR(50),
  `approval_method` VARCHAR(100),
  `approval_department` VARCHAR(100),
  `approval_date` DATE,
  `standard_validity` VARCHAR(100),
  `enterprise_code` VARCHAR(100),
  `enterprise_year` INT,
  `enterprise_effective_date` DATE,
  `standard_link` TEXT,
  `enterprise_link` TEXT,
  `remark` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='产品信息'",
            ["product_items"] = @"
CREATE TABLE IF NOT EXISTS `product_items` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `product_id` VARCHAR(100) NOT NULL,
  `sort_order` INT DEFAULT 0,
  `item_name` VARCHAR(200),
  `content` TEXT,
  FOREIGN KEY (`product_id`) REFERENCES `product_infos`(`product_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='产品明细'",
            ["product_barcodes"] = @"
CREATE TABLE IF NOT EXISTS `product_barcodes` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `barcode_id` VARCHAR(100) UNIQUE,
  `company_code` VARCHAR(100),
  `barcode_number` VARCHAR(100),
  `product_id` VARCHAR(100) DEFAULT '',
  `product_name` VARCHAR(200),
  `brand_series` VARCHAR(100),
  `package_category` VARCHAR(100),
  `package_spec` VARCHAR(100),
  `net_content` VARCHAR(100),
  `unit` VARCHAR(50),
  `generate_date` DATE,
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='产品条码'",
            ["food_categories"] = @"
CREATE TABLE IF NOT EXISTS `food_categories` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `category_code` VARCHAR(100) UNIQUE NOT NULL,
  `category_name` VARCHAR(100) NOT NULL,
  `parent_code` VARCHAR(100) DEFAULT '',
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='食品分类'",
            ["material_infos"] = @"
CREATE TABLE IF NOT EXISTS `material_infos` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `material_id` VARCHAR(100),
  `node_code` VARCHAR(100) NOT NULL,
  `first_level_code` VARCHAR(100),
  `material_code` VARCHAR(100),
  `material_name` VARCHAR(200),
  `specification` VARCHAR(200),
  `packaging_spec` VARCHAR(200),
  `brand_series` VARCHAR(100),
  `expiry_date` INT,
  `unit` VARCHAR(50),
  `standard` VARCHAR(200),
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物料信息'",
            ["customer_infos"] = @"
CREATE TABLE IF NOT EXISTS `customer_infos` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `customer_id` VARCHAR(100),
  `source` VARCHAR(100),
  `customer_name` VARCHAR(200),
  `customer_type` VARCHAR(100),
  `license_no` VARCHAR(100),
  `license_validity` VARCHAR(100),
  `business_license` VARCHAR(100),
  `business_validity` VARCHAR(100),
  `contact_address` TEXT,
  `postal_code` VARCHAR(20),
  `contact_person` VARCHAR(100),
  `contact_phone` VARCHAR(50),
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户信息'",
            ["employee_infos"] = @"
CREATE TABLE IF NOT EXISTS `employee_infos` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `employee_id` VARCHAR(50) NOT NULL DEFAULT '',
  `employee_name` VARCHAR(100) NOT NULL DEFAULT '',
  `department` VARCHAR(100) NOT NULL DEFAULT '',
  `title` VARCHAR(100) NOT NULL DEFAULT '',
  `position` VARCHAR(100) NOT NULL DEFAULT '',
  `hire_date` DATE NOT NULL DEFAULT '',
  `id_card_no` VARCHAR(50) NOT NULL DEFAULT '',
  `phone` VARCHAR(50) NOT NULL DEFAULT '',
  `gender` VARCHAR(10) NOT NULL DEFAULT '',
  `graduation_school` VARCHAR(200) NOT NULL DEFAULT '',
  `education` VARCHAR(50) NOT NULL DEFAULT '',
  `email` VARCHAR(100) NOT NULL DEFAULT '',
  `status` VARCHAR(20) NOT NULL DEFAULT '在职',
  `remark` TEXT NOT NULL DEFAULT '',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_employee_id` (`employee_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='员工信息'",
            ["standard_specifications"] = @"
CREATE TABLE IF NOT EXISTS `standard_specifications` (
  `standard_id` VARCHAR(100) PRIMARY KEY,
  `node_code` VARCHAR(100) NOT NULL,
  `category` VARCHAR(100),
  `series` VARCHAR(100),
  `standard_name` VARCHAR(200),
  `standard_code` VARCHAR(100),
  `publish_dept` VARCHAR(200),
  `publish_year` VARCHAR(20),
  `applies_to_haccp` TINYINT(1),
  `publish_date` DATE,
  `implement_date` DATE,
  `revision_date` DATE,
  `effective_date` DATE,
  `standard_link` TEXT,
  `new_standard_link` TEXT,
  `is_invalid` TINYINT(1),
  `remark` TEXT,
  `sort_order` INT DEFAULT 1,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='标准规范'",
            ["standard_spec_items"] = @"
CREATE TABLE IF NOT EXISTS `standard_spec_items` (
  `project_id` VARCHAR(100) PRIMARY KEY,
  `standard_id` VARCHAR(100) NOT NULL,
  `project_name` VARCHAR(200) NOT NULL,
  `unit` VARCHAR(50),
  `cost` DECIMAL(12,4),
  `sort_order` INT DEFAULT 0,
  `disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  FOREIGN KEY (`standard_id`) REFERENCES `standard_specifications`(`standard_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='标准检测项目'",
            ["document_files"] = @"
CREATE TABLE IF NOT EXISTS `document_files` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `node_code` VARCHAR(100),
  `file_unique_id` VARCHAR(100),
  `department` VARCHAR(100),
  `std_category` VARCHAR(100),
  `std_level_1` VARCHAR(100),
  `std_level_2` VARCHAR(100),
  `file_name` VARCHAR(200),
  `file_code` VARCHAR(100),
  `version` VARCHAR(50),
  `revision` VARCHAR(50),
  `revision_date` DATE,
  `effective_date` DATE,
  `file_link` TEXT,
  `is_invalid` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='编制文件'",
            ["content_claims"] = @"
CREATE TABLE IF NOT EXISTS `content_claims` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `claim_code` VARCHAR(100) UNIQUE NOT NULL,
  `claim_name` VARCHAR(200) NOT NULL,
  `nutrient_name` VARCHAR(100),
  `claim_condition` TEXT,
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='含量声称'",
            ["comparative_claims"] = @"
CREATE TABLE IF NOT EXISTS `comparative_claims` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `claim_code` VARCHAR(100) UNIQUE NOT NULL,
  `claim_name` VARCHAR(200) NOT NULL,
  `comparison_type` VARCHAR(100),
  `reference_food` VARCHAR(200),
  `claim_condition` TEXT,
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='比较声称'",
            ["function_claims"] = @"
CREATE TABLE IF NOT EXISTS `function_claims` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `claim_code` VARCHAR(100) UNIQUE NOT NULL,
  `claim_name` VARCHAR(200) NOT NULL,
  `function_category` VARCHAR(100),
  `claim_condition` TEXT,
  `scientific_basis` TEXT,
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='功能声称'"
        };

        // 2.3 配置数据表
        private static readonly Dictionary<string, string> ConfigTables = new()
        {
            ["menu_buttons"] = @"
CREATE TABLE IF NOT EXISTS `menu_buttons` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `node_code` VARCHAR(100),
  `button_name_cn` VARCHAR(100) NOT NULL,
  `button_name_en` VARCHAR(100),
  `button_key` VARCHAR(100) NOT NULL,
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `enabled` TINYINT(1) DEFAULT 1,
  `icon_path` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='菜单按钮'",
            ["user_preferences"] = @"
CREATE TABLE IF NOT EXISTS `user_preferences` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `username` VARCHAR(100) NOT NULL,
  `pref_key` VARCHAR(100) NOT NULL,
  `pref_value` TEXT,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_user_pref` (`username`,`pref_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户界面偏好'",
            ["common_dict_field_defs"] = @"
CREATE TABLE IF NOT EXISTS `common_dict_field_defs` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `field_key` VARCHAR(100) NOT NULL,
  `field_label` VARCHAR(100),
  `field_type` VARCHAR(50) NOT NULL,
  `placeholder` VARCHAR(200),
  `min_value` INT DEFAULT -999999,
  `max_value` INT DEFAULT 999999,
  `options` TEXT,
  `description` TEXT,
  `sort_order` INT DEFAULT 0,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `node_code` VARCHAR(100) DEFAULT '',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='通用字典字段定义'",
            ["common_dict_data"] = @"
CREATE TABLE IF NOT EXISTS `common_dict_data` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `node_code` VARCHAR(100) NOT NULL,
  `code` VARCHAR(100) NOT NULL,
  `name` VARCHAR(200) NOT NULL,
  `field1` TEXT,
  `field2` TEXT,
  `field3` TEXT,
  `field4` TEXT,
  `field5` TEXT,
  `number1` DECIMAL(12,4),
  `number2` DECIMAL(12,4),
  `date1` DATE,
  `date2` DATE,
  `flag1` TINYINT(1) DEFAULT 0,
  `flag2` TINYINT(1) DEFAULT 0,
  `amount` DECIMAL(12,4),
  `remark` TEXT,
  `sort_order` INT DEFAULT 1,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='通用字典数据'",
            ["nutrition_labels"] = @"
CREATE TABLE IF NOT EXISTS `nutrition_labels` (
  `label_id` VARCHAR(100) PRIMARY KEY,
  `name` VARCHAR(200) NOT NULL,
  `detection_mode` VARCHAR(50) DEFAULT 'core',
  `heavy_metal` TINYINT(1) DEFAULT 0,
  `claim_types` TEXT DEFAULT '[]',
  `remark` TEXT,
  `nutrient_data` TEXT DEFAULT '{}',
  `created_at` DATETIME,
  `updated_at` DATETIME
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='营养标签配置'",
            ["nutrient_parameters"] = @"
CREATE TABLE IF NOT EXISTS `nutrient_parameters` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `sort_order` INT,
  `nutrient` VARCHAR(100) UNIQUE,
  `unit` VARCHAR(50),
  `rounding_interval` DECIMAL(10,4),
  `zero_threshold` DECIMAL(10,4),
  `energy_value` DECIMAL(10,4) DEFAULT 0,
  `lower_error` DECIMAL(10,4),
  `upper_error` DECIMAL(10,4),
  `nrv` DECIMAL(10,4),
  `core` TINYINT(1) DEFAULT 0,
  `heavy_metal` TINYINT(1) DEFAULT 0,
  `is_sports_nutrition` TINYINT(1) DEFAULT 0,
  `daily_usage_range` TEXT,
  `disabled` TINYINT(1) DEFAULT 0,
  `param_key` VARCHAR(100),
  `param_value` TEXT,
  `description` TEXT,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='营养参数'",
            ["inspection_params"] = @"
CREATE TABLE IF NOT EXISTS `inspection_params` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `inspection_id` VARCHAR(100),
  `node_code` VARCHAR(100) NOT NULL,
  `inspection_name` VARCHAR(200),
  `material_code` VARCHAR(100),
  `standard` VARCHAR(200),
  `specification` VARCHAR(200),
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='检品参数主表'",
            ["inspection_param_items"] = @"
CREATE TABLE IF NOT EXISTS `inspection_param_items` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `inspection_id` VARCHAR(100) NOT NULL,
  `param_id` VARCHAR(100),
  `sort_order` INT DEFAULT 0,
  `item_name` VARCHAR(200),
  `unit` VARCHAR(50),
  `requirement` TEXT,
  `must_check` TINYINT(1) DEFAULT 0,
  `self_check` TINYINT(1) DEFAULT 0,
  `keep_observation` TINYINT(1) DEFAULT 0,
  `disabled` TINYINT(1) DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='检品参数明细'",
            ["process_params_main"] = @"
CREATE TABLE IF NOT EXISTS `process_params_main` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `process_step_id` VARCHAR(100) UNIQUE,
  `product_id` VARCHAR(100),
  `process_name` VARCHAR(200),
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序参数主表'",
            ["process_params_detail"] = @"
CREATE TABLE IF NOT EXISTS `process_params_detail` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `process_step_id` VARCHAR(100),
  `param_id` VARCHAR(100),
  `sort_order` INT DEFAULT 0,
  `item_name` VARCHAR(200),
  `index_requirement` TEXT,
  `is_disabled` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`process_step_id`) REFERENCES `process_params_main`(`process_step_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序参数明细'"
        };

        // 2.4 业务事务数据表
        private static readonly Dictionary<string, string> TransactionTables = new()
        {
            ["sample_receive_send"] = @"
CREATE TABLE IF NOT EXISTS `sample_receive_send` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `receive_send_id` VARCHAR(100) UNIQUE NOT NULL,
  `receive_send_date` DATE,
  `inspection_date` DATE,
  `report_date` DATE,
  `sample_name` VARCHAR(200),
  `sample_batch` VARCHAR(100),
  `sample_quantity` VARCHAR(100),
  `retention_quantity` VARCHAR(100),
  `representative_quantity` VARCHAR(100),
  `sample_source` VARCHAR(100),
  `is_reinspection` TINYINT(1) DEFAULT 0,
  `remark` TEXT,
  `node_code` VARCHAR(100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='样品收发记录'",
            ["qa_sampling_records"] = @"
CREATE TABLE IF NOT EXISTS `qa_sampling_records` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `sampling_id` VARCHAR(100) NOT NULL,
  `node_code` VARCHAR(100) NOT NULL,
  `sampling_date` DATE NOT NULL,
  `inspection_date` DATE NOT NULL,
  `sample_name` VARCHAR(200) NOT NULL,
  `sample_batch` VARCHAR(100) NOT NULL,
  `sampling_quantity` VARCHAR(100),
  `representative_quantity` VARCHAR(100),
  `sample_source` VARCHAR(100) NOT NULL,
  `sampler` VARCHAR(100) NOT NULL,
  `brand_series` VARCHAR(100),
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `created_by` BIGINT,
  `updated_by` BIGINT,
  `is_deleted` TINYINT(1) DEFAULT 0,
  FOREIGN KEY (`created_by`) REFERENCES `user_accounts`(`id`) ON DELETE SET NULL,
  FOREIGN KEY (`updated_by`) REFERENCES `user_accounts`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='QA取样记录'",
            ["self_check_records"] = @"
CREATE TABLE IF NOT EXISTS `self_check_records` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `self_check_id` VARCHAR(100) UNIQUE NOT NULL,
  `sampling_date` DATE,
  `report_date` DATE,
  `inspection_id` VARCHAR(100),
  `sample_batch` VARCHAR(100),
  `brand_series` VARCHAR(100),
  `sample_quantity` VARCHAR(100),
  `representative_quantity` VARCHAR(100),
  `sample_source` VARCHAR(100),
  `remark` TEXT,
  `node_code` VARCHAR(100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='自检记录'",
            ["self_check_items"] = @"
CREATE TABLE IF NOT EXISTS `self_check_items` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `self_check_id` VARCHAR(100) NOT NULL,
  `param_id` VARCHAR(100),
  `test_value` VARCHAR(200),
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='自检项目明细'",
            ["report_numbers"] = @"
CREATE TABLE IF NOT EXISTS `report_numbers` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `report_code` VARCHAR(100) UNIQUE NOT NULL,
  `production_date` DATE,
  `inspection_date` DATE,
  `report_date` DATE,
  `material_id` VARCHAR(100),
  `sample_batch` VARCHAR(100),
  `sample_quantity` VARCHAR(100),
  `batch_quantity` VARCHAR(100),
  `sample_source` VARCHAR(100),
  `report_result` VARCHAR(100),
  `remark` TEXT,
  `node_code` VARCHAR(100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='检验报告主表'",
            ["report_data_main"] = @"
CREATE TABLE IF NOT EXISTS `report_data_main` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `node_code` VARCHAR(100),
  `report_number` VARCHAR(100),
  `sample_name` VARCHAR(200),
  `sample_batch` VARCHAR(100),
  `type` VARCHAR(100),
  `frequency` VARCHAR(100),
  `testing_institution` VARCHAR(200),
  `testing_date` DATE,
  `report_date` DATE,
  `conclusion` TEXT,
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报告数据主表'",
            ["report_data_sub"] = @"
CREATE TABLE IF NOT EXISTS `report_data_sub` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `report_number` VARCHAR(100),
  `param_id` VARCHAR(100),
  `result` VARCHAR(200),
  `remark` TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报告数据明细'",
            ["type_inspection_records"] = @"
CREATE TABLE IF NOT EXISTS `type_inspection_records` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `inspection_id` VARCHAR(100) UNIQUE NOT NULL,
  `product_id` VARCHAR(100) NOT NULL,
  `batch_no` VARCHAR(100),
  `send_date` DATE,
  `report_date` DATE,
  `conclusion` VARCHAR(100) DEFAULT '合格',
  `testing_org` VARCHAR(200),
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='型式检验记录'",
            ["external_sampling"] = @"
CREATE TABLE IF NOT EXISTS `external_sampling` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `sampling_id` VARCHAR(100) NOT NULL,
  `sampling_date` DATE NOT NULL,
  `product_id` VARCHAR(100) NOT NULL DEFAULT '',
  `batch_no` VARCHAR(100),
  `product_quantity` VARCHAR(100),
  `sampling_quantity` VARCHAR(100),
  `sampling_price` VARCHAR(100),
  `monitor_type` VARCHAR(100),
  `sampling_org` VARCHAR(200),
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='外部抽检记录'",
            ["sample_retention_records"] = @"
CREATE TABLE IF NOT EXISTS `sample_retention_records` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `retention_code` VARCHAR(100) NOT NULL,
  `report_code` VARCHAR(100),
  `material_id` VARCHAR(100),
  `batch_number` VARCHAR(100),
  `retention_date` DATE,
  `retention_person` VARCHAR(100),
  `retention_deadline` DATE,
  `retention_location` VARCHAR(200),
  `retention_quantity` INT,
  `storage_condition` VARCHAR(200),
  `sample_status` VARCHAR(50) DEFAULT '在库',
  `dispose_date` DATE,
  `dispose_person` VARCHAR(100),
  `remark` TEXT,
  `created_at` DATETIME,
  `updated_at` DATETIME,
  FOREIGN KEY (`report_code`) REFERENCES `report_numbers`(`report_code`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='留样记录'",
            ["report_distribution"] = @"
CREATE TABLE IF NOT EXISTS `report_distribution` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `report_code` VARCHAR(100) NOT NULL,
  `distribution_date` DATE,
  `distributor` VARCHAR(100),
  `recipient` VARCHAR(100),
  `receive_date` DATE,
  `acceptor` VARCHAR(100),
  `is_received` TINYINT(1) DEFAULT 0,
  `accept_date` DATE,
  `is_accepted` TINYINT(1) DEFAULT 0,
  `remarks` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `uk_report_dist` (`report_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报告发放记录'",
            ["qa_supervision"] = @"
CREATE TABLE IF NOT EXISTS `qa_supervision` (
  `supervision_id` VARCHAR(100) PRIMARY KEY,
  `discovery_date` DATE NOT NULL,
  `project_category` VARCHAR(100),
  `project_name` VARCHAR(200),
  `batch_number` VARCHAR(100),
  `quantity` VARCHAR(100),
  `non_compliance` TEXT,
  `rectification_actions` TEXT,
  `rectification_deadline` DATE,
  `rectification_result` VARCHAR(100),
  `supervisor` VARCHAR(100),
  `is_reviewed` TINYINT(1) DEFAULT 0,
  `remarks` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `created_by` VARCHAR(100),
  `updated_by` VARCHAR(100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='QA质量监督整改'",
            ["order_forms"] = @"
CREATE TABLE IF NOT EXISTS `order_forms` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `order_id` VARCHAR(100) UNIQUE NOT NULL,
  `order_date` DATE NOT NULL,
  `customer_name` VARCHAR(200) NOT NULL,
  `contact_person` VARCHAR(100),
  `contact_phone` VARCHAR(50),
  `product_name` VARCHAR(200) NOT NULL,
  `product_spec` VARCHAR(200),
  `order_quantity` VARCHAR(100),
  `order_type` VARCHAR(100),
  `inspection_items` TEXT,
  `inspection_standard` VARCHAR(200),
  `required_date` DATE,
  `actual_date` DATE,
  `order_status` VARCHAR(100),
  `inspection_fee` VARCHAR(100),
  `payment_status` VARCHAR(100),
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='委托检验单'",
            ["process_data_main"] = @"
CREATE TABLE IF NOT EXISTS `process_data_main` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `process_data_id` VARCHAR(100) UNIQUE NOT NULL,
  `process_step_id` VARCHAR(100) NOT NULL,
  `batch_no` VARCHAR(100) NOT NULL,
  `production_date` DATE,
  `end_date` DATE,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`process_step_id`) REFERENCES `process_params_main`(`process_step_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序生产数据主表'",
            ["process_data_detail"] = @"
CREATE TABLE IF NOT EXISTS `process_data_detail` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `process_data_id` VARCHAR(100) NOT NULL,
  `param_id` VARCHAR(100) NOT NULL,
  `result_data` TEXT,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`process_data_id`) REFERENCES `process_data_main`(`process_data_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序生产数据明细'",
            ["overpacking_records"] = @"
CREATE TABLE IF NOT EXISTS `overpacking_records` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `test_id` VARCHAR(100) UNIQUE NOT NULL,
  `test_date` DATE,
  `product_name` VARCHAR(200),
  `brand_series` VARCHAR(100),
  `shape_type` VARCHAR(100),
  `dimensions` VARCHAR(200),
  `package_layers` INT DEFAULT 0,
  `package_weight` DECIMAL(12,4) DEFAULT 0,
  `package_cost` DECIMAL(12,4) DEFAULT 0,
  `sales_price` DECIMAL(12,4) DEFAULT 0,
  `material` VARCHAR(100),
  `is_mixed` TINYINT(1) DEFAULT 0,
  `is_freeze_dried` TINYINT(1) DEFAULT 0,
  `process_type` VARCHAR(100),
  `conclusion` VARCHAR(100),
  `remarks` TEXT,
  `inner_items_json` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='过度包装检测记录'",
            ["exam_question_bank"] = @"
CREATE TABLE IF NOT EXISTS `exam_question_bank` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `question_type` VARCHAR(50) NOT NULL DEFAULT '单选题',
  `question_content` TEXT NOT NULL,
  `options_json` TEXT,
  `answer` TEXT NOT NULL,
  `analysis` TEXT,
  `category` VARCHAR(100),
  `difficulty` VARCHAR(50) DEFAULT '中等',
  `is_enabled` TINYINT(1) DEFAULT 1,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='考试题库'",
            ["exam_config"] = @"
CREATE TABLE IF NOT EXISTS `exam_config` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `config_id` VARCHAR(100),
  `node_code` VARCHAR(100),
  `exam_name` VARCHAR(200) NOT NULL,
  `exam_type` VARCHAR(100) NOT NULL DEFAULT '电脑考试',
  `department` VARCHAR(100),
  `total_score` INT DEFAULT 100,
  `pass_score` INT DEFAULT 60,
  `duration_minutes` INT DEFAULT 60,
  `judge_count` INT DEFAULT 0,
  `judge_single_score` DECIMAL(10,2) DEFAULT 0,
  `single_count` INT DEFAULT 0,
  `single_single_score` DECIMAL(10,2) DEFAULT 0,
  `multi_count` INT DEFAULT 0,
  `multi_single_score` DECIMAL(10,2) DEFAULT 0,
  `essay_count` INT DEFAULT 0,
  `essay_single_score` DECIMAL(10,2) DEFAULT 0,
  `category_filter` TEXT,
  `difficulty_filter` TEXT,
  `is_enabled` TINYINT(1) DEFAULT 1,
  `remark` TEXT,
  `created_by` VARCHAR(100),
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='考试配置'",
            ["exam_record"] = @"
CREATE TABLE IF NOT EXISTS `exam_record` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `config_id` BIGINT,
  `examinee_name` VARCHAR(100) NOT NULL,
  `department` VARCHAR(100),
  `exam_type` VARCHAR(100) NOT NULL DEFAULT '电脑考试',
  `start_time` DATETIME,
  `end_time` DATETIME,
  `total_score` DECIMAL(10,2) DEFAULT 0,
  `is_passed` TINYINT(1) DEFAULT 0,
  `status` VARCHAR(50) DEFAULT '未开始',
  `answers_json` TEXT,
  `auto_grade_result` TEXT,
  `remark` TEXT,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (`config_id`) REFERENCES `exam_config`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='考生考试记录'",
            ["exam_detail"] = @"
CREATE TABLE IF NOT EXISTS `exam_detail` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `record_id` BIGINT NOT NULL,
  `question_id` BIGINT,
  `question_type` VARCHAR(50) NOT NULL,
  `question_content` TEXT NOT NULL,
  `options_json` TEXT,
  `correct_answer` TEXT NOT NULL,
  `user_answer` TEXT,
  `is_correct` TINYINT(1) DEFAULT 0,
  `score` DECIMAL(10,2) DEFAULT 0,
  `sort_order` INT DEFAULT 0,
  FOREIGN KEY (`record_id`) REFERENCES `exam_record`(`id`) ON DELETE CASCADE,
  FOREIGN KEY (`question_id`) REFERENCES `exam_question_bank`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='考试单题详情'"
        };
        #endregion

        #region 全部索引（已补齐 report_data_main.sample_name）
        private static readonly List<string> AllIndexSqls = new()
        {
            // 系统索引
            "CREATE INDEX IF NOT EXISTS idx_permissions_key ON permissions(permission_key);",
            "CREATE INDEX IF NOT EXISTS idx_role_permissions_role ON role_permissions(role_id);",
            "CREATE INDEX IF NOT EXISTS idx_role_permissions_perm ON role_permissions(permission_id);",
            "CREATE INDEX IF NOT EXISTS idx_user_accounts_username ON user_accounts(username);",
            "CREATE INDEX IF NOT EXISTS idx_user_accounts_role_id ON user_accounts(role_id);",

            // 基础数据索引
            "CREATE INDEX IF NOT EXISTS idx_tree_nodes_parent ON tree_nodes(tree_key, parent_code, sort_order);",
            "CREATE INDEX IF NOT EXISTS idx_product_infos_node_code ON product_infos(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_product_infos_code ON product_infos(product_code);",
            "CREATE INDEX IF NOT EXISTS idx_product_items_pid ON product_items(product_id);",
            "CREATE INDEX IF NOT EXISTS idx_product_barcodes_barcode_id ON product_barcodes(barcode_id);",
            "CREATE INDEX IF NOT EXISTS idx_product_barcodes_company_code ON product_barcodes(company_code);",
            "CREATE INDEX IF NOT EXISTS idx_product_barcodes_barcode_number ON product_barcodes(barcode_number);",
            "CREATE INDEX IF NOT EXISTS idx_employee_infos_employee_name ON employee_infos(employee_name);",
            "CREATE INDEX IF NOT EXISTS idx_employee_infos_department ON employee_infos(department);",
            "CREATE INDEX IF NOT EXISTS idx_document_files_node_code ON document_files(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_document_files_file_name ON document_files(file_name);",
            "CREATE INDEX IF NOT EXISTS idx_document_files_file_code ON document_files(file_code);",
            "CREATE INDEX IF NOT EXISTS idx_document_files_created_at ON document_files(created_at);",
            "CREATE INDEX IF NOT EXISTS idx_document_files_file_unique_id ON document_files(file_unique_id);",

            // 配置索引
            "CREATE INDEX IF NOT EXISTS idx_inspection_params_node_code ON inspection_params(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_inspection_param_items_inspection ON inspection_param_items(inspection_id);",

            // 样品业务索引
            "CREATE INDEX IF NOT EXISTS idx_self_check_records_node_code ON self_check_records(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_self_check_records_node_sampling_id ON self_check_records(node_code, sampling_date DESC, self_check_id DESC);",
            "CREATE INDEX IF NOT EXISTS idx_self_check_records_sampling_id ON self_check_records(sampling_date DESC, self_check_id DESC);",
            "CREATE INDEX IF NOT EXISTS idx_self_check_records_inspection_id ON self_check_records(inspection_id);",
            "CREATE INDEX IF NOT EXISTS idx_self_check_records_report_date ON self_check_records(report_date);",
            "CREATE INDEX IF NOT EXISTS idx_sample_receive_send_receive_date_id ON sample_receive_send(receive_send_date DESC, receive_send_id DESC);",
            "CREATE INDEX IF NOT EXISTS idx_sample_receive_send_inspection_date ON sample_receive_send(inspection_date);",
            "CREATE INDEX IF NOT EXISTS idx_sample_receive_send_report_date ON sample_receive_send(report_date);",
            "CREATE INDEX IF NOT EXISTS idx_sample_receive_send_node_code ON sample_receive_send(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_material_infos_material_id ON material_infos(material_id);",
            "CREATE INDEX IF NOT EXISTS idx_material_infos_material_name ON material_infos(material_name);",
            "CREATE INDEX IF NOT EXISTS idx_material_infos_material_code ON material_infos(material_code);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_node_code ON qa_sampling_records(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_sampling_date ON qa_sampling_records(sampling_date);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_inspection_date ON qa_sampling_records(inspection_date);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_sample_name ON qa_sampling_records(sample_name);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_sample_batch ON qa_sampling_records(sample_batch);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_is_deleted ON qa_sampling_records(is_deleted);",
            "CREATE INDEX IF NOT EXISTS idx_qa_sampling_records_created_at ON qa_sampling_records(created_at);",

            // 检验业务索引（新增 idx_report_data_main_sample_name）
            "CREATE INDEX IF NOT EXISTS idx_report_numbers_node_code ON report_numbers(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_report_numbers_report_date_code ON report_numbers(report_date DESC, report_code DESC);",
            "CREATE INDEX IF NOT EXISTS idx_report_numbers_node_date_code ON report_numbers(node_code, report_date DESC, report_code DESC);",
            "CREATE INDEX IF NOT EXISTS idx_report_numbers_material_id ON report_numbers(material_id);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_main_node_code ON report_data_main(node_code);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_main_report_date_code ON report_data_main(report_date DESC, report_number DESC);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_main_node_date_code ON report_data_main(node_code, report_date DESC, report_number DESC);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_main_sample_name ON report_data_main(sample_name);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_sub_report_number ON report_data_sub(report_number);",
            "CREATE INDEX IF NOT EXISTS idx_report_data_sub_param_id ON report_data_sub(param_id);",
            "CREATE INDEX IF NOT EXISTS idx_type_inspection_inspection_id ON type_inspection_records(inspection_id);",
            "CREATE INDEX IF NOT EXISTS idx_type_inspection_product_id ON type_inspection_records(product_id);",
            "CREATE INDEX IF NOT EXISTS idx_type_inspection_send_date ON type_inspection_records(send_date);",
            "CREATE INDEX IF NOT EXISTS idx_external_sampling_sampling_id ON external_sampling(sampling_id);",
            "CREATE INDEX IF NOT EXISTS idx_external_sampling_sampling_date ON external_sampling(sampling_date);",
            "CREATE INDEX IF NOT EXISTS idx_external_sampling_product_id ON external_sampling(product_id);",
            "CREATE INDEX IF NOT EXISTS idx_external_sampling_batch_no ON external_sampling(batch_no);",
            "CREATE INDEX IF NOT EXISTS idx_external_sampling_created_at ON external_sampling(created_at);",

            // 留样索引
            "CREATE INDEX IF NOT EXISTS idx_sample_retention_retention_code ON sample_retention_records(retention_code);",
            "CREATE INDEX IF NOT EXISTS idx_sample_retention_report_code ON sample_retention_records(report_code);",
            "CREATE INDEX IF NOT EXISTS idx_sample_retention_retention_date ON sample_retention_records(retention_date);",
            "CREATE INDEX IF NOT EXISTS idx_sample_retention_retention_deadline ON sample_retention_records(retention_deadline);",
            "CREATE INDEX IF NOT EXISTS idx_sample_retention_sample_status ON sample_retention_records(sample_status);",

            // QA监督索引
            "CREATE INDEX IF NOT EXISTS idx_qa_supervision_discovery_date ON qa_supervision(discovery_date);",
            "CREATE INDEX IF NOT EXISTS idx_qa_supervision_project_category ON qa_supervision(project_category);",
            "CREATE INDEX IF NOT EXISTS idx_qa_supervision_is_reviewed ON qa_supervision(is_reviewed);",
            "CREATE INDEX IF NOT EXISTS idx_qa_supervision_supervisor ON qa_supervision(supervisor);",
            "CREATE INDEX IF NOT EXISTS idx_qa_supervision_batch_number ON qa_supervision(batch_number);",

            // 委托单索引
            "CREATE INDEX IF NOT EXISTS idx_order_forms_order_id ON order_forms(order_id);",
            "CREATE INDEX IF NOT EXISTS idx_order_forms_order_date ON order_forms(order_date);",
            "CREATE INDEX IF NOT EXISTS idx_order_forms_customer_name ON order_forms(customer_name);",
            "CREATE INDEX IF NOT EXISTS idx_order_forms_order_status ON order_forms(order_status);",
            "CREATE INDEX IF NOT EXISTS idx_order_forms_payment_status ON order_forms(payment_status);",
            "CREATE INDEX IF NOT EXISTS idx_order_forms_required_date ON order_forms(required_date);",

            // 包装索引
            "CREATE INDEX IF NOT EXISTS idx_overpacking_records_test_id ON overpacking_records(test_id);",
            "CREATE INDEX IF NOT EXISTS idx_overpacking_records_test_date ON overpacking_records(test_date);",

            // 考试索引
            "CREATE INDEX IF NOT EXISTS idx_exam_question_bank_question_type ON exam_question_bank(question_type);",
            "CREATE INDEX IF NOT EXISTS idx_exam_question_bank_category ON exam_question_bank(category);",
            "CREATE INDEX IF NOT EXISTS idx_exam_question_bank_difficulty ON exam_question_bank(difficulty);",
            "CREATE INDEX IF NOT EXISTS idx_exam_question_bank_is_enabled ON exam_question_bank(is_enabled);",
            "CREATE INDEX IF NOT EXISTS idx_exam_config_exam_type ON exam_config(exam_type);",
            "CREATE INDEX IF NOT EXISTS idx_exam_config_department ON exam_config(department);",
            "CREATE INDEX IF NOT EXISTS idx_exam_record_config_id ON exam_record(config_id);",
            "CREATE INDEX IF NOT EXISTS idx_exam_record_examinee_name ON exam_record(examinee_name);",
            "CREATE INDEX IF NOT EXISTS idx_exam_record_department ON exam_record(department);",
            "CREATE INDEX IF NOT EXISTS idx_exam_record_status ON exam_record(status);",
            "CREATE INDEX IF NOT EXISTS idx_exam_detail_record_id ON exam_detail(record_id);",
            "CREATE INDEX IF NOT EXISTS idx_exam_detail_question_id ON exam_detail(question_id);",

            // 工艺索引
            "CREATE INDEX IF NOT EXISTS idx_process_data_main_production_date_id ON process_data_main(production_date DESC, id DESC);",
            "CREATE INDEX IF NOT EXISTS idx_process_data_main_step_date_id ON process_data_main(process_step_id, production_date DESC, id DESC);",
            "CREATE INDEX IF NOT EXISTS idx_process_data_main_batch_no ON process_data_main(batch_no);"
        };
        #endregion

        #region 对外初始化入口
        /// <summary>
        /// 程序启动调用入口，自动创建库、全部表、所有索引
        /// 幂等安全，重复执行不会报错
        /// </summary>
        public static void InitDbOnce()
        {
            var cfg = LoadMysqlConfig();
            CreateDatabaseIfNotExists(cfg);
            var connStr = GetConnString(cfg);

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // 按依赖顺序建表：系统 → 基础 → 配置 → 业务
            void CreateTableBatch(Dictionary<string, string> tables)
            {
                foreach (var sql in tables.Values)
                {
                    ExecuteSql(conn, sql);
                }
            }

            CreateTableBatch(SystemTables);
            CreateTableBatch(MasterTables);
            CreateTableBatch(ConfigTables);
            CreateTableBatch(TransactionTables);

            // 批量创建所有索引
            foreach (var idxSql in AllIndexSqls)
            {
                ExecuteSql(conn, idxSql);
            }

            Console.WriteLine("数据库初始化完成：库、全部数据表、优化索引已创建完毕");
        }
        #endregion
    }

    /// <summary>
    /// MySQL连接配置实体类
    /// </summary>
    public class MySqlConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
    }
}