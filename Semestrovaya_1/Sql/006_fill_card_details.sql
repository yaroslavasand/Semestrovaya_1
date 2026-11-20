-- 006_fill_card_details.sql
-- Источник: данные пользователя. Card Type берётся из отдельного поля.
-- Полные тексты способностей сохраняют обозначения из начального описания.
-- Не меняет image_path, display_order, is_new, card_set_id и created_at.
-- Перезаписывает перечисленные характеристики и связи только этих 43 карточек.
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'oris_riftbound' THEN
        RAISE EXCEPTION 'Неверная база: %. Ожидалась oris_riftbound.', current_database();
    END IF;
END;
$$;

CREATE TEMP TABLE card_details_seed ON COMMIT DROP AS
SELECT *
FROM jsonb_to_recordset($cards_json$
[
  {
    "code": "rad-sp1-005",
    "name": "Ahri, Confident",
    "energy": 4,
    "power": 1,
    "might": 4,
    "domains": [
      "Calm"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Ahri",
      "Ionia"
    ],
    "rarity": "Showcase",
    "artist": "HCuu",
    "ability": "[Disarm]\nWhen you reduce the Might of an enemy unit here by 1 or more, give me +1 [S] this turn."
  },
  {
    "code": "rad-001-167",
    "name": "Amateur Demolitionist",
    "energy": 2,
    "power": null,
    "might": 1,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Kudos Productions",
    "ability": "When you play me, play a Bomb gear token to a battlefield. (It has [Deploy]. When it dies, deal 2 to an enemy there and kill your other Bombs there.)"
  },
  {
    "code": "rad-r01",
    "name": "Fury Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r01a",
    "name": "Fury Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-r02",
    "name": "Calm Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Calm"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r02a",
    "name": "Calm Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Calm"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-r03",
    "name": "Mind Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Mind"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r03a",
    "name": "Mind Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Mind"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-r04",
    "name": "Body Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Body"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r04a",
    "name": "Body Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Body"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-r05",
    "name": "Chaos Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r05a",
    "name": "Chaos Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-r06",
    "name": "Order Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Order"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Greg Ghielmetti & Leah Chen",
    "ability": ""
  },
  {
    "code": "rad-r06a",
    "name": "Order Rune",
    "energy": null,
    "power": null,
    "might": null,
    "domains": [
      "Order"
    ],
    "types": [
      "Rune",
      "Basic"
    ],
    "tags": [],
    "rarity": "Showcase",
    "artist": "",
    "ability": ""
  },
  {
    "code": "rad-sp2-005",
    "name": "Akali, Brash",
    "energy": 5,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Ionia",
      "Akali"
    ],
    "rarity": "Showcase",
    "artist": "",
    "ability": "The first time I move each turn, [Add] [1][A].\nThe second time I move each turn, banish the top card of your Main Deck. Until your next Ending Phase, you may play that card."
  },
  {
    "code": "rad-002-167",
    "name": "Banner of Conquest",
    "energy": 1,
    "power": 1,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Gear"
    ],
    "tags": [
      "Device"
    ],
    "rarity": "Common",
    "artist": "Fish Art Studio",
    "ability": "[Deploy] (Play this only to a battlefield. When an opponent holds here, kill this.)\nFriendly units here have [Assault]. (+1 [S] while they're attackers.)"
  },
  {
    "code": "rad-003-167",
    "name": "Bilgewater Tracker",
    "energy": 3,
    "power": null,
    "might": 3,
    "domains": [
      "Body"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Six More Vodka",
    "ability": "When you play me, deal 1 to each of up to 2 units."
  },
  {
    "code": "rad-sp3-005",
    "name": "Evelynn, In Control",
    "energy": 4,
    "power": 1,
    "might": 4,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Evelynn"
    ],
    "rarity": "Showcase",
    "artist": "Yuande Wu",
    "ability": "[Disarm]\nWhen I hold, if you control a facedown card here, score 1 point."
  },
  {
    "code": "rad-004-167",
    "name": "Brawl",
    "energy": 3,
    "power": 1,
    "might": null,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Spell"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Kudos Productions",
    "ability": "[Action] (Play on your turn or in showdowns.)\nGive a unit [Assault] this turn equal to its Might. (+1 [S] for each instance of Assault while it's an attacker.)"
  },
  {
    "code": "rad-sp4-005",
    "name": "Kai'Sa, Rebel",
    "energy": 5,
    "power": 1,
    "might": 5,
    "domains": [
      "Mind"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Kai'Sa",
      "The Void"
    ],
    "rarity": "Showcase",
    "artist": "Zhongqi Li",
    "ability": "[Disarm]\nWhile I'm in a showdown, your spells have [Reaction]."
  },
  {
    "code": "rad-005-167",
    "name": "Lingering Animus",
    "energy": 2,
    "power": null,
    "might": 2,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Six More Vodka",
    "ability": "I enter ready if a friendly unit or gear died this turn."
  },
  {
    "code": "rad-sp5-005",
    "name": "Seraphine, Not Alone",
    "energy": 5,
    "power": 1,
    "might": 1,
    "domains": [
      "Order"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Piltover",
      "Seraphine"
    ],
    "rarity": "Showcase",
    "artist": "",
    "ability": "When you play me or when I become exhausted, play a 1 [S] Recruit unit token to your base.\nI have +1 [S] for each other exhausted unit you control."
  },
  {
    "code": "rad-006-167",
    "name": "Scorched Earth",
    "energy": 4,
    "power": 1,
    "might": null,
    "domains": [],
    "types": [],
    "tags": [],
    "rarity": "Common",
    "artist": "Kudos Productions",
    "ability": "[Action] (Play on your turn or in showdowns.)\nDeal 5 damage split among units at a single battlefield."
  },
  {
    "code": "rad-007-167",
    "name": "Rally for War",
    "energy": 3,
    "power": 1,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Gear"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Wild Blue Studios",
    "ability": "Choose an occupied enemy battlefield. Play three 1 [S] Recruit unit tokens there."
  },
  {
    "code": "rad-008-167",
    "name": "War Scout",
    "energy": 3,
    "power": null,
    "might": 2,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Common",
    "artist": "Six More Vodka",
    "ability": "[Accelerate] (You may pay [1][C] as an additional cost to have me enter ready.)\nWhen you play me, give a unit +2 [S] this turn."
  },
  {
    "code": "rad-009-167",
    "name": "Antagonistic Apparatus",
    "energy": 6,
    "power": 1,
    "might": 3,
    "domains": [],
    "types": [],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Caravan Studio",
    "ability": "I enter ready.\nWhen you play me, play a ready 3 [S] Mech unit token here."
  },
  {
    "code": "rad-010-167",
    "name": "Forbidden Technique",
    "energy": 2,
    "power": null,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Spell"
    ],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Kudos Productions",
    "ability": "Ready a unit.\nWhen you play a card with Energy cost [7] or more, you may play this from your trash for [1][C][C]. Banish this."
  },
  {
    "code": "rad-011-167",
    "name": "Incorrigible Construct",
    "energy": 4,
    "power": null,
    "might": 4,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Six More Vodka",
    "ability": "[Show Off] a Mech. (As you play me, you may reveal a Mech from hand or pick another friendly Mech.)\nI enter ready if you showed off."
  },
  {
    "code": "rad-012-167",
    "name": "Runeblade Anvil",
    "energy": 1,
    "power": null,
    "might": null,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Kudos Productions",
    "ability": "When you play this or when this leaves the board, banish the top card of your Main Deck. Until your next Ending Phase, you may play that card. (You still pay its costs.)\n[2][C], [T]: Kill this."
  },
  {
    "code": "rad-013-167",
    "name": "Lost to the Sands",
    "energy": 3,
    "power": 1,
    "might": null,
    "domains": [
      "Fury"
    ],
    "types": [
      "Spell"
    ],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Kudos Productions",
    "ability": "[Action] (Play on your turn or in showdowns.)\nChoose one —\n- Deal 4 to a unit at a battlefield.\n- Kill a gear."
  },
  {
    "code": "rad-014-167",
    "name": "Shimmering Swimmer",
    "energy": 3,
    "power": null,
    "might": 3,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Uncommon",
    "artist": "Kudos Productions",
    "ability": "You may kill a friendly unit or gear as an additional cost to play me.\nWhen you play me, if you paid the additional cost, deal 2 to a unit."
  },
  {
    "code": "rad-015-167",
    "name": "Akali, Brash",
    "energy": 5,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Ionia",
      "Akali"
    ],
    "rarity": "Rare",
    "artist": "Luscinia Studio",
    "ability": "The first time I move each turn, [Add] [1][A].\nThe second time I move each turn, banish the top card of your Main Deck. Until your next Ending Phase, you may play that card. (You still pay its costs.)"
  },
  {
    "code": "rad-016-167",
    "name": "Coalesced Energy",
    "energy": 4,
    "power": 2,
    "might": 4,
    "domains": [
      "Mind"
    ],
    "types": [
      "Spell"
    ],
    "tags": [],
    "rarity": "Rare",
    "artist": "Michal Ivan",
    "ability": "At the end of your turn, recycle all your runes, then channel 4 runes exhausted."
  },
  {
    "code": "rad-017-167",
    "name": "Detonation Amplifier",
    "energy": 4,
    "power": 1,
    "might": null,
    "domains": [
      "Chaos"
    ],
    "types": [
      "Gear"
    ],
    "tags": [
      "Device"
    ],
    "rarity": "Rare",
    "artist": "Fish Art Studio",
    "ability": "[Deploy] (Play this only to a battlefield. When an opponent holds here, kill this.)\nWhen you play this, you may kill another friendly unit or gear to draw 1.\nFriendly [Deathknell] effects here trigger an additional time."
  },
  {
    "code": "rad-018-167",
    "name": "Drakehound Packmaster",
    "energy": 9,
    "power": 2,
    "might": 9,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Rare",
    "artist": "Six More Vodka",
    "ability": "When you play me, reveal the top card of each opponent's Main Deck. Choose one and banish it, then play it, ignoring its cost. Recycle the rest."
  },
  {
    "code": "rad-019-167",
    "name": "Firebrand Raider",
    "energy": 6,
    "power": 1,
    "might": 6,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit"
    ],
    "tags": [],
    "rarity": "Rare",
    "artist": "Six More Vodka",
    "ability": "I enter ready.\nWhen you play me, ready all enemy units.\nOther units enter ready.\nUnits don't ready during the Awaken Phase."
  },
  {
    "code": "rad-020a-167",
    "name": "Mordekaiser, Twice Slain",
    "energy": 7,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Noxus",
      "Mordekaiser"
    ],
    "rarity": "Showcase",
    "artist": "",
    "ability": "[Accelerate]\n[Assault 3]\nOnce each turn, if I would die, you may kill a friendly unit to heal me, exhaust me, and recall me instead."
  },
  {
    "code": "rad-020-167",
    "name": "Mordekaiser, Twice Slain",
    "energy": 7,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Noxus",
      "Mordekaiser"
    ],
    "rarity": "Rare",
    "artist": "",
    "ability": "[Accelerate] (You may pay [1][C] as an additional cost to have me enter ready.)\n[Assault 3] (+3 [S] while I'm an attacker.)\nOnce each turn, if I would die, you may kill a friendly unit to heal me, exhaust me, and recall me instead."
  },
  {
    "code": "rad-021-167",
    "name": "Ekko, Revolutionary",
    "energy": 5,
    "power": 1,
    "might": 4,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Ekko",
      "Zaun"
    ],
    "rarity": "Epic",
    "artist": "",
    "ability": "I enter ready.\n[Assault] (+1 [S] while I'm an attacker.)\n[Action][>] Discard 1, [1][C]: Banish me, then play me to your base, ignoring my cost."
  },
  {
    "code": "rad-021a-167",
    "name": "Ekko, Revolutionary",
    "energy": 5,
    "power": 1,
    "might": 4,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Ekko",
      "Zaun"
    ],
    "rarity": "Showcase",
    "artist": "黯荧岛Dark Glow",
    "ability": "I enter ready.\n[Assault]\n[Action][>] Discard 1, [1][C]: Banish me, then play me to your base, ignoring my cost."
  },
  {
    "code": "rad-022-167",
    "name": "Whirling Twirligig",
    "energy": 4,
    "power": 1,
    "might": null,
    "domains": [],
    "types": [],
    "tags": [],
    "rarity": "Epic",
    "artist": "Caravan Studio",
    "ability": "[T]: Play a Gold gear token exhausted. (It has \"[Reaction][>] Kill this, [T]: [Add] [A].\")\nKill a friendly gear, [T]: Play a 1 [S] Recruit unit token.\nKill a friendly unit, [T]: Play a 3 [S] Mech unit token."
  },
  {
    "code": "rad-023-167",
    "name": "Ziggs, Short-Fused",
    "energy": 5,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Bandle City",
      "Ziggs"
    ],
    "rarity": "Epic",
    "artist": "",
    "ability": "When you play me, play a Bomb gear token to a battlefield. (It has [Deploy]. When it dies, deal 2 to an enemy there and kill your other Bombs there.)\nAbilities of friendly gear deal 1 Bonus Damage."
  },
  {
    "code": "rad-023a-167",
    "name": "Ziggs, Short-Fused",
    "energy": 5,
    "power": 1,
    "might": 5,
    "domains": [
      "Fury"
    ],
    "types": [
      "Unit",
      "Champion"
    ],
    "tags": [
      "Bandle City",
      "Ziggs"
    ],
    "rarity": "Showcase",
    "artist": "Ziyan Lin",
    "ability": "When you play me, play a Bomb gear token to a battlefield.\nAbilities of friendly gear deal 1 Bonus Damage."
  }
]
$cards_json$::jsonb) AS x(
    code text,
    name text,
    energy integer,
    power integer,
    might integer,
    domains text[],
    types text[],
    tags text[],
    rarity text,
    artist text,
    ability text
);

