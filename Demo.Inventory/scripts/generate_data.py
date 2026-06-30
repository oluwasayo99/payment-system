#!/usr/bin/env python3
"""
Inventory Data Generator
Generates realistic CSV data for the inventory system.

Usage:
    python generate_data.py --scale small    # 1K products
    python generate_data.py --scale medium   # 10K products
    python generate_data.py --scale large    # 100K products
    python generate_data.py --scale huge     # 1M products
    python generate_data.py --custom --products 50000 --users 5000
"""

import argparse
import uuid
import random
import shutil
from datetime import datetime, timedelta
from pathlib import Path
import pandas as pd
from faker import Faker
from tqdm import tqdm

fake = Faker()

# Configuration presets
SCALE_CONFIG = {
    'small': {
        'categories': 10,
        'users': 100,
        'products': 1000,
        'reviews_ratio': 0.8,  # 80% of products have reviews
        'orders_ratio': 0.1,   # orders = 10% of products
        'items_per_order': 3,
    },
    'medium': {
        'categories': 50,
        'users': 1000,
        'products': 10000,
        'reviews_ratio': 0.8,
        'orders_ratio': 0.1,
        'items_per_order': 3,
    },
    'large': {
        'categories': 100,
        'users': 10000,
        'products': 100000,
        'reviews_ratio': 0.8,
        'orders_ratio': 0.1,
        'items_per_order': 3,
    },
    'huge': {
        'categories': 100,
        'users': 100000,
        'products': 1000000,
        'reviews_ratio': 0.8,
        'orders_ratio': 0.1,
        'items_per_order': 3,
    },
}

PRODUCT_CATEGORIES = [
    # Electronics & Technology (1-15)
    "Electronics", "Computers", "Smartphones", "Tablets", "Laptops", "Cameras", "Audio Equipment",
    "Smart Home Devices", "Wearable Technology", "Gaming Consoles", "Drones", "Virtual Reality",
    "Networking Equipment", "Software", "Tech Accessories",
    # Fashion & Apparel (16-30)
    "Men's Clothing", "Women's Clothing", "Kids Clothing", "Shoes", "Accessories", "Jewelry",
    "Watches", "Bags & Luggage", "Sunglasses", "Activewear", "Formal Wear", "Casual Wear",
    "Underwear", "Socks", "Fashion Accessories",
    # Home & Living (31-45)
    "Furniture", "Home Decor", "Kitchen & Dining", "Bedding", "Bath", "Lighting", "Storage",
    "Cleaning Supplies", "Home Improvement", "Garden Tools", "Outdoor Furniture", "Rugs",
    "Curtains", "Wall Art", "Home Appliances",
    # Health & Beauty (46-55)
    "Skincare", "Makeup", "Hair Care", "Fragrances", "Personal Care", "Health Supplements",
    "Medical Supplies", "Fitness Equipment", "Yoga & Pilates", "Sports Nutrition",
    # Sports & Outdoors (56-65)
    "Sports Equipment", "Outdoor Gear", "Camping", "Hiking", "Cycling", "Water Sports",
    "Winter Sports", "Team Sports", "Exercise Equipment", "Sportswear",
    # Food & Beverages (66-75)
    "Groceries", "Snacks", "Beverages", "Coffee & Tea", "Wine & Spirits", "Organic Food",
    "Health Food", "Baby Food", "Pet Food", "Gourmet Food",
    # Entertainment & Media (76-85)
    "Books", "Movies", "Music", "Video Games", "Board Games", "Musical Instruments",
    "Party Supplies", "Collectibles", "Art Supplies", "Crafts",
    # Toys & Baby (86-92)
    "Toys", "Educational Toys", "Stuffed Animals", "Baby Gear", "Baby Care", "Maternity",
    "Kids Furniture",
    # Automotive & Industrial (93-100)
    "Car Accessories", "Car Electronics", "Motorcycle Gear", "Tools", "Hardware",
    "Safety Equipment", "Office Supplies", "Industrial Equipment"
]

PRODUCT_ADJECTIVES = [
    "Premium", "Professional", "Ultimate", "Deluxe", "Compact",
    "Wireless", "Smart", "Digital", "Portable", "Advanced",
    "Classic", "Modern", "Essential", "Pro", "Lite",
    "Heavy-Duty", "Lightweight", "Ergonomic", "Multi-functional", "High-Performance"
]

