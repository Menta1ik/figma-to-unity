import sys
sys.path.append('/Users/macbook/Projects/GameDev')
from figma_mcp import fetch_figma_screen, download_figma_sprites

try:
    print("Fetching JSON...")
    res1 = fetch_figma_screen("1:31546", "/Users/macbook/Projects/GameDev/docs/lobby_1_31546.json")
    print(res1)
    print("Downloading sprites...")
    res2 = download_figma_sprites("1:31546", "/Users/macbook/Projects/GameDev/Game/Assets/UI/Sprites")
    print(res2)
except Exception as e:
    print("Error:", e)
