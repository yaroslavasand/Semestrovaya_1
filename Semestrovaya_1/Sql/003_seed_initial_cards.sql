BEGIN;

-- Защита от выполнения в базе другого предмета.
DO $$
BEGIN
    IF current_database() <> 'oris_riftbound' THEN
        RAISE EXCEPTION
            'Неверная база: %. Ожидалась oris_riftbound.',
            current_database();
END IF;
END;
$$;

-- Набор, который сейчас показываем в галерее.
INSERT INTO public.card_sets (code, name, description)
VALUES ('rad', 'Radiance', '')
    ON CONFLICT (code) DO NOTHING;

-- Первые карточки для проверки загрузки из базы.
-- Пока добавляем только известные данные:
-- код, название, набор и путь к изображению.
INSERT INTO public.cards
(
    card_set_id,
    code,
    name,
    image_path
)
SELECT
    card_set.id,
    initial_card.code,
    initial_card.name,
    initial_card.image_path
FROM public.card_sets AS card_set
         CROSS JOIN
     (
         VALUES
             (
                 'rad-008-167',
                 'War Scout',
                 '/Static/images/cards/rad-008-167.webp'
             ),
             (
                 'rad-009-167',
                 'Antagonistic Apparatus',
                 '/Static/images/cards/rad-009-167.webp'
             )
     ) AS initial_card (code, name, image_path)
WHERE card_set.code = 'rad'
    ON CONFLICT (code) DO NOTHING;

COMMIT;

-- Проверяем добавленные карточки.
SELECT
    card.id,
    card.code,
    card.name,
    card_set.name AS set_name,
    card.image_path
FROM public.cards AS card
         JOIN public.card_sets AS card_set
              ON card_set.id = card.card_set_id
WHERE card.code IN ('rad-008-167', 'rad-009-167')
ORDER BY card.code;