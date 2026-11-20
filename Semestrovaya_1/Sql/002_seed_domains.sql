BEGIN;

-- Проверяем, что работаем в нужной базе.
DO $$
BEGIN
    IF current_database() <> 'oris_riftbound' THEN
        RAISE EXCEPTION
            'Неверная база: %. Ожидалась oris_riftbound.',
            current_database();
END IF;
END;
$$;

INSERT INTO public.domains (code, name, description)
VALUES
    ('fury',  'Fury',  ''),
    ('calm',  'Calm',  ''),
    ('mind',  'Mind',  ''),
    ('body',  'Body',  ''),
    ('chaos', 'Chaos', ''),
    ('order', 'Order', '')
    --если запись с таким кодом уже есть, скрипт просто пропустит её, а не упадёт
    ON CONFLICT (code) DO NOTHING;

COMMIT;

-- Показываем добавленные записи.
SELECT id, code, name
FROM public.domains
ORDER BY id;