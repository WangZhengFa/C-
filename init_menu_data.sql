-- 初始化 system_menu 菜单树数据
-- 对应 Python 版本的完整菜单结构

-- 清空现有数据（可选）
-- DELETE FROM tree_nodes WHERE tree_key = 'system_menu';

-- 根节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'quality_assurance', '', '质量保证', 1, NULL),
('system_menu', 'quality_control', '', '质量控制', 2, NULL),
('system_menu', 'quality_management', '', '质量管理', 3, NULL),
('system_menu', 'verification', '', '验证管理', 4, NULL),
('system_menu', 'personnel', '', '人员管理', 5, NULL),
('system_menu', 'basic_data', '', '基础资料', 6, NULL),
('system_menu', 'common_tools', '', '常用工具', 7, NULL),
('system_menu', 'system_settings', '', '系统设置', 8, NULL),
('system_menu', 'system_management', '', '系统管理', 9, NULL),
('system_menu', 'help', '', '帮助', 10, NULL);

-- 质量保证的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'sample_record', 'quality_assurance', '取样记录', 1, '{"component_path": "SampleRecordPage", "csharp_class": "食品信息管理系统.Views.Pages.SamplingRecordPage"}'),
('system_menu', 'quality_supervision', 'quality_assurance', '质量监督', 2, '{"component_path": "QualitySupervisionPage", "csharp_class": "食品信息管理系统.Views.Pages.QualitySupervisionPage"}'),
('system_menu', 'report_issuance', 'quality_assurance', '报告发放', 3, '{"component_path": "ReportIssuancePage"}'),
('system_menu', 'type_inspection', 'quality_assurance', '型式检验', 4, '{"component_path": "TypeInspectionPage"}'),
('system_menu', 'external_sampling', 'quality_assurance', '外部抽检', 5, '{"component_path": "ExternalSamplingPage"}');

-- 质量控制的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'sample_distribution', 'quality_control', '样品分发', 1, '{"component_path": "SampleDistributionPage"}'),
('system_menu', 'self_check_data', 'quality_control', '自检数据', 2, '{"component_path": "SelfCheckDataPage"}'),
('system_menu', 'report_numbering', 'quality_control', '报告编号', 3, '{"component_path": "ReportNumberingPage"}'),
('system_menu', 'retention_management', 'quality_control', '留样管理', 4, '{"component_path": "RetentionManagementPage"}'),
('system_menu', 'overpackaging', 'quality_control', '过度包装', 5, '{"component_path": "OverpackagingPage"}'),
('system_menu', 'nutrition_label', 'quality_control', '营养标签', 6, '{"component_path": "NutritionLabelPage"}');

-- 质量管理的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'product_barcode', 'quality_management', '产品条码', 1, '{"component_path": "ProductBarcodePage"}'),
('system_menu', 'document_management', 'quality_management', '文件管理', 2, '{"component_path": "DocumentManagementPage"}'),
('system_menu', 'commissioned_order', 'quality_management', '委托订单', 3, '{"component_path": "CommissionedOrderPage"}');

-- 验证管理的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'report_data', 'verification', '报告数据', 1, '{"component_path": "ReportDataPage"}'),
('system_menu', 'data_analysis', 'verification', '数据分析', 2, '{"component_path": "DataAnalysisPage"}'),
('system_menu', 'process_data', 'verification', '工序数据', 3, '{"component_path": "ProcessDataPage"}');

-- 人员管理的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'employee_info', 'personnel', '员工信息', 1, '{"component_path": "EmployeeInfoPage"}'),
('system_menu', 'exam_management', 'personnel', '考试管理', 2, '{"component_path": "ExamManagementPage"}');

-- 基础资料的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'product_info', 'basic_data', '产品信息', 1, '{"component_path": "ProductInfoPage"}'),
('system_menu', 'material_info', 'basic_data', '物料信息', 2, '{"component_path": "MaterialInfoPage"}'),
('system_menu', 'inspection_params', 'basic_data', '检品参数', 3, '{"component_path": "InspectionParamsPage"}'),
('system_menu', 'nutrition_params', 'basic_data', '营养参数', 4, '{"component_path": "NutritionParamsPage"}'),
('system_menu', 'exam_question_bank', 'basic_data', '考试题库', 5, '{"component_path": "ExamQuestionBankPage"}'),
('system_menu', 'standard_regulations', 'basic_data', '标规法规', 6, '{"component_path": "StandardRegulationsPage"}'),
('system_menu', 'general_dictionary', 'basic_data', '通用字典', 7, '{"component_path": "GeneralDictionaryPage"}'),
('system_menu', 'customer_info', 'basic_data', '客户信息', 8, '{"component_path": "CustomerInfoPage"}'),
('system_menu', 'process_params', 'basic_data', '工序参数', 9, '{"component_path": "ProcessParamsPage"}'),
('system_menu', 'food_categories', 'basic_data', '食品分类', 10, '{"component_path": "FoodCategoriesPage"}');

-- 常用工具的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'seal_stamp', 'common_tools', '加盖公章', 1, '{"component_path": "SealStampPage"}');

-- 系统设置的子节点
INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
('system_menu', 'system_config', 'system_settings', '系统配置', 1, '{"component_path": "SystemConfigPage"}'),
('system_menu', 'version_management', 'system_settings', '版本管理', 2, '{"component_path": "VersionManagementPage"}'),
('system_menu', 'update_management', 'system_settings', '更新管理', 3, '{"component_path": "UpdateManagementPage"}'),
('system_menu', 'user_management', 'system_settings', '用户管理', 4, '{"component_path": "UserManagementPage"}');

-- 系统管理的子节点（如果有）
-- INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
-- ('system_menu', 'xxx', 'system_management', 'xxx', 1, '{"component_path": "XxxPage"}');

-- 帮助的子节点（如果有）
-- INSERT INTO tree_nodes (tree_key, node_code, parent_code, title, sort_order, payload_json) VALUES
-- ('system_menu', 'xxx', 'help', 'xxx', 1, '{"component_path": "XxxPage"}');
