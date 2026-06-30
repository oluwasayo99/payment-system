-- This file is commented out because data is now generated via Python script
-- Use Demo.Inventory/scripts/generate_data.py instead

/*
-- Generate 100 categories
INSERT INTO categories (id, name, description)
SELECT 
    gen_random_uuid(),
    'Category ' || i,
    'Description for category ' || i
FROM generate_series(1, 100) AS i;

-- Generate 200 users
INSERT INTO users (id, username, email)
SELECT 
    gen_random_uuid(),
    'user' || i,
    'user' || i || '@example.com'
FROM generate_series(1, 200) AS i;

-- Generate 2,000 products (20 products per category)
INSERT INTO products (id, name, description, price, category_id, created_at)
SELECT 
    gen_random_uuid(),
    'Product ' || i,
    'Description for product ' || i,
    (random() * 100000 + 1000)::numeric(15,2),
    c.id,
    CURRENT_TIMESTAMP - (random() * INTERVAL '365 days')
FROM generate_series(1, 2000) AS i
CROSS JOIN LATERAL (
    SELECT id FROM categories 
    ORDER BY random() 
    LIMIT 1
) AS c;

-- Generate 5,000 reviews
INSERT INTO reviews (id, product_id, user_id, rating, comment, created_at)
SELECT 
    gen_random_uuid(),
    p.id,
    u.id,
    floor(random() * 5 + 1)::int,
    'Review comment ' || i,
    CURRENT_TIMESTAMP - (random() * INTERVAL '180 days')
FROM generate_series(1, 5000) AS i
CROSS JOIN LATERAL (
    SELECT id FROM products ORDER BY random() LIMIT 1
) AS p
CROSS JOIN LATERAL (
    SELECT id FROM users ORDER BY random() LIMIT 1
) AS u;

-- Generate 500 orders
INSERT INTO orders (id, user_id, order_status, total, created_at)
SELECT 
    gen_random_uuid(),
    u.id,
    CASE floor(random() * 4)::int 
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Delivered'
        ELSE 'Cancelled'
    END,
    0.00,
    CURRENT_TIMESTAMP - (random() * INTERVAL '365 days')
FROM generate_series(1, 500) AS i
CROSS JOIN LATERAL (
    SELECT id FROM users ORDER BY random() LIMIT 1
) AS u;

-- Generate 1,500 order items (3 items per order on average)
INSERT INTO order_items (id, order_id, product_id, quantity, price)
SELECT 
    gen_random_uuid(),
    o.id,
    p.id,
    floor(random() * 5 + 1)::int,
    p.price
FROM generate_series(1, 1500) AS i
CROSS JOIN LATERAL (
    SELECT id FROM orders ORDER BY random() LIMIT 1
) AS o
CROSS JOIN LATERAL (
    SELECT id, price FROM products ORDER BY random() LIMIT 1
) AS p;

-- Update order totals based on order items
UPDATE orders o
SET total = (
    SELECT COALESCE(SUM(oi.quantity * oi.price), 0)
    FROM order_items oi
    WHERE oi.order_id = o.id
);
*/
