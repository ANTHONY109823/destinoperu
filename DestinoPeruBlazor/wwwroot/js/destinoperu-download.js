window.destinoPeruDownload = {
    csv: function (filename, content) {
        const blob = new Blob(["\ufeff" + content], { type: "text/csv;charset=utf-8;" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = filename || "descarga.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },
    printPage: function () {
        window.print();
    }
};
