/// <summary>Maximum allowed file size</summary>
//const MaxFileSize = 5242880; // 5 MB
//const MaxFileSize = 10485760; // 10 MB
const MaxFileSize = 26214400; // 25 MB
//const MaxFileSize = 104857600; // 100 MB

/// <summary>Base URL of the current page (protocol + host + pathname).</summary>
const PageURL = window.location.protocol + '//' + window.location.host + window.location.pathname;

/// <summary>True if on a mobile device, false otherwise.</summary>
const is_mobile = isMobileDevice();
const focus_on_mobile = true;

/// <summary>
/// Checks if the user is on a mobile device based on the browser's user agent.
/// </summary>
/// <returns>True if it’s a mobile device; otherwise, false.</returns>
function isMobileDevice() {
    const ua = navigator.userAgent || navigator.vendor || window.opera;
    return /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(ua);
}

/// <summary>
/// Initializes the page: stops all loaders, highlights the active menu item,
/// reveals the body, binds event handlers, and sets up Bootstrap tooltips/popovers.
/// </summary>
$(document).ready(function () {

    StopLoadingAll();
    SetMenuHighlight();

    $('#body').css('opacity', '1');

    // Show loading overlay and start polling cookie
    $('.cus-show-loading').on('click', function () {
        $('#divLoading').show();
        setCookie('show-loading', '0', 1); // mark “loading” state
        startDownloadCheck();
    });

    // Desktop-only: initialize Bootstrap tooltips and popovers
    //if (!is_mobile) {
    //    document.querySelectorAll('[data-bs-toggle="tooltip"], [data-bs-toggle="popover"]').forEach(el => {
    //        const mode = el.getAttribute('data-bs-toggle');
    //        if (mode === 'tooltip') new bootstrap.Tooltip(el);
    //        else if (mode === 'popover') new bootstrap.Popover(el);
    //    });
    //}

    document.querySelectorAll('[data-bs-toggle="tooltip"], [data-bs-toggle="popover"]').forEach(el => {
        const mode = el.getAttribute('data-bs-toggle');
        if (mode === 'tooltip') new bootstrap.Tooltip(el);
        else if (mode === 'popover') new bootstrap.Popover(el);
    });

    // Execute any server-provided startup script (beware of eval risks)
    const startupScript = $('#StartupJavaScript').text().trim();
    if (startupScript) {
        /* eslint-disable no-eval */
        eval(startupScript);
        /* eslint-enable no-eval */
    }
});

/// <summary>
/// Highlights the current navigation link by comparing pathnames, including subpaths.
/// </summary>
function SetMenuHighlight() {
    const nav = document.getElementById('nav-bar');
    if (!nav) return;

    const currentPath = window.location.pathname.replace(/\/$/, '').toLowerCase();

    nav.querySelectorAll('a.nav_link').forEach(link => {
        try {
            const href = link.getAttribute('href');
            if (!href || href === '#' || href.startsWith('javascript:')) return;

            const linkPath = new URL(href, window.location.origin).pathname.replace(/\/$/, '').toLowerCase();

            if (
                currentPath === linkPath ||
                currentPath.startsWith(linkPath + '/')
            ) {
                link.classList.add('active');
            }
        } catch (e) {
            console.warn('Skipping invalid href:', link.getAttribute('href'), e);
        }
    });
}

/// <summary>
/// Retrieves a query-string parameter by name.
/// </summary>
/// <param name="name">Parameter name.</param>
/// <param name="url">URL to parse (defaults to current location).</param>
/// <returns>The parameter value, empty string if present without value, or null if absent.</returns>
function getParameterByName(name, url = window.location.href) {
    name = name.replace(/[[\]]/g, '\\$&');
    const regex = new RegExp(`[?&]${name}(=([^&#]*)|&|#|$)`);
    const results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}

// --- Download-complete polling logic ---
let downloadTimeout;

/// <summary>
/// Kicks off the cookie-polling loop to detect when loading is done.
/// </summary>
function startDownloadCheck() {
    clearTimeout(downloadTimeout);
    checkDownloadCookie();
}

/// <summary>
/// Polls the 'show-loading' cookie every 500ms.  
/// When it flips to '1', hides the overlay and resets the cookie to '0'.
/// </summary>
function checkDownloadCookie() {
    if (getCookie('show-loading') === '1') {
        setCookie('show-loading', '0', 1);  // reset for next time
        $('#divLoading').hide();
    } else {
        downloadTimeout = setTimeout(checkDownloadCookie, 500);
    }
}

