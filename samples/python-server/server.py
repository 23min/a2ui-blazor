"""
Minimal A2UI server in Python — proves the Blazor client is protocol-agnostic.

Serves the same agents as the .NET server (restaurant finder, contacts, gallery)
using raw SSE/JSONL. No .NET dependency. ~100 lines.

Usage:
    pip install -r requirements.txt
    uvicorn server:app --port 8000
"""

import json
import asyncio
from fastapi import FastAPI, Request
from fastapi.responses import StreamingResponse
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])

ALL_RESTAURANTS = [
    {"name": "The Golden Fork", "cuisine": "Italian", "rating": 4.5, "priceRange": "$$"},
    {"name": "Sushi Zen", "cuisine": "Japanese", "rating": 4.8, "priceRange": "$$$"},
    {"name": "Taco Fiesta", "cuisine": "Mexican", "rating": 4.2, "priceRange": "$"},
    {"name": "Le Petit Bistro", "cuisine": "French", "rating": 4.7, "priceRange": "$$$"},
]

ALL_CONTACTS = [
    {"name": "Alice Johnson", "email": "alice@example.com", "department": "Engineering"},
    {"name": "Bob Smith", "email": "bob@example.com", "department": "Marketing"},
    {"name": "Carol Williams", "email": "carol@example.com", "department": "Engineering"},
    {"name": "David Brown", "email": "david@example.com", "department": "Sales"},
    {"name": "Eve Davis", "email": "eve@example.com", "department": "Engineering"},
]


def sse(data: dict) -> str:
    return f"data: {json.dumps(data)}\n\n"


def components_msg(surface_id: str, components: list) -> dict:
    return {"type": "updateComponents", "surfaceId": surface_id, "components": components}


# ── Restaurant Finder ────────────────────────────────────────────────

@app.get("/agents/restaurant")
async def restaurant_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "restaurant-finder", "sendDataModel": True})
        yield sse({"type": "updateDataModel", "surfaceId": "restaurant-finder", "path": "/", "value": {"query": "", "restaurants": ALL_RESTAURANTS}})
        yield sse(components_msg("restaurant-finder", [
            {"id": "root", "component": "Column", "children": ["header", "search-row", "divider1", "results-list"]},
            {"id": "header", "component": "Text", "text": "Restaurant Finder", "usageHint": "h2"},
            {"id": "search-row", "component": "Row", "children": ["search-field", "search-btn"], "gap": "8", "alignment": "end"},
            {"id": "search-field", "component": "TextField", "placeholder": "Search restaurants...", "label": "Search", "action": {"event": {"name": "search"}}},
            {"id": "search-btn", "component": "Button", "label": "Search", "action": {"event": {"name": "search"}}},
            {"id": "divider1", "component": "Divider"},
            {"id": "results-list", "component": "List", "data": "/restaurants", "template": {"componentId": "restaurant-card"}},
            {"id": "restaurant-card", "component": "Card", "title": "name", "children": ["card-body"]},
            {"id": "card-body", "component": "Row", "children": ["card-cuisine", "card-rating", "card-price"], "distribution": "spaceBetween"},
            {"id": "card-cuisine", "component": "Text", "text": "cuisine", "usageHint": "body"},
            {"id": "card-rating", "component": "Text", "text": "rating", "usageHint": "caption"},
            {"id": "card-price", "component": "Text", "text": "priceRange", "usageHint": "caption"},
        ]))
        # Keep alive
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


@app.post("/agents/restaurant")
async def restaurant_action(request: Request):
    body = await request.json()
    query = (body.get("context") or {}).get("value", "")

    if query:
        filtered = [r for r in ALL_RESTAURANTS if query.lower() in r["name"].lower() or query.lower() in r["cuisine"].lower()]
    else:
        filtered = ALL_RESTAURANTS

    async def generate():
        yield sse({"type": "updateDataModel", "surfaceId": "restaurant-finder", "path": "/restaurants", "value": filtered})

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Contact Lookup ───────────────────────────────────────────────────

