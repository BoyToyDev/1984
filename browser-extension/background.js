const endpoint = 'http://127.0.0.1:39877/browser-activity';
let current = null;

function browserName() {
  const userAgent = navigator.userAgent.toLowerCase();
  if (userAgent.includes('edg/')) return 'Edge';
  if (userAgent.includes('chrome/')) return 'Chrome';
  return 'Chromium';
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
  const now = Date.now();
  await flush(now);

  const tab = await getActiveTab();
  if (!tab || !tab.url || tab.url.startsWith('chrome://') || tab.url.startsWith('edge://')) {
    current = null;
    return;
  }

  current = {
    url: tab.url,
    title: tab.title || '',
    startedAtUnixMs: now
  };
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
setInterval(() => updateCurrent(), 60000);
