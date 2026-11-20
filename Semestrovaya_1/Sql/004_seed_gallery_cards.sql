--все карточки галереи
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'oris_riftbound' THEN
        RAISE EXCEPTION 'Неверная база: %. Ожидалась oris_riftbound.', current_database();
    END IF;
END;
$$;

-- Отдельный порядок галереи, не игровой номер карточки.
ALTER TABLE public.cards
    ADD COLUMN IF NOT EXISTS display_order integer NOT NULL DEFAULT 0;

INSERT INTO public.card_sets (code, name, description)
VALUES ('rad', 'Radiance', '')
ON CONFLICT (code) DO NOTHING;

CREATE TEMP TABLE gallery_seed
(
    display_order integer PRIMARY KEY,
    code text NOT NULL UNIQUE,
    name text NOT NULL,
    image_path text NOT NULL
) ON COMMIT DROP;

INSERT INTO gallery_seed (display_order, code, name, image_path)
VALUES
    (1, 'rad-sp1-005', 'Ahri, Confident', '/Static/images/cards/rad-sp1-005.webp'),
    (2, 'rad-001-167', 'Amateur Demolitionist', '/Static/images/cards/rad-001-167.webp'),
    (3, 'rad-r01', 'Fury Rune', '/Static/images/cards/rad-r01.webp'),
    (4, 'rad-r01a', 'Fury Rune — Showcase', '/Static/images/cards/rad-r01a.webp'),
    (5, 'rad-sp2-005', 'Akali, Brash', '/Static/images/cards/rad-sp2-005.webp'),
    (6, 'rad-002-167', 'Banner of Conquest', '/Static/images/cards/rad-002-167.webp'),
    (7, 'rad-r02', 'Calm Rune', '/Static/images/cards/rad-r02.webp'),
    (8, 'rad-r02a', 'Calm Rune', '/Static/images/cards/rad-r02a.webp'),
    (9, 'rad-003-167', 'Bilgewater Tracker', '/Static/images/cards/rad-003-167.webp'),
    (10, 'rad-sp3-005', 'Evelynn, In Control', '/Static/images/cards/rad-sp3-005.webp'),
    (11, 'rad-r03a', 'Mind Rune', '/Static/images/cards/rad-r03a.webp'),
    (12, 'rad-r03', 'Mind Rune', '/Static/images/cards/rad-r03.webp'),
    (13, 'rad-r04', 'Body Rune', '/Static/images/cards/rad-r04.webp'),
    (14, 'rad-r04a', 'Body Rune', '/Static/images/cards/rad-r04a.webp'),
    (15, 'rad-004-167', 'Brawl', '/Static/images/cards/rad-004-167.webp'),
    (16, 'rad-sp4-005', 'Kai''Sa, Rebel', '/Static/images/cards/rad-sp4-005.webp'),
    (17, 'rad-r05', 'Chaos Rune', '/Static/images/cards/rad-r05.webp'),
    (18, 'rad-r05a', 'Chaos Rune', '/Static/images/cards/rad-r05a.webp'),
    (19, 'rad-005-167', 'Lingering Animus', '/Static/images/cards/rad-005-167.webp'),
    (20, 'rad-sp5-005', 'Seraphine, Not Alone', '/Static/images/cards/rad-sp5-005.webp'),
    (21, 'rad-r06', 'Order Rune', '/Static/images/cards/rad-r06.webp'),
    (22, 'rad-r06a', 'Order Rune', '/Static/images/cards/rad-r06a.webp'),
    (23, 'rad-006-167', 'Scorched Earth', '/Static/images/cards/rad-006-167.webp'),
    (24, 'rad-007-167', 'Rally for War', '/Static/images/cards/rad-007-167.webp'),
    (25, 'rad-008-167', 'War Scout', '/Static/images/cards/rad-008-167.webp'),
    (26, 'rad-009-167', 'Antagonistic Apparatus', '/Static/images/cards/rad-009-167.webp'),
    (27, 'rad-010-167', 'Forbidden Technique', '/Static/images/cards/rad-010-167.webp'),
    (28, 'rad-011-167', 'Incorrigible Construct', '/Static/images/cards/rad-011-167.webp'),
    (29, 'rad-012-167', 'Runeblade Anvil', '/Static/images/cards/rad-012-167.webp'),
    (30, 'rad-013-167', 'Lost to the Sands', '/Static/images/cards/rad-013-167.webp'),
    (31, 'rad-014-167', 'Shimmering Swimmer', '/Static/images/cards/rad-014-167.webp'),
    (32, 'rad-015-167', 'Akali, Brash', '/Static/images/cards/rad-015-167.webp'),
    (33, 'rad-016-167', 'Coalesced Energy', '/Static/images/cards/rad-016-167.webp'),
    (34, 'rad-017-167', 'Detonation Amplifier', '/Static/images/cards/rad-017-167.webp'),
    (35, 'rad-018-167', 'Drakehound Packmaster', '/Static/images/cards/rad-018-167.webp'),
    (36, 'rad-019-167', 'Firebrand Raider', '/Static/images/cards/rad-019-167.webp'),
    (37, 'rad-020-167', 'Mordekaiser, Twice Slain', '/Static/images/cards/rad-020-167.webp'),
    (38, 'rad-020a-167', 'Mordekaiser, Twice Slain', '/Static/images/cards/rad-020a-167.webp'),
    (39, 'rad-021-167', 'Ekko, Revolutionary', '/Static/images/cards/rad-021-167.webp'),
    (40, 'rad-021a-167', 'Ekko, Revolutionary', '/Static/images/cards/rad-021a-167.webp'),
    (41, 'rad-022-167', 'Whirling Twirligig', '/Static/images/cards/rad-022-167.webp'),
    (42, 'rad-023a-167', 'Ziggs, Short-Fused', '/Static/images/cards/rad-023a-167.webp'),
    (43, 'rad-023-167', 'Ziggs, Short-Fused', '/Static/images/cards/rad-023-167.webp');

-- У существующих карточек обновляем только порядок показа.
-- Уже заполненные характеристики, названия и изображения не перезаписываем.
INSERT INTO public.cards (card_set_id, code, name, image_path, display_order)
SELECT s.id, g.code, g.name, g.image_path, g.display_order
FROM gallery_seed AS g
CROSS JOIN public.card_sets AS s
WHERE s.code = 'rad'
ON CONFLICT (code) DO UPDATE
SET display_order = EXCLUDED.display_order;

DO $$
DECLARE
    imported_count integer;
BEGIN
    SELECT count(*) INTO imported_count
    FROM public.cards AS c
    JOIN gallery_seed AS g ON g.code = c.code
    JOIN public.card_sets AS s ON s.id = c.card_set_id
    WHERE s.code = 'rad';

    IF imported_count <> 43 THEN
        RAISE EXCEPTION 'Ожидались 43 карточки Radiance, найдено: %.', imported_count;
    END IF;
END;
$$;

COMMIT;

SELECT c.display_order, c.code, c.name, c.image_path
FROM public.cards AS c
JOIN public.card_sets AS s ON s.id = c.card_set_id
WHERE s.code = 'rad'
ORDER BY c.display_order, c.code;
