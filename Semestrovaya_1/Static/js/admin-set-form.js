"use strict";

const setForm = document.querySelector("[data-set-form]");

if (setForm) {
    const code = setForm.querySelector("#code");
    const name = setForm.querySelector("#name");
    const description = setForm.querySelector("#description");

    const codeError = setForm.querySelector("#code-error");
    const nameError = setForm.querySelector("#name-error");
    const descriptionError = setForm.querySelector("#description-error");
    const counter = setForm.querySelector("#description-count");

    const codePattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
    const namePattern = /^[^\p{Cc}]+$/u;

    function showError(input, output, message) {
        output.textContent = message;
        input.setAttribute("aria-invalid", String(Boolean(message)));
    }

    function updateCounter() {
        counter.textContent =
            `${description.value.length} / ${description.maxLength} символов`;
    }

    function validate() {
        code.value = code.value.trim();
        name.value = name.value.trim();
        description.value = description.value.trim();

        const codeMessage =
            code.value.length < 1
            || code.value.length > code.maxLength
            || !codePattern.test(code.value)
                ? `Код: 1–${code.maxLength} символов. `
                + "Маленькие латинские буквы, цифры "
                + "и одиночные дефисы между частями."
                : "";

        const nameMessage =
            name.value.length < 1
            || name.value.length > name.maxLength
            || !namePattern.test(name.value)
                ? `Название: 1–${name.maxLength} символов `
                + "без управляющих символов."
                : "";

        const descriptionMessage =
            description.value.length > description.maxLength
                ? `Описание: не более ${description.maxLength} символов.`
                : "";

        showError(code, codeError, codeMessage);
        showError(name, nameError, nameMessage);
        showError(description, descriptionError, descriptionMessage);

        updateCounter();

        return !codeMessage && !nameMessage && !descriptionMessage;
    }

    setForm.noValidate = true;

    setForm.addEventListener("submit", (event) => {
        if (!validate()) {
            event.preventDefault();
            setForm.querySelector('[aria-invalid="true"]')?.focus();
        }
    });

    [code, name, description].forEach((input) => {
        input.addEventListener("input", () => {
            input.removeAttribute("aria-invalid");

            setForm.querySelector(
                `#${input.id}-error`
            ).textContent = "";
        });
    });

    description.addEventListener("input", updateCounter);
    updateCounter();
}