def generate_categories(count: int, output_dir: Path):
    """Generate categories CSV with realistic names"""
    print(f"\n📁 Generating {count} categories...")
    
    # If requesting more categories than we have names for, cycle through with numbers
    data = []
    for i in tqdm(range(count), desc="Categories"):
        if i < len(PRODUCT_CATEGORIES):
            name = PRODUCT_CATEGORIES[i]
        else:
            # Cycle through categories with a suffix for extras
            base_name = PRODUCT_CATEGORIES[i % len(PRODUCT_CATEGORIES)]
            name = f"{base_name} {i + 1}"
        
        data.append({
            'id': str(uuid.uuid4()),
            'name': name,
            'description': fake.sentence(nb_words=10)
        })
    
    df = pd.DataFrame(data)
    output_file = output_dir / "categories.csv"
    df.to_csv(output_file, index=False)
    print(f"✅ Categories saved to {output_file}")
    return df

def generate_users(count: int, output_dir: Path):
    """Generate users CSV"""
    print(f"\n👥 Generating {count} users...")
    
    data = []
    for i in tqdm(range(count), desc="Users"):
        data.append({
            'id': str(uuid.uuid4()),
            'username': fake.user_name() + str(random.randint(1, 9999)),
            'email': fake.email(),
            'created_at': fake.date_time_between(start_date='-2y', end_date='now').isoformat()
        })
    
    df = pd.DataFrame(data)
    output_file = output_dir / "users.csv"
    df.to_csv(output_file, index=False)
    print(f"✅ Users saved to {output_file}")
    return df

def generate_products(count: int, categories_df: pd.DataFrame, output_dir: Path):
    """Generate products CSV"""
    print(f"\n📦 Generating {count} products...")
    
    category_ids = categories_df['id'].tolist()
    data = []
    
    for i in tqdm(range(count), desc="Products"):
        category_id = random.choice(category_ids)
        product_type = fake.word().title()
        adjective = random.choice(PRODUCT_ADJECTIVES)
        
        data.append({
            'id': str(uuid.uuid4()),
            'name': f"{adjective} {product_type} {fake.word().title()}",
            'description': fake.text(max_nb_chars=200),
            'price': round(random.uniform(10.0, 5000.0), 2),
            'category_id': category_id,
            'created_at': fake.date_time_between(start_date='-1y', end_date='now').isoformat()
        })
    
    df = pd.DataFrame(data)
    output_file = output_dir / "products.csv"
    df.to_csv(output_file, index=False)
    print(f"✅ Products saved to {output_file}")
    return df

def generate_reviews(products_df: pd.DataFrame, users_df: pd.DataFrame, ratio: float, output_dir: Path):
    """Generate reviews CSV"""
    count = int(len(products_df) * ratio)
    print(f"\n⭐ Generating {count} reviews...")
    
    product_ids = products_df['id'].tolist()
    user_ids = users_df['id'].tolist()
    
    data = []
    for i in tqdm(range(count), desc="Reviews"):
        data.append({
            'id': str(uuid.uuid4()),
            'product_id': random.choice(product_ids),
            'user_id': random.choice(user_ids),
            'rating': random.randint(1, 5),
            'comment': fake.paragraph(nb_sentences=2),
            'created_at': fake.date_time_between(start_date='-6m', end_date='now').isoformat()
        })
    
    df = pd.DataFrame(data)
    output_file = output_dir / "reviews.csv"
    df.to_csv(output_file, index=False)
    print(f"✅ Reviews saved to {output_file}")
    return df

