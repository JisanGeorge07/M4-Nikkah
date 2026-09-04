/* ============================================
   IMAGE VALIDATOR — Shared JavaScript
   Used by: Manage Photos (Image.cshtml)
             Registration Step 5 (SignUp.cshtml)
   ============================================ */

var ImageValidator = (function () {

    var REQUIRED_WIDTH = 630;
    var REQUIRED_HEIGHT = 800;
    var cropperInstance = null;
    var currentFileInput = null;
    var currentWrapper = null;
    var currentPreviewImg = null;
    var currentMode = null; // 'manage' or 'signup'

    /**
     * Helper to check if the current page has any invalid/non-cropped uploads.
     */
    function hasInvalidUploads() {
        if (currentMode === 'manage') {
            var activeWarnBadges = document.querySelectorAll('.img-dimension-badge.badge-warn');
            return activeWarnBadges.length > 0;
        } else if (currentMode === 'signup') {
            var signupBadge = document.querySelector('.signup-dimension-badge');
            if (signupBadge && signupBadge.classList.contains('badge-warn')) {
                return true;
            }
        }
        return false;
    }

    /**
     * Initialize for the Manage Photos page (Image.cshtml).
     * Call this after DOMContentLoaded.
     */
    function initManagePhotos() {
        currentMode = 'manage';

        var wrappers = document.querySelectorAll('.photo-box-wrapper');

        wrappers.forEach(function (wrapper) {
            var fileInput = wrapper.querySelector('.photo-input');
            var badge = wrapper.querySelector('.img-dimension-badge');

            if (!fileInput || !badge) return;

            // Intercept file selection
            fileInput.addEventListener('change', function (e) {
                var file = e.target.files[0];
                if (file) {
                    validateImageFile(file, badge, fileInput, wrapper);
                }
            });
        });

        // Intercept form submission to block saving with uncropped images
        var form = document.getElementById('imageForm') || document.querySelector('form[asp-action="Image"]') || document.querySelector('form');
        if (form) {
            form.addEventListener('submit', function (e) {
                if (hasInvalidUploads()) {
                    e.preventDefault();
                    var errorMsg = 'Image must be exactly 630×800 pixels. Please click the "Edit" button to crop before saving.';
                    if (window.Swal) {
                        window.Swal.fire({
                            title: 'Invalid Dimensions',
                            text: errorMsg,
                            icon: 'warning',
                            width: '360px',
                            confirmButtonColor: '#00662d'
                        });
                    } else {
                        alert(errorMsg);
                    }
                }
            });
        }
    }

    /**
     * Initialize for the SignUp page (SignUp.cshtml Step 5).
     * Call this after DOMContentLoaded.
     */
    function initSignup() {
        currentMode = 'signup';

        var fileInput = document.getElementById('Image');
        var badge = document.querySelector('.signup-dimension-badge');

        if (!fileInput || !badge) return;

        fileInput.addEventListener('change', function (e) {
            var file = e.target.files[0];
            if (file) {
                validateImageFileSignup(file, badge, fileInput);
            }
        });

        // Intercept step 5 submit transitions
        var submitButtons = document.querySelectorAll('#btnSubmitRegistration');
        submitButtons.forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                if (hasInvalidUploads()) {
                    // Block execution of other click listeners (e.g. multi-step next)
                    e.stopImmediatePropagation();
                    e.preventDefault();

                    var errorMsg = 'Image must be exactly 630×800 pixels. Please click the "Edit" button on the badge to crop before submitting.';
                    if (window.Swal) {
                        window.Swal.fire({
                            title: 'Invalid Dimensions',
                            text: errorMsg,
                            icon: 'warning',
                            width: '360px',
                            confirmButtonColor: '#00662d'
                        });
                    } else {
                        alert(errorMsg);
                    }
                }
            }, true); // Capture phase to run before jQuery bubbling click listener
        });
    }

    /**
     * Validate an image file's dimensions (Manage Photos flow).
     */
    function validateImageFile(file, badge, fileInput, wrapper) {
        var url = URL.createObjectURL(file);
        var img = new Image();

        img.onload = function () {
            var w = img.naturalWidth;
            var h = img.naturalHeight;

            URL.revokeObjectURL(url);

            if (w === REQUIRED_WIDTH && h === REQUIRED_HEIGHT) {
                showBadgeOk(badge, w, h);
            } else {
                showBadgeWarn(badge, w, h, function () {
                    openCropModal(file, fileInput, wrapper, null, w, h);
                });
            }
        };

        img.onerror = function () {
            URL.revokeObjectURL(url);
        };

        img.src = url;
    }

    /**
     * Validate an image file's dimensions (SignUp flow).
     */
    function validateImageFileSignup(file, badge, fileInput) {
        var url = URL.createObjectURL(file);
        var img = new Image();

        img.onload = function () {
            var w = img.naturalWidth;
            var h = img.naturalHeight;

            URL.revokeObjectURL(url);

            if (w === REQUIRED_WIDTH && h === REQUIRED_HEIGHT) {
                showSignupBadgeOk(badge, w, h);
            } else {
                showSignupBadgeWarn(badge, w, h, function () {
                    openCropModal(file, fileInput, null, null, w, h);
                });
            }
        };

        img.onerror = function () {
            URL.revokeObjectURL(url);
        };

        img.src = url;
    }

    // ---------- Badge Display (Manage Photos) ----------

    function showBadgeOk(badge, w, h) {
        badge.className = 'img-dimension-badge badge-ok';
        badge.innerHTML = '<span class="badge-inner">' + w + '×' + h + ' ✓</span>';
        badge.style.display = 'block';
    }

    function showBadgeWarn(badge, w, h, editCallback) {
        badge.className = 'img-dimension-badge badge-warn';
        badge.innerHTML =
            '<span class="badge-inner">' +
            w + '×' + h + ' ⚠' +
            '<button type="button" class="badge-edit-btn">Edit</button>' +
            '</span>';
        badge.style.display = 'block';

        var editBtn = badge.querySelector('.badge-edit-btn');
        if (editBtn) {
            editBtn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                editCallback();
            });
        }
    }

    // ---------- Badge Display (SignUp) ----------

    function showSignupBadgeOk(badge, w, h) {
        badge.className = 'signup-dimension-badge badge-ok';
        badge.innerHTML = '<span class="badge-inner">' + w + '×' + h + ' ✓</span>';
        badge.style.display = 'flex';
    }

    function showSignupBadgeWarn(badge, w, h, editCallback) {
        badge.className = 'signup-dimension-badge badge-warn';
        badge.innerHTML =
            '<span class="badge-inner">' +
            w + '×' + h + ' ⚠' +
            '<button type="button" class="badge-edit-btn">Edit</button>' +
            '</span>';
        badge.style.display = 'flex';

        var editBtn = badge.querySelector('.badge-edit-btn');
        if (editBtn) {
            editBtn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                editCallback();
            });
        }
    }

    // ---------- Crop Modal ----------

    function openCropModal(file, fileInput, wrapper, previewImg, origW, origH) {
        currentFileInput = fileInput;
        currentWrapper = wrapper;
        currentPreviewImg = previewImg;

        var modal = document.getElementById('imgCropModal');
        if (!modal) return;

        var cropImage = document.getElementById('imgCropImage');
        var dimOriginal = document.getElementById('imgCropDimOriginal');
        var dimTarget = document.getElementById('imgCropDimTarget');

        // Set original dimensions text
        if (dimOriginal) dimOriginal.textContent = origW + '×' + origH;
        if (dimTarget) dimTarget.textContent = REQUIRED_WIDTH + '×' + REQUIRED_HEIGHT;

        // Load image into cropper
        var url = URL.createObjectURL(file);
        cropImage.src = url;

        // Destroy previous cropper if exists
        if (cropperInstance) {
            cropperInstance.destroy();
            cropperInstance = null;
        }

        // Show modal
        modal.classList.add('active');

        // Initialize Cropper.js once image loads
        cropImage.onload = function () {
            cropperInstance = new Cropper(cropImage, {
                aspectRatio: REQUIRED_WIDTH / REQUIRED_HEIGHT,
                viewMode: 1,
                responsive: true,
                restore: false,
                autoCropArea: 1,
                movable: true,
                zoomable: true,
                rotatable: false,
                scalable: false,
                guides: true,
                center: true,
                highlight: true,
                cropBoxMovable: true,
                cropBoxResizable: true,
                toggleDragModeOnDblclick: false
            });
        };
    }

    function closeCropModal() {
        var modal = document.getElementById('imgCropModal');
        if (modal) {
            modal.classList.remove('active');
        }

        if (cropperInstance) {
            cropperInstance.destroy();
            cropperInstance = null;
        }

        var cropImage = document.getElementById('imgCropImage');
        if (cropImage) {
            URL.revokeObjectURL(cropImage.src);
            cropImage.src = '';
        }
    }

    function applyCrop() {
        if (!cropperInstance || !currentFileInput) return;

        var canvas = cropperInstance.getCroppedCanvas({
            width: REQUIRED_WIDTH,
            height: REQUIRED_HEIGHT,
            imageSmoothingEnabled: true,
            imageSmoothingQuality: 'high'
        });

        if (!canvas) return;

        canvas.toBlob(function (blob) {
            if (!blob) return;

            // Create a new File from the cropped blob
            var fileName = 'cropped_' + Date.now() + '.jpg';
            var croppedFile = new File([blob], fileName, { type: 'image/jpeg' });

            // Replace the file in the input using DataTransfer
            var dt = new DataTransfer();
            dt.items.add(croppedFile);
            currentFileInput.files = dt.files;

            // Update preview based on which page we're on
            if (currentMode === 'manage' && currentWrapper) {
                updateManagePreview(currentWrapper, croppedFile);
            } else if (currentMode === 'signup') {
                updateSignupPreview(croppedFile);
            }

            // Close modal
            closeCropModal();

            // Trigger change event so any other listeners pick it up
            // (but we need to avoid recursion, so we use a flag)
            currentFileInput._croppedUpdate = true;
            var event = new Event('change', { bubbles: true });
            currentFileInput.dispatchEvent(event);

        }, 'image/jpeg', 0.92);
    }

    /**
     * Update the preview in Manage Photos page after cropping.
     */
    function updateManagePreview(wrapper, file) {
        var previewImg = wrapper.querySelector('.preview-img');
        var placeholderImg = wrapper.querySelector('.placeholder-img');
        var photoBox = wrapper.querySelector('.photo-box');
        var removeBtn = wrapper.querySelector('.remove-btn');
        var uploadText = wrapper.querySelector('.upload-text');
        var badge = wrapper.querySelector('.img-dimension-badge');

        if (previewImg) {
            var objectUrl = URL.createObjectURL(file);
            previewImg.src = objectUrl;
            previewImg.classList.remove('d-none');
        }

        if (placeholderImg) {
            placeholderImg.classList.add('d-none');
        }

        if (removeBtn) {
            removeBtn.classList.add('visible');
        }

        if (photoBox) {
            photoBox.classList.add('has-image');
        }

        if (uploadText) {
            uploadText.innerText = file.name;
        }

        // Update badge to OK
        if (badge) {
            showBadgeOk(badge, REQUIRED_WIDTH, REQUIRED_HEIGHT);
        }
    }

    /**
     * Update the preview in SignUp page after cropping.
     */
    function updateSignupPreview(file) {
        var profileImg = document.getElementById('profile-img');
        var uploadedPhoto = document.getElementById('uploaded-photo');
        var fileUploadedDiv = document.getElementById('file-uploaded');
        var profileImgDiv = document.getElementById('profile-img-div');
        var badge = document.querySelector('.signup-dimension-badge');

        var objectUrl = URL.createObjectURL(file);

        if (uploadedPhoto) {
            uploadedPhoto.src = objectUrl;
            uploadedPhoto.style.display = 'block';
        }
        if (fileUploadedDiv) {
            fileUploadedDiv.style.display = 'block';
        }
        if (profileImgDiv) {
            profileImgDiv.style.display = 'none';
        }
        if (profileImg && !uploadedPhoto) {
            profileImg.src = objectUrl;
        }

        // Update badge to OK
        if (badge) {
            showSignupBadgeOk(badge, REQUIRED_WIDTH, REQUIRED_HEIGHT);
        }
    }

    // ---------- Event Binding for Modal Buttons ----------

    function bindModalEvents() {
        // Crop button
        var cropBtn = document.getElementById('imgCropBtnApply');
        if (cropBtn) {
            cropBtn.addEventListener('click', function (e) {
                e.preventDefault();
                applyCrop();
            });
        }

        // Cancel button
        var cancelBtn = document.getElementById('imgCropBtnCancel');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', function (e) {
                e.preventDefault();
                closeCropModal();
            });
        }

        // Close X button
        var closeBtn = document.getElementById('imgCropBtnClose');
        if (closeBtn) {
            closeBtn.addEventListener('click', function (e) {
                e.preventDefault();
                closeCropModal();
            });
        }

        // Click outside to close
        var modal = document.getElementById('imgCropModal');
        if (modal) {
            modal.addEventListener('click', function (e) {
                if (e.target === modal) {
                    closeCropModal();
                }
            });
        }
    }

    // ---------- Public API ----------

    return {
        /** Init for Manage Photos (Image.cshtml) */
        initManagePhotos: function () {
            bindModalEvents();
            initManagePhotos();
        },
        /** Init for Registration (SignUp.cshtml) */
        initSignup: function () {
            bindModalEvents();
            initSignup();
        },
        /** Close modal programmatically */
        closeModal: closeCropModal,
        /** Required dimensions (for external use) */
        REQUIRED_WIDTH: REQUIRED_WIDTH,
        REQUIRED_HEIGHT: REQUIRED_HEIGHT
    };

})();
