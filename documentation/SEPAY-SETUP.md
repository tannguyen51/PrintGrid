# SePay setup for PrintGrid

The application code creates VietQR payment instructions and accepts authenticated SePay
incoming-transfer webhooks. No database migration is required.

## Runtime configuration

Set these environment variables on the API deployment (do not commit real credentials):

```text
SePay__Enabled=true
SePay__BankCode=MB
SePay__AccountNumber=YOUR_ACCOUNT_NUMBER
SePay__AccountName=YOUR_ACCOUNT_NAME
SePay__WebhookApiKey=GENERATE_A_LONG_RANDOM_VALUE
SePay__WebhookSecret=
```

Use either `WebhookApiKey` or `WebhookSecret`, not both. When both are present the API
requires HMAC-SHA256. `BankCode` must be the VietQR/SePay bank identifier.

## SePay dashboard

1. Connect the receiving bank account to SePay. A QR-only NAPAS terminal is not enough
   because those transactions do not currently produce webhooks.
2. Under company payment-code configuration, add prefix `PG`, with an alphanumeric suffix.
   PrintGrid generates 26-character suffixes.
   For VietinBank personal/household accounts, PrintGrid automatically prepends the required
   `SEVQR` marker to the transfer description while retaining the `PG...` payment code.
3. Create an incoming-money webhook with JSON content at:

   `https://YOUR_API_HOST/api/v1/webhooks/sepay`

4. Choose the same authentication configured above:
   - API Key: SePay sends `Authorization: Apikey <key>`.
   - HMAC-SHA256: SePay sends `X-SePay-Signature`.
5. Enable retry for non-2xx responses and use SePay Test Mode before enabling live payments.

The endpoint returns HTTP 201 and `{ "success": true }` for an accepted or duplicate
transaction. Duplicate delivery is safe because `SEPAY:<transaction id>` is stored in the
existing financial ledger unique idempotency index.

## Smoke test

1. Place an order and open **My orders**.
2. Open a `PaymentPending` order and verify its QR, amount, account and `PG...` content.
3. Use SePay Test Mode to send the exact amount and content shown.
4. Confirm that the order becomes `Confirmed`, a single receipt appears in the finance ledger,
   and sending the same webhook again does not increase `PaidAmount`.

Keep the existing Ops **mark paid** screen enabled as a manual recovery path for transfers
with a wrong code or during a SePay outage.
