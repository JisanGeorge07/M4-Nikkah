
window.onload = function loader() {
    // Preloader
    if (document.getElementById('preloader')) {
        setTimeout(() => {
            document.getElementById('preloader').style.visibility = 'hidden';
            document.getElementById('preloader').style.opacity = '0';
        }, 350);
    }

    // Menus
    activateMenu();
    activateSidebarMenu();
}

//Menu
// Toggle menu
function toggleMenu() {
    document.getElementById('isToggle').classList.toggle('open');
    var isOpen = document.getElementById('navigation')
    if (isOpen.style.display === "block") {
        isOpen.style.display = "none";
    } else {
        isOpen.style.display = "block";
    }
};

//Menu Active
function getClosest(elem, selector) {

    // Element.matches() polyfill
    if (!Element.prototype.matches) {
        Element.prototype.matches =
            Element.prototype.matchesSelector ||
            Element.prototype.mozMatchesSelector ||
            Element.prototype.msMatchesSelector ||
            Element.prototype.oMatchesSelector ||
            Element.prototype.webkitMatchesSelector ||
            function (s) {
                var matches = (this.document || this.ownerDocument).querySelectorAll(s),
                    i = matches.length;
                while (--i >= 0 && matches.item(i) !== this) { }
                return i > -1;
            };
    }

    // Get the closest matching element
    for (; elem && elem !== document; elem = elem.parentNode) {
        if (elem.matches(selector)) return elem;
    }
    return null;

};

function activateMenu() {
    var menuItems = document.getElementsByClassName("sub-menu-item");
    if (menuItems) {

        var matchingMenuItem = null;
        for (var idx = 0; idx < menuItems.length; idx++) {
            if (menuItems[idx].href === window.location.href) {
                matchingMenuItem = menuItems[idx];
            }
        }

        if (matchingMenuItem) {
            matchingMenuItem.classList.add('active');
            var immediateParent = getClosest(matchingMenuItem, 'li');
            if (immediateParent) {
                immediateParent.classList.add('active');
            }

            var parent = getClosest(matchingMenuItem, '.parent-menu-item');
            if (parent) {
                parent.classList.add('active');
                var parentMenuitem = parent.querySelector('.menu-item');
                if (parentMenuitem) {
                    parentMenuitem.classList.add('active');
                }
                var parentOfParent = getClosest(parent, '.parent-parent-menu-item');
                if (parentOfParent) {
                    parentOfParent.classList.add('active');
                }
            } else {
                var parentOfParent = getClosest(matchingMenuItem, '.parent-parent-menu-item');
                if (parentOfParent) {
                    parentOfParent.classList.add('active');
                }
            }
        }
    }
}


//Admin Menu
function activateSidebarMenu() {
    var current = location.pathname.substring(location.pathname.lastIndexOf('/') + 1);
    if (current !== "" && document.getElementById("sidebar")) {
        var menuItems = document.querySelectorAll('#sidebar a');
        for (var i = 0, len = menuItems.length; i < len; i++) {
            if (menuItems[i].getAttribute("href").indexOf(current) !== -1) {
                menuItems[i].parentElement.className += " active";
                if (menuItems[i].closest(".sidebar-submenu")) {
                    menuItems[i].closest(".sidebar-submenu").classList.add("d-block");
                }
                if (menuItems[i].closest(".sidebar-dropdown")) {
                    menuItems[i].closest(".sidebar-dropdown").classList.add("active");
                }
            }
        }
    }
}

if (document.getElementById("close-sidebar")) {
    document.getElementById("close-sidebar").addEventListener("click", function () {
        document.getElementsByClassName("page-wrapper")[0].classList.toggle("toggled");
    });
}

// Clickable Menu
if (document.getElementById("navigation")) {
    var elements = document.getElementById("navigation").getElementsByTagName("a");
    for (var i = 0, len = elements.length; i < len; i++) {
        elements[i].onclick = function (elem) {
            if (elem.target.getAttribute("href") === "javascript:void(0)") {
                var submenu = elem.target.nextElementSibling.nextElementSibling;
                submenu.classList.toggle('open');
            }
        }
    }
}

if (document.getElementById("sidebar")) {
    var elements = document.getElementById("sidebar").getElementsByTagName("a");
    for (var i = 0, len = elements.length; i < len; i++) {
        elements[i].onclick = function (elem) {
            if (elem.target !== document.querySelectorAll("li.sidebar-dropdown.active > a")[0]) {
                document.querySelectorAll("li.sidebar-dropdown.active")[0]?.classList?.toggle("active");
                document.querySelectorAll("div.sidebar-submenu.d-block")[0]?.classList?.toggle("d-block");
            }
            if (elem.target.getAttribute("href") === "javascript:void(0)") {
                elem.target.parentElement.classList.toggle("active");
                elem.target.nextElementSibling.classList.toggle("d-block");
            }
        }
    }
}

