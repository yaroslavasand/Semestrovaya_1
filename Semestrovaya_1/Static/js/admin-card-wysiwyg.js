"use strict";

// WYSIWYG-редактор текста способности карточки (Quill).
// Работает только на странице редактирования карточки:
// ищет форму с атрибутом data-card-form.

const cardForm = document.querySelector("[data-card-form]");

if (cardForm && typeof Quill !== "undefined") {
    const abilityHidden = cardForm.querySelector("#ability");
    const editorContainer = cardForm.querySelector("#ability-editor");

    const editor = new Quill(editorContainer, {
        theme: "snow",
        placeholder: "Текст способности карточки…",
        modules: {
            toolbar: [
                ["bold", "italic", "underline"],
                [{ list: "ordered" }, { list: "bullet" }],
                ["clean"]
            ]
        }
    });

    // Начальное содержимое — текущий текст карточки,
    // который сервер подставил в скрытое поле.
    editor.root.innerHTML = abilityHidden.value;

    cardForm.addEventListener("submit", () => {
        // Пустой редактор Quill отдаёт "<p><br></p>" —
        // в этом случае сохраняем пустую строку.
        abilityHidden.value = editor.getText().trim() === ""
            ? ""
            : editor.root.innerHTML;
    });
}
