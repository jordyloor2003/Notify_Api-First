-- ==============================================================================
-- Script de Inicialización de Esquema Relacional para PostgreSQL 16
-- Plataforma: Notify API Platform
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Tabla Applications (Clientes B2B)
CREATE TABLE IF NOT EXISTS applications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    client_id VARCHAR(100) UNIQUE NOT NULL,
    client_secret_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'APPLICATION',
    tier VARCHAR(50) NOT NULL DEFAULT 'Growth',
    rate_limit_rps INT NOT NULL DEFAULT 50,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Tabla Templates (Plantillas de Mensajería)
CREATE TABLE IF NOT EXISTS templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    application_id UUID NOT NULL,
    code VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(150) NOT NULL,
    channel VARCHAR(20) NOT NULL,
    subject VARCHAR(255),
    body_template TEXT NOT NULL,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. Tabla Notifications (Historial Transaccional)
CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    application_id UUID NOT NULL,
    channel VARCHAR(20) NOT NULL,
    recipient VARCHAR(255) NOT NULL,
    subject VARCHAR(255),
    body TEXT NOT NULL,
    template_code VARCHAR(100),
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    idempotency_key VARCHAR(100) UNIQUE,
    retry_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMP WITH TIME ZONE,
    last_error TEXT
);

-- 4. Tabla Notification Attempts (Trazabilidad de Intentos de Envío)
CREATE TABLE IF NOT EXISTS notification_attempts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    notification_id UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    attempt_number INT NOT NULL,
    provider VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    http_status_code INT NOT NULL,
    latency_ms INT NOT NULL,
    attempted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    error_detail TEXT
);

-- 5. Tabla Audit Logs (Bitácora Inmutable)
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    action VARCHAR(100) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    performed_by VARCHAR(100) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    details JSONB
);

-- Datos Semilla Iniciales
INSERT INTO applications (id, name, client_id, client_secret_hash, role, tier, rate_limit_rps)
VALUES 
    ('c8a4b679-b1d5-4422-b91c-148c3b4a2e11', 'Banca Móvil Producción', 'app_bancamovil_prod', 'sec_99a8b7c6d5e4f3a2b1c0', 'APPLICATION', 'Growth', 50),
    ('a1b2c3d4-e5f6-7890-abcd-ef1234567890', 'Panel de Administración', 'app_admin_master', 'sec_admin_master_12345', 'ADMIN', 'Enterprise', 200)
ON CONFLICT (client_id) DO NOTHING;

INSERT INTO templates (id, application_id, code, name, channel, subject, body_template, version)
VALUES 
    ('9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d', 'c8a4b679-b1d5-4422-b91c-148c3b4a2e11', 'WELCOME_USER', 'Plantilla de Bienvenida', 'EMAIL', 'Bienvenido {{nombre}} a Nuestra Plataforma', '<h1>Hola {{nombre}}</h1><p>Tu cuenta ha sido activada en Notify API.</p>', 1),
    ('e2a4c6e8-1122-3344-5566-778899aabbcc', 'c8a4b679-b1d5-4422-b91c-148c3b4a2e11', '2FA_CODE', 'Código de Verificación', 'SMS', NULL, 'Tu codigo de seguridad es: {{codigo}}. Valido por 5 minutos.', 1)
ON CONFLICT (code) DO NOTHING;