ALTER TABLE card_details_seed ADD PRIMARY KEY (code);

DO $$
DECLARE
    matched_count integer;
BEGIN
    IF (SELECT count(*) FROM card_details_seed) <> 43 THEN
        RAISE EXCEPTION 'В исходных данных должны быть 43 карточки.';
    END IF;

    SELECT count(*) INTO matched_count
    FROM card_details_seed AS seed
    JOIN public.cards AS c ON c.code = seed.code
    JOIN public.card_sets AS s ON s.id = c.card_set_id
    WHERE s.code = 'rad';

    IF matched_count <> 43 THEN
        RAISE EXCEPTION 'В базе найдены не все 43 карточки Radiance: найдено %.', matched_count;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM card_details_seed AS seed
        CROSS JOIN LATERAL unnest(seed.domains) AS item(name)
        LEFT JOIN public.domains AS d ON d.code = lower(item.name)
        WHERE d.id IS NULL
    ) THEN
        RAISE EXCEPTION 'В справочнике domains отсутствует нужный домен. Проверь скрипт 002.';
    END IF;
END;
$$;

-- Добавляем недостающие типы.
INSERT INTO public.card_types (code, name)
SELECT DISTINCT lower(item.name), item.name
FROM card_details_seed AS seed
CROSS JOIN LATERAL unnest(seed.types) AS item(name)
ON CONFLICT (code) DO NOTHING;

