#!/usr/bin/env bash
set -uo pipefail
export MSYS_NO_PATHCONV=1

cd "$(dirname "$0")"

ENDPOINT="${ENDPOINT:-http://localhost:4566}"
export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-us-east-1}"

awsl() { aws --endpoint-url "$ENDPOINT" "$@" | tr -d '\r'; }

API_ID=$(awsl apigateway get-rest-apis --query "items[?name=='reviews-api'].id | [0]" --output text)
if [ -n "$API_ID" ] && [ "$API_ID" != "None" ]; then
  awsl apigateway delete-rest-api --rest-api-id "$API_ID" && echo "deleted API $API_ID"
fi
awsl lambda delete-function --function-name reviews-api >/dev/null && echo "deleted function reviews-api"
awsl iam delete-role-policy --role-name reviews-lambda-role --policy-name reviews-table-access
awsl iam delete-role --role-name reviews-lambda-role && echo "deleted role reviews-lambda-role"
awsl dynamodb delete-table --table-name Reviews >/dev/null && echo "deleted table Reviews"
rm -rf build