// Menu sticky
function windowScroll() {
    var navbar = document.getElementById("topnav");
    if (navbar === null) {

    } else if (document.body.scrollTop >= 50 ||
        document.documentElement.scrollTop >= 50) {
        navbar.classList.add("nav-sticky");
    } else {
        navbar.classList.remove("nav-sticky");
    }
}

window.addEventListener('scroll', (ev) => {
    ev.preventDefault();
    windowScroll();
})

// back-to-top
window.onscroll = function () {
    scrollFunction();
};

function scrollFunction() {
    var mybutton = document.getElementById("back-to-top");
    if (mybutton === null) {

    } else if (document.body.scrollTop > 500 || document.documentElement.scrollTop > 500) {
        mybutton.style.display = "block";
    } else {
        mybutton.style.display = "none";
    }
}

function topFunction() {
    document.body.scrollTop = 0;
    document.documentElement.scrollTop = 0;
}

//Feather icon
feather.replace();

// dd-menu
if (document.getElementsByClassName("dd-menu")) {
    var ddmenu = document.getElementsByClassName("dd-menu");
    for (var i = 0, len = ddmenu.length; i < len; i++) {
        ddmenu[i].onclick = function (elem) {
            elem.stopPropagation();
        }
    }
}

//ACtive Sidebar
(function () {
    var current = location.pathname.substring(location.pathname.lastIndexOf('/') + 1);;
    if (current === "") return;
    var menuItems = document.querySelectorAll('.sidebar-nav a');
    for (var i = 0, len = menuItems.length; i < len; i++) {
        if (menuItems[i].getAttribute("href").indexOf(current) !== -1) {
            menuItems[i].parentElement.className += " active";
        }
    }
})();


//Validation Shop Checkouts
(function () {
    'use strict'

    if (document.getElementsByClassName('needs-validation').length > 0) {
        // Fetch all the forms we want to apply custom Bootstrap validation styles to
        var forms = document.querySelectorAll('.needs-validation')

        // Loop over them and prevent submission
        Array.prototype.slice.call(forms)
            .forEach(function (form) {
                form.addEventListener('submit', function (event) {
                    if (!form.checkValidity()) {
                        event.preventDefault()
                        event.stopPropagation()
                    }

                    form.classList.add('was-validated')
                }, false)
            })
    }
})();

//Tooltip
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl)
});

//Popovers
var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
    return new bootstrap.Popover(popoverTriggerEl)
})






const rangeInput = document.querySelectorAll(".range-input input"),
    priceInput = document.querySelectorAll(".price-input input"),
    range = document.querySelector(".slider .progress");
let priceGap = 1000;

priceInput.forEach(input => {
    input.addEventListener("input", e => {
        let minPrice = parseInt(priceInput[0].value),
            maxPrice = parseInt(priceInput[1].value);

        if ((maxPrice - minPrice >= priceGap) && maxPrice <= rangeInput[1].max) {
            if (e.target.className === "input-min") {
                rangeInput[0].value = minPrice;
                range.style.left = ((minPrice / rangeInput[0].max) * 100) + "%";
            } else {
                rangeInput[1].value = maxPrice;
                range.style.right = 100 - (maxPrice / rangeInput[1].max) * 100 + "%";
            }
        }
    });
});

rangeInput.forEach(input => {
    input.addEventListener("input", e => {
        let minVal = parseInt(rangeInput[0].value),
            maxVal = parseInt(rangeInput[1].value);

        if ((maxVal - minVal) < priceGap) {
            if (e.target.className === "range-min") {
                rangeInput[0].value = maxVal - priceGap
            } else {
                rangeInput[1].value = minVal + priceGap;
            }
        } else {
            priceInput[0].value = minVal;
            priceInput[1].value = maxVal;
            range.style.left = ((minVal / rangeInput[0].max) * 100) + "%";
            range.style.right = 100 - (maxVal / rangeInput[1].max) * 100 + "%";
        }
    });
});



//New Ui 

window.onload = function loader() {
    // Preloader
    if (document.getElementById("preloader")) {
        setTimeout(() => {
            document.getElementById("preloader").style.visibility = "hidden";
            document.getElementById("preloader").style.opacity = "0";
        }, 350);
    }

    // Menus
    activateMenu();
    activateSidebarMenu();
};

