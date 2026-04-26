/* global echarts */
window.ptEcharts = (function () {
  const instances = new Map();

  const theme = {
    color: [
      "#2D3436", // slate
      "#91A363", // Scandi green
      "#FFB300", // amber
      "#5C6B76", // steel
      "#BDBDBD"  // gray
    ],
    textStyle: {
      fontFamily: "Inter, Segoe UI, sans-serif"
    }
  };

  function ensure(id) {
    const el = document.getElementById(id);
    if (!el) return null;

    let chart = instances.get(id);
    if (!chart) {
      chart = echarts.init(el, theme);
      instances.set(id, chart);
      window.addEventListener("resize", () => chart.resize());
    }
    return chart;
  }

  function setOption(id, option) {
    const chart = ensure(id);
    if (!chart) return;
    // Allow passing JS function bodies as strings from .NET.
    // If option.tooltip.formatterJs is present, convert to a real function.
    if (option && option.tooltip && option.tooltip.formatterJs) {
      try {
        option.tooltip.formatter = new Function("params", option.tooltip.formatterJs);
      } catch (e) {
        // ignore and fall back to default tooltip
      }
      delete option.tooltip.formatterJs;
    }
    chart.setOption(option, true);
  }

  function onClick(id, dotnetRef) {
    const chart = ensure(id);
    if (!chart) return;
    chart.off("click");
    chart.on("click", function (params) {
      if (!params) return;
      // category is the x-axis label for bar series
      const name = params.name || "";
      dotnetRef.invokeMethodAsync("HandleClick", name);
    });
  }

  function dispose(id) {
    const chart = instances.get(id);
    if (!chart) return;
    chart.dispose();
    instances.delete(id);
  }

  return { setOption, onClick, dispose };
})();

window.ptDownload = {
  saveBase64File: function (filename, base64, mimeType) {
    const byteChars = atob(base64);
    const byteNumbers = new Array(byteChars.length);
    for (let i = 0; i < byteChars.length; i++) {
      byteNumbers[i] = byteChars.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType || "application/octet-stream" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename || "export.xlsx";
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  }
};

