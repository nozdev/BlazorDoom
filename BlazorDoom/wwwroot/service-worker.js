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
      "./BlazorDoom/404.html",
     
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