def generate_orders(users_df: pd.DataFrame, products_df: pd.DataFrame, ratio: float, items_per_order: int, output_dir: Path):
    """Generate orders and order_items CSVs - 80% of users have orders"""
    count = int(len(products_df) * ratio)
    print(f"\n📋 Generating {count} orders (80% of users will have orders)...")
    
    user_ids = users_df['id'].tolist()
    product_ids = products_df['id'].tolist()
    
    # Select 80% of users to have orders (realistic distribution)
    total_users = len(user_ids)
    users_with_orders_count = int(total_users * 0.8)
    selected_user_ids = random.sample(user_ids, users_with_orders_count)
    
    order_statuses = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled']
    
    orders_data = []
    order_items_data = []
    
    for i in tqdm(range(count), desc="Orders"):
        order_id = str(uuid.uuid4())
        # Only select from users who have orders (80% of total)
        user_id = random.choice(selected_user_ids)
        order_status = random.choice(order_statuses)
        created_at = fake.date_time_between(start_date='-1y', end_date='now')
        
        # Generate order items
        total = 0.0
        num_items = random.randint(1, items_per_order * 2)  # Variable items per order
        
        for j in range(num_items):
            product_id = random.choice(product_ids)
            quantity = random.randint(1, 5)
            price = round(random.uniform(10.0, 5000.0), 2)
            item_total = quantity * price
            total += item_total
            
            order_items_data.append({
                'id': str(uuid.uuid4()),
                'order_id': order_id,
                'product_id': product_id,
                'quantity': quantity,
                'price': price
            })
        
        orders_data.append({
            'id': order_id,
            'user_id': user_id,
            'order_status': order_status,
            'total': round(total, 2),
            'created_at': created_at.isoformat()
        })
    
    # Save orders
    orders_df = pd.DataFrame(orders_data)
    orders_file = output_dir / "orders.csv"
    orders_df.to_csv(orders_file, index=False)
    print(f"✅ Orders saved to {orders_file}")
    
    # Save order items
    order_items_df = pd.DataFrame(order_items_data)
    order_items_file = output_dir / "order_items.csv"
    order_items_df.to_csv(order_items_file, index=False)
    print(f"✅ Order items saved to {order_items_file}")
    
    return orders_df, order_items_df

def main():
    parser = argparse.ArgumentParser(description='Generate inventory data')
    parser.add_argument('--scale', choices=['small', 'medium', 'large', 'huge'], 
                       default='medium', help='Data scale preset')
    parser.add_argument('--output', type=str, default='./data', 
                       help='Output directory for CSV files')
    parser.add_argument('--custom', action='store_true',
                       help='Use custom counts instead of presets')
    parser.add_argument('--categories', type=int, default=100)
    parser.add_argument('--users', type=int, default=1000)
    parser.add_argument('--products', type=int, default=10000)
    parser.add_argument('--keep-existing', action='store_true',
                       help='Keep existing CSV files (default: delete before generating)')
    
    args = parser.parse_args()
    
    # Create output directory
    output_dir = Path(args.output)
    
    # Clean existing data folder unless --keep-existing flag is set
    if not args.keep_existing and output_dir.exists():
        print(f"🗑️  Cleaning existing data folder: {output_dir}")
        shutil.rmtree(output_dir)
        print("✅ Data folder cleaned")
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    print("=" * 60)
    print("🚀 Inventory Data Generator")
    print("=" * 60)
    
    # Get configuration
    if args.custom:
        config = {
            'categories': args.categories,
            'users': args.users,
            'products': args.products,
            'reviews_ratio': 0.8,
            'orders_ratio': 0.1,
            'items_per_order': 3,
        }
        print(f"\n⚙️  Custom Configuration:")
    else:
        config = SCALE_CONFIG[args.scale]
        print(f"\n⚙️  Scale: {args.scale.upper()}")
    
    print(f"   Categories: {config['categories']}")
    print(f"   Users: {config['users']}")
    print(f"   Products: {config['products']}")
    print(f"   Reviews: ~{int(config['products'] * config['reviews_ratio'])}")
    print(f"   Orders: ~{int(config['products'] * config['orders_ratio'])}")
    print(f"   Output: {output_dir.absolute()}")
    print()
    
    # Generate data
    start_time = datetime.now()
    
    categories_df = generate_categories(config['categories'], output_dir)
    users_df = generate_users(config['users'], output_dir)
    products_df = generate_products(config['products'], categories_df, output_dir)
    reviews_df = generate_reviews(products_df, users_df, config['reviews_ratio'], output_dir)
    orders_df, order_items_df = generate_orders(
        users_df, products_df, config['orders_ratio'], config['items_per_order'], output_dir
    )
    
    # Summary
    elapsed = datetime.now() - start_time
    print("\n" + "=" * 60)
    print("📊 Generation Complete!")
    print("=" * 60)
    print(f"⏱️  Time elapsed: {elapsed}")
    print(f"📁 Files generated in: {output_dir.absolute()}")
    print(f"   - categories.csv: {len(categories_df)} rows")
    print(f"   - users.csv: {len(users_df)} rows")
    print(f"   - products.csv: {len(products_df)} rows")
    print(f"   - reviews.csv: {len(reviews_df)} rows")
    print(f"   - orders.csv: {len(orders_df)} rows")
    print(f"   - order_items.csv: {len(order_items_df)} rows")
    print(f"\n💡 Next step: Run load_data.py to import into PostgreSQL")
    print("=" * 60)

if __name__ == "__main__":
    main()
