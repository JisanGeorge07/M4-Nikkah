//validations
function validatePassword(password) {
    const minLength = 8;
    const maxLength = 20;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumbers = /[0-9]/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

    if (password.length < minLength) {
        return { valid: false, message: `Password must be at least ${minLength} characters long.` };
    }
    if (password.length > maxLength) {
        return { valid: false, message: `Password within ${maxLength} characters long.` };
    }
    if (!hasUpperCase) {
        return { valid: false, message: 'Password must contain at least one uppercase letter.' };
    }
    if (!hasLowerCase) {
        return { valid: false, message: 'Password must contain at least one lowercase letter.' };
    }
    if (!hasNumbers) {
        return { valid: false, message: 'Password must contain at least one number.' };
    }
    if (!hasSpecialChar) {
        return { valid: false, message: 'Password must contain at least one special character.' };
    }

    return { valid: true, message: '' };
}

document.addEventListener('DOMContentLoaded', (event) => {
    const passwordInput = document.getElementById('Password');
    const validationMessage = document.getElementById('validation-message');
    const nextButton = document.querySelector('input[name="next"]');

    passwordInput.addEventListener('input', function () {
        const { valid, message } = validatePassword(passwordInput.value);
        validationMessage.textContent = message;
        if (!valid) {
            passwordInput.style.border = '3px solid red';
        } else {
            passwordInput.style.border = '';
        }
        nextButton.disabled = !valid;
    });
});