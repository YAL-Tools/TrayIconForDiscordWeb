// ==UserScript==
// @name         Discord Tray Icon
// @namespace    https://yal.cc
// @version      1.0
// @description  Gives the web version of Discord client a tray icon akin to native
// @author       YellowAfterlife
// @match        https://discord.com/channels/*
// @match        https://discord.com/login/*
// @icon         https://www.google.com/s2/favicons?sz=64&domain=discord.com
// @grant        none
// @run-at       document-start
// ==/UserScript==

(function() {
	'use strict';
	// These selectors are a little janky but that's what you get when class names have auto-generated suffixes.
	const qryPanels = `div[class^="sidebar-"] section[class^="panels"]`;
	const qryIsInVC = `${qryPanels} div[class^="rtcConnectionStatus"]`;
	const qryIsSpeaking = `${qryPanels} div[class^="avatarWrapper"] div[class^="avatarSpeaking"]`;
	const qryIsMuted = `${qryPanels} div[class^="avatarWrapper"] + div > button[role="switch"]:nth-child(1)[aria-checked="true"]`;
	const qryIsDeafened = `${qryPanels} div[class^="avatarWrapper"] + div > button[role="switch"]:nth-child(2)[aria-checked="true"]`;

	// Discord hijacks XMLHttpRequest to also log to Sentry so we gotta store copies:
	const _XMLHttpRequest = XMLHttpRequest;
	const _XMLHttpRequest_open = _XMLHttpRequest.prototype.open;
	const _XMLHttpRequest_send = _XMLHttpRequest.prototype.send;

	function send(status) {
		//console.info("sending status:", status);
		const xhr = new _XMLHttpRequest();
		xhr.open = _XMLHttpRequest_open;
		xhr.send = _XMLHttpRequest_send;
		_XMLHttpRequest_open.call(xhr, "GET", "http://127.0.0.1:15341/set-status/" + status);
		_XMLHttpRequest_send.call(xhr);
	}
	
	const _querySelector = document.querySelector;
	const check = (qry) => _querySelector.call(document, qry);

	let oldStatus = null;
	const rxHasUnreads = /^\(\d+/g;
	setInterval(() => {
		let status;
		if (check(qryIsInVC)) {
			if (check(qryIsDeafened)) {
				status = "deafened";
			} else if (check(qryIsMuted)) {
				status = "muted";
			} else if (check(qryIsSpeaking)) {
				status = "speaking";
			} else status = "connected";
		} else status = "default";
		
		// mark unread if page title starts with "("
		if (rxHasUnreads.test(document.title)) status += "+";
		
		if (status != oldStatus) {
			oldStatus = status;
			send(status);
		}
	}, 100);
})();