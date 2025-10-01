/* ==================================
 * 4) wwwroot/js/file-uploader.js
 *    (Component logic — supports multiple instances)
 * ================================== */
(function () {
    const bytesToString = (b) => { const u = ['B', 'KB', 'MB', 'GB']; let i = 0, n = b; while (n >= 1024 && i < u.length - 1) { n /= 1024; i++; } return `${n.toFixed(1)} ${u[i]}`; };
    // Describe accepted types nicely for tooltip. Supports extensions (with/without dot), mimes, and wildcards
    const describeTypes = (types) => types.map(t => {
        if (!t) return '';
        const v = String(t).trim();
        if (v === 'image/*') return 'Images (JPG, PNG, GIF, …)';
        if (v.endsWith('/*')) return v;               // e.g., audio/*
        if (v.includes('/')) return v;                // exact MIME
        return (v.startsWith('.') ? v : '.' + v).toLowerCase(); // extension with/without dot
    }).filter(Boolean).join(', ');

    // Helpers for extension + type matching
    const fileExt = (file) => { const name = (file?.name || ''); const i = name.lastIndexOf('.'); return i >= 0 ? ('.' + name.slice(i + 1)).toLowerCase() : ''; };
    const normalizeExt = (s) => { if (!s) return ''; const v = String(s).trim(); return (v.startsWith('.') ? v : '.' + v).toLowerCase(); };
    function typeAllowed(file, accepted) {
        if (!accepted || accepted.length === 0) return true; // no limits
        const mime = (file.type || '').toLowerCase();
        const ext = fileExt(file);
        return accepted.some(t => {
            if (!t) return false;
            const v = String(t).toLowerCase().trim();
            if (v.endsWith('/*')) { // image/*
                const prefix = v.slice(0, -1); // keep trailing '/'
                return mime.startsWith(prefix);
            }
            if (v.includes('/')) { // exact MIME
                return mime === v;
            }
            // extension (with or without dot)
            return ext === normalizeExt(v);
        });
    }

    // Filename helpers
    function splitNameExt(name) {
        const idx = name.lastIndexOf('.');
        if (idx <= 0 || idx === name.length - 1) return [name, '']; // no ext or dot at end
        return [name.slice(0, idx), name.slice(idx)]; // [base, .ext]
    }
    function truncateFilename(name, max) {
        if (max <= 0 || name.length <= max) return name;
        const [base, ext] = splitNameExt(name);
        const ELL = '…';
        if (ext.length + 1 > max) {
            // Edge case: extension itself is almost the whole budget, keep the tail of extension
            return ELL + ext.slice(-(max - 1));
        }
        const roomForBase = max - ext.length - ELL.length; // space for base + ellipsis
        const safeBase = roomForBase > 0 ? base.slice(0, roomForBase) : '';
        return safeBase + ELL + ext;
    }

    function init(root, opts) {
        const el = typeof root === 'string' ? document.querySelector(root) : root; if (!el) return;
        // Read options from data-attrs (server) + opts (client)
        const cfg = {
            inputName: el.dataset.inputName || 'Uploads',
            accepted: [],
            perFileBytes: Number(el.dataset.perFileBytes || (10 * 1024 * 1024)),
            totalBytes: Number(el.dataset.totalBytes || (25 * 1024 * 1024)),
            simulateUpload: (el.dataset.simulateUpload || 'true') === 'true',
            showDiagnostics: (el.dataset.showDiagnostics || 'false') === 'true',
            maxFiles: Number(el.dataset.maxFiles || 0),               // 0 = unlimited
            alreadyUploaded: Number(el.dataset.alreadyUploaded || 0), // existing on server
            maxNameLength: Number(el.dataset.maxNameLength || 0),     // 0 = unlimited
            truncateLongNames: (el.dataset.truncateLongFilenames || 'true') === 'true',
            ...opts
        };
        try { cfg.accepted = opts?.accepted ?? JSON.parse(el.dataset.accepted || '[]'); } catch { cfg.accepted = []; }

        // Scope queries
        const q = (sel) => el.querySelector(sel);
        const dropzone = q('[data-role="dropzone"]');
        const input = q('[data-role="input"]'); if (input && cfg.inputName) input.setAttribute('name', cfg.inputName);
        const list = q('[data-role="list"]');
        const errors = q('[data-role="errors"]');
        const btnSubmit = q('[data-role="submit"]');
        const btnClear = q('[data-role="clear"]');
        const btnInfo = q('[data-role="info"]');
        const globalWrap = q('[data-role="global-wrap"]');
        const globalBar = q('[data-role="global-bar"]');
        const globalPct = q('[data-role="global-pct"]');
        const globalText = q('[data-role="global-text"]');
        const diag = q('[data-role="diagnostics"]');

        // Input accept attribute for native picker filtering
        if (input && Array.isArray(cfg.accepted)) {
            const acceptAttr = cfg.accepted
                .map(v => {
                    v = String(v || '').trim();
                    if (!v) return null;
                    if (v.endsWith('/*') || v.includes('/')) return v.toLowerCase();
                    return (v.startsWith('.') ? v : '.' + v).toLowerCase();
                })
                .filter(Boolean)
                .join(',');
            if (acceptAttr) input.setAttribute('accept', acceptAttr);
        }

        // Tooltip content helpers
        const infoBase = () => {
            let base = `Allowed: ${describeTypes(cfg.accepted)} • Max ${bytesToString(cfg.perFileBytes)} per file • ${bytesToString(cfg.totalBytes)} total`;
            if (cfg.maxNameLength > 0) base += ` • Max name ${cfg.maxNameLength} chars`;
            return base;
        };
        const remainingSlots = () => {
            if (!(cfg.maxFiles > 0)) return Infinity;
            const rem = cfg.maxFiles - cfg.alreadyUploaded - workingFiles.length;
            return Math.max(0, rem);
        };
        function updateInfoTooltip() {
            if (!btnInfo) return;
            let text = infoBase();
            if (cfg.maxFiles > 0) text += ` • Max files: ${cfg.maxFiles} (remaining ${remainingSlots() === Infinity ? '∞' : remainingSlots()})`;
            btnInfo.setAttribute('title', text);
            btnInfo.setAttribute('data-bs-original-title', text);
            try { const inst = bootstrap.Tooltip.getInstance(btnInfo) || new bootstrap.Tooltip(btnInfo); inst.update(); } catch { }
        }

        // State for this instance
        let workingFiles = [];
        const newId = () => Math.random().toString(36).slice(2) + Date.now().toString(36);

        function updateSubmitState() { const anyUp = workingFiles.some(f => f.status !== 'ready'); btnSubmit.disabled = anyUp || workingFiles.length === 0; }
        function updateGlobal() {
            if (workingFiles.length === 0) { globalWrap.style.display = 'none'; updateInfoTooltip(); return; }
            globalWrap.style.display = '';
            const total = workingFiles.reduce((s, f) => s + f.size, 0) || 1;
            const loaded = workingFiles.reduce((s, f) => s + f.size * (f.progress || 0) / 100, 0);
            const pct = Math.round((loaded / total) * 100);
            globalBar.style.width = pct + '%';
            globalBar.setAttribute('aria-valuenow', String(pct));
            globalPct.textContent = pct + '%';
            const uploading = workingFiles.filter(f => f.status === 'uploading').length;
            const ready = workingFiles.filter(f => f.status === 'ready').length;
            const error = workingFiles.filter(f => f.status === 'error').length;
            const remaining = remainingSlots();
            const tail = (cfg.maxFiles > 0) ? ` • remaining ${remaining === Infinity ? '∞' : remaining}` : '';
            globalText.textContent = (uploading > 0 ? `Uploading ${ready}/${workingFiles.length} • ${pct}%` : `Ready ${ready}/${workingFiles.length}` + (error ? ` • ${error} error(s)` : '')) + tail;
            updateInfoTooltip();
        }

        function render({ animateNew } = { animateNew: false }) {
            list.innerHTML = ''; const frag = document.createDocumentFragment();
            for (let i = 0; i < workingFiles.length; i++) {
                const it = workingFiles[i]; const li = document.createElement('li'); li.className = 'list-group-item'; li.dataset.id = it.id;
                const row = document.createElement('div'); row.className = 'd-flex align-items-center gap-2';

                const nameBox = document.createElement('input');
                nameBox.type = 'text'; nameBox.className = 'form-control';
                if (cfg.maxNameLength > 0) nameBox.maxLength = cfg.maxNameLength; nameBox.value = it.name; nameBox.disabled = (it.status !== 'ready');

                nameBox.addEventListener('keydown', (e) => { if (e.key === 'Enter') { e.preventDefault(); } });
                nameBox.addEventListener('change', () => rename(i, nameBox.value));


                const meta = document.createElement('span'); meta.className = 'ms-auto small text-muted file-status'; meta.textContent = (it.status === 'ready') ? 'Ready' : (it.status === 'error' ? 'Error' : 'Uploading…');
                const removeBtn = document.createElement('button'); removeBtn.type = 'button'; removeBtn.className = 'btn btn-outline-danger btn-sm'; removeBtn.setAttribute('data-role', 'remove'); removeBtn.innerHTML = '<i class="fa-solid fa-trash"></i>'; removeBtn.disabled = (it.status !== 'ready'); removeBtn.onclick = () => remove(i, li);
                row.append(nameBox, meta, removeBtn);
                const progWrap = document.createElement('div'); progWrap.className = 'progress file-progress mt-2';
                const progBar = document.createElement('div'); progBar.className = 'progress-bar'; progBar.style.width = `${it.progress}%`; progBar.setAttribute('aria-valuemin', '0'); progBar.setAttribute('aria-valuemax', '100'); progBar.setAttribute('aria-valuenow', `${Math.round(it.progress)}`);
                progWrap.appendChild(progBar);
                li.append(row, progWrap);
                if (animateNew) { li.classList.add('enter-init'); requestAnimationFrame(() => { li.classList.add('enter-active'); }); }
                frag.appendChild(li);
            }
            list.appendChild(frag);
            updateInfoTooltip();
        }

        function renderProgressOnly(id, progress, status) { const li = list.querySelector(`li[data-id=\"${id}\"]`); if (!li) return; const bar = li.querySelector('.progress-bar'); const statusEl = li.querySelector('.file-status'); if (bar) { bar.style.width = progress + '%'; bar.setAttribute('aria-valuenow', String(Math.round(progress))); } if (statusEl) { statusEl.textContent = status === 'ready' ? 'Ready' : (status === 'error' ? 'Error' : 'Uploading…'); } }

        function handlePicked(files) {
            const errs = [];
            for (const file of files) {
                // Enforce overall file count limit if set
                if (cfg.maxFiles > 0 && (cfg.alreadyUploaded + workingFiles.length) >= cfg.maxFiles) {
                    errs.push(`File limit reached (max ${cfg.maxFiles}). Skipped: ${file.name}`);
                    continue;
                }
                const allowed = typeAllowed(file, cfg.accepted);
                if (!allowed) { errs.push(`Type not allowed: ${file.name}`); continue; }
                if (file.size > cfg.perFileBytes) { errs.push(`Too large: ${file.name}`); continue; }
                const curTotal = workingFiles.reduce((s, it) => s + it.size, 0);
                if (curTotal + file.size > cfg.totalBytes) { errs.push(`Total limit exceeded adding: ${file.name}`); continue; }

                let newName = file.name;
                if (cfg.maxNameLength > 0 && newName.length > cfg.maxNameLength) {
                    if (cfg.truncateLongNames) {
                        newName = truncateFilename(newName, cfg.maxNameLength);
                    } else {
                        errs.push(`Filename too long (max ${cfg.maxNameLength} chars): ${file.name}`);
                        continue;
                    }
                }
                const fileForStore = (newName === file.name) ? file : new File([file], newName, { type: file.type, lastModified: file.lastModified });
                const it = { id: newId(), file: fileForStore, name: newName, size: file.size, status: 'uploading', progress: 0 };
                workingFiles.push(it);
                upload(it);
            }
            if (errs.length) { errors.classList.remove('d-none'); errors.innerHTML = `<strong>Issues:</strong><ul class=\"mb-0\">${errs.map(e => `<li>${e}</li>`).join('')}</ul>`; } else { errors.classList.add('d-none'); errors.innerHTML = ''; }
            render({ animateNew: true }); updateSubmitState(); updateGlobal();
        }

        function upload(it) {
            if (cfg.simulateUpload) {
                it.timer = setInterval(() => {
                    const inc = 4 + Math.random() * 12; it.progress = Math.min(100, it.progress + inc); renderProgressOnly(it.id, it.progress, 'uploading'); updateGlobal(); if (it.progress >= 100) {
                        it.status = 'ready'; clearInterval(it.timer); renderProgressOnly(it.id, 100, 'ready'); // enable rename & delete
                        const li = list.querySelector(`li[data-id=\"${it.id}\"]`); if (li) { const nameBox = li.querySelector('input[type=\"text\"]'); const del = li.querySelector('button[data-role=\"remove\"]'); if (nameBox) nameBox.disabled = false; if (del) del.disabled = false; }
                        updateSubmitState(); updateGlobal();
                    }
                }, 250);
            } else {
                // wire your real pre-upload here if desired (XHR/fetch with progress)
            }
        }

        async function rename(index, newName) {
            const it = workingFiles[index];
            if (!it || !newName || newName === it.name) return;

            // Enforce name length on rename
            if (cfg.maxNameLength > 0 && newName.length > cfg.maxNameLength) {
                if (cfg.truncateLongNames) {
                    newName = truncateFilename(newName, cfg.maxNameLength);
                } else {
                    // block and show a validity message
                    const li = list.querySelector(`li[data-id=\"${it.id}\"]`);
                    const box = li?.querySelector('input[type=\"text\"]');
                    if (box) {
                        box.value = it.name;
                        box.setCustomValidity(`Max ${cfg.maxNameLength} characters.`);
                        box.reportValidity();
                        setTimeout(() => box.setCustomValidity(''), 1500);
                    }
                    return;
                }
            }

            const buf = await it.file.arrayBuffer();
            const renamed = new File([new Uint8Array(buf)], newName, { type: it.file.type, lastModified: it.file.lastModified });
            workingFiles[index] = { ...it, file: renamed, name: newName };
            render(); updateGlobal();
        }

        function remove(idx, li) { const it = workingFiles[idx]; const h = li.scrollHeight; li.style.height = h + 'px'; void li.offsetHeight; li.classList.add('collapsing'); li.style.height = '0px'; const id = it?.id; li.addEventListener('transitionend', function onEnd(e) { if (e.propertyName !== 'height') return; li.removeEventListener('transitionend', onEnd); const pos = workingFiles.findIndex(x => x.id === id); if (pos !== -1) workingFiles.splice(pos, 1); render(); updateSubmitState(); updateGlobal(); }); }

        // --- Binding files to the *actual* submitted form (outer or inner) ---
        function bindFilesToInput() {
            const dt = new DataTransfer();
            workingFiles.forEach(f => { if (f.status === 'ready') dt.items.add(f.file); });
            input.files = dt.files;
            if (cfg.showDiagnostics && diag) {
                const lines = [...dt.files].map(f => `• ${f.name} (${bytesToString(f.size)})`);
                diag.textContent = `Ready to POST:\n` + lines.join('\n');
            }
        }

        // Capture submit on the document so this works even if the uploader is inside a host form
        document.addEventListener('submit', (e) => {
            const submittedForm = e.target;
            if (!(submittedForm instanceof HTMLFormElement)) return;
            if (!submittedForm.contains(el)) return; // this submit isn't for our uploader

            // Ensure enctype
            if (!submittedForm.hasAttribute('enctype')) {
                submittedForm.setAttribute('enctype', 'multipart/form-data');
            }
            // Ensure the file input is associated with the submitted form even if it's nested in another form
            if (!submittedForm.id) submittedForm.id = 'form_' + Math.random().toString(36).slice(2);
            input.setAttribute('form', submittedForm.id);

            window.top.StartLoadingModal('#divLoadingBlockModal');

            bindFilesToInput();
        }, true);

        // events
        const openPicker = () => input.click();
        dropzone.addEventListener('click', openPicker);
        dropzone.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); openPicker(); } });
        dropzone.addEventListener('dragover', (e) => { e.preventDefault(); dropzone.classList.add('dropzone--hover'); });
        dropzone.addEventListener('dragleave', () => dropzone.classList.remove('dropzone--hover'));
        dropzone.addEventListener('drop', (e) => { e.preventDefault(); dropzone.classList.remove('dropzone--hover'); if (e.dataTransfer?.files) handlePicked(e.dataTransfer.files); });
        input.addEventListener('change', () => { handlePicked(input.files); input.value = ''; window.top.ResizeModal(); });
        btnClear.addEventListener('click', () => { workingFiles = []; render(); updateSubmitState(); updateGlobal(); window.top.ResizeModal(); errors.classList.add('d-none'); errors.innerHTML = ''; });

        // init state
        updateSubmitState(); updateGlobal();
    }

    function autoInit(selector) { document.querySelectorAll(selector).forEach(el => init(el)); }

    window.FileUploader = { init, autoInit };
})();