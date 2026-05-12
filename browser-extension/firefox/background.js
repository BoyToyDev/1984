const endpoint = 'http://127.0.0.1:39877/browser-activity';
const heartbeatEndpoint = 'http://127.0.0.1:39877/browser-heartbeat';
let current = null;

function browserName() {
  const userAgent = navigator.userAgent.toLowerCase();
  if (userAgent.includes('firefox/')) return 'Firefox';
  if (userAgent.includes('edg/')) return 'Edge';
  if (userAgent.includes('chrome/')) return 'Chrome';
  if (userAgent.includes('brave/')) return 'Brave';
  if (userAgent.includes('opera/')) return 'Opera';
  return 'Chromium';
}

function isInternalUrl(url) {
  return url.startsWith('chrome://')
      || url.startsWith('edge://')
      || url.startsWith('about:')
      || url.startsWith('moz-extension://');
}

async function getActiveTab() {
  const tabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  return tabs && tabs.length > 0 ? tabs[0] : null;
}

async function flush(endTime) {
  if (!current || !current.url) return;
  if (endTime <= current.startedAtUnixMs) return;

  const payload = {
    browser: browserName(),
    url: current.url,
    title: current.title || '',
    startedAtUnixMs: current.startedAtUnixMs,
    endedAtUnixMs: endTime
  };

  try {
    await fetch(endpoint, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify(payload)
    });
  } catch (_) {
  }
}

async function updateCurrent() {
  const tab = await getActiveTab();
  if (!tab || !tab.url || tab.url === 'about:blank' || isInternalUrl(tab.url)) {
    return;
  }

  const now = Date.now();

  if (!current || current.url !== tab.url) {
    await flush(now);
    current = {
      url: tab.url,
      title: tab.title || '',
      startedAtUnixMs: now
    };
  } else if (tab.title && tab.title !== current.title) {
    current.title = tab.title;
  }
}

async function sendHeartbeat() {
  try {
    await fetch(heartbeatEndpoint, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({
        type: 'heartbeat',
        browser: browserName(),
        extensionVersion: chrome.runtime.getManifest().version,
        timestamp: Date.now()
      })
    });
  } catch (_) {
  }
}

chrome.tabs.onActivated.addListener(() => updateCurrent());
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (tab.active && (changeInfo.status === 'complete' || changeInfo.title || changeInfo.url)) {
    updateCurrent();
  }
});
chrome.windows.onFocusChanged.addListener(() => updateCurrent());
chrome.runtime.onStartup.addListener(() => updateCurrent());
chrome.runtime.onInstalled.addListener(() => updateCurrent());
chrome.runtime.onStartup.addListener(() => sendHeartbeat());
chrome.runtime.onInstalled.addListener(() => sendHeartbeat());
setInterval(() => updateCurrent(), 60000);
setInterval(() => sendHeartbeat(), 60000);
