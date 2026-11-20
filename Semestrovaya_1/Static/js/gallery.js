"use strict";

function initializeGallery() {
    const gallery = document.querySelector(
        '[data-testid="riftbound-card-gallery"]'
    );

    if (!gallery || gallery.dataset.initialized === "true") {
        return;
    }

    gallery.dataset.initialized = "true";
    const queryParameters = new URLSearchParams(
        window.location.search
    );

    const supportedSorts = new Set([
        "gallery",
        "card-asc",
        "card-desc",
        "name-asc",
        "name-desc",
        "might-asc",
        "might-desc",
        "rarity-asc",
        "rarity-desc"
    ]);

    const requestedSort = queryParameters.get("sort");

    const selectedSort = supportedSorts.has(requestedSort)
        ? requestedSort
        : "gallery";

    const supportedSets = new Set([
        "radiance",
        "vendetta",
        "unleashed",
        "spiritforged",
        "origins-main",
        "origins-proving-grounds",
        "all"
    ]);

    const requestedSet = queryParameters.get("set");

    const selectedSet = supportedSets.has(requestedSet)
        ? requestedSet
        : "radiance";

    const selectedNewCards =
        queryParameters.get("new")?.toLowerCase() === "true";

    const searchInput = gallery.querySelector(
        '[data-testid="search-input-input-element"]'
    );

    const searchForm = gallery.querySelector(
        'form[data-testid="search-input"]'
    );

    if (searchInput && searchForm) {
        searchInput.value = queryParameters.get("q") ?? "";

        const sortInput = document.createElement("input");

        sortInput.type = "hidden";
        sortInput.name = "sort";
        sortInput.value = selectedSort;

        searchForm.append(sortInput);
        const setInput = document.createElement("input");

        setInput.type = "hidden";
        setInput.name = "set";
        setInput.value = selectedSet;

        searchForm.append(setInput);
        const newCardsInput = document.createElement("input");

        newCardsInput.type = "hidden";
        newCardsInput.name = "new";
        newCardsInput.value = String(selectedNewCards);

        searchForm.append(newCardsInput);

        searchForm.addEventListener("submit", () => {
            searchInput.value = searchInput.value.trim();
        });
    }

    const filtersButton = gallery.querySelector(".sc-5617087-3");
    const filtersButtonText = filtersButton.querySelector(".sc-5617087-4");
    const filtersPanel = gallery.querySelector(".sc-5617087-2");

    filtersPanel.id = "gallery-filters";
    filtersButton.type = "button";
    filtersButton.removeAttribute("aria-disabled");
    filtersButton.setAttribute("aria-controls", filtersPanel.id);

    function setPanelOpen(isOpen) {
        gallery.classList.toggle("options-panel-closed", !isOpen);

        filtersButtonText.textContent = isOpen
            ? "Hide Filters"
            : "Show Filters";

        filtersButton.setAttribute("aria-expanded", String(isOpen));
        filtersButton.setAttribute(
            "aria-label",
            isOpen ? "Hide Filters" : "Show Filters"
        );
    }

    filtersButton.addEventListener("click", () => {
        const isClosed = gallery.classList.contains("options-panel-closed");
        setPanelOpen(isClosed);
    });

    setPanelOpen(false);

    const sortOptions = [
        ["gallery", "Default order"],
        ["card-asc", "Card # Ascending"],
        ["card-desc", "Card # Descending"],
        ["rarity-asc", "Rarity Ascending"],
        ["rarity-desc", "Rarity Descending"],
        ["might-asc", "Might Ascending"],
        ["might-desc", "Might Descending"],
        ["name-asc", "Name (A-Z)"],
        ["name-desc", "Name (Z-A)"]
    ];

    const setOptions = [
        ["radiance", "Radiance"],
        ["vendetta", "Vendetta"],
        ["unleashed", "Unleashed"],
        ["spiritforged", "Spiritforged"],
        ["origins-main", "Origins - Main Set"],
        ["origins-proving-grounds", "Origins - Proving Grounds"],
        ["all", "All Sets"]
    ];

    gallery.dataset.sort = selectedSort;
    gallery.dataset.set = selectedSet;
    gallery.dataset.newCards = String(selectedNewCards);

    function initializeAccordion(
        containerTestId,
        options,
        selectedValue,
        stateKey,
        initiallyOpen
    ) {
        const container = gallery.querySelector(
            `[data-testid="${containerTestId}"]`
        );

        const trigger = container.querySelector("h3 button");
        const header = container.querySelector("h3");
        const content = container.querySelector(
            '[data-testid="accordion-content"]'
        );

        trigger.removeAttribute("aria-disabled");

        const optionButtons = [];

        function selectOption(value) {
            gallery.dataset[stateKey] = value;

            optionButtons.forEach((button) => {
                button.setAttribute(
                    "aria-pressed",
                    String(button.dataset.value === value)
                );
            });
        }

        const fragment = document.createDocumentFragment();

        options.forEach(([value, text]) => {
            const button = document.createElement("button");

            button.type = "button";
            button.className = "gallery-filter-option";
            button.dataset.value = value;

            const caption = document.createElement("span");
            caption.textContent = text;

            const checkmark = document.createElement("span");
            checkmark.className = "gallery-option-check";
            checkmark.setAttribute("aria-hidden", "true");

            button.append(caption, checkmark);

            button.addEventListener("click", () => {
                selectOption(value);

                if (stateKey !== "sort" && stateKey !== "set") {
                    return;
                }

                const url = new URL(window.location.href);

                const searchText = (
                    searchInput?.value
                    ?? url.searchParams.get("q")
                    ?? ""
                ).trim();

                if (searchText) {
                    url.searchParams.set("q", searchText);
                } else {
                    url.searchParams.delete("q");
                }

                if (stateKey === "sort") {
                    if (value === "gallery") {
                        url.searchParams.delete("sort");
                    } else {
                        url.searchParams.set("sort", value);
                    }
                }

                if (stateKey === "set") {
                    url.searchParams.set("set", value);
                }

                window.location.assign(url.href);
            });

            optionButtons.push(button);
            fragment.append(button);
        });

        content.replaceChildren(fragment);
        selectOption(selectedValue);

        function setAccordionOpen(isOpen) {
            const state = isOpen ? "open" : "closed";

            container.dataset.state = state;
            header.dataset.state = state;
            trigger.dataset.state = state;
            content.dataset.state = state;

            trigger.setAttribute("aria-expanded", String(isOpen));
            content.hidden = !isOpen;
        }

        trigger.addEventListener("click", () => {
            const isOpen = trigger.getAttribute("aria-expanded") === "true";
            setAccordionOpen(!isOpen);
        });

        setAccordionOpen(initiallyOpen);
    }

    initializeAccordion(
        "sortby-accordion",
        sortOptions,
        selectedSort,
        "sort",
        true
    );

    initializeAccordion(
        "card-sets-input",
        setOptions,
        selectedSet,
        "set",
        false
    );
    const newCardsSwitch = gallery.querySelector("#newCards");
    const switchThumb = newCardsSwitch.querySelector(".thumb");
    const switchCheckmark = newCardsSwitch.querySelector(".icon-checked");

    newCardsSwitch.removeAttribute("aria-disabled");

    function setNewCardsChecked(isChecked) {
        const state = isChecked ? "checked" : "unchecked";

        newCardsSwitch.setAttribute("aria-checked", String(isChecked));
        newCardsSwitch.dataset.state = state;
        switchThumb.dataset.state = state;
        switchCheckmark.hidden = !isChecked;
        gallery.dataset.newCards = String(isChecked);
    }

    newCardsSwitch.addEventListener("click", () => {
        const isChecked =
            newCardsSwitch.getAttribute("aria-checked") === "true";

        const nextChecked = !isChecked;

        setNewCardsChecked(nextChecked);

        const url = new URL(window.location.href);

        const searchText = (
            searchInput?.value
            ?? url.searchParams.get("q")
            ?? ""
        ).trim();

        if (searchText) {
            url.searchParams.set("q", searchText);
        } else {
            url.searchParams.delete("q");
        }

        if (nextChecked) {
            url.searchParams.set("new", "true");
        } else {
            url.searchParams.delete("new");
        }

        window.location.assign(url.href);
    });

    setNewCardsChecked(selectedNewCards);
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializeGallery);
} else {
    initializeGallery();
}