// --- Input slot-mask formatter ---
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[placeholder][data-bs-slots]').forEach(el => {
        const pattern = el.getAttribute('placeholder');
        const slots = new Set((el.dataset.slots || '_').split(''));
        const accept = new RegExp(el.dataset.accept || '\\d', 'g');
        const prev = pattern.split('').map((c, i) => slots.has(c) ? i + 1 : 0);
        const first = pattern.split('').findIndex(c => slots.has(c));

        let back = false;
        const clean = value => {
            const input = value.match(accept) || [];
            return pattern.split('').map(c => slots.has(c) ? input.shift() || c : c);
        };

        const format = () => {
            const [start, end] = [el.selectionStart, el.selectionEnd].map(pos => {
                const cleaned = clean(el.value.slice(0, pos));
                const idx = cleaned.findIndex(c => slots.has(c));
                return idx < 0 ? prev[prev.length - 1] : (back ? prev[idx - 1] || first : idx);
            });
            el.value = clean(el.value).join('');
            el.setSelectionRange(start, end);
            back = false;
        };

        el.addEventListener('keydown', e => back = e.key === 'Backspace');
        el.addEventListener('input', format);
        el.addEventListener('focus', format);
        el.addEventListener('blur', () => { if (el.value === pattern) el.value = ''; });
    });
});

/// <summary>
/// Debounces a callback so it only fires `ms` milliseconds after typing stops.
/// </summary>
/// <param name="input">jQuery-wrapped input element.</param>
/// <param name="callback">Function to invoke.</param>
/// <param name="ms">Delay in milliseconds.</param>
function OnKeyUpAfter(input, callback, ms) {
    let timer;
    input.on('keyup', () => {
        clearTimeout(timer);
        timer = setTimeout(callback, ms);
    }).on('keydown', () => clearTimeout(timer));
}

/// <summary>Map of characters to their HTML-escaped equivalents.</summary>
const entityMap = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
    '/': '&#x2F;',
    '`': '&#x60;',
    '=': '&#x3D;'
};