@app.get("/agents/contacts")
async def contacts_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "contacts", "sendDataModel": True})
        yield sse({"type": "updateDataModel", "surfaceId": "contacts", "path": "/", "value": {"contacts": ALL_CONTACTS}})
        yield sse(components_msg("contacts", [
            {"id": "root", "component": "Column", "children": ["header", "search-row", "divider", "contact-list"], "gap": "12"},
            {"id": "header", "component": "Text", "text": "Contact Directory", "usageHint": "h2"},
            {"id": "search-row", "component": "Row", "children": ["search-input", "search-btn"], "gap": "8", "alignment": "end"},
            {"id": "search-input", "component": "TextField", "placeholder": "Search contacts...", "label": "Search", "action": {"event": {"name": "search"}}},
            {"id": "search-btn", "component": "Button", "label": "Search", "action": {"event": {"name": "search"}}},
            {"id": "divider", "component": "Divider"},
            {"id": "contact-list", "component": "List", "data": "/contacts", "template": {"componentId": "contact-row"}},
            {"id": "contact-row", "component": "Row", "children": ["contact-name", "contact-email", "contact-dept"], "distribution": "spaceBetween"},
            {"id": "contact-name", "component": "Text", "text": "name", "usageHint": "body"},
            {"id": "contact-email", "component": "Text", "text": "email", "usageHint": "caption"},
            {"id": "contact-dept", "component": "Text", "text": "department", "usageHint": "caption"},
        ]))
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


@app.post("/agents/contacts")
async def contacts_action(request: Request):
    body = await request.json()
    query = (body.get("context") or {}).get("value", "")

    if query:
        filtered = [c for c in ALL_CONTACTS if query.lower() in c["name"].lower() or query.lower() in c["department"].lower()]
    else:
        filtered = ALL_CONTACTS

    async def generate():
        yield sse({"type": "updateDataModel", "surfaceId": "contacts", "path": "/contacts", "value": filtered})

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Component Gallery ────────────────────────────────────────────────

@app.get("/agents/gallery")
async def gallery_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "gallery"})
        yield sse(components_msg("gallery", [
            {"id": "root", "component": "Column", "children": ["title", "subtitle", "divider-top", "display-section", "divider1", "input-section"], "gap": "16"},
            {"id": "title", "component": "Text", "text": "A2UI Component Gallery", "usageHint": "h1"},
            {"id": "subtitle", "component": "Text", "text": "Served from Python, rendered in Blazor", "usageHint": "caption"},
            {"id": "divider-top", "component": "Divider"},
            {"id": "display-section", "component": "Card", "title": "Display Components", "children": ["display-col"]},
            {"id": "display-col", "component": "Column", "children": ["text-h2", "text-body", "icon1"], "gap": "8"},
            {"id": "text-h2", "component": "Text", "text": "Heading 2", "usageHint": "h2"},
            {"id": "text-body", "component": "Text", "text": "This text is coming from a Python FastAPI server.", "usageHint": "body"},
            {"id": "icon1", "component": "Icon", "icon": "★", "size": "32"},
            {"id": "divider1", "component": "Divider"},
            {"id": "input-section", "component": "Card", "title": "Input Components", "children": ["input-col"]},
            {"id": "input-col", "component": "Column", "children": ["btn-primary", "textfield1", "checkbox1", "slider1"], "gap": "12"},
            {"id": "btn-primary", "component": "Button", "label": "Click Me", "variant": "primary"},
            {"id": "textfield1", "component": "TextField", "label": "Text Field", "placeholder": "Type here..."},
            {"id": "checkbox1", "component": "CheckBox", "label": "Check me"},
            {"id": "slider1", "component": "Slider", "label": "Volume", "min": 0, "max": 100, "step": 1, "value": 50},
        ]))
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")