//Menu
// Toggle menu
function toggleMenu() {
    document.getElementById("isToggle").classList.toggle("open");
    var isOpen = document.getElementById("navigation");
    if (isOpen.style.display === "block") {
        isOpen.style.display = "none";
    } else {
        isOpen.style.display = "block";
    }
}

//Menu Active
function getClosest(elem, selector) {
    // Element.matches() polyfill
    if (!Element.prototype.matches) {
        Element.prototype.matches =
            Element.prototype.matchesSelector ||
            Element.prototype.mozMatchesSelector ||
            Element.prototype.msMatchesSelector ||
            Element.prototype.oMatchesSelector ||
            Element.prototype.webkitMatchesSelector ||
            function (s) {
                var matches = (this.document || this.ownerDocument).querySelectorAll(s),
                    i = matches.length;
                while (--i >= 0 && matches.item(i) !== this) { }
                return i > -1;
            };
    }

    // Get the closest matching element
    for (; elem && elem !== document; elem = elem.parentNode) {
        if (elem.matches(selector)) return elem;
    }
    return null;
}

function activateMenu() {
    var menuItems = document.getElementsByClassName("sub-menu-item");
    if (menuItems) {
        var matchingMenuItem = null;
        for (var idx = 0; idx < menuItems.length; idx++) {
            if (menuItems[idx].href === window.location.href) {
                matchingMenuItem = menuItems[idx];
            }
        }

        if (matchingMenuItem) {
            matchingMenuItem.classList.add("active");
            var immediateParent = getClosest(matchingMenuItem, "li");
            if (immediateParent) {
                immediateParent.classList.add("active");
            }

            var parent = getClosest(matchingMenuItem, ".parent-menu-item");
            if (parent) {
                parent.classList.add("active");
                var parentMenuitem = parent.querySelector(".menu-item");
                if (parentMenuitem) {
                    parentMenuitem.classList.add("active");
                }
                var parentOfParent = getClosest(parent, ".parent-parent-menu-item");
                if (parentOfParent) {
                    parentOfParent.classList.add("active");
                }
            } else {
                var parentOfParent = getClosest(matchingMenuItem, ".parent-parent-menu-item");
                if (parentOfParent) {
                    parentOfParent.classList.add("active");
                }
            }
        }
    }
}

//Admin Menu
function activateSidebarMenu() {
    var current = location.pathname.substring(location.pathname.lastIndexOf("/") + 1);
    if (current !== "" && document.getElementById("sidebar")) {
        var menuItems = document.querySelectorAll("#sidebar a");
        for (var i = 0, len = menuItems.length; i < len; i++) {
            if (menuItems[i].getAttribute("href").indexOf(current) !== -1) {
                menuItems[i].parentElement.className += " active";
                if (menuItems[i].closest(".sidebar-submenu")) {
                    menuItems[i].closest(".sidebar-submenu").classList.add("d-block");
                }
                if (menuItems[i].closest(".sidebar-dropdown")) {
                    menuItems[i].closest(".sidebar-dropdown").classList.add("active");
                }
            }
        }
    }
}

if (document.getElementById("close-sidebar")) {
    document.getElementById("close-sidebar").addEventListener("click", function () {
        document.getElementsByClassName("page-wrapper")[0].classList.toggle("toggled");
    });
}

// Clickable Menu
if (document.getElementById("navigation")) {
    var elements = document.getElementById("navigation").getElementsByTagName("a");
    for (var i = 0, len = elements.length; i < len; i++) {
        elements[i].onclick = function (elem) {
            if (elem.target.getAttribute("href") === "javascript:void(0)") {
                var submenu = elem.target.nextElementSibling.nextElementSibling;
                submenu.classList.toggle("open");
            }
        };
    }
}

if (document.getElementById("sidebar")) {
    var elements = document.getElementById("sidebar").getElementsByTagName("a");
    for (var i = 0, len = elements.length; i < len; i++) {
        elements[i].onclick = function (elem) {
            if (elem.target !== document.querySelectorAll("li.sidebar-dropdown.active > a")[0]) {
                document.querySelectorAll("li.sidebar-dropdown.active")[0]?.classList?.toggle("active");
                document.querySelectorAll("div.sidebar-submenu.d-block")[0]?.classList?.toggle("d-block");
            }
            if (elem.target.getAttribute("href") === "javascript:void(0)") {
                elem.target.parentElement.classList.toggle("active");
                elem.target.nextElementSibling.classList.toggle("d-block");
            }
        };
    }
}

// Menu sticky
function windowScroll() {
    var navbar = document.getElementById("topnav");
    if (navbar === null) {
    } else if (document.body.scrollTop >= 50 || document.documentElement.scrollTop >= 50) {
        navbar.classList.add("nav-sticky");
    } else {
        navbar.classList.remove("nav-sticky");
    }
}

