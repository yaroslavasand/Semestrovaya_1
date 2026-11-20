BEGIN;

DO $$
BEGIN
    IF current_database() <> 'oris_riftbound' THEN
        RAISE EXCEPTION
            'Неверная база: %. Ожидалась oris_riftbound.',
            current_database();
END IF;
END;
$$;

ALTER TABLE public.cards
    ADD COLUMN IF NOT EXISTS is_new boolean NOT NULL DEFAULT false;

COMMIT;

SELECT code, name, is_new
FROM public.cards
ORDER BY display_order, code;