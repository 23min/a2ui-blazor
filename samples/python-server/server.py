"""
Minimal A2UI server in Python — proves the Blazor client is protocol-agnostic.

Serves the same agents as the .NET server (restaurant finder, contacts, gallery)
using raw SSE/JSONL. No .NET dependency. ~100 lines.

Usage:
    uv run uvicorn server:app --port 5050
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
            {"id": "header", "component": "Text", "text": "Restaurant Finder", "variant": "h2"},
            {"id": "search-row", "component": "Row", "children": ["search-field", "search-btn"], "gap": "8", "align": "end"},
            {"id": "search-field", "component": "TextField", "placeholder": "Try 'Italian' or 'Sushi'...", "label": "Search by name or cuisine", "action": {"event": {"name": "search"}}},
            {"id": "search-btn", "component": "Button", "label": "Search", "action": {"event": {"name": "search", "context": {"value": "/query"}}}},
            {"id": "divider1", "component": "Divider"},
            {"id": "results-list", "component": "List", "data": "/restaurants", "template": {"componentId": "restaurant-card"}},
            {"id": "restaurant-card", "component": "Card", "title": "name", "children": ["card-body"]},
            {"id": "card-body", "component": "Row", "children": ["card-cuisine", "card-rating", "card-price"], "justify": "spaceBetween"},
            {"id": "card-cuisine", "component": "Text", "text": "cuisine", "variant": "body"},
            {"id": "card-rating", "component": "Text", "text": "rating", "variant": "caption"},
            {"id": "card-price", "component": "Text", "text": "priceRange", "variant": "caption"},
        ]))
        # Keep alive
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


@app.post("/agents/restaurant")
async def restaurant_action(request: Request):
    body = await request.json()
    if "error" in body:
        return StreamingResponse(iter([]), media_type="text/event-stream")
    action = body.get("action", {})
    query = (action.get("context") or {}).get("value", "")

    if query:
        filtered = [r for r in ALL_RESTAURANTS if query.lower() in r["name"].lower() or query.lower() in r["cuisine"].lower()]
    else:
        filtered = ALL_RESTAURANTS

    async def generate():
        yield sse({"type": "updateDataModel", "surfaceId": "restaurant-finder", "path": "/query", "value": query})
        yield sse({"type": "updateDataModel", "surfaceId": "restaurant-finder", "path": "/restaurants", "value": filtered})

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Contact Lookup ───────────────────────────────────────────────────

@app.get("/agents/contacts")
async def contacts_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "contacts", "sendDataModel": True})
        yield sse({"type": "updateDataModel", "surfaceId": "contacts", "path": "/", "value": {"query": "", "contacts": ALL_CONTACTS}})
        yield sse(components_msg("contacts", [
            {"id": "root", "component": "Column", "children": ["header", "search-row", "divider", "contact-list"], "gap": "12"},
            {"id": "header", "component": "Text", "text": "Contact Directory", "variant": "h2"},
            {"id": "search-row", "component": "Row", "children": ["search-input", "search-btn"], "gap": "8", "align": "end"},
            {"id": "search-input", "component": "TextField", "placeholder": "Try 'Engineering' or 'Alice'...", "label": "Search by name or department", "action": {"event": {"name": "search"}}},
            {"id": "search-btn", "component": "Button", "label": "Search", "action": {"event": {"name": "search", "context": {"value": "/query"}}}},
            {"id": "divider", "component": "Divider"},
            {"id": "contact-list", "component": "List", "data": "/contacts", "template": {"componentId": "contact-row"}},
            {"id": "contact-row", "component": "Row", "children": ["contact-name", "contact-email", "contact-dept"], "justify": "spaceBetween"},
            {"id": "contact-name", "component": "Text", "text": "name", "variant": "body"},
            {"id": "contact-email", "component": "Text", "text": "email", "variant": "caption"},
            {"id": "contact-dept", "component": "Text", "text": "department", "variant": "caption"},
        ]))
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


@app.post("/agents/contacts")
async def contacts_action(request: Request):
    body = await request.json()
    if "error" in body:
        return StreamingResponse(iter([]), media_type="text/event-stream")
    action = body.get("action", {})
    query = (action.get("context") or {}).get("value", "")

    if query:
        filtered = [c for c in ALL_CONTACTS if query.lower() in c["name"].lower() or query.lower() in c["department"].lower()]
    else:
        filtered = ALL_CONTACTS

    async def generate():
        yield sse({"type": "updateDataModel", "surfaceId": "contacts", "path": "/query", "value": query})
        yield sse({"type": "updateDataModel", "surfaceId": "contacts", "path": "/contacts", "value": filtered})

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Component Gallery ────────────────────────────────────────────────

@app.get("/agents/gallery")
async def gallery_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "gallery"})
        yield sse(components_msg("gallery", [
            # Root layout
            {"id": "root", "component": "Column", "children": [
                "title", "subtitle", "divider-top",
                "display-section", "divider1",
                "layout-section", "divider2",
                "input-section", "divider3",
                "media-section",
            ], "gap": "16"},
            {"id": "title", "component": "Text", "text": "A2UI Component Gallery", "variant": "h1"},
            {"id": "subtitle", "component": "Text", "text": "Served from Python, rendered in Blazor", "variant": "caption"},
            {"id": "divider-top", "component": "Divider"},

            # ── Display Components ──────────────────────────────────────
            {"id": "display-section", "component": "Card", "title": "Display Components", "children": ["display-col"]},
            {"id": "display-col", "component": "Column", "children": ["text-h2", "text-body", "icon1", "image1"], "gap": "8"},
            {"id": "text-h2", "component": "Text", "text": "Heading 2", "variant": "h2"},
            {"id": "text-body", "component": "Text", "text": "This text is coming from a Python FastAPI server.", "variant": "body"},
            {"id": "icon1", "component": "Icon", "icon": "★", "size": "32"},
            {"id": "image1", "component": "Image", "src": "https://picsum.photos/seed/a2ui/600/200", "alt": "Sample landscape", "fit": "cover"},
            {"id": "divider1", "component": "Divider"},

            # ── Layout Components ───────────────────────────────────────
            {"id": "layout-section", "component": "Card", "title": "Layout Components", "children": ["layout-col"]},
            {"id": "layout-col", "component": "Column", "children": ["tabs1"], "gap": "8"},
            {"id": "tabs1", "component": "Tabs", "tabs": [
                {"label": "Tab One", "contentId": "tab1-content"},
                {"label": "Tab Two", "contentId": "tab2-content"},
            ]},
            {"id": "tab1-content", "component": "Text", "text": "Content of the first tab.", "variant": "body"},
            {"id": "tab2-content", "component": "Text", "text": "Content of the second tab.", "variant": "body"},
            {"id": "divider2", "component": "Divider"},

            # ── Input Components ────────────────────────────────────────
            {"id": "input-section", "component": "Card", "title": "Input Components", "children": ["input-col"]},
            {"id": "input-col", "component": "Column", "children": [
                "btn-primary", "textfield1", "checkbox1", "slider1",
                "choicepicker1", "dateinput1",
                "validation-divider", "validation-label",
                "tf-error", "tf-helper", "cb-error", "cp-error",
            ], "gap": "12"},
            {"id": "btn-primary", "component": "Button", "label": "Click Me", "variant": "primary"},
            {"id": "textfield1", "component": "TextField", "label": "Text Field", "placeholder": "Type here..."},
            {"id": "checkbox1", "component": "CheckBox", "label": "Check me"},
            {"id": "slider1", "component": "Slider", "label": "Volume", "min": 0, "max": 100, "step": 1, "value": 50},
            {"id": "choicepicker1", "component": "ChoicePicker", "label": "Favorite color", "options": ["Red", "Green", "Blue", "Yellow"]},
            {"id": "dateinput1", "component": "DateTimeInput", "label": "Pick a date", "inputType": "date"},

            # Validation examples
            {"id": "validation-divider", "component": "Divider"},
            {"id": "validation-label", "component": "Text", "text": "Validation & Helper Text", "variant": "h3"},
            {"id": "tf-error", "component": "TextField", "label": "Email", "placeholder": "you@example.com", "error": "Please enter a valid email address"},
            {"id": "tf-helper", "component": "TextField", "label": "Username", "placeholder": "Choose a username", "helperText": "Must be 3-20 characters, letters and numbers only"},
            {"id": "cb-error", "component": "CheckBox", "label": "I accept the terms", "error": "You must accept the terms to continue"},
            {"id": "cp-error", "component": "ChoicePicker", "label": "Country", "options": ["USA", "Canada", "UK", "Germany"], "error": "Please select your country"},
            {"id": "divider3", "component": "Divider"},

            # ── Media Components ────────────────────────────────────────
            {"id": "media-section", "component": "Card", "title": "Media Components", "children": ["media-col"]},
            {"id": "media-col", "component": "Column", "children": ["video1", "audio1"], "gap": "12"},
            {"id": "video1", "component": "Video", "src": "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.webm", "controls": True},
            {"id": "audio1", "component": "AudioPlayer", "src": "https://interactive-examples.mdn.mozilla.net/media/cc0-audio/t-rex-roar.mp3", "controls": True},
        ]))
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Live State Machine ──────────────────────────────────────────────

PIPELINE_STATES = [
    {"id": "received",   "label": "Received"},
    {"id": "validating", "label": "Validating"},
    {"id": "processing", "label": "Processing"},
    {"id": "billing",    "label": "Billing"},
    {"id": "shipping",   "label": "Shipping"},
    {"id": "delivered",  "label": "Delivered"},
]


@app.get("/agents/state-machine")
async def state_machine_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "state-machine", "sendDataModel": True})

        # Initial data model — all pending
        initial_states = [{**s, "status": "pending"} for s in PIPELINE_STATES]
        yield sse({"type": "updateDataModel", "surfaceId": "state-machine", "path": "/", "value": {
            "pipeline": {
                "title": "Order Processing Pipeline",
                "states": initial_states,
                "statusMessage": "Waiting to start...",
            }
        }})

        # Component tree
        yield sse(components_msg("state-machine", [
            {"id": "root", "component": "Column", "children": ["header", "pipeline", "status-text"], "gap": "12"},
            {"id": "header", "component": "Text", "text": "Live State Machine", "variant": "h2"},
            {"id": "pipeline", "component": "StateMachine", "data": "/pipeline", "title": "/pipeline/title"},
            {"id": "status-text", "component": "Text", "text": "/pipeline/statusMessage", "variant": "caption"},
        ]))

        # Auto-advance through states in a loop
        while True:
            for step in range(len(PIPELINE_STATES)):
                await asyncio.sleep(2)
                states = []
                for i, s in enumerate(PIPELINE_STATES):
                    if i < step:
                        states.append({**s, "status": "completed"})
                    elif i == step:
                        states.append({**s, "status": "active"})
                    else:
                        states.append({**s, "status": "pending"})
                msg = f"Step {step + 1}/{len(PIPELINE_STATES)}: {PIPELINE_STATES[step]['label']}"
                yield sse({"type": "updateDataModel", "surfaceId": "state-machine", "path": "/pipeline", "value": {
                    "title": "Order Processing Pipeline",
                    "states": states,
                    "statusMessage": msg,
                }})

            # All completed
            await asyncio.sleep(2)
            yield sse({"type": "updateDataModel", "surfaceId": "state-machine", "path": "/pipeline", "value": {
                "title": "Order Processing Pipeline",
                "states": [{**s, "status": "completed"} for s in PIPELINE_STATES],
                "statusMessage": "All steps completed! Restarting in 3s...",
            }})
            await asyncio.sleep(3)

            # Reset
            yield sse({"type": "updateDataModel", "surfaceId": "state-machine", "path": "/pipeline", "value": {
                "title": "Order Processing Pipeline",
                "states": [{**s, "status": "pending"} for s in PIPELINE_STATES],
                "statusMessage": "Pipeline reset. Starting...",
            }})

    return StreamingResponse(generate(), media_type="text/event-stream")


# ── Error Demo ──────────────────────────────────────────────────────

error_demo_count = 0


@app.get("/agents/error-demo")
async def error_demo_stream():
    async def generate():
        yield sse({"type": "createSurface", "surfaceId": "error-demo", "sendDataModel": True})
        yield sse({"type": "updateDataModel", "surfaceId": "error-demo", "path": "/", "value": {
            "lastErrorMessage": "No errors reported yet.",
            "errorCount": 0,
        }})
        yield sse(components_msg("error-demo", [
            {"id": "root", "component": "Column", "children": [
                "header", "description", "divider1", "unknown-section", "divider2", "report-section",
            ], "gap": "16"},
            {"id": "header", "component": "Text", "text": "Error Handling Demo", "variant": "h2"},
            {"id": "description", "component": "Text", "text": "This demo shows how A2UI handles errors gracefully \u2014 unknown components render fallback UI, and errors can be reported back to the server.", "variant": "body"},
            {"id": "divider1", "component": "Divider"},
            # Unknown component section
            {"id": "unknown-section", "component": "Card", "title": "Unknown Component", "children": ["unknown-col"]},
            {"id": "unknown-col", "component": "Column", "children": ["unknown-desc", "unknown-component"], "gap": "8"},
            {"id": "unknown-desc", "component": "Text", "text": "The component below uses type 'FancyWidget' which doesn't exist in the standard catalog. The renderer shows a graceful fallback:", "variant": "body"},
            {"id": "unknown-component", "component": "FancyWidget"},
            {"id": "divider2", "component": "Divider"},
            # Error reporting section
            {"id": "report-section", "component": "Card", "title": "Error Reporting", "children": ["report-col"]},
            {"id": "report-col", "component": "Column", "children": ["report-desc", "report-btn", "error-status"], "gap": "8"},
            {"id": "report-desc", "component": "Text", "text": "Click the button to send a VALIDATION_FAILED error report to the server via the v0.9 error envelope. The server will acknowledge receipt.", "variant": "body"},
            {"id": "report-btn", "component": "Button", "label": "Report Error to Server", "action": {"event": {"name": "report-error"}}},
            {"id": "error-status", "component": "Text", "text": "/lastErrorMessage", "variant": "caption"},
        ]))
        while True:
            await asyncio.sleep(30)
            yield ": keepalive\n\n"

    return StreamingResponse(generate(), media_type="text/event-stream")


@app.post("/agents/error-demo")
async def error_demo_action(request: Request):
    global error_demo_count
    body = await request.json()

    if "error" in body:
        error = body["error"]
        error_demo_count += 1
        message = f"Server received error #{error_demo_count}: [{error.get('code', '')}] {error.get('message', '')}"
        path = error.get("path")
        if path:
            message += f" (path: {path})"

        async def generate():
            yield sse({"type": "updateDataModel", "surfaceId": "error-demo", "path": "/", "value": {
                "lastErrorMessage": message,
                "errorCount": error_demo_count,
            }})

        return StreamingResponse(generate(), media_type="text/event-stream")

    return StreamingResponse(iter([]), media_type="text/event-stream")
