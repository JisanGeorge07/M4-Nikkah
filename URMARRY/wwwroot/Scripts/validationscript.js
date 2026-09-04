function validateEmail(email) {
    // Regular expression for validating email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    // Test the email against the regex
    if (emailRegex.test(email)) {
        return true; // Email is valid
    } else {
        return false; // Email is invalid
    }
}