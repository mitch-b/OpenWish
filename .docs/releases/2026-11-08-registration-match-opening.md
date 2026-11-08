# A thoughtful welcome and a private gift to unwrap

Registration has a quieter, more intentional layout with visible field labels,
clear invitation context, specific validation, and an optional **Show passwords**
control. The invited address and return destination survive validation and the
switch to sign in. Password requirements remain unchanged.

Organizers can select **Let each person unwrap their match** when creating or
editing a gift exchange. Each participant then taps or clicks a ribboned gift
box, or opens it with the keyboard, to see their drawn name. The name, budget,
date, and gift-idea action take over immediately. Reduced-motion preferences
disable the reveal animation.

Opening is remembered on the assignment, not in browser storage: later visits
and other devices go directly to the match. A new draw gets a fresh opening.
The option is off by default for existing and new events; running an event
again carries its organizer's choice forward. Turning it off shows matches
directly without changing the draw.

This is a presentation choice, not an extra security boundary. Participants
still have authorized access to their own assignment through the API. It does
not reveal everyone's pairings and is separate from the organizer's
post-event **Reveal assignments** action.

## Ten focused UX passes

| Pass | User-facing outcome |
|---|---|
| 1. Registration hierarchy | One compact form beside a restrained, gift-focused introduction; no equal-height sign-in card competing with it. |
| 2. Invitation context | The welcome, email guidance, and next-step copy explain how to get back to the waiting invitation. |
| 3. Field labels | Labels stay above inputs instead of floating inside them; email uses the appropriate keyboard and autofill. |
| 4. Password guidance | Requirements sit with the password field; the confirmation field has its own visible label. |
| 5. Password visibility | A keyboard-operable toggle shows both fields without changing their values or names; it stays hidden without JavaScript. |
| 6. Validation and recovery | Specific missing, short, and mismatched-password messages replace generic form text; corrected email and return navigation are preserved. |
| 7. Organizer choice | An explicitly optional setting explains first-open and later-visit behavior and saves in both server and WebAssembly flows. |
| 8. Participant reveal | One ribboned gift button becomes the expressive moment; recipient content appears only after opening it. |
| 9. Return visits and new draws | Opening persists across devices, is idempotent, and cannot mark a different or stale assignment as opened. |
| 10. Responsive and accessible states | Mobile/tablet/desktop, light/dark, visible focus, keyboard activation, long-name wrapping, and reduced motion retain the same journey. |

The direction follows OpenWish's local Manrope, violet, berry, and deep-ink
tokens rather than changing UI frameworks. Public references:
[Nielsen Norman Group's form design guidance](https://www.nngroup.com/articles/web-form-design/)
and [MDN's reduced-motion guidance](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion).

The schema update is additive: an event-level option defaults to false and a
nullable assignment-opening timestamp records the first successful opening.
