import sys
import json
import urllib.request
import urllib.parse
from mcp.server.fastmcp import FastMCP

# Инициализируем MCP сервер
mcp = FastMCP("Figma Unity Importer")

FIGMA_TOKEN = "figd_J73THJ7uqFKZWvAX--W2dGa6P9Qez1eEbz-nx_J1"
FILE_KEY = "8uHIGlJoLtloWpsDTLwEiI"
HEADERS = {"X-Figma-Token": FIGMA_TOKEN}

def figma_request(endpoint):
    req = urllib.request.Request(f"https://api.figma.com/v1/{endpoint}", headers=HEADERS)
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read())

def get_all_nodes(node, nodes_list):
    if not node: return
    nodes_list.append(node)
    for child in node.get("children", []):
        get_all_nodes(child, nodes_list)

@mcp.tool()
def fetch_figma_screen(node_id: str, save_docs_path: str = "/Users/macbook/Projects/GameDev/docs/new_screen.json") -> str:
    """Скачивает JSON-структуру экрана из Figma и сохраняет в папку docs."""
    formatted_id = node_id.replace("-", ":")
    try:
        data = figma_request(f"files/{FILE_KEY}/nodes?ids={formatted_id}")
        
        with open(save_docs_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
            
        return f"Успешно сохранено в {save_docs_path}"
    except Exception as e:
        return f"Ошибка: {str(e)}"

@mcp.tool()
def download_figma_sprites(node_id: str, sprites_dir: str = "/Users/macbook/Projects/GameDev/Game/Assets/UI/Sprites") -> str:
    """Скачивает все картинки (спрайты) для указанного экрана и кладет их в Unity."""
    formatted_id = node_id.replace("-", ":")
    try:
        # 1. Получаем структуру
        data = figma_request(f"files/{FILE_KEY}/nodes?ids={formatted_id}")
        nodes = data.get("nodes", {}).get(formatted_id, {}).get("document", {})
        
        # 2. Собираем ID всех НЕ-текстовых нод
        all_nodes = []
        get_all_nodes(nodes, all_nodes)
        
        image_ids = []
        for n in all_nodes:
            if n.get("type") != "TEXT" and n.get("type") != "DOCUMENT" and n.get("type") != "CANVAS":
                image_ids.append(n.get("id"))
                
        if not image_ids:
            return "Не найдено элементов для скачивания."
            
        # 3. Запрашиваем ссылки на рендер картинок из Figma (пачками по 50, чтобы не перегрузить API)
        downloaded = 0
        chunk_size = 50
        for i in range(0, len(image_ids), chunk_size):
            chunk = image_ids[i:i + chunk_size]
            ids_str = ",".join(urllib.parse.quote(id) for id in chunk)
            
            img_data = figma_request(f"images/{FILE_KEY}?ids={ids_str}&format=png&scale=1")
            images = img_data.get("images", {})
            
            # 4. Скачиваем каждую картинку
            for figma_id, url in images.items():
                if not url: continue
                safe_id = figma_id.replace(":", "_").replace(";", "_")
                filepath = f"{sprites_dir}/sprite_{safe_id}.png"
                
                urllib.request.urlretrieve(url, filepath)
                downloaded += 1
                
        return f"Успешно скачано {downloaded} спрайтов в {sprites_dir}"
    except Exception as e:
        return f"Ошибка: {str(e)}"

if __name__ == "__main__":
    mcp.run(transport='stdio')
