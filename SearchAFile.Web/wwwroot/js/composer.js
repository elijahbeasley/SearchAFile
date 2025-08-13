(function () {
    // --- Configuration hooks (plug into your app) -----------------------------
    // When the user sends a message:
    function onSend(text) {
        // Prefer your existing Send() if present:
        if (typeof window.Send === 'function') {
            window.Send();
        } else {
            // Or emit an event your code can listen for:
            document.dispatchEvent(new CustomEvent('saf-composer:submit', { detail: { text } }));
        }
    }
    // When the user clicks New Chat:
    function onNewChat() {
        // If you have a StartNewChat() function, call it:
        if (typeof window.StartNewChat === 'function') {
            window.StartNewChat();
        } else {
            document.dispatchEvent(new CustomEvent('saf-composer:newchat'));
        }
    }

    // --- Elements -------------------------------------------------------------
    const wrap = document.querySelector('.saf-composer-wrap');
    const ta = document.getElementById('txtQuestion');
    const btnSend = document.querySelector('.saf-btn.saf-send');
    const btnReset = document.querySelector('.saf-btn.saf-reset');

    if (!wrap || !ta || !btnSend || !btnReset) return;

    // --- Auto-size the textarea like ChatGPT ----------------------------------
    function autosize(el) {
        el.style.height = 'auto';
        const cap = parseInt(getComputedStyle(el).maxHeight, 10) || 200;
        el.style.height = Math.min(el.scrollHeight, cap) + 'px';
    }
    autosize(ta);
    ta.addEventListener('input', () => autosize(ta));

    // --- Send handlers ---------------------------------------------------------
    function trySend() {

        const text = (ta.value || '').trim();
        onSend(text);
        return;

        if (!text) {
            ta.classList.add('input-validation-error');
            ta.focus();

            window.top.ShowToast("danger", "The following errors have occured", '<ul><li>Question is required.</li></ul>', 0, false);
            return;
        }
        ta.classList.remove('input-validation-error');
        onSend(text);
    }

    btnSend.addEventListener('click', trySend);

    // Enter = send; Shift+Enter = newline (Ctrl+Enter also sends)
    ta.addEventListener('keydown', (e) => {
        const isEnter = (e.key === 'Enter' || e.keyCode === 13);
        if (!isEnter) return;

        // Send on Enter unless Shift is held; also send on Ctrl+Enter
        if ((!e.shiftKey) || e.ctrlKey) {
            e.preventDefault();
            trySend();
        }
    });

    // --- New Chat --------------------------------------------------------------
    btnReset.addEventListener('click', () => {
        // Optional confirm — add your own UI if preferred
        if (confirm('Start a new chat? This will clear the current conversation.')) {
            onNewChat();
        }
    });

    // --- Keep chat content visible above the composer --------------------------
    // Add .saf-pad-bottom to your scrollable chat body container in code,
    // or do it here by finding a likely element:
    // Example: if you use #divLoadingBlock as the scroller:
    const scroller = document.getElementById('divLoadingBlock');
    if (scroller) scroller.classList.add('saf-pad-bottom');

    // --- Helper to reset after send (call from your app when done) ------------
    // Example usage after you append bot response:
    //   window.SAF_Composer.reset();
    window.SAF_Composer = {
        reset: function () {
            ta.disabled = false;
            ta.value = '';
            autosize(ta);
            ta.focus();
        },
        focus: () => ta.focus()
    };
})();