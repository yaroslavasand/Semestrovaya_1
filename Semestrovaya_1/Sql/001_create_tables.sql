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

-- Наборы карточек.
CREATE TABLE public.card_sets
(
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        varchar(30) NOT NULL UNIQUE,
    name        varchar(150) NOT NULL,
    description text NOT NULL DEFAULT '',

    CONSTRAINT card_sets_code_not_empty CHECK (btrim(code) <> ''),
    CONSTRAINT card_sets_name_not_empty CHECK (btrim(name) <> '')
);

-- Домены: Order, Fury и другие.
CREATE TABLE public.domains
(
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        varchar(30) NOT NULL UNIQUE,
    name        varchar(100) NOT NULL,
    description text NOT NULL DEFAULT '',

    CONSTRAINT domains_code_not_empty CHECK (btrim(code) <> ''),
    CONSTRAINT domains_name_not_empty CHECK (btrim(name) <> '')
);

-- Типы карточек: Unit, Champion, Spell и другие.
CREATE TABLE public.card_types
(
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        varchar(30) NOT NULL UNIQUE,
    name        varchar(100) NOT NULL,
    description text NOT NULL DEFAULT '',

    CONSTRAINT card_types_code_not_empty CHECK (btrim(code) <> ''),
    CONSTRAINT card_types_name_not_empty CHECK (btrim(name) <> '')
);

-- Теги: Piltover, Seraphine и другие.
CREATE TABLE public.tags
(
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        varchar(80) NOT NULL UNIQUE,
    name        varchar(100) NOT NULL,
    description text NOT NULL DEFAULT '',

    CONSTRAINT tags_code_not_empty CHECK (btrim(code) <> ''),
    CONSTRAINT tags_name_not_empty CHECK (btrim(name) <> '')
);

-- Карточки и их характеристики.
CREATE TABLE public.cards
(
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    card_set_id integer NOT NULL REFERENCES public.card_sets (id),

    code        varchar(50) NOT NULL UNIQUE,
    name        varchar(200) NOT NULL,
    image_path  varchar(500) NOT NULL,
    ability     text NOT NULL DEFAULT '',

    energy      integer,
    power       integer,
    might       integer,

    rarity      varchar(80) NOT NULL DEFAULT '',
    artist      varchar(200) NOT NULL DEFAULT '',

    created_at  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT cards_code_not_empty CHECK (btrim(code) <> ''),
    CONSTRAINT cards_name_not_empty CHECK (btrim(name) <> ''),
    CONSTRAINT cards_image_path_not_empty CHECK (btrim(image_path) <> ''),

    CONSTRAINT cards_energy_not_negative CHECK (energy >= 0),
    CONSTRAINT cards_power_not_negative CHECK (power >= 0),
    CONSTRAINT cards_might_not_negative CHECK (might >= 0)
);

-- У карточки может быть несколько доменов.
CREATE TABLE public.card_domains
(
    card_id   integer NOT NULL
        REFERENCES public.cards (id) ON DELETE CASCADE,

    domain_id integer NOT NULL
        REFERENCES public.domains (id),

    PRIMARY KEY (card_id, domain_id)
);

-- У карточки может быть несколько типов, например Unit и Champion.
CREATE TABLE public.card_type_links
(
    card_id      integer NOT NULL
        REFERENCES public.cards (id) ON DELETE CASCADE,

    card_type_id integer NOT NULL
        REFERENCES public.card_types (id),

    PRIMARY KEY (card_id, card_type_id)
);

-- У карточки может быть несколько тегов.
CREATE TABLE public.card_tags
(
    card_id integer NOT NULL
        REFERENCES public.cards (id) ON DELETE CASCADE,

    tag_id  integer NOT NULL
        REFERENCES public.tags (id),

    PRIMARY KEY (card_id, tag_id)
);

-- Индексы для связей и фильтрации.
CREATE INDEX ix_cards_card_set_id
    ON public.cards (card_set_id);

CREATE INDEX ix_card_domains_domain_id
    ON public.card_domains (domain_id);

CREATE INDEX ix_card_type_links_card_type_id
    ON public.card_type_links (card_type_id);

CREATE INDEX ix_card_tags_tag_id
    ON public.card_tags (tag_id);

COMMIT;