/// <summary>
/// Escapes HTML special characters in a string for safe insertion.
/// </summary>
/// <param name="str">Input string.</param>
/// <returns>HTML-escaped string.</returns>
function escapeHtml(str) {
    return String(str).replace(/[&<>"'`=\/]/g, s => entityMap[s]);
}

/**
 * <summary>
 * Initializes Google Maps Places Autocomplete on the input with ID "txtAutocomplete",
 * then populates the textarea with ID "txtAddress" once the user selects a place.
 * 
 * This version:
 *   - Defaults to U.S. ("us"), Dominican Republic ("do"), and Argentina ("ar").
 *   - If Geolocation succeeds, reverse‐geocodes to find the user's ISO country code
 *     and appends it if not already in the default set.
 *   - If Geolocation fails or is denied, simply uses ["us","do","ar"].
 * </summary>
 *
 * <remarks>
 * - The input must be an <input> element; otherwise the function returns immediately.
 * - The textarea must be a <textarea> element; otherwise the function returns immediately.
 * - If the user selects a place without valid address_components, a warning message is shown.
 * - Uses the legacy Autocomplete class (google.maps.places.Autocomplete). If you migrate
 *   to PlaceAutocompleteElement, the componentRestrictions logic remains the same.
 * </remarks>
 */
function initAutocomplete() {
    // 1) Grab references to the input and textarea
    const input = document.getElementById("txtAutocomplete");
    const addressTextarea = document.getElementById("txtAddress");

    // 2) Guard: ensure that 'input' exists and is an <input>
    if (!input || !(input instanceof HTMLInputElement)) {
        //console.warn("initAutocomplete: '#txtAutocomplete' not found or not an <input>.");
        return;
    }

    // 3) Guard: ensure that 'addressTextarea' exists and is a <textarea>
    if (!addressTextarea || !(addressTextarea instanceof HTMLTextAreaElement)) {
        //console.warn("initAutocomplete: '#txtAddress' not found or not a <textarea>.");
        return;
    }

    // 4) Helper to create the Autocomplete with a given array of country codes
    const createAutocompleteWithCountries = (countryArray) => {
        const autocomplete = new google.maps.places.Autocomplete(input, {
            types: ["address"],
            componentRestrictions: { country: countryArray }
        });

        // 5) Attach place_changed listener to populate the textarea
        autocomplete.addListener("place_changed", () => {
            const place = autocomplete.getPlace();

            if (!place.address_components) {
                addressTextarea.value = "Address not found. Please try again.";
                return;
            }

            // Helper to pull out a specific component by type
            const getComponent = (type) => {
                const comp = place.address_components.find((c) =>
                    c.types.includes(type)
                );
                return comp ? comp.long_name : "";
            };

            // 6) Extract each piece of the address
            const streetNumber = getComponent("street_number");
            const route = getComponent("route");
            const addressLine1 = `${streetNumber} ${route}`.trim();

            const subpremise = getComponent("subpremise"); // apt/suite if any
            const addressLine2 = subpremise ? `Apt/Suite ${subpremise}` : "";

            const city = getComponent("locality") || getComponent("sublocality");
            const state = getComponent("administrative_area_level_1");
            const postalCode = getComponent("postal_code");
            const country = getComponent("country");

            // 7) Build the multi-line string, omitting an empty second line
            const formattedAddress =
                addressLine1 +
                (addressLine2 !== "" ? `\n${addressLine2}` : "") +
                `\n${city}, ${state} ${postalCode}\n${country}`;

            // 8) Populate the textarea
            addressTextarea.value = formattedAddress;
        });
    };

    // 9) Define our default fallback countries: US, DO, AR
    const defaultCountries = ["us", "do", "ar"];

    // 10) Try to detect the user's country via Geolocation + reverse‐geocode
    if ("geolocation" in navigator) {
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const geocoder = new google.maps.Geocoder();
                geocoder.geocode(
                    { location: { lat: pos.coords.latitude, lng: pos.coords.longitude } },
                    (results, status) => {
                        let userCountryCode = null;

                        if (status === "OK" && Array.isArray(results) && results.length) {
                            // Find the first address_component whose types include "country"
                            for (const result of results) {
                                const countryComp = result.address_components.find((c) =>
                                    c.types.includes("country")
                                );
                                if (countryComp) {
                                    userCountryCode = countryComp.short_name.toLowerCase(); // e.g. "ca", "gb", "fr"
                                    break;
                                }
                            }
                        }

                        // Always start with US, DO, AR
                        const allowed = [...defaultCountries];
                        // If userCountryCode is not null and not already in our array, add it
                        if (userCountryCode && !allowed.includes(userCountryCode)) {
                            allowed.push(userCountryCode);
                        }

                        // Finally, build Autocomplete with the combined array
                        createAutocompleteWithCountries(allowed);
                    }
                );
            },
            (error) => {
                // Geolocation denied or failed → fall back to US, DO, AR
                console.warn("Geolocation denied/failed. Using defaults: US, DO, AR.", error);
                createAutocompleteWithCountries(defaultCountries);
            },
            {
                maximumAge: 300_000,   // accept cached position up to 5 minutes old
                timeout: 10_000,    // wait up to 10s for position
                enableHighAccuracy: false
            }
        );
    } else {
        // Browser doesn't support Geolocation → fall back to US, DO, AR
        console.warn("No geolocation support. Using defaults: US, DO, AR.");
        createAutocompleteWithCountries(defaultCountries);
    }
}

// Animate Counter
// Select all elements with the class 'cus-load-number' and iterate over them
$('.cus-load-number').each(function () {
    let $element = $(this); // Store the current jQuery element
    let targetNumber = parseInt($element.text().trim(), 10); // Extract and convert text to an integer

    // Check if the extracted value is a valid integer
    if (!isNaN(targetNumber) && Number.isInteger(targetNumber)) {
        // If valid, call the animateCounter function
        animateCounter(0, targetNumber, 1000, $element);
    } else {
        console.warn(`Skipping animation: Element with ID '${$element.attr('id')}' does not contain a valid integer.`);
    }
});

/**
 * Function to animate a counter from start to end within a given duration.
 * @param {number} start - The starting number (usually 0).
 * @param {number} end - The target number to count up to.
 * @param {number} duration - Duration of the animation in milliseconds.
 * @param {jQuery} $element - The jQuery-wrapped element to update.
 */