window.addEventListener("scroll", (ev) => {
    ev.preventDefault();
    windowScroll();
});

// back-to-top
window.onscroll = function () {
    scrollFunction();
};

function scrollFunction() {
    var mybutton = document.getElementById("back-to-top");
    if (mybutton === null) {
    } else if (document.body.scrollTop > 500 || document.documentElement.scrollTop > 500) {
        mybutton.style.display = "block";
    } else {
        mybutton.style.display = "none";
    }
}

function topFunction() {
    document.body.scrollTop = 0;
    document.documentElement.scrollTop = 0;
}

//Feather icon
feather.replace();

// dd-menu
if (document.getElementsByClassName("dd-menu")) {
    var ddmenu = document.getElementsByClassName("dd-menu");
    for (var i = 0, len = ddmenu.length; i < len; i++) {
        ddmenu[i].onclick = function (elem) {
            elem.stopPropagation();
        };
    }
}

//ACtive Sidebar
(function () {
    var current = location.pathname.substring(location.pathname.lastIndexOf("/") + 1);
    if (current === "") return;
    var menuItems = document.querySelectorAll(".sidebar-nav a");
    for (var i = 0, len = menuItems.length; i < len; i++) {
        if (menuItems[i].getAttribute("href").indexOf(current) !== -1) {
            menuItems[i].parentElement.className += " active";
        }
    }
})();

//Validation Shop Checkouts
(function () {
    "use strict";

    if (document.getElementsByClassName("needs-validation").length > 0) {
        // Fetch all the forms we want to apply custom Bootstrap validation styles to
        var forms = document.querySelectorAll(".needs-validation");

        // Loop over them and prevent submission
        Array.prototype.slice.call(forms).forEach(function (form) {
            form.addEventListener(
                "submit",
                function (event) {
                    if (!form.checkValidity()) {
                        event.preventDefault();
                        event.stopPropagation();
                    }

                    form.classList.add("was-validated");
                },
                false,
            );
        });
    }
})();

//Tooltip
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

//Popovers
var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
    return new bootstrap.Popover(popoverTriggerEl);
});


// ACCOUNT SECTION

// EXPLORE PAGE TABS

// Main TAb

function showMainTab(tabId) {
    // Hide profile detail section if it exists
    const detailSection = document.getElementById("profileDetailSection");
    if (detailSection) detailSection.classList.add("d-none");

    document.querySelectorAll(".main-tab-content").forEach((tab) => {
        tab.classList.add("d-none");
        // Show rows inside the tab (to restore from detail view)
        tab.querySelectorAll(".row").forEach((row) => row.classList.remove("d-none"));
    });

    document.getElementById(tabId).classList.remove("d-none");

    document.querySelectorAll(".main-tab").forEach((btn) => {
        btn.classList.remove("active");
    });

    if (event && event.target) {
        event.target.classList.add("active");
    }
}

// Bottom Tab
function showTab(tab) {
    // Hide profile detail section if it exists
    const detailSection = document.getElementById("profileDetailSection");
    if (detailSection) detailSection.classList.add("d-none");

    // Hide all tab contents
    document.querySelectorAll(".tab-content").forEach((t) => {
        t.classList.add("d-none");
        // Show rows inside the tab
        t.classList.remove("d-none"); // Ensure the container itself is not hidden if we want it shown
    });

    // Re-hide all and show selected
    document.querySelectorAll(".tab-content").forEach((t) => t.classList.add("d-none"));
    document.getElementById(tab).classList.remove("d-none");
    document.querySelectorAll("#" + tab + " .row").forEach((row) => row.classList.remove("d-none"));

    // Remove active class from all buttons
    document.querySelectorAll(".bottom-tabs").forEach((b) => b.classList.remove("active"));

    // Add active class to clicked button
    if (event && event.target) {
        event.target.classList.add("active");
    }
}
function showProfileDetail(image, name, info) {
    // Hide only the profile cards (rows) across all tabs
    document.querySelectorAll(".main-tab-content .row").forEach((row) => {
        row.classList.add("d-none");
    });

    // Show detail section
    document.getElementById("profileDetailSection").classList.remove("d-none");

    // Update data
    document.getElementById("detailImage").src = image;
    document.getElementById("detailName").innerText = name;
    document.getElementById("detailInfo").innerText = info;

    // Scroll to the tabs container to keep navigation visible
    const tabsContainer = document.querySelector(".main-tab-container");
    if (tabsContainer) {
        tabsContainer.scrollIntoView({ behavior: "smooth", block: "start" });
    }
}

