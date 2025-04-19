var CACHE = "cache-and-update";
self.addEventListener("install", function (evt) {
  console.log("The service worker is being installed.");
  evt.waitUntil(precache());
});

self.addEventListener("fetch", function (evt) {
  console.log("The service worker is serving the asset.");
  evt.respondWith(fromCache(evt.request));
  evt.waitUntil(update(evt.request));
});

function precache() {
  return caches.open(CACHE).then((cache) => {
    return cache.addAll([
      "./404.html",
      "./app.css",
      "./bootstrap.min.css",
      "./browserconfig.xml",
      "./favicon.ico",
      "./index.html",
      "./main.js",
      "./manifest.json",
      "./renderer.js",
      "./service-worker.js",
      "./_framework/blazor.boot.json",
      "./_framework/BlazorDoom.l4zfoze0p1.wasm",
      "./_framework/dotnet.js",
      "./_framework/dotnet.native.2mq7o9g23g.wasm",
      "./_framework/dotnet.native.b68top6k1y.js",
      "./_framework/dotnet.runtime.qrl1fuqt3c.js",
      "./_framework/icudt_CJK.tjcz0u77k5.dat",
      "./_framework/icudt_EFIGS.tptq2av103.dat",
      "./_framework/icudt_no_CJK.lfu7j35m59.dat",
      "./_framework/System.Collections.hltcqaus6n.wasm",
      "./_framework/System.Console.aagxx1eodi.wasm",
      "./_framework/System.Diagnostics.DiagnosticSource.3a6ssr5sln.wasm",
      "./_framework/System.Linq.x6xtpxc67p.wasm",
      "./_framework/System.Net.Http.a6cj5k1np2.wasm",
      "./_framework/System.Net.Primitives.qp4b0cp8qy.wasm",
      "./_framework/System.Private.CoreLib.fan99cjh78.wasm",
      "./_framework/System.Private.Uri.qmxt8c7v5t.wasm",
      "./_framework/System.Runtime.InteropServices.JavaScript.3xcra01ymf.wasm",
      "./css/open-iconic/FONT-LICENSE",
      "./css/open-iconic/ICON-LICENSE",
      "./css/open-iconic/README.md",
      "./css/open-iconic/font/css/open-iconic-bootstrap.min.css",
      "./css/open-iconic/font/fonts/open-iconic.eot",
      "./css/open-iconic/font/fonts/open-iconic.otf",
      "./css/open-iconic/font/fonts/open-iconic.svg",
      "./css/open-iconic/font/fonts/open-iconic.ttf",
      "./css/open-iconic/font/fonts/open-iconic.woff",
      "./favicon/android-icon-144x144.png",
      "./favicon/android-icon-192x192.png",
      "./favicon/android-icon-36x36.png",
      "./favicon/android-icon-48x48.png",
      "./favicon/android-icon-72x72.png",
      "./favicon/android-icon-96x96.png",
      "./favicon/apple-icon-114x114.png",
      "./favicon/apple-icon-120x120.png",
      "./favicon/apple-icon-144x144.png",
      "./favicon/apple-icon-152x152.png",
      "./favicon/apple-icon-180x180.png",
      "./favicon/apple-icon-57x57.png",
      "./favicon/apple-icon-60x60.png",
      "./favicon/apple-icon-72x72.png",
      "./favicon/apple-icon-76x76.png",
      "./favicon/apple-icon-precomposed.png",
      "./favicon/apple-icon.png",
      "./favicon/favicon-16x16.png",
      "./favicon/favicon-32x32.png",
      "./favicon/favicon-96x96.png",
      "./favicon/ms-icon-144x144.png",
      "./favicon/ms-icon-150x150.png",
      "./favicon/ms-icon-310x310.png",
      "./favicon/ms-icon-70x70.png",
      "./js/gamecontroller.js",
      "./sf2/TimGM6mb.sf2",
      "./wad/acheron.wad",
      "./wad/doom1.wad",
    ]);
  });
}

function fromCache(request) {
  return caches.open(CACHE).then((cache) => {
    return cache.match(request).then((matching) => {
      return matching || Promise.reject("no-match");
    });
  });
}

function update(request) {
  return caches.open(CACHE).then((cache) => {
    return fetch(request).then((response) => {
      return cache.put(request, response);
    });
  });
}
