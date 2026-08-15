/**
 * Encrypt query parameters via server and call a GET endpoint with ?q=...
 */
window.KrsQueryString = {
    async pack(values) {
        const response = await fetch('/QueryString/Pack', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(values ?? {})
        });
        const data = await response.json();
        if (!data.success) throw new Error(data.message || 'Failed to encrypt query string.');
        return data.q;
    },

    async fetchGet(baseUrl, values) {
        const q = await this.pack(values);
        const separator = baseUrl.includes('?') ? '&' : '?';
        return fetch(`${baseUrl}${separator}q=${encodeURIComponent(q)}`);
    }
};
