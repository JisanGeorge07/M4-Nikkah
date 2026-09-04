


window.addEventListener('load', fn, false)

//  window.onload = function loader() {
function fn() {
    // Preloader
    if (document.getElementById('preloader')) {
        setTimeout(() => {
            document.getElementById('preloader').style.visibility = 'hidden';
            document.getElementById('preloader').style.opacity = '0';
        }, 350);
    }
    // Menus
    activateMenu();
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

            var parent = getClosest(immediateParent, '.child-menu-item');
            if (parent) {
                parent.classList.add('active');
            }

            var parent = getClosest(parent || immediateParent, '.parent-menu-item');

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

// Menu sticky
function windowScroll() {
    const navbar = document.getElementById("topnav");
    if (navbar != null) {
        if (
            document.body.scrollTop >= 50 ||
            document.documentElement.scrollTop >= 50
        ) {
            navbar.classList.add("nav-sticky");
        } else {
            navbar.classList.remove("nav-sticky");
        }
    }
}

window.addEventListener('scroll', (ev) => {
    ev.preventDefault();
    windowScroll();
})

// back-to-top
var mybutton = document.getElementById("back-to-top");
window.onscroll = function () {
    scrollFunction();
};

function scrollFunction() {
    if (mybutton != null) {
        if (document.body.scrollTop > 500 || document.documentElement.scrollTop > 500) {
            mybutton.style.display = "block";
        } else {
            mybutton.style.display = "none";
        }
    }
}

function topFunction() {
    document.body.scrollTop = 0;
    document.documentElement.scrollTop = 0;
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

//Feather icon
feather.replace();

// dd-menu
var ddmenu = document.getElementsByClassName("dd-menu");
for (var i = 0, len = ddmenu.length; i < len; i++) {
    ddmenu[i].onclick = function (elem) {
        elem.stopPropagation();
    }
}

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

//small menu
try {
    var spy = new Gumshoe('#navmenu-nav a');
} catch (err) {

}


//Contact js
try {
    function validateForm() {
        var name = document.forms["myForm"]["name"].value;
        var email = document.forms["myForm"]["email"].value;
        var subject = document.forms["myForm"]["subject"].value;
        var comments = document.forms["myForm"]["comments"].value;
        document.getElementById("error-msg").style.opacity = 0;
        document.getElementById('error-msg').innerHTML = "";
        if (name == "" || name == null) {
            document.getElementById('error-msg').innerHTML = "<div class='alert alert-warning error_message'>*Please enter a Name*</div>";
            fadeIn();
            return false;
        }
        if (email == "" || email == null) {
            document.getElementById('error-msg').innerHTML = "<div class='alert alert-warning error_message'>*Please enter a Email*</div>";
            fadeIn();
            return false;
        }
        if (subject == "" || subject == null) {
            document.getElementById('error-msg').innerHTML = "<div class='alert alert-warning error_message'>*Please enter a Subject*</div>";
            fadeIn();
            return false;
        }
        if (comments == "" || comments == null) {
            document.getElementById('error-msg').innerHTML = "<div class='alert alert-warning error_message'>*Please enter a Comments*</div>";
            fadeIn();
            return false;
        }
        var xhttp = new XMLHttpRequest();
        xhttp.onreadystatechange = function () {
            if (this.readyState == 4 && this.status == 200) {
                document.getElementById("simple-msg").innerHTML = this.responseText;
                document.forms["myForm"]["name"].value = "";
                document.forms["myForm"]["email"].value = "";
                document.forms["myForm"]["subject"].value = "";
                document.forms["myForm"]["comments"].value = "";
            }
        };
        xhttp.open("POST", "php/contact.php", true);
        xhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
        xhttp.send("name=" + name + "&email=" + email + "&subject=" + subject + "&comments=" + comments);
        return false;
    }

    function fadeIn() {
        var fade = document.getElementById("error-msg");
        var opacity = 0;
        var intervalID = setInterval(function () {
            if (opacity < 1) {
                opacity = opacity + 0.5
                fade.style.opacity = opacity;
            } else {
                clearInterval(intervalID);
            }
        }, 200);
    }
} catch (error) {

}




document.addEventListener("DOMContentLoaded", function () {
    var otpInputs = document.querySelectorAll(".otp-input");
    var emailOtpInputs = document.querySelectorAll(".email-otp-input");

    function setupOtpInputListeners(inputs) {
        inputs.forEach(function (input, index) {
            input.addEventListener("paste", function (ev) {
                var clip = ev.clipboardData.getData('text').trim();
                if (!/^\d{6}$/.test(clip)) {
                    ev.preventDefault();
                    return;
                }

                var characters = clip.split("");
                inputs.forEach(function (otpInput, i) {
                    otpInput.value = characters[i] || "";
                });

                enableNextBox(inputs[0], 0);
                inputs[5].removeAttribute("disabled");
                inputs[5].focus();
                updateOTPValue(inputs);
            });

            input.addEventListener("input", function () {
                var currentIndex = Array.from(inputs).indexOf(this);
                var inputValue = this.value.trim();

                if (!/^\d$/.test(inputValue)) {
                    this.value = "";
                    return;
                }

                if (inputValue && currentIndex < 5) {
                    inputs[currentIndex + 1].removeAttribute("disabled");
                    inputs[currentIndex + 1].focus();
                }

                if (currentIndex === 4 && inputValue) {
                    inputs[5].removeAttribute("disabled");
                    inputs[5].focus();
                }

                updateOTPValue(inputs);
            });

            input.addEventListener("keydown", function (ev) {
                var currentIndex = Array.from(inputs).indexOf(this);

                if (!this.value && ev.key === "Backspace" && currentIndex > 0) {
                    inputs[currentIndex - 1].focus();
                }
            });
        });
    }

    function enableNextBox(input, currentIndex) {
        var inputValue = input.value;

        if (inputValue === "") {
            return;
        }

        var nextIndex = currentIndex + 1;
        var nextBox = otpInputs[nextIndex] || emailOtpInputs[nextIndex];

        if (nextBox) {
            nextBox.removeAttribute("disabled");
        }
    }

    function updateOTPValue(inputs) {
        var otpValue = "";

        inputs.forEach(function (input) {
            otpValue += input.value;
        });

        if (inputs === otpInputs) {
            var el = document.getElementById("verificationCode");
            if (el) el.value = otpValue;
        } else if (inputs === emailOtpInputs) {
            var el = document.getElementById("emailverificationCode");
            if (el) el.value = otpValue;
        }
    }

    if (otpInputs && otpInputs.length > 0) {
        setupOtpInputListeners(otpInputs);
        if (otpInputs[0]) otpInputs[0].focus();
        if (otpInputs[5]) {
            otpInputs[5].addEventListener("input", function () {
                updateOTPValue(otpInputs);
            });
        }
    }
    if (emailOtpInputs && emailOtpInputs.length > 0) {
        setupOtpInputListeners(emailOtpInputs);
        if (emailOtpInputs[0]) emailOtpInputs[0].focus();
        if (emailOtpInputs[5]) {
            emailOtpInputs[5].addEventListener("input", function () {
                updateOTPValue(emailOtpInputs);
            });
        }
    }
});



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



