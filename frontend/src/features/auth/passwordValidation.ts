export function validateNewPassword(
    password: string,
    confirmation: string
): string | null {
    if (password.length < 8 || password.length > 128) {
        return "Parola 8 ile 128 karakter arasında olmalıdır.";
    }

    if (password !== confirmation) {
        return "Parola alanları birbiriyle eşleşmelidir.";
    }

    return null;
}
