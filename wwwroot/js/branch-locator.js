(function () {
    "use strict";

    var root = document.querySelector("[data-branch-locator]");
    if (!root) {
        return;
    }

    var form = root.querySelector("[data-branch-search-form]");
    var input = root.querySelector("[data-branch-search-input]");
    var suggestions = root.querySelector("[data-branch-suggestions]");
    var dataNode = root.querySelector("[data-branch-suggestion-data]");

    if (!form || !input || !suggestions || !dataNode) {
        return;
    }

    var branches = [];

    try {
        branches = JSON.parse(dataNode.textContent || "[]");
    } catch {
        branches = [];
    }

    if (!Array.isArray(branches) || branches.length === 0) {
        return;
    }

    var activeIndex = -1;
    var visibleItems = [];

    function normalize(value) {
        return String(value || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .trim();
    }

    function buildHaystack(branch) {
        return normalize([
            branch.name,
            branch.address,
            branch.city,
            branch.district,
            branch.hotline
        ].join(" "));
    }

    var indexedBranches = branches.map(function (branch) {
        return {
            name: branch.name || "",
            address: branch.address || "",
            city: branch.city || "",
            district: branch.district || "",
            haystack: buildHaystack(branch)
        };
    }).filter(function (branch) {
        return branch.name;
    });

    function closeSuggestions() {
        activeIndex = -1;
        visibleItems = [];
        suggestions.classList.add("hidden");
        suggestions.innerHTML = "";
        input.setAttribute("aria-expanded", "false");
        input.removeAttribute("aria-activedescendant");
    }

    function chooseSuggestion(index) {
        var item = visibleItems[index];
        if (!item) {
            return;
        }

        input.value = item.name;
        closeSuggestions();
        form.submit();
    }

    function setActive(index) {
        var options = suggestions.querySelectorAll("[data-branch-suggestion-item]");
        if (!options.length) {
            activeIndex = -1;
            return;
        }

        activeIndex = (index + options.length) % options.length;

        options.forEach(function (option, optionIndex) {
            var isActive = optionIndex === activeIndex;
            option.classList.toggle("bg-brand-light", isActive);
            option.setAttribute("aria-selected", isActive ? "true" : "false");
        });

        input.setAttribute("aria-activedescendant", options[activeIndex].id);
    }

    function renderSuggestions(items) {
        if (!items.length) {
            closeSuggestions();
            return;
        }

        visibleItems = items;
        activeIndex = -1;

        suggestions.innerHTML = items.map(function (item, index) {
            var meta = [item.district, item.city].filter(Boolean).join(", ") || item.address;
            var address = item.address ? "<span class=\"mt-0.5 block truncate text-xs font-semibold text-brand-gray\">" + escapeHtml(item.address) + "</span>" : "";

            return [
                "<button type=\"button\"",
                " id=\"branch-suggestion-" + index + "\"",
                " role=\"option\"",
                " aria-selected=\"false\"",
                " data-branch-suggestion-item",
                " data-index=\"" + index + "\"",
                " class=\"block w-full px-4 py-3 text-left transition hover:bg-brand-light focus:bg-brand-light focus:outline-none\">",
                "<span class=\"block text-sm font-black text-brand-dark\">" + escapeHtml(item.name) + "</span>",
                "<span class=\"mt-1 block text-xs font-bold uppercase tracking-[0.12em] text-brand-goldDeep\">" + escapeHtml(meta) + "</span>",
                address,
                "</button>"
            ].join("");
        }).join("");

        suggestions.classList.remove("hidden");
        input.setAttribute("aria-expanded", "true");
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function updateSuggestions() {
        var query = normalize(input.value);

        if (query.length < 2) {
            closeSuggestions();
            return;
        }

        var matches = indexedBranches.filter(function (branch) {
            return branch.haystack.indexOf(query) !== -1;
        }).slice(0, 5);

        renderSuggestions(matches);
    }

    input.addEventListener("input", updateSuggestions);

    input.addEventListener("keydown", function (event) {
        if (suggestions.classList.contains("hidden")) {
            return;
        }

        if (event.key === "ArrowDown") {
            event.preventDefault();
            setActive(activeIndex + 1);
        } else if (event.key === "ArrowUp") {
            event.preventDefault();
            setActive(activeIndex - 1);
        } else if (event.key === "Enter" && activeIndex >= 0) {
            event.preventDefault();
            chooseSuggestion(activeIndex);
        } else if (event.key === "Escape") {
            event.preventDefault();
            closeSuggestions();
        }
    });

    suggestions.addEventListener("click", function (event) {
        var button = event.target.closest("[data-branch-suggestion-item]");
        if (!button) {
            return;
        }

        chooseSuggestion(Number(button.getAttribute("data-index")));
    });

    document.addEventListener("click", function (event) {
        if (!root.contains(event.target)) {
            closeSuggestions();
        }
    });
})();