-- Внутренние коды тегов: Kai'Sa -> kai-sa, The Void -> the-void.
INSERT INTO public.tags (code, name)
SELECT DISTINCT
    lower(regexp_replace(item.name, '[^a-zA-Z0-9]+', '-', 'g')),
    item.name
FROM card_details_seed AS seed
CROSS JOIN LATERAL unnest(seed.tags) AS item(name)
ON CONFLICT (code) DO NOTHING;

UPDATE public.cards AS c
SET name = seed.name,
    energy = seed.energy,
    power = seed.power,
    might = seed.might,
    rarity = seed.rarity,
    artist = seed.artist,
    ability = seed.ability,
    updated_at = CURRENT_TIMESTAMP
FROM card_details_seed AS seed
WHERE c.code = seed.code;

-- Заменяем связи этих карточек данными из источника.
-- Если поле пустое, у карточки не будет соответствующих связей.
DELETE FROM public.card_domains AS link
USING public.cards AS c, card_details_seed AS seed
WHERE link.card_id = c.id AND c.code = seed.code;

INSERT INTO public.card_domains (card_id, domain_id)
SELECT c.id, d.id
FROM card_details_seed AS seed
JOIN public.cards AS c ON c.code = seed.code
CROSS JOIN LATERAL unnest(seed.domains) AS item(name)
JOIN public.domains AS d ON d.code = lower(item.name);

