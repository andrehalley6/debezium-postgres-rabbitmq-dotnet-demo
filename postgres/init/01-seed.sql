-- Demo tables
CREATE TABLE IF NOT EXISTS products (
  id    SERIAL PRIMARY KEY,
  name  TEXT,
  sku   TEXT,
  price NUMERIC(10,2)
);

CREATE TABLE IF NOT EXISTS orders (
  id            SERIAL PRIMARY KEY,
  customer_name TEXT,
  order_date    TIMESTAMP,
  status        TEXT
);

-- Seed data (idempotent insert)
INSERT INTO products (name, sku, price) VALUES
  ('Widget', 'WGT-001', 9.99)
ON CONFLICT DO NOTHING;

INSERT INTO orders (customer_name, order_date, status) VALUES
  ('Alice', NOW(), 'NEW')
ON CONFLICT DO NOTHING;

-- Enable logical replication if not preset (safety net)
ALTER SYSTEM SET wal_level = logical;
SELECT pg_reload_conf();