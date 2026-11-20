//превращает плитку в кнопку и по клику по карточке Серафины 
// просит у сервера адрес /cards/seraphine
"use strict";
function replaceMightSymbols(container) {
    const ability = container.querySelector(
        ".card-details-ability"
    );

    if (!ability) {
        return;
    }

    ability.innerHTML = ability.innerHTML.replaceAll(
        "[S]",
        '<img src="/Static/images/icons/might.svg"'
        + ' alt="Might"'
        + ' class="card-details-ability-icon">'
    );
}
function decorateCardTypes(container) {
    const typesContainer = container.querySelector(
        "[data-card-detail-types]"
    );

    if (!typesContainer) {
        return;
    }

    const typeIcons = {
        Unit: "/Static/images/icons/unit.webp",
        Champion: "/Static/images/icons/champion.webp"
    };

    const typeOrder = {
        Unit: 0,
        Champion: 1
    };

    const types = typesContainer.textContent
        .split(",")
        .map((type) => type.trim())
        .filter(Boolean)
        .sort((first, second) => {
            return (typeOrder[first] ?? 99)
                - (typeOrder[second] ?? 99);
        });

    const fragment = document.createDocumentFragment();

    types.forEach((type) => {
        const item = document.createElement("span");
        item.className = "card-details-type";

        const iconPath = typeIcons[type];

        if (iconPath) {
            const icon = document.createElement("img");

            icon.src = iconPath;
            icon.alt = "";
            icon.width = 24;
            icon.height = 24;
            icon.className = "card-details-icon";

            item.append(icon);
        }

        const caption = document.createElement("span");
        caption.textContent = type;

        item.append(caption);
        fragment.append(item);
    });

    typesContainer.replaceChildren(fragment);
}
function initializeCardDetails() {
    const card = document.querySelector(
        '.gallery-card[data-card-id="rad-sp5-005"]'
    );

    if (!card || card.dataset.detailsInitialized === "true") {
        return;
    }

    const button = document.createElement("button");

    button.type = "button";
    button.className = card.className + " gallery-card--details";
    button.dataset.cardId = card.dataset.cardId;
    button.dataset.detailsInitialized = "true";

    button.setAttribute(
        "aria-label",
        "Open details: Seraphine, Not Alone"
    );

    button.setAttribute("aria-haspopup", "dialog");

    while (card.firstChild) {
        button.append(card.firstChild);
    }

    card.replaceWith(button);

    const dialog = document.createElement("dialog");

    dialog.className = "card-details-dialog";
    dialog.setAttribute(
        "aria-label",
        "Seraphine, Not Alone — card details"
    );

    const closeButton = document.createElement("button");

    closeButton.type = "button";
    closeButton.className = "card-details-dialog__close";
    closeButton.textContent = "";
    closeButton.setAttribute("aria-label", "Close card details");
    closeButton.autofocus = true;

    const content = document.createElement("div");
    content.className = "card-details-dialog__content";

    dialog.append(closeButton, content);
    document.body.append(dialog);

    let activeRequest = null;

    closeButton.addEventListener("click", () => {
        dialog.close();
    });

    dialog.addEventListener("close", () => {
        activeRequest?.abort();
        activeRequest = null;
    });

    button.addEventListener("click", async () => {
        activeRequest?.abort();

        const controller = new AbortController();
        activeRequest = controller;

        content.textContent = "Loading…";
        content.setAttribute("aria-busy", "true");

        dialog.showModal();

        try {
            //JS отправляет фоновый запрос на сервер. AJAX 
            const response = await fetch("/cards/seraphine", {
                signal: controller.signal
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
            
            //Сервер отдаёт HTML деталей карты, JS забирает его как текст
            const html = await response.text();

            if (activeRequest !== controller || !dialog.open) {
                return;
            }
            //вставка внутрь модального окна
            content.innerHTML = html;
            // JS причёсывает вставленный кусок
            replaceMightSymbols(content);
            decorateCardTypes(content);
        } catch (error) {
            if (controller.signal.aborted) {
                return;
            }

            if (activeRequest === controller && dialog.open) {
                content.textContent =
                    "Не удалось загрузить карточку. Закрой окно и попробуй ещё раз.";
            }

            console.error("Ошибка загрузки карточки:", error);
        } finally {
            if (activeRequest === controller) {
                content.removeAttribute("aria-busy");
            }
        }
    });
}

if (document.readyState === "loading") {
    document.addEventListener(
        "DOMContentLoaded",
        initializeCardDetails
    );
} else {
    initializeCardDetails();
}