function showCardsSection() {
    // Hide detail section
    document.getElementById("profileDetailSection").classList.add("d-none");

    // Show the profile cards (rows) for the currently active tab
    // Find which main tab is active
    const activeMainTabId =
        document
            .querySelector(".main-tab.active")
            ?.getAttribute("onclick")
            ?.match(/'([^']+)'/)?.[1] || "favorite";
    const activeMainTab = document.getElementById(activeMainTabId);

    if (activeMainTab) {
        // Show the row in the active main tab
        activeMainTab.querySelectorAll(".row").forEach((row) => {
            row.classList.remove("d-none");
        });
    }

    // Also ensure active bottom tab row is shown if in favorite
    const activeBottomTabId = document
        .querySelector(".bottom-tabs.active")
        ?.getAttribute("onclick")
        ?.match(/'([^']+)'/)?.[1];
    if (activeBottomTabId) {
        const bottomTab = document.getElementById(activeBottomTabId);
        if (bottomTab) bottomTab.classList.remove("d-none");
    }
}

function showUpgradeModal() {
    const modal = document.getElementById("upgradeModal");

    // Sync modal data with current detail view if available
    const detailName = document.getElementById("detailName").innerText;
    const detailImage = document.getElementById("detailImage").src;

    if (detailName) document.getElementById("modalProfileName").innerText = detailName;
    if (detailImage) document.getElementById("modalProfileImg").src = detailImage;

    modal.classList.add("active");
    document.body.style.overflow = "hidden";
}

function hideUpgradeModal() {
    const modal = document.getElementById("upgradeModal");
    modal.classList.remove("active");
    document.body.style.overflow = "auto";
}

function showPackagesModal() {
    // Close upgrade modal first
    const upgradeModal = document.getElementById("upgradeModal");
    upgradeModal.classList.remove("active");

    // Sync profile image
    const detailImage = document.getElementById("detailImage").src;
    if (detailImage) document.getElementById("packageModalProfileImg").src = detailImage;

    // Show packages modal
    const modal = document.getElementById("packagesModal");
    modal.classList.add("active");
    document.body.style.overflow = "hidden";
}

function hidePackagesModal() {
    const modal = document.getElementById("packagesModal");
    modal.classList.remove("active");
    document.body.style.overflow = "auto";
}

