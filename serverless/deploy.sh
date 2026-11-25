#!/usr/bin/env bash
set -euo pipefail
export MSYS_NO_PATHCONV=1

cd "$(dirname "$0")"

ENDPOINT="${ENDPOINT:-http://localhost:4566}"
export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-us-east-1}"

TABLE=Reviews
ROLE=reviews-lambda-role
FUNCTION=reviews-api
API=reviews-api
STAGE=dev

awsl() { aws --endpoint-url "$ENDPOINT" "$@" | tr -d '\r'; }
step() { echo; echo "==> $*"; }

step "Waiting for LocalStack at $ENDPOINT"
for _ in $(seq 1 60); do
  curl -sf "$ENDPOINT/_localstack/health" >/dev/null && break
  sleep 2
done
curl -sf "$ENDPOINT/_localstack/health" >/dev/null || { echo "LocalStack is not reachable"; exit 1; }

step "DynamoDB table $TABLE"
if awsl dynamodb describe-table --table-name "$TABLE" >/dev/null 2>&1; then
  echo "exists"
else
  awsl dynamodb create-table \
    --table-name "$TABLE" \
    --attribute-definitions AttributeName=id,AttributeType=S \
    --key-schema AttributeName=id,KeyType=HASH \
    --billing-mode PAY_PER_REQUEST >/dev/null
  awsl dynamodb wait table-exists --table-name "$TABLE"
  echo "created"
fi
TABLE_ARN=$(awsl dynamodb describe-table --table-name "$TABLE" --query Table.TableArn --output text)

step "IAM role $ROLE"
if ! awsl iam get-role --role-name "$ROLE" >/dev/null 2>&1; then
  awsl iam create-role --role-name "$ROLE" \
    --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}' >/dev/null
fi
awsl iam put-role-policy --role-name "$ROLE" --policy-name reviews-table-access \
  --policy-document "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Action\":[\"dynamodb:PutItem\",\"dynamodb:GetItem\",\"dynamodb:DeleteItem\",\"dynamodb:Scan\"],\"Resource\":\"$TABLE_ARN\"},{\"Effect\":\"Allow\",\"Action\":[\"logs:CreateLogGroup\",\"logs:CreateLogStream\",\"logs:PutLogEvents\"],\"Resource\":\"*\"}]}"
ROLE_ARN=$(awsl iam get-role --role-name "$ROLE" --query Role.Arn --output text)
echo "$ROLE_ARN"

step "Packaging src/ into build/function.zip"
mkdir -p build
python -c "import zipfile; z = zipfile.ZipFile('build/function.zip', 'w', zipfile.ZIP_DEFLATED); z.write('src/handler.py', 'handler.py'); z.close()"

step "Lambda function $FUNCTION"
if awsl lambda get-function --function-name "$FUNCTION" >/dev/null 2>&1; then
  awsl lambda update-function-code --function-name "$FUNCTION" --zip-file fileb://build/function.zip >/dev/null
  awsl lambda wait function-updated-v2 --function-name "$FUNCTION"
  echo "code updated"
else
  awsl lambda create-function \
    --function-name "$FUNCTION" \
    --runtime python3.12 \
    --handler handler.lambda_handler \
    --role "$ROLE_ARN" \
    --zip-file fileb://build/function.zip \
    --timeout 15 \
    --environment "Variables={TABLE_NAME=$TABLE}" >/dev/null
  awsl lambda wait function-active-v2 --function-name "$FUNCTION"
  echo "created"
fi
FUNCTION_ARN=$(awsl lambda get-function --function-name "$FUNCTION" --query Configuration.FunctionArn --output text)

step "API Gateway REST API $API"
API_ID=$(awsl apigateway get-rest-apis --query "items[?name=='$API'].id | [0]" --output text)
if [ -z "$API_ID" ] || [ "$API_ID" = "None" ]; then
  API_ID=$(awsl apigateway create-rest-api --name "$API" --query id --output text)
  ROOT_ID=$(awsl apigateway get-resources --rest-api-id "$API_ID" --query "items[?path=='/'].id | [0]" --output text)
  REVIEWS_ID=$(awsl apigateway create-resource --rest-api-id "$API_ID" --parent-id "$ROOT_ID" --path-part reviews --query id --output text)
  REVIEW_ID=$(awsl apigateway create-resource --rest-api-id "$API_ID" --parent-id "$REVIEWS_ID" --path-part "{id}" --query id --output text)
  URI="arn:aws:apigateway:$AWS_DEFAULT_REGION:lambda:path/2015-03-31/functions/$FUNCTION_ARN/invocations"
  for RES in "$REVIEWS_ID" "$REVIEW_ID"; do
    awsl apigateway put-method --rest-api-id "$API_ID" --resource-id "$RES" \
      --http-method ANY --authorization-type NONE >/dev/null
    awsl apigateway put-integration --rest-api-id "$API_ID" --resource-id "$RES" \
      --http-method ANY --type AWS_PROXY --integration-http-method POST --uri "$URI" >/dev/null
  done
  ACCOUNT=$(awsl sts get-caller-identity --query Account --output text)
  awsl lambda add-permission --function-name "$FUNCTION" \
    --statement-id apigateway-invoke --action lambda:InvokeFunction \
    --principal apigateway.amazonaws.com \
    --source-arn "arn:aws:execute-api:$AWS_DEFAULT_REGION:$ACCOUNT:$API_ID/*/*" >/dev/null
  echo "created $API_ID"
else
  echo "exists $API_ID"
fi
awsl apigateway create-deployment --rest-api-id "$API_ID" --stage-name "$STAGE" >/dev/null
echo "deployed to stage $STAGE"

BASE_URL="$ENDPOINT/restapis/$API_ID/$STAGE/_user_request_"
echo "$BASE_URL" > build/api-url.txt

step "Done"
echo "API: $BASE_URL/reviews"
echo "(alternative host: http://$API_ID.execute-api.localhost.localstack.cloud:4566/$STAGE/reviews)"
