(() => {
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
        } else {
            windowEl.classList.add("active");
            windowEl.style.display = "flex";
            windowEl.style.opacity = "1";
            windowEl.style.visibility = "visible";
            windowEl.style.pointerEvents = "auto";
            input.focus();
        }
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

        addMessage("Σκέφτομαι...", "bot", true);

        try {
            const response = await fetch("/AI/Chat", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    message: text
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText);
            }

            const data = await response.json();

            removeTyping();
            addMessage(data.message, "bot");

            if (data.requiresConfirmation && data.responseType === "offer_preview") {
                addPreviewButton(data, text);
            }
        }
        catch {
            removeTyping();
            addMessage("Κάτι πήγε στραβά. Δοκίμασε ξανά.", "bot");
        }
    });

    function addMessage(text, type, isTyping = false) {
        const div = document.createElement("div");
        div.className = `ai-chat-message ai-chat-message-${type}`;

        if (isTyping) {
            div.classList.add("ai-chat-typing-message");
        }

        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    }

    function removeTyping() {
        const typing = messages.querySelector(".ai-chat-typing-message");
        if (typing) typing.remove();
    }

    function addPreviewButton(data, originalPrompt) {
        const wrapper = document.createElement("div");
        wrapper.className = "ai-chat-action";

        const form = document.createElement("form");
        form.method = "post";
        form.action = data.actionUrl;

        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const tokenInput = document.createElement("input");
            tokenInput.type = "hidden";
            tokenInput.name = "__RequestVerificationToken";
            tokenInput.value = token.value;
            form.appendChild(tokenInput);
        }

        const draftInput = document.createElement("input");
        draftInput.type = "hidden";
        draftInput.name = "serializedDraft";
        draftInput.value = data.serializedDraft || "";
        form.appendChild(draftInput);

        const promptInput = document.createElement("input");
        promptInput.type = "hidden";
        promptInput.name = "originalPrompt";
        promptInput.value = originalPrompt;
        form.appendChild(promptInput);

        const button = document.createElement("button");
        button.type = "submit";
        button.className = "btn btn-success btn-sm app-btn";
        button.innerHTML = `<i class="bi bi-eye me-1"></i>${data.actionLabel || "Προβολή Preview"}`;

        form.appendChild(button);
        wrapper.appendChild(form);
        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }
})();