import base64
import binascii
import json
import os
import uuid
from datetime import datetime, timezone
from decimal import Decimal

_table = None


def get_table():
    global _table
    if _table is None:
        import boto3

        _table = boto3.resource("dynamodb").Table(os.environ["TABLE_NAME"])
    return _table


def response(status, body=None):
    return {
        "statusCode": status,
        "headers": {"Content-Type": "application/json; charset=utf-8"},
        "body": "" if body is None else json.dumps(body, default=to_json, ensure_ascii=False),
    }


def to_json(value):
    if isinstance(value, Decimal):
        return int(value) if value == value.to_integral_value() else float(value)
    raise TypeError(f"Cannot serialize {type(value).__name__}")


def parse_guid(value):
    try:
        parsed = uuid.UUID(str(value))
    except (ValueError, TypeError, AttributeError):
        return None
    return None if parsed.int == 0 else str(parsed)


def read_body(event):
    body = event.get("body") or ""
    if event.get("isBase64Encoded") and body:
        body = base64.b64decode(body).decode("utf-8")
    return body


def is_json_request(event):
    headers = {k.lower(): v for k, v in (event.get("headers") or {}).items()}
    return "application/json" in (headers.get("content-type") or "").lower()


def create_review(event):
    if not is_json_request(event):
        return response(415, {"error": "Content-Type must be application/json."})
    try:
        data = json.loads(read_body(event) or "{}")
    except (json.JSONDecodeError, UnicodeDecodeError, binascii.Error):
        return response(400, {"error": "Body must be valid JSON."})
    if not isinstance(data, dict):
        return response(400, {"error": "Body must be a JSON object."})

    product_id = parse_guid(data.get("productId"))
    if product_id is None:
        return response(400, {"error": "A valid productId is required."})

    rating = data.get("rating")
    if not isinstance(rating, int) or isinstance(rating, bool) or not 1 <= rating <= 5:
        return response(400, {"error": "Rating must be between 1 and 5."})

    comment = data.get("comment", "")
    if not isinstance(comment, str) or len(comment) > 1000:
        return response(400, {"error": "Comment must be a string up to 1000 characters."})

    review = {
        "id": str(uuid.uuid4()),
        "productId": product_id,
        "rating": rating,
        "comment": comment,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    get_table().put_item(Item=review)
    return response(201, review)


def list_reviews(event):
    query = event.get("queryStringParameters") or {}
    product_id = None
    if query.get("productId") is not None:
        product_id = parse_guid(query["productId"])
        if product_id is None:
            return response(400, {"error": "productId must be a valid GUID."})

    items, kwargs = [], {}
    while True:
        page = get_table().scan(**kwargs)
        items.extend(page.get("Items", []))
        if "LastEvaluatedKey" not in page:
            break
        kwargs["ExclusiveStartKey"] = page["LastEvaluatedKey"]

    if product_id is not None:
        items = [i for i in items if i.get("productId") == product_id]
    items.sort(key=lambda i: i.get("createdAtUtc", ""), reverse=True)
    return response(200, items)


def get_review(review_id):
    item = get_table().get_item(Key={"id": review_id}).get("Item")
    return response(200, item) if item else response(404, {"error": "Review not found."})


def delete_review(review_id):
    old = get_table().delete_item(Key={"id": review_id}, ReturnValues="ALL_OLD")
    return response(204) if "Attributes" in old else response(404, {"error": "Review not found."})


def lambda_handler(event, context):
    method = event.get("httpMethod")
    resource = event.get("resource")

    if resource == "/reviews":
        if method == "POST":
            return create_review(event)
        if method == "GET":
            return list_reviews(event)
        return response(405, {"error": f"Method {method} is not allowed."})

    if resource == "/reviews/{id}":
        review_id = parse_guid((event.get("pathParameters") or {}).get("id"))
        if review_id is None:
            return response(400, {"error": "Review id must be a valid GUID."})
        if method == "GET":
            return get_review(review_id)
        if method == "DELETE":
            return delete_review(review_id)
        return response(405, {"error": f"Method {method} is not allowed."})

    return response(404, {"error": "Route not found."})
