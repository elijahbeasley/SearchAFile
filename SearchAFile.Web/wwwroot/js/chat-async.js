(function () {
    let isBusy = false;

    // ---- Busy UI helpers ---------------------------------------------------
    function setComposerBusy(on) {
        isBusy = !!on;
        const $wrap = $('.saf-composer');
        $wrap.toggleClass('is-busy', isBusy);
        $wrap.find('button, textarea').prop('disabled', isBusy);
        $wrap.attr('aria-busy', isBusy ? 'true' : 'false');
    }
    function composerIsBusy() { return isBusy; }

    // ---- DOM refs ----------------------------------------------------------
    const chatBody = document.querySelector('.chat-body');
    const scroller = document.getElementById('divLoadingBlock');

    // ---- Smart follow behavior (like ChatGPT) ------------------------------
    const NEAR_BOTTOM_PX = 120;   // how close counts as "near bottom"
    let followOutput = true;      // whether to auto-follow right now

    function isNearBottom() {
        if (!scroller) return true;
        const distance = scroller.scrollHeight - (scroller.scrollTop + scroller.clientHeight);
        return distance <= NEAR_BOTTOM_PX;
    }

    function maybeFollow() {
        if (!scroller) return;
        if (followOutput) scroller.scrollTop = scroller.scrollHeight;
    }

    function onUserScroll() {
        // If user scrolls up, stop following; resume when they come back near bottom
        followOutput = isNearBottom();
    }
    if (scroller) scroller.addEventListener('scroll', onUserScroll, { passive: true });

    // Set initial state based on where the user is when the page loads
    followOutput = isNearBottom();

    // Create a chat bubble
    function bubble(role, html, ts) {
        const outer = document.createElement('div');
        outer.className = `chat-message ${role}`;

        const msg = document.createElement('div');
        msg.className = 'message-text';
        if (html) msg.innerHTML = html; // server already returns safe HTML
        outer.appendChild(msg);

        if (ts) {
            const time = document.createElement('div');
            time.className = 'timestamp';
            time.textContent = ts;
            outer.appendChild(time);
        }

        chatBody.appendChild(outer);
        return msg; // return the inner text container for typing
    }

    function escapeHtml(s) {
        return s.replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
    }

    // Inline typing dots
    function showInlineTyping(targetEl) {
        const wrap = document.createElement('span');
        wrap.className = 'saf-typing';
        wrap.innerHTML = '<span></span><span></span><span></span>';
        targetEl.appendChild(wrap);
        return () => wrap.remove(); // cleanup
    }

    // POST helper for ?handler=AskAjax
    async function postAsk(question) {
        const url = document.getElementById('askAjaxUrl')?.value || '?handler=AskAjax';
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const resp = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { 'RequestVerificationToken': token } : {})
            },
            body: JSON.stringify({ question })
        });

        if (!resp.ok) {
            const body = await resp.text();
            throw new Error(`HTTP ${resp.status} ${resp.statusText}: ${body || '(no body)'}`);
        }
        return resp.json();
    }

    // Types PLAIN text with visible newlines, then swaps to FINAL HTML
    async function typeOutPlainThenSwap(el, plainText, finalHtml, chunk = 1, delay = 12) {
        const prevWhiteSpace = el.style.whiteSpace;
        el.style.whiteSpace = 'pre-wrap';   // preserve line breaks during typing

        el.textContent = '';
        let i = 0;
        while (i < plainText.length) {
            el.textContent += plainText.slice(i, i + chunk);
            i += chunk;
            // follow only if the user hasn't scrolled away
            maybeFollow();
            if (delay) await new Promise(r => setTimeout(r, delay));
        }

        // Swap to final HTML (links + <br>) and restore white-space
        el.innerHTML = finalHtml || escapeHtml(plainText).replace(/\n/g, '<br>');
        el.style.whiteSpace = prevWhiteSpace || '';
    }

    // ---- Send handler (hooked to Send button) ------------------------------
    window.Send = async function () {
        if (composerIsBusy()) return;

        const ta = document.getElementById('txtQuestion');
        const text = (ta.value || '').trim();
        if (!text) {
            ta.classList.add('input-validation-error');
            ta.focus();
            return;
        }
        ta.classList.remove('input-validation-error');

        setComposerBusy(true);

        const ts = new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });

        // Echo user bubble
        bubble('user', escapeHtml(text).replace(/\n/g, '<br>'), ts);
        window.SAF_Composer?.reset?.();

        // If we were at the bottom when sending, stay following
        followOutput = isNearBottom();
        maybeFollow();

        // Create assistant bubble and show typing dots
        const assistantEl = bubble('bot', '', ts);
        const removeTyping = showInlineTyping(assistantEl);
        maybeFollow();

        try {
            const data = await postAsk(text); // returns { ok, assistantPlain, assistantHtml }
            removeTyping();

            let plain = data.assistantPlain || '';
            let html = data.assistantHtml || '';

            // Fallback (if only HTML returned)
            if (!plain && html) {
                const withNewlines = html.replace(/<br\s*\/?>/gi, '\n');
                const tmp = document.createElement('div');
                tmp.innerHTML = withNewlines;
                plain = tmp.textContent || tmp.innerText || '';
            }

            await typeOutPlainThenSwap(assistantEl, plain, html);
        } catch (err) {
            removeTyping();
            assistantEl.innerHTML = `<em style="color:#b42318;">${escapeHtml(String(err.message || err))}</em>`;
        } finally {
            setComposerBusy(false);
            // If the user stayed near the bottom, snap once; otherwise leave their scroll alone
            if (isNearBottom()) followOutput = true;
            maybeFollow();
        }
    };

    // Optional: block Enter while busy
    $('#txtQuestion').on('keydown', function (e) {
        if (composerIsBusy() && (e.key === 'Enter' || e.keyCode === 13)) {
            e.preventDefault();
        }
    });

    // Export tiny state if needed
    window.SAF_ComposerState = { setComposerBusy, composerIsBusy };
})();