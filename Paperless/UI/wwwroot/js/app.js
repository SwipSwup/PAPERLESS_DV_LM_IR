// Global Application Scripts
console.log('Paperless UI Loaded');

// Helper to format dates
function formatDate(dateString) {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString();
}
