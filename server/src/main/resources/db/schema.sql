CREATE TABLE IF NOT EXISTS student (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    student_number VARCHAR(50) NOT NULL,
    group_id BIGINT,
    total_score INT DEFAULT 0,
    avatar VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS score_record (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    student_id BIGINT NOT NULL,
    score_change INT NOT NULL,
    reason VARCHAR(500),
    category VARCHAR(50),
    operator_id BIGINT,
    can_quick_revert BOOLEAN DEFAULT TRUE,
    reverted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS student_group (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS evaluation_item (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    score_change INT NOT NULL,
    category VARCHAR(50),
    is_quick_access BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS settlement_record (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    period VARCHAR(50),
    snapshot_data CLOB,
    status INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS admin_settings (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    setting_key VARCHAR(100) NOT NULL,
    setting_value VARCHAR(2000),
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 初始管理员设置
MERGE INTO admin_settings (setting_key, setting_value) KEY(setting_key) VALUES ('admin_password', 'admin123');
MERGE INTO admin_settings (setting_key, setting_value) KEY(setting_key) VALUES ('admin_username', 'admin');
MERGE INTO admin_settings (setting_key, setting_value) KEY(setting_key) VALUES ('site_name', 'ClassIsScore');
MERGE INTO admin_settings (setting_key, setting_value) KEY(setting_key) VALUES ('default_score', '0');
MERGE INTO admin_settings (setting_key, setting_value) KEY(setting_key) VALUES ('enable_websocket', 'true');

-- 默认评估项
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (1, '按时交作业', 2, '作业', TRUE);
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (2, '未交作业', -2, '作业', TRUE);
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (3, '课堂积极发言', 1, '纪律', TRUE);
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (4, '课堂违纪', -1, '纪律', TRUE);
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (5, '参加活动', 3, '活动', FALSE);
MERGE INTO evaluation_item (id, name, score_change, category, is_quick_access) KEY(id) VALUES (6, '帮助同学', 2, '活动', FALSE);
