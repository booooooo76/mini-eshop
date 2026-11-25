#!/usr/bin/env bash
set -euo pipefail
export MSYS_NO_PATHCONV=1

cd "$(dirname "$0")"

BASE="${BASE_URL:-$(cat build/api-url.txt)}/reviews"
PRODUCT=11111111-1111-1111-1111-111111111111
JSON='Content-Type: application/json'
FAILED=0
BODY=""

check() {
  local name=$1 expected=$2
  shift 2
  local out code
  out=$(curl -s -w '\n%{http_code}' "$@")
  code=${out##*$'\n'}
  BODY=${out%$'\n'*}
  if [ "$code" = "$expected" ]; then
    echo "PASS $code $name"
  else
    echo "FAIL $name: expected $expected, got $code: $BODY"
    FAILED=1
  fi
}

echo "API: $BASE"
check "create review" 201 -X POST "$BASE" -H "$JSON" \
  -d "{\"productId\":\"$PRODUCT\",\"rating\":5,\"comment\":\"Great keyboard\"}"
ID=$(printf '%s' "$BODY" | python -c "import sys, json; print(json.load(sys.stdin)['id'])")
echo "     id = $ID"

check "list reviews" 200 "$BASE"
check "list by product" 200 "$BASE?productId=$PRODUCT"
check "get review" 200 "$BASE/$ID"
check "rating out of range" 400 -X POST "$BASE" -H "$JSON" -d "{\"productId\":\"$PRODUCT\",\"rating\":9}"
check "missing productId" 400 -X POST "$BASE" -H "$JSON" -d '{"rating":3}'
check "not json" 400 -X POST "$BASE" -H "$JSON" -d 'not json'
check "wrong content type" 415 -X POST "$BASE" -d "{\"productId\":\"$PRODUCT\",\"rating\":5}"
check "invalid id" 400 "$BASE/abc"
check "method not allowed" 405 -X PUT "$BASE"
check "delete review" 204 -X DELETE "$BASE/$ID"
check "get deleted review" 404 "$BASE/$ID"
check "delete again" 404 -X DELETE "$BASE/$ID"

[ "$FAILED" = 0 ] && echo "All checks passed" || { echo "Some checks failed"; exit 1; }
