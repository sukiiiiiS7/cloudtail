import os
from motor.motor_asyncio import AsyncIOMotorClient
from dotenv import load_dotenv

load_dotenv()

MONGO_URI = os.environ.get("MONGO_URI")
MONGO_DB_NAME = os.environ.get("MONGO_DB_NAME")

client = AsyncIOMotorClient(MONGO_URI)
db = client[MONGO_DB_NAME]

def get_memory_collection():
    return db["memory_entries"]
