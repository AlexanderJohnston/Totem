# Todos — Roll Inventory Tracker

## 1. define-events
**Status:** pending
**Depends on:** _(none)_

Add RollPathDetected, NewRollDiscovered, DuplicateRollIgnored to Outermind\_Events.cs. ScanForNARA already exists and will be reused.

---

## 2. client-classifier
**Status:** pending
**Depends on:** define-events

Singleton topic in Outermind\Topics\ClientClassifier.cs. Subscribes to ScanDetected. Extracts client from e.Scan.FolderPath seg[2]. If client starts with "NARA", emits ScanForNARA(path, owner, changeType). Extension point for future clients.

---

## 3. nara-path-parser
**Status:** pending
**Depends on:** define-events, client-classifier

Singleton topic in Outermind\Topics\NaraPathParser.cs. Subscribes to ScanForNARA. Parses e.FolderPath into 8 segments. Emits RollPathDetected with client, project, pallet, stage, box, roll.

---

## 4. box-inventory-topic
**Status:** pending
**Depends on:** define-events, nara-path-parser

Routed topic in Outermind\Topics\BoxInventory.cs. Key={Client}:{Pallet}:{Box}. RouteFirst on RollPathDetected. Given(NewRollDiscovered) adds roll to HashSet. When checks dedup, emits NewRollDiscovered or DuplicateRollIgnored.

---

## 5. box-roll-list-query
**Status:** pending
**Depends on:** define-events

Routed query in Outermind\Queries\BoxRollList.cs. Key={Client}:{Pallet}:{Box}. RouteFirst on NewRollDiscovered. Given appends to List\<RollEntry\>.

---

## 6. client-pallet-list
**Status:** pending
**Depends on:** define-events

Lightweight query in Outermind\Queries\ClientPalletList.cs. Routed by {Client}. HashSet\<string\> of pallet names. RouteFirst on NewRollDiscovered.

---

## 7. pallet-box-list
**Status:** pending
**Depends on:** define-events

Lightweight query in Outermind\Queries\PalletBoxList.cs. Routed by {Client}:{Pallet}. HashSet\<string\> of box names. RouteFirst on NewRollDiscovered.

---

## 8. api-endpoint
**Status:** pending
**Depends on:** box-roll-list-query, client-pallet-list, pallet-box-list

Add InventoryController.cs to Outermind.Web\Controllers\ (namespace Outermind.Controllers, matching ScanController pattern). Three endpoints: /api/inventory/pallets/{client}, /boxes/{id}, /rolls/{id}.

---

## 9. write-tests
**Status:** pending
**Depends on:** box-inventory-topic, box-roll-list-query, client-pallet-list, pallet-box-list

TopicTests for ClientClassifier (NARA match, non-NARA ignored, short path ignored), NaraPathParser (valid 8-seg path, short path ignored), BoxInventory (new roll, duplicate). QueryTests for BoxRollList, ClientPalletList, PalletBoxList.