function showFavouriteModal() {
    const modal = document.getElementById("favouriteModal");
    if (modal) {
        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function hideFavouriteModal() {
    const modal = document.getElementById("favouriteModal");
    if (modal) {
        modal.classList.remove("active");
        document.body.style.overflow = "auto";
    }
}

function showBlockedModal() {
    const modal = document.getElementById("blockedModal");
    if (modal) {
        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function hideBlockedModal() {
    const modal = document.getElementById("blockedModal");
    if (modal) {
        modal.classList.remove("active");
        document.body.style.overflow = "auto";
    }
}

function showReportMainModal() {
    const modal = document.getElementById("reportMainModal");
    if (modal) {
        const detailName = document.getElementById("detailName").innerText;
        const nameOnly = detailName.split(",")[0]; // Get "Faaziya" from "Faaziya, 25"

        document.getElementById("reportMainTitle").innerText = "Reporting " + nameOnly;
        document.getElementById("reportMainName").innerText = nameOnly;

        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function showReportDetailsModal(reason) {
    // Hide all report modals first
    hideReportModals();
    const modal = document.getElementById("reportDetailsModal");
    if (modal) {
        // Small delay to ensure smooth transition
        setTimeout(() => {
            modal.classList.add("active");
            document.body.style.overflow = "hidden";
        }, 50);
    }
}

function showReportFinalModal() {
    // Hide all report modals first
    hideReportModals();
    const modal = document.getElementById("reportFinalModal");
    if (modal) {
        // Sync profile details
        const detailName = document.getElementById("detailName").innerText;
        const detailImage = document.getElementById("detailImage").src;

        document.getElementById("reportFinalName").innerText = detailName;
        document.getElementById("reportFinalImage").src = detailImage;

        // Small delay to ensure smooth transition
        setTimeout(() => {
            modal.classList.add("active");
            document.body.style.overflow = "hidden";
        }, 50);
    }
}

function hideReportModals() {
    const modals = ["reportMainModal", "reportDetailsModal", "reportFinalModal"];
    modals.forEach((id) => {
        const modal = document.getElementById(id);
        if (modal) modal.classList.remove("active");
    });
    document.body.style.overflow = "auto";
}

//  Profile Edit Script
function toggleAccordion(header) {
    const item = header.parentElement;
    const isActive = item.classList.contains("active");

    // Close all other items
    document.querySelectorAll(".accordion-item-custom").forEach((i) => {
        if (i !== item) i.classList.remove("active");
    });

    if (isActive) {
        item.classList.remove("active");
    } else {
        item.classList.add("active");
    }
}

function openEditModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function closeEditModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove("active");
        document.body.style.overflow = "auto";
    }
}

// Gender toggle logic
function initGenderToggles() {
    document.querySelectorAll(".gender-btn").forEach((btn) => {
        // Remove existing listener
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);

        newBtn.addEventListener("click", function (e) {
            e.preventDefault();
            const wrapper = this.parentElement;
            wrapper.querySelectorAll(".gender-btn").forEach((b) => b.classList.remove("active"));
            this.classList.add("active");

            const hiddenInput = wrapper.querySelector('input[type="hidden"]');
            if (hiddenInput) {
                hiddenInput.value = this.innerText.trim();
            }
        });
    });
}

// Custom Select Logic
function initCustomSelects() {
    document.querySelectorAll(".custom-select-wrapper").forEach((wrapper) => {
        const trigger = wrapper.querySelector(".custom-select-trigger");
        const options = wrapper.querySelector(".custom-options");

        if (!trigger || !options) return;

        // Remove existing listeners if any (to avoid duplicates)
        const newTrigger = trigger.cloneNode(true);
        trigger.parentNode.replaceChild(newTrigger, trigger);

        newTrigger.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();

            const isOpen = options.classList.contains("show");

            // Close all other dropdowns
            document.querySelectorAll(".custom-options").forEach((opt) => {
                if (opt !== options) opt.classList.remove("show");
            });
            document.querySelectorAll(".custom-select-wrapper").forEach((w) => {
                if (w !== wrapper) w.classList.remove("active");
            });

            if (!isOpen) {
                options.classList.add("show");
                wrapper.classList.add("active");
            } else {
                options.classList.remove("show");
                wrapper.classList.remove("active");
            }
        });

        const isMulti = wrapper.classList.contains("multi-select");

        wrapper.querySelectorAll(".custom-option").forEach((option) => {
            // Clone the option node to detach previous click listeners and make it idempotent
            const newOption = option.cloneNode(true);
            option.parentNode.replaceChild(newOption, option);

            newOption.addEventListener("click", function (e) {
                e.stopPropagation();
                const value = this.getAttribute("data-value") || this.innerText;

                if (isMulti) {
                    const isAlreadySelected = this.classList.contains("selected");
                    if (!isAlreadySelected) {
                        const currentSelectedCount = wrapper.querySelectorAll(".custom-option.selected").length;
                        if (currentSelectedCount >= 4) {
                            alert('You can select a maximum of 4 courses.');
                            return;
                        }
                    }
                    this.classList.toggle("selected");
                    updateMultiSelect(wrapper);
                } else {
                    // Update only the text span, preserving the icon
                    const textSpan = newTrigger.querySelector("span");
                    if (textSpan) {
                        textSpan.innerText = this.innerText;
                    } else {
                        newTrigger.innerText = this.innerText;
                    }

                    wrapper.querySelectorAll(".custom-option").forEach((opt) => opt.classList.remove("selected"));
                    this.classList.add("selected");

                    options.classList.remove("show");
                    wrapper.classList.remove("active");

                    const hiddenInput = wrapper.querySelector('input[type="hidden"]');
                    if (hiddenInput) {
                        hiddenInput.value = value;
                        $(hiddenInput).trigger('change');
                    }
                }
            });
        });

        // Initialize display if multi-select
        if (isMulti) {
            updateMultiSelect(wrapper);
        }
    });
}

// Initialize on load
document.addEventListener("DOMContentLoaded", () => {
    initCustomSelects();
    initGenderToggles();
});
// Also re-init when modals open to be safe
const originalOpenEditModal = window.openEditModal;
window.openEditModal = function (id) {
    if (typeof originalOpenEditModal === "function") originalOpenEditModal(id);
    initCustomSelects();
    initGenderToggles();
};

// Close dropdowns on outside click
document.addEventListener("click", function () {
    document.querySelectorAll(".custom-options").forEach((opt) => opt.classList.remove("show"));
    document.querySelectorAll(".custom-select-wrapper").forEach((w) => w.classList.remove("active"));
});
console.log("Loading js");


let currentActionBtn = null;
let currentActionType = null;
let isRemovingInteraction = false;

function showInteractionModal(btn, type) {
    if (typeof event !== 'undefined' && event) event.stopPropagation();
    currentActionBtn = btn;
    currentActionType = type;

    const iconEl = btn.querySelector('i, svg');
    isRemovingInteraction = false;

    if (iconEl) {
        if (type === 'favourite' && iconEl.classList.contains('glow-star')) {
            isRemovingInteraction = true;
        } else if (type === 'interest') {
            if (iconEl.classList.contains('glow-heart-accepted')) {
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        title: 'Interest Accepted!',
                        text: 'You already have an accepted/mutual interest with this profile.',
                        icon: 'success',
                        confirmButtonColor: '#0d6b38'
                    });
                } else {
                    alert('Interest already accepted!');
                }
                return;
            } else if (iconEl.classList.contains('glow-heart-pending')) {
                isRemovingInteraction = true;
            } else if (iconEl.classList.contains('glow-heart')) {
                isRemovingInteraction = true;
            }
        }
    }


    const modal = document.getElementById('interactionModal');
    const title = document.getElementById('interactionModalTitle');

    if (type === 'favourite') {
        if (isRemovingInteraction) {
            title.innerText = 'Are you sure you want to remove the person from favourite';
        } else {
            title.innerText = 'Are you sure you want to make this person as favourite';
        }
    } else if (type === 'interest') {
        if (isRemovingInteraction) {
            title.innerText = 'Are you sure you want to remove the person from intrested';
        } else {
            title.innerText = 'Are you sure you want to make this person as intrested';
        }
    } else if (type === 'reject') {
        title.innerText = 'Are you sure you want to remove the person ?';
    }

    if (modal) {
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }
}

