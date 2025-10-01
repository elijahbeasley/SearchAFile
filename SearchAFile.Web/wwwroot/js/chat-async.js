/* ============================================================================
   SearchAFile - Async Chat (front-end)
   ----------------------------------------------------------------------------
   Responsibilities:
     - Collect the user's question from #txtQuestion
     - Show the user's message and a "typing…" state for the assistant
     - POST to ?handler=AskAjax (or a custom URL in #askAjaxUrl)
     - Stream-like effect: type plain text, then swap to final HTML
     - Smart autoscroll that pauses when user scrolls up
     - Robust error handling for HTML redirects / non-JSON responses
   Dependencies:
     - Minimal: vanilla JS. (jQuery optional; used only for a tiny Enter guard)
     - CSS expects .chat-body container and .saf-composer wrapper
   ----------------------------------------------------------------------------
   Public surface:
     - window.SAF_Chat.send()      -> triggers a send (hook your button to this)
     - window.SAF_Chat.setBusy(on) -> toggle busy UI if you need externally
   ========================================================================== */
(() => {
    'use strict';

    /* ------------------------------------------------------------------------
       Section 1: DOM lookups + small guards
       ---------------------------------------------------------------------- */
    const chatBody = document.querySelector('.chat-body');       // where bubbles go
    const scroller = document.getElementById('divLoadingBlock'); // scrollable container
    const textarea = document.getElementById('txtQuestion');     // user input
    const askUrl = document.getElementById('askAjaxUrl')?.value || '?handler=AskAjax';
    const antiforgeryToken =
        document.querySelector('input[name="__RequestVerificationToken"]')?.value || null;

    if (!chatBody) console.warn('[chat-async] .chat-body not found. Messages won’t render.');
    if (!scroller) console.warn('[chat-async] #divLoadingBlock not found. Autoscroll disabled.');
    if (!textarea) console.warn('[chat-async] #txtQuestion not found. Input disabled.');

    /* ------------------------------------------------------------------------
       Section 2: Busy-state helpers (disables composer UI)
       ---------------------------------------------------------------------- */
    let isBusy = false;
    function setBusy(on) {
        isBusy = !!on;
        const wrap = document.querySelector('.saf-composer');
        if (!wrap) return;
        wrap.classList.toggle('is-busy', isBusy);
        wrap.setAttribute('aria-busy', isBusy ? 'true' : 'false');
        // Disable only interactive elements inside composer
        wrap.querySelectorAll('button, textarea, input, select').forEach(el => {
            el.disabled = isBusy;
        });
    }

    /* ------------------------------------------------------------------------
       Section 3: Smart follow (autoscroll like ChatGPT)
       - Follow output if user is near the bottom
       - If user scrolls up, stop following until they come back down
       ---------------------------------------------------------------------- */
    const NEAR_BOTTOM_PX = 120;
    let followOutput = true;

    function isNearBottom() {
        if (!scroller) return true;
        const distance = scroller.scrollHeight - (scroller.scrollTop + scroller.clientHeight);
        return distance <= NEAR_BOTTOM_PX;
        // distance < 0 can happen briefly during layout; still treated as near bottom
    }

    function maybeFollow() {
        if (!scroller) return;
        if (followOutput) scroller.scrollTop = scroller.scrollHeight;
    }

    function onUserScroll() {
        followOutput = isNearBottom();
    }

    if (scroller) {
        scroller.addEventListener('scroll', onUserScroll, { passive: true });
        // Initialize once after first layout tick
        queueMicrotask(() => (followOutput = isNearBottom()));
    }

    /* ------------------------------------------------------------------------
       Section 4: Message bubble rendering
       ---------------------------------------------------------------------- */
    function bubble(role /* 'user' | 'bot' | 'system' */, html, tsText) {
        const outer = document.createElement('div');
        outer.className = `chat-message ${role}`;

        const msg = document.createElement('div');
        msg.className = 'message-text';
        if (html) msg.innerHTML = html; // NOTE: server-provided HTML is assumed safe
        outer.appendChild(msg);

        if (tsText) {
            const time = document.createElement('div');
            time.className = 'timestamp';
            time.textContent = tsText;
            outer.appendChild(time);
        }

        chatBody?.appendChild(outer);
        return msg; // return inner message element for dynamic updates
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&gt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[ch]));
    }

    // Inline "typing…" dots; returns a cleanup function
    function showTyping(targetEl) {
        const wrap = document.createElement('span');
        wrap.className = 'saf-typing';
        wrap.innerHTML = '<span></span><span></span><span></span>';
        targetEl.appendChild(wrap);
        return () => wrap.remove();
    }

    /* ------------------------------------------------------------------------
       Section 5: Network call (robust)
       - Sends cookies for auth (credentials: 'same-origin')
       - Sends antiforgery header if present
       - Ensures JSON content-type before parsing
       - Gives helpful errors when HTML is returned (login redirect / error page)
       ---------------------------------------------------------------------- */
    async function postAsk(question) {
        const resp = await fetch(askUrl, {
            method: 'POST',
            credentials: 'same-origin',                 // send auth cookies
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',     // ASP.NET treats as AJAX
                ...(antiforgeryToken ? { 'RequestVerificationToken': antiforgeryToken } : {})
            },
            body: JSON.stringify({ question })
        });

        // Non-OK -> read text to surface server error content
        if (!resp.ok) {
            const body = await resp.text().catch(() => '');
            throw new Error(`HTTP ${resp.status} ${resp.statusText}: ${body?.slice(0, 400) || '(no body)'}`);
        }

        // Check we really got JSON (not a login page)
        const ct = (resp.headers.get('content-type') || '').toLowerCase();
        if (!ct.includes('application/json')) {
            const body = await resp.text().catch(() => '');
            if (body.toLowerCase().includes('<!doctype html')) {
                throw new Error('Unexpected HTML response (likely a login redirect or server error page).');
            }
            throw new Error(`Expected JSON but got: ${body.slice(0, 400)}`);
        }

        return resp.json(); // expected shape: { ok, assistantPlain, assistantHtml }
    }

    /* ------------------------------------------------------------------------
       Section 6: Typing effect (plain text first, then swap to final HTML)
       - We "type" the plain text (with preserved newlines) for a live feel
       - Then we replace with the final HTML which may include links and <br>
       ---------------------------------------------------------------------- */
    async function typeOutThenSwap(el, plainText, finalHtml, chunk = 1, delayMs = 12) {
        const prevWhiteSpace = el.style.whiteSpace;
        el.style.whiteSpace = 'pre-wrap'; // show \n as line breaks while typing

        el.textContent = '';
        let i = 0;
        while (i < plainText.length) {
            el.textContent += plainText.slice(i, i + chunk);
            i += chunk;
            maybeFollow();
            if (delayMs > 0) {
                // micro-sleep without blocking layout/scroll
                await new Promise(r => setTimeout(r, delayMs));
            }
        }

        // Swap to final HTML (or a safe fallback)
        el.innerHTML = finalHtml || escapeHtml(plainText).replace(/\n/g, '<br>');
        el.style.whiteSpace = prevWhiteSpace || '';
    }

    /* ------------------------------------------------------------------------
       Section 7: Main send flow
       ---------------------------------------------------------------------- */
    async function send() {
        if (isBusy) return;
        if (!textarea) return;

        const text = (textarea.value || '').trim();
        if (!text) {
            textarea.classList.add('input-validation-error');
            textarea.focus();
            return;
        }
        textarea.classList.remove('input-validation-error');

        setBusy(true);

        const ts = new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });

        // 1) Echo the user message
        bubble('user', escapeHtml(text).replace(/\n/g, '<br>'), ts);
        // If you have a separate composer object that resets the form, call it:
        // window.SAF_Composer?.reset?.();
        textarea.value = '';

        // 2) Ensure we follow if the user was already near the bottom
        followOutput = isNearBottom();
        maybeFollow();

        // 3) Create assistant bubble with typing dots
        const assistantEl = bubble('bot', '', ts);
        const removeTyping = showTyping(assistantEl);
        maybeFollow();

        try {
            const data = await postAsk(text); // -> { ok, assistantPlain, assistantHtml }

            // Remove typing indicator as soon as we have a response
            removeTyping();

            // Normalize outputs
            let plain = data?.assistantPlain || '';
            let html = data?.assistantHtml || '';

            // Fallback: derive plain from html if only HTML was returned
            if (!plain && html) {
                const withNewlines = html.replace(/<br\s*\/?>/gi, '\n');
                const tmp = document.createElement('div');
                tmp.innerHTML = withNewlines;
                plain = tmp.textContent || tmp.innerText || '';
            }

            await typeOutThenSwap(assistantEl, plain || '', html || '');
        } catch (err) {
            // Swap typing for a readable error bubble
            try { removeTyping(); } catch { }
            assistantEl.innerHTML =
                `<em style="color:#b42318;">${escapeHtml(err?.message || String(err))}</em>`;
        } finally {
            setBusy(false);
            // If user stayed at the bottom, keep following; otherwise leave their scroll alone
            if (isNearBottom()) followOutput = true;
            maybeFollow();
        }
    }

    /* ------------------------------------------------------------------------
       Section 8: Keyboard niceties
       - Enter submits when not busy
       - Shift+Enter inserts a newline
       - While busy, Enter is suppressed (prevents accidental re-sends)
       ---------------------------------------------------------------------- */
    if (textarea) {
        textarea.addEventListener('keydown', (e) => {
            const isEnter = e.key === 'Enter' || e.keyCode === 13;
            if (!isEnter) return;

            if (e.shiftKey) {
                // Allow newline
                return;
            }

            // Prevent native submit behavior
            e.preventDefault();
            if (!isBusy) send();
        });

        // Optional: while busy, block Enter entirely (also covers jQuery usage)
        if (window.jQuery) {
            jQuery(textarea).on('keydown', function (e) {
                const isEnter = e.key === 'Enter' || e.keyCode === 13;
                if (isBusy && isEnter) e.preventDefault();
            });
        }
    }

    /* ------------------------------------------------------------------------
       Section 9: Public API
       ---------------------------------------------------------------------- */
    window.SAF_Chat = {
        send,
        setBusy,
        get isBusy() { return isBusy; }
    };

    // If your Send button has onclick="SAF_Chat.send()", you’re done.
    // Otherwise you can wire it up here by selector:
    // document.getElementById('btnSend')?.addEventListener('click', send);
})();