function animateCounter(start, end, duration, $element) {
    let startTime = null; // Track start time for smooth animation

    function step(currentTime) {
        if (!startTime) startTime = currentTime; // Set start time on first frame
        let progress = (currentTime - startTime) / duration; // Calculate progress (0 to 1)
        let currentValue = Math.floor(start + (end - start) * progress); // Interpolate value

        // Ensure it doesn't exceed the target number
        $element.text(currentValue >= end ? end : currentValue);

        if (progress < 1) {
            requestAnimationFrame(step); // Continue animation
        }
    }

    requestAnimationFrame(step); // Start animation
}
function startReloadCountdown(seconds) {
    let countdown = seconds;

    // Create a div to display the countdown message if it doesn't exist
    let countdownDiv = document.getElementById('reloadCountdown');
    if (!countdownDiv) {
        countdownDiv = document.createElement('div');
        countdownDiv.id = 'reloadCountdown';
        countdownDiv.style.position = 'fixed';
        countdownDiv.style.bottom = '20px';
        countdownDiv.style.right = '20px';
        countdownDiv.style.background = 'rgba(0, 0, 0, 0.75)';
        countdownDiv.style.color = 'white';
        countdownDiv.style.padding = '10px 15px';
        countdownDiv.style.borderRadius = '5px';
        countdownDiv.style.fontSize = '16px';
        countdownDiv.style.zIndex = '10000';
        document.body.appendChild(countdownDiv);
    }

    // Update the countdown every second
    let interval = setInterval(() => {
        countdownDiv.innerHTML = `Reloading page in ${countdown}...`;  // Template literals used here
        countdown--;

        if (countdown < 0) {
            clearInterval(interval);
            window.top.location.reload(); // Reload the page
        }
    }, 1000);
}

/**
 * Calculates the available distance in pixels between the bottom of the first element
 * and the top of the second element, considering optional padding.
 *
 * @param {string} element1 - The first element.
 * @param {string} element2 - The second element.
 * @param {number} [offset=5] - Optional padding to subtract from the final distance.
 * @returns {number} The distance in pixels between the two elements.
 */
function calculateDistanceBetweenElements(element1, element2, offset = 0) {
    // Get the size of 1rem in pixels
    var remInPixels = parseFloat(getComputedStyle(document.documentElement).fontSize);

    // Get the bottom position of the first element (element1)
    let elem1Bottom = $(element1).offset().top + $(element1).outerHeight() + remInPixels;

    // Get the top position of the second element (element2)
    let elem2Top = $(element2).offset().top - remInPixels;

    // Calculate the distance between the two elements and subtract the offset
    let distance = elem2Top - elem1Bottom - offset;

    // Return the calculated distance
    return distance;
}

/**
 * Keyboard navigation for all Bootstrap 5 dropdowns
 * Supports:
 * - [Tab] into the dropdown
 * - [Arrow Up/Down] to move between items
 * - [Enter] to select the focused item
 */

$(document).on('keydown', '.dropdown-menu .dropdown-item', function (e) {
    const $items = $(this).closest('.dropdown-menu').find('.dropdown-item:visible');
    const index = $items.index(this);

    switch (e.key) {
        case 'ArrowDown':
            e.preventDefault();
            if (index < $items.length - 1) {
                $items.eq(index + 1).focus();
            } else {
                $items.eq(0).focus(); // wrap to top
            }
            break;

        case 'ArrowUp':
            e.preventDefault();
            if (index > 0) {
                $items.eq(index - 1).focus();
            } else {
                $items.eq($items.length - 1).focus(); // wrap to bottom
            }
            break;

        case 'Enter':
            e.preventDefault();
            $(this).click(); // trigger selection
            break;
    }
});

// Optional: auto-focus first item when dropdown opens
$(document).on('shown.bs.dropdown', function (e) {
    const $menu = $(e.target).find('.dropdown-menu:visible');
    const $firstItem = $menu.find('.dropdown-item:visible').first();
    if ($firstItem.length) {
        setTimeout(() => $firstItem.focus(), 0); // short delay to allow DOM to settle
    }
});

function copyToClipboard(text) {
    navigator.clipboard.writeText(text)
    .catch(err => {
        window.top.ShowSnack('danger', 'Error copying to clipboard.', 5000, true);
        return;
    });

    window.top.ShowSnack('success', 'Successfully copied to clipboard.', 5000, true);
}