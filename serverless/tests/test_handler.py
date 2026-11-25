import json
import os
import sys
import unittest
from decimal import Decimal

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))

import handler  # noqa: E402

PRODUCT = "11111111-1111-1111-1111-111111111111"


class FakeTable:
    def __init__(self):
        self.items = {}

    def put_item(self, Item):
        self.items[Item["id"]] = {k: Decimal(v) if isinstance(v, int) else v for k, v in Item.items()}

    def get_item(self, Key):
        item = self.items.get(Key["id"])
        return {"Item": item} if item else {}

    def delete_item(self, Key, ReturnValues):
        item = self.items.pop(Key["id"], None)
        return {"Attributes": item} if item else {}

    def scan(self, **kwargs):
        return {"Items": list(self.items.values())}


def call(method, resource, body=None, path_id=None, query=None, content_type="application/json"):
    event = {
        "httpMethod": method,
        "resource": resource,
        "headers": {"Content-Type": content_type} if content_type else {},
        "body": body if body is None or isinstance(body, str) else json.dumps(body),
        "pathParameters": {"id": path_id} if path_id else None,
        "queryStringParameters": query,
    }
    result = handler.lambda_handler(event, None)
    return result["statusCode"], json.loads(result["body"]) if result["body"] else None


class HandlerTests(unittest.TestCase):
    def setUp(self):
        handler._table = FakeTable()

    def create(self, rating=5, product=PRODUCT, comment="ok"):
        return call("POST", "/reviews", {"productId": product, "rating": rating, "comment": comment})

    def test_create_returns_201_with_generated_id(self):
        status, body = self.create()
        self.assertEqual(status, 201)
        self.assertEqual(body["productId"], PRODUCT)
        self.assertEqual(body["rating"], 5)
        self.assertIn("id", body)

    def test_get_returns_created_review_with_integer_rating(self):
        _, created = self.create(rating=4)
        status, body = call("GET", "/reviews/{id}", path_id=created["id"])
        self.assertEqual(status, 200)
        self.assertEqual(body["rating"], 4)

    def test_list_filters_by_product(self):
        self.create()
        self.create(product="22222222-2222-2222-2222-222222222222")
        status, body = call("GET", "/reviews", query={"productId": PRODUCT})
        self.assertEqual(status, 200)
        self.assertEqual(len(body), 1)
        self.assertEqual(len(call("GET", "/reviews")[1]), 2)

    def test_delete_then_get_returns_404(self):
        _, created = self.create()
        self.assertEqual(call("DELETE", "/reviews/{id}", path_id=created["id"])[0], 204)
        self.assertEqual(call("GET", "/reviews/{id}", path_id=created["id"])[0], 404)
        self.assertEqual(call("DELETE", "/reviews/{id}", path_id=created["id"])[0], 404)

    def test_invalid_input_returns_400(self):
        self.assertEqual(self.create(rating=0)[0], 400)
        self.assertEqual(self.create(rating=6)[0], 400)
        self.assertEqual(self.create(rating=True)[0], 400)
        self.assertEqual(self.create(product="00000000-0000-0000-0000-000000000000")[0], 400)
        self.assertEqual(self.create(product="abc")[0], 400)
        self.assertEqual(call("POST", "/reviews", "not json")[0], 400)
        self.assertEqual(call("POST", "/reviews", "[1]")[0], 400)
        self.assertEqual(call("GET", "/reviews/{id}", path_id="abc")[0], 400)

    def test_non_json_content_type_returns_415(self):
        status, _ = call("POST", "/reviews", {"productId": PRODUCT, "rating": 5}, content_type="text/plain")
        self.assertEqual(status, 415)

    def test_base64_body_is_decoded(self):
        import base64

        raw = json.dumps({"productId": PRODUCT, "rating": 3}).encode()
        event = {
            "httpMethod": "POST",
            "resource": "/reviews",
            "headers": {"content-type": "application/json"},
            "body": base64.b64encode(raw).decode(),
            "isBase64Encoded": True,
        }
        self.assertEqual(handler.lambda_handler(event, None)["statusCode"], 201)

    def test_unsupported_method_returns_405(self):
        self.assertEqual(call("PUT", "/reviews")[0], 405)
        self.assertEqual(call("PATCH", "/reviews/{id}", path_id=PRODUCT)[0], 405)


if __name__ == "__main__":
    unittest.main()
