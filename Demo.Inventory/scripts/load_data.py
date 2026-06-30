#!/usr/bin/env python3
"""
Inventory Data Loader
Loads CSV data into PostgreSQL database using COPY command.

Usage:
    python load_data.py --host localhost --port 5433 --dbname inventorydb
    python load_data.py --data-dir ./data --host localhost --port 5433
"""

import argparse
import sys
from pathlib import Path
from datetime import datetime
import psycopg2
from psycopg2.extras import execute_values
from tqdm import tqdm
import pandas as pd

def create_tables(conn):
    """Create database tables if they don't exist"""
    print(" Creating tables...")
    
    with conn.cursor() as cur:
        # Enable uuid extension
        cur.execute("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";")
        
        # Categories
        cur.execute("""
            CREATE TABLE IF NOT EXISTS categories (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(100) NOT NULL,
                description TEXT
            );
        """)
        
        # Users
        cur.execute("""
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                username VARCHAR(100) NOT NULL,
                email VARCHAR(200) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        """)
        
        # Products
        cur.execute("""
            CREATE TABLE IF NOT EXISTS products (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(200) NOT NULL,
                description TEXT,
                price DECIMAL(15, 2) NOT NULL,
                category_id UUID REFERENCES categories(id),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        """)
        
        # Reviews
        cur.execute("""
            CREATE TABLE IF NOT EXISTS reviews (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                product_id UUID REFERENCES products(id) ON DELETE CASCADE,
                user_id UUID REFERENCES users(id) ON DELETE CASCADE,
                rating INTEGER CHECK (rating >= 1 AND rating <= 5),
                comment TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        """)
        
        # Orders
        cur.execute("""
            CREATE TABLE IF NOT EXISTS orders (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID REFERENCES users(id) ON DELETE CASCADE,
                order_status VARCHAR(50) NOT NULL,
                total DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        """)
        
        # Order Items
        cur.execute("""
            CREATE TABLE IF NOT EXISTS order_items (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                order_id UUID REFERENCES orders(id) ON DELETE CASCADE,
                product_id UUID REFERENCES products(id),
                quantity INTEGER NOT NULL,
                price DECIMAL(15, 2) NOT NULL
            );
        """)
        
        # Create indexes
        cur.execute("CREATE INDEX IF NOT EXISTS idx_products_category_id ON products(category_id);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_reviews_product_id ON reviews(product_id);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_reviews_user_id ON reviews(user_id);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items(order_id);")
        cur.execute("CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON order_items(product_id);")
        
        conn.commit()
    print(" Tables created")

def clear_tables(conn):
    """Clear all data from tables"""
    print("  Clearing existing data...")
    with conn.cursor() as cur:
        cur.execute("TRUNCATE TABLE order_items, orders, reviews, products, users, categories CASCADE;")
        conn.commit()
    print(" Tables cleared")

def load_csv_to_postgres(conn, table_name, csv_file, columns):
    """Load CSV file into PostgreSQL table using COPY"""
    df = pd.read_csv(csv_file)
    total_rows = len(df)
    
    print(f"\n Loading {total_rows} rows into {table_name}...")
    
    with conn.cursor() as cur:
        # Use COPY for fast bulk insert
        # Convert DataFrame to CSV in memory
        from io import StringIO
        buffer = StringIO()
        df.to_csv(buffer, index=False, header=False)
        buffer.seek(0)
        
        try:
            cur.copy_from(buffer, table_name, sep=',', columns=columns)
            conn.commit()
            print(f" Loaded {total_rows} rows into {table_name}")
        except Exception as e:
            conn.rollback()
            print(f" Error loading {table_name}: {e}")
            raise

def load_data_with_progress(conn, table_name, csv_file):
    """Load data with progress bar using upsert (INSERT ... ON CONFLICT DO NOTHING)"""
    df = pd.read_csv(csv_file)
    total_rows = len(df)
    
    print(f"\n Loading {total_rows} rows into {table_name} (upsert mode)...")
    
    # Convert DataFrame to list of tuples for execute_values
    columns = list(df.columns)
    data = [tuple(row) for row in df.values]
    
    with conn.cursor() as cur:
        # Get primary key column(s) for the table
        primary_keys = {
            'categories': 'id',
            'users': 'id',
            'products': 'id',
            'reviews': 'id',
            'orders': 'id',
            'order_items': 'id'
        }
        pk_column = primary_keys.get(table_name, 'id')
        
        # Insert in batches with progress bar using upsert
        batch_size = 10000
        inserted_count = 0
        skipped_count = 0
        
        for i in tqdm(range(0, len(data), batch_size), desc=f"Loading {table_name}"):
            batch = data[i:i+batch_size]
            placeholders = ','.join(['%s'] * len(columns))
            
            # Use INSERT ... ON CONFLICT DO NOTHING to skip duplicates
            query = f"""
                INSERT INTO {table_name} ({','.join(columns)}) 
                VALUES ({placeholders})
                ON CONFLICT ({pk_column}) DO NOTHING
            """
            
            # Execute and count affected rows
            for row in batch:
                cur.execute(query, row)
                if cur.rowcount > 0:
                    inserted_count += 1
                else:
                    skipped_count += 1
        
        conn.commit()
    
    print(f" Inserted: {inserted_count}, Skipped: {skipped_count} duplicates")
    return inserted_count, skipped_count

def verify_data(conn):
    """Verify data loaded correctly"""
    print("\n Verifying data...")
    
    tables = ['categories', 'users', 'products', 'reviews', 'orders', 'order_items']
    
    with conn.cursor() as cur:
        for table in tables:
            cur.execute(f"SELECT COUNT(*) FROM {table};")
            count = cur.fetchone()[0]
            print(f"   {table}: {count} rows")

def main():
    parser = argparse.ArgumentParser(description='Load inventory data into PostgreSQL')
    parser.add_argument('--host', type=str, default='localhost',
                       help='PostgreSQL host')
    parser.add_argument('--port', type=int, default=5433,
                       help='PostgreSQL port')
    parser.add_argument('--dbname', type=str, default='inventorydb',
                       help='Database name')
    parser.add_argument('--user', type=str, default='postgres',
                       help='Database user')
    parser.add_argument('--password', type=str, default='password1234',
                       help='Database password')
    parser.add_argument('--data-dir', type=str, default='./data',
                       help='Directory containing CSV files')
    parser.add_argument('--clear', action='store_true',
                       help='Clear existing data before loading')
    parser.add_argument('--verify', action='store_true', default=True,
                       help='Verify data after loading')
    
    args = parser.parse_args()
    
    data_dir = Path(args.data_dir)
    if not data_dir.exists():
        print(f"[ERROR] Data directory not found: {data_dir}")
        print("[TIP] Run generate_data.py first to create CSV files")
        sys.exit(1)
    
    print("=" * 60)
    print("Inventory Data Loader")
    print("=" * 60)
    print(f"\nData directory: {data_dir.absolute()}")
    print(f"Database: {args.dbname}@{args.host}:{args.port}")
    print()
    
    # Connect to database
    print("Connecting to PostgreSQL...")
    try:
        conn = psycopg2.connect(
            host=args.host,
            port=args.port,
            dbname=args.dbname,
            user=args.user,
            password=args.password
        )
        print("[OK] Connected to PostgreSQL")
    except psycopg2.OperationalError as e:
        print(f"[ERROR] Failed to connect: {e}")
        print("\n[TIP] Tips:")
        print("   - Make sure PostgreSQL is running")
        print("   - Check if the database 'inventorydb' exists")
        print("   - Verify connection credentials")
        sys.exit(1)
    
    try:
        start_time = datetime.now()
        
        # Create tables
        create_tables(conn)
        
        # Clear tables if requested
        if args.clear:
            clear_tables(conn)
        
        # Load data
        print("\n" + "=" * 60)
        print(" Loading CSV Files (Upsert Mode - No Duplicates)")
        print("=" * 60)
        
        # Track totals across all tables
        total_stats = {'inserted': 0, 'skipped': 0}
        table_stats = {}
        
        # Load order matters due to foreign keys
        tables = [
            ('categories', 'categories.csv'),
            ('users', 'users.csv'),
            ('products', 'products.csv'),
            ('reviews', 'reviews.csv'),
            ('orders', 'orders.csv'),
            ('order_items', 'order_items.csv')
        ]
        
        for table_name, csv_name in tables:
            inserted, skipped = load_data_with_progress(conn, table_name, data_dir / csv_name)
            table_stats[table_name] = {'inserted': inserted, 'skipped': skipped}
            total_stats['inserted'] += inserted
            total_stats['skipped'] += skipped
        
        # Verify data
        if args.verify:
            print("\n" + "=" * 60)
            verify_data(conn)
        
        # Summary
        elapsed = datetime.now() - start_time
        print("\n" + "=" * 60)
        print(" Data Loading Complete!")
        print("=" * 60)
        print(f" Summary:")
        for table, stats in table_stats.items():
            print(f"   {table}: {stats['inserted']} inserted, {stats['skipped']} skipped")
        print(f"\n   Total: {total_stats['inserted']} rows inserted")
        print(f"         {total_stats['skipped']} duplicates skipped")
        print(f"\nTime elapsed: {elapsed}")
        print(f"\n You can now test the API endpoints!")
        print("=" * 60)
        
    except Exception as e:
        print(f"\n Error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    finally:
        conn.close()
        print("\n Database connection closed")

if __name__ == "__main__":
    main()