DELETE FROM public.card_type_links AS link
USING public.cards AS c, card_details_seed AS seed
WHERE link.card_id = c.id AND c.code = seed.code;

INSERT INTO public.card_type_links (card_id, card_type_id)
SELECT c.id, t.id
FROM card_details_seed AS seed
JOIN public.cards AS c ON c.code = seed.code
CROSS JOIN LATERAL unnest(seed.types) AS item(name)
JOIN public.card_types AS t ON t.code = lower(item.name);

DELETE FROM public.card_tags AS link
USING public.cards AS c, card_details_seed AS seed
WHERE link.card_id = c.id AND c.code = seed.code;

INSERT INTO public.card_tags (card_id, tag_id)
SELECT c.id, t.id
FROM card_details_seed AS seed
JOIN public.cards AS c ON c.code = seed.code
CROSS JOIN LATERAL unnest(seed.tags) AS item(name)
JOIN public.tags AS t
    ON t.code = lower(regexp_replace(item.name, '[^a-zA-Z0-9]+', '-', 'g'));

DO $$
BEGIN
    IF (SELECT count(*) FROM public.card_domains AS l
        JOIN public.cards AS c ON c.id = l.card_id
        JOIN card_details_seed AS seed ON seed.code = c.code)
       <> (SELECT sum(cardinality(domains)) FROM card_details_seed) THEN
        RAISE EXCEPTION 'Не совпало количество связей с доменами.';
    END IF;
    IF (SELECT count(*) FROM public.card_type_links AS l
        JOIN public.cards AS c ON c.id = l.card_id
        JOIN card_details_seed AS seed ON seed.code = c.code)
       <> (SELECT sum(cardinality(types)) FROM card_details_seed) THEN
        RAISE EXCEPTION 'Не совпало количество связей с типами.';
    END IF;
    IF (SELECT count(*) FROM public.card_tags AS l
        JOIN public.cards AS c ON c.id = l.card_id
        JOIN card_details_seed AS seed ON seed.code = c.code)
       <> (SELECT sum(cardinality(tags)) FROM card_details_seed) THEN
        RAISE EXCEPTION 'Не совпало количество связей с тегами.';
    END IF;
END;
$$;

COMMIT;

-- Итог по набору Radiance.
SELECT
    count(*) AS total_cards,
    count(*) FILTER (WHERE btrim(c.rarity) <> '') AS cards_with_rarity,
    count(c.might) AS cards_with_might,
    count(*) FILTER (WHERE c.is_new) AS marked_as_new
FROM public.cards AS c
JOIN public.card_sets AS s ON s.id = c.card_set_id
WHERE s.code = 'rad';

-- Для детальной проверки выполни отдельно:
-- SELECT code, name, energy, power, might, rarity, artist, ability
-- FROM public.cards ORDER BY display_order, code;