function hideInteractionModal() {
    const modal = document.getElementById('interactionModal');
    if (modal) {
        modal.classList.remove('active');
        document.body.style.overflow = '';
    }
    currentActionBtn = null;
    currentActionType = null;
    isRemovingInteraction = false;
}

function handleInteractionYes() {
    if (currentActionBtn) {
        const idParts = currentActionBtn.id.split('-');
        const itemId = idParts[1];
        if (!itemId) {
            console.error("No profile item ID found on button: ", currentActionBtn.id);
            hideInteractionModal();
            return;
        }

        const iconEl = currentActionBtn.querySelector('i, svg');
        const cardColumn = document.getElementById('matchingProfile-' + itemId) || document.getElementById('profile-' + itemId) || currentActionBtn.closest('.col-12');

        const fadeAndRemoveCard = () => {
            if (cardColumn) {
                cardColumn.style.transition = 'opacity 0.4s ease';
                cardColumn.style.opacity = '0';
                setTimeout(() => cardColumn.remove(), 400);
            }
        };

        if (currentActionType === 'reject') {
            const formData = new FormData();
            formData.append('NotLikedId', itemId);

            fetch('/User/NotLikeProfile', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(r => r.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            title: 'Dismissed!',
                            text: 'Profile has been dismissed successfully.',
                            icon: 'success',
                            width: '320px',
                            confirmButtonColor: '#00662d'
                        });
                        fadeAndRemoveCard();
                    } else {
                        Swal.fire('Error', data.message || 'An error occurred.', 'error');
                    }
                })
                .catch(err => {
                    console.error(err);
                    Swal.fire('Error', 'Failed to process request.', 'error');
                });
        } else {
            const isAdding = !isRemovingInteraction;
            const actionText = isAdding ? 'Add' : 'Remove';

            if (currentActionType === 'favourite') {
                const formData = new FormData();
                formData.append('StarId', itemId);
                formData.append('StarredId', itemId);
                formData.append('Action', actionText);

                fetch('/User/StarredIcon', {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(r => r.json())
                    .then(data => {
                        if (data.success) {
                            Swal.fire({
                                title: isAdding ? 'Shortlisted!' : 'Removed!',
                                text: isAdding ? 'Profile has been added to your favorites.' : 'Profile has been removed from your favorites.',
                                icon: 'success',
                                width: '320px',
                                confirmButtonColor: '#00662d'
                            });
                            if (iconEl) {
                                if (isAdding) {
                                    iconEl.classList.add('glow-star');
                                } else {
                                    iconEl.classList.remove('glow-star');
                                }
                            }
                            if (isAdding) {
                                fadeAndRemoveCard();
                            }
                        } else {
                            Swal.fire('Error', data.message || 'An error occurred.', 'error');
                        }
                    })
                    .catch(err => {
                        console.error(err);
                        Swal.fire('Error', 'Failed to process request.', 'error');
                    });
            } else if (currentActionType === 'interest') {
                const formData = new FormData();
                formData.append('LikedId', itemId);
                formData.append('Action', actionText);

                fetch('/User/UserFavouriteProfile', {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(r => r.json())
                    .then(data => {
                        if (data.success) {
                            Swal.fire({
                                title: isAdding ? 'Interest Sent!' : 'Interest Removed!',
                                text: isAdding ? 'Interest has been expressed successfully.' : 'Interest has been removed successfully.',
                                icon: 'success',
                                width: '320px',
                                confirmButtonColor: '#00662d'
                            });
                            if (iconEl) {
                                if (isAdding) {
                                    iconEl.classList.add('glow-heart-pending');
                                    iconEl.classList.remove('glow-heart');
                                    iconEl.classList.remove('glow-heart-accepted');
                                } else {
                                    iconEl.classList.remove('glow-heart-pending');
                                    iconEl.classList.remove('glow-heart-accepted');
                                    iconEl.classList.remove('glow-heart');
                                }
                            }
                        } else {
                            Swal.fire('Error', data.message || 'An error occurred.', 'error');
                        }
                    })
                    .catch(err => {
                        console.error(err);
                        Swal.fire('Error', 'Failed to process request.', 'error');
                    });
            }
        }
    }
    hideInteractionModal();
}

function handleInteractionNo() {
    hideInteractionModal();
}


/* Delete Account Modal Controls */
function showDeleteAccountModal() {
    const modal = document.getElementById("deleteAccountModal");
    if (modal) {
        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }
}

function hideDeleteAccountModal() {
    const modal = document.getElementById("deleteAccountModal");
    if (modal) {
        modal.classList.remove("active");
        document.body.style.overflow = "auto";
    }
}

function handleDeleteAccount() {
    hideDeleteAccountModal();
    if (typeof showDeleteReasonModal === "function") {
        showDeleteReasonModal();
    } else {
        alert("Account delete requested successfully!");
    }
}

/* Multi-Select Core Handler */
function updateMultiSelect(wrapper) {
    const selectedOptions = Array.from(wrapper.querySelectorAll(".custom-option.selected"));
    const selectedValues = selectedOptions.map(opt => opt.getAttribute("data-value") || opt.innerText);

    // Sync the hidden input element with selected values
    const hiddenInput = wrapper.querySelector('input[type="hidden"]');
    if (hiddenInput) {
        hiddenInput.value = selectedValues.join(", ");
    }

    // Update dropdown trigger text
    const trigger = wrapper.querySelector(".custom-select-trigger");
    const textSpan = trigger.querySelector("span");
    if (textSpan) {
        if (selectedValues.length === 0) {
            textSpan.innerText = "Select Course(s)";
        } else if (selectedValues.length === 1) {
            textSpan.innerText = selectedValues[0];
        } else {
            textSpan.innerText = `${selectedValues.length} Courses Selected`;
        }
    }

    // Find or create selected-tags-container directly below wrapper
    const parentGroup = wrapper.closest(".form-group-custom");
    let tagsContainer = parentGroup.querySelector(".selected-tags-container");
    if (!tagsContainer) {
        tagsContainer = document.createElement("div");
        tagsContainer.className = "selected-tags-container mt-2";
        parentGroup.appendChild(tagsContainer);
    }

    // Clear existing tags
    tagsContainer.innerHTML = "";

    // Render tag badges dynamically
    selectedOptions.forEach(opt => {
        const val = opt.getAttribute("data-value") || opt.innerText;

        const tag = document.createElement("div");
        tag.className = "selected-tag-badge";
        tag.innerHTML = `
      <span>${val}</span>
      <i class="uil uil-times remove-tag-btn" data-value="${val}"></i>
    `;

        // Hook click event to close button to unselect option and update
        tag.querySelector(".remove-tag-btn").addEventListener("click", function (e) {
            e.stopPropagation();
            opt.classList.remove("selected");
            updateMultiSelect(wrapper);
        });

        tagsContainer.appendChild(tag);
    });
}

/* Education Accordion Save & Sync Handler */
function saveEducationInfo() {
    const modal = document.getElementById("educationModal");
    if (modal) {
        const educationVal = modal.querySelector('input[name="education"]')?.value;
        const courseVal = modal.querySelector('input[name="course"]')?.value;
        const professionVal = modal.querySelector('input[name="profession"]')?.value;
        const professionTypeVal = modal.querySelector('input[name="profession_type"]')?.value;

        const displayEducation = document.getElementById("displayEducation");
        const displayCourse = document.getElementById("displayCourse");
        const displayProfession = document.getElementById("displayProfession");
        const displayProfessionType = document.getElementById("displayProfessionType");

        if (displayEducation && educationVal) displayEducation.innerText = educationVal;
        if (displayCourse && courseVal) displayCourse.innerText = courseVal || "None Selected";
        if (displayProfession && professionVal) displayProfession.innerText = professionVal;
        if (displayProfessionType && professionTypeVal) displayProfessionType.innerText = professionTypeVal;
    }
    closeEditModal("educationModal");
}
