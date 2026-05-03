window.ptImportValidation = {
    clickFileInput: function () {
        document.querySelector('.lt-shell-file-input-native')?.click();
    },
    downloadCsv: function (fileName, content) {
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.href = url;
        link.download = fileName || 'import-validation.csv';
        link.click();
        URL.revokeObjectURL(url);
    }
};
