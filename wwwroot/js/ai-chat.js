//ai-chat.js

(() => {
    const AI_CHAT_HISTORY_KEY = "aiChatHistory_v3";

    const AI_WELCOME_MESSAGE =
        "Γεια σου! 👋\n\n" +
        "Μπορώ να βοηθήσω με:\n" +
        "• Δημιουργία draft προσφοράς\n" +
        "• Αλλαγές σε ανοιχτή προσφορά\n" +
        "• Αναζήτηση υλικών\n" +
        "• Σύνοψη προσφοράς\n\n" +
        "Παραδείγματα:\n" +
        "• Προσφορά για αντλία ABB με 2 inverter\n" +
        "• Άλλαξε το ODE-3-120023-1F12 σε 8 τεμ\n" +
        "• Βγάλε το Prisma cabinet";

    const RESPONSE_TYPES = {
        HELP: "help",
        OUT_OF_SCOPE: "out_of_scope",
        OFFER_PREVIEW: "offer_preview",
        OFFER_OPERATION_SUCCESS: "offer_operation_success",
        OFFER_OPERATION_FAILED: "offer_operation_failed",
        OFFER_OPERATION_MISSING_CONTEXT: "offer_operation_missing_context",
        OFFER_SUMMARY: "offer_summary",
        OFFER_SUMMARY_MISSING_CONTEXT: "offer_summary_missing_context",
        ERROR: "error"
    };

    const toggle = document.getElementById("aiPopupToggle");
    const windowEl = document.getElementById("aiPopupWindow");
    const close = document.getElementById("aiPopupClose");
    const form = document.getElementById("aiChatForm");
    const input = document.getElementById("aiChatInput");
    const messages = document.getElementById("aiChatMessages");

    if (!toggle || !windowEl || !close || !form || !input || !messages) return;

    toggle.addEventListener("click", () => {
        const isOpen = windowEl.classList.contains("active");

        if (isOpen) {
            windowEl.classList.remove("active");
            windowEl.style.display = "none";
            return;
        }

        windowEl.classList.add("active");
        windowEl.style.display = "flex";
        windowEl.style.opacity = "1";
        windowEl.style.visibility = "visible";
        windowEl.style.pointerEvents = "auto";
        input.focus();
    });

    close.addEventListener("click", () => {
        windowEl.classList.remove("active");
        windowEl.style.display = "none";
    });

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const text = input.value.trim();
        if (!text) return;

        addMessage(text, "user");
        input.value = "";
        setChatBusy(true);

        showTyping("Σκέφτομαι...");

        try {
            await handleChatMessage(text);
        }
        catch {
            await replyWithDelay("Κάτι πήγε στραβά. Δοκίμασε ξανά.", 700);
        }
        finally {
            setChatBusy(false);
            input.focus();
        }
    });

    async function handleChatMessage(text) {
        const offerId = document.querySelector(".offer-details-page")?.dataset.offerId || null;

        const response = await fetch("/AI/Chat", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                message: text,
                offerId: offerId ? Number(offerId) : null
            })
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const data = await response.json();

        await replyWithDelay(data.message, 1400);

        switch (data.responseType) {

            case RESPONSE_TYPES.OFFER_PREVIEW:
                if (data.requiresConfirmation) {
                    addPreviewButton(data, text);
                }
                break;

            case RESPONSE_TYPES.OFFER_OPERATION_SUCCESS:
                break;

            case RESPONSE_TYPES.OFFER_OPERATION_FAILED:
                break;

            case RESPONSE_TYPES.OFFER_OPERATION_MISSING_CONTEXT:
                break;

            case RESPONSE_TYPES.OUT_OF_SCOPE:
                break;

            case RESPONSE_TYPES.HELP:
                break;

            case RESPONSE_TYPES.OFFER_SUMMARY:
                break;

            case RESPONSE_TYPES.OFFER_SUMMARY_MISSING_CONTEXT:
                break;

            default:
                break;
        }
    }


       

    function addMessage(text, type, isTyping = false, skipSave = false) {
        const div = document.createElement("div");
        div.className = `ai-chat-message ai-chat-message-${type}`;

        if (isTyping) {
            div.classList.add("ai-chat-typing-message");
            div.innerHTML = `
                <span class="ai-thinking-dots">
                    <span></span>
                    <span></span>
                    <span></span>
                </span>
            `;
        }
        else {
            div.textContent = text;
        }

        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;

        if (!skipSave && !isTyping) {
            saveChatHistory();
        }
    }

    function showTyping(message) {
        removeTyping();
        addMessage(message, "bot", true);
    }

    function removeTyping() {
        const typing = messages.querySelector(".ai-chat-typing-message");
        if (typing) typing.remove();
    }

    async function replyWithDelay(message, ms = 1200) {
        await delay(ms);
        removeTyping();
        addMessage(message, "bot");
    }

    async function replyManyWithDelay(items, ms = 1200) {
        await delay(ms);
        removeTyping();

        items.forEach(message => addMessage(message, "bot"));
    }

    function setChatBusy(isBusy) {
        input.disabled = isBusy;

        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            submitButton.disabled = isBusy;
            submitButton.classList.toggle("btn-loading", isBusy);
        }
    }



    function saveChatHistory() {
        const items = [...messages.querySelectorAll(".ai-chat-message")]
            .filter(x =>
                !x.classList.contains("ai-chat-typing-message") &&
                x.textContent.trim() !== AI_WELCOME_MESSAGE.trim()
            )
            .map(x => ({
                text: x.textContent,
                type: x.classList.contains("ai-chat-message-user") ? "user" : "bot"
            }));

        sessionStorage.setItem(AI_CHAT_HISTORY_KEY, JSON.stringify(items.slice(-30)));
    }

    function loadChatHistory() {
        messages.innerHTML = "";

        addMessage(AI_WELCOME_MESSAGE, "bot", false, true);
        addQuickActions();
        const raw = sessionStorage.getItem(AI_CHAT_HISTORY_KEY);

        if (!raw) {
            return;
        }

        try {
            const items = JSON.parse(raw);

            if (!Array.isArray(items) || items.length === 0) {
                return;
            }

            items.forEach(item => {
                if (!item?.text || !item?.type) return;
                addMessage(item.text, item.type, false, true);
            });
        }
        catch {
            sessionStorage.removeItem(AI_CHAT_HISTORY_KEY);
        }
    }

    function addPreviewButton(data, originalPrompt) {
        const wrapper = document.createElement("div");
        wrapper.className = "ai-chat-action";

        const previewForm = document.createElement("form");
        previewForm.method = "post";
        previewForm.action = data.actionUrl;

        const token = document.querySelector('input[name="__RequestVerificationToken"]');

        if (token) {
            const tokenInput = document.createElement("input");
            tokenInput.type = "hidden";
            tokenInput.name = "__RequestVerificationToken";
            tokenInput.value = token.value;
            previewForm.appendChild(tokenInput);
        }

        const draftInput = document.createElement("input");
        draftInput.type = "hidden";
        draftInput.name = "serializedDraft";
        draftInput.value = data.serializedDraft || "";
        previewForm.appendChild(draftInput);

        const promptInput = document.createElement("input");
        promptInput.type = "hidden";
        promptInput.name = "originalPrompt";
        promptInput.value = originalPrompt;
        previewForm.appendChild(promptInput);

        const button = document.createElement("button");
        button.type = "submit";
        button.className = "btn btn-success btn-sm app-btn";
        button.innerHTML = `<i class="bi bi-eye me-1"></i>${data.actionLabel || "Προβολή Preview"}`;

        previewForm.appendChild(button);
        wrapper.appendChild(previewForm);
        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    function delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }


    function addQuickActions() {
        const wrapper = document.createElement("div");
        wrapper.className = "ai-chat-quick-actions";

        const actions = [
            {
                label: "Draft προσφοράς",
                prompt: "Δημιούργησε προσφορά για πελάτη με υλικά, ποσότητες, εργατικά και κέρδος."
            },
            {
                label: "Αναζήτηση υλικού",
                prompt: "Βρες υλικό [κωδικός ή περιγραφή]"
            },
            {
                label: "Αλλαγή ποσότητας",
                prompt: "Άλλαξε το [κωδικός υλικού] σε [ποσότητα] τεμ"
            },
            {
                label: "Αφαίρεση υλικού",
                prompt: "Βγάλε το [κωδικός ή περιγραφή υλικού]"
            },
            {
                label: "Σύνοψη προσφοράς",
                prompt: "Κάνε σύνοψη της ανοιχτής προσφοράς"
            }
        ];

        actions.forEach(action => {
            const button = document.createElement("button");

            button.type = "button";
            button.className = "ai-chat-chip";
            button.textContent = action.label;

            button.addEventListener("click", () => {
                input.value = action.prompt;
                input.focus();
            });

            wrapper.appendChild(button);
        });

        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    loadChatHistory();
})();