# Join a gift exchange without creating an account

Organizers can turn on **Allow guests without an account** in the event form's
**Guest access and reveal** section. It is off by default. Guests still need an
invited email address and a name the group will recognize; there is no public
join link or name-only participant directory.
People who already have accounts keep their existing sign-in flow.

An email guest opens their private invitation, explicitly accepts or declines,
and joins the draw without registering. After names are drawn, a fresh private
email lets them see only their own recipient, date, budget, and organizer's
guidance. The optional gift box works for guests too; opening is remembered on
the assignment across visits and devices. Guest emails never name the recipient.
Recipient labels preserve the participant's chosen group name after an account
upgrade. For existing accounts whose username is an email, the label uses the
account handle rather than exposing the full email address.

Guests cannot browse event membership, group pairings, wishlists, reservations,
comments, or ordinary account APIs. Sharing gift ideas remains an optional
account upgrade: use the invited email, confirm it, and the existing participation
and opened assignment carry over.

## Private links and recovery

Treat the link like a password. Anyone who receives a forwarded link can use it.
The invitation makes this limitation explicit. Links expire after 30 days, and
private browser sessions last up to eight hours without extending themselves.
Reopening the current email restores access without registration.

The organizer can resend private access, revoke it on all devices, or remove a
guest before drawing. Resending rotates the link and invalidates earlier links
and sessions. Removing an accepted participant after drawing requires a reset.
Declined invitations can be removed and invited again before drawing.
Disabling guest access revokes credentials immediately; turning it back on does
not resurrect them, so resend private access. A reset invalidates old access,
and a new draw issues new links. Running an event again copies the setting but
creates fresh, pending invitations with separate credentials.
Account attachment and assignment relinking share the draw's event lock and
commit together. A failed draw email does not stop other deliveries; the draw
is preserved and the organizer gets an explicit delivery-recovery message.

Only token hashes are stored. A random 256-bit credential arrives in the URL
fragment rather than server logs or query strings. The page removes that
fragment from browser history and waits for an explicit action before exchanging
it for a separate, HttpOnly guest cookie. Following a link with an email scanner
does not accept an invitation or open a match. Credential validity and the
event setting are checked for every guest operation; forms use antiforgery
protection, access attempts are rate-limited, and pages are not cached or indexed.
Production access links require an HTTPS public application URL.
Initial link activation needs JavaScript to read the fragment; an established
session can accept and view its match without JavaScript.

The schema update is additive: default-off event configuration, nullable guest
credential/name/delivery fields on participants, and participant references on
assignments. Existing account-required events retain their behavior.

Verification uses only synthetic addresses and an isolated mail-capture service.
Do not expose that service or the Development-only test login in production.
`Development:UseInsecureMailCapture` permits plaintext SMTP only in Development
for that isolated service; production ignores it and retains its TLS sender.
