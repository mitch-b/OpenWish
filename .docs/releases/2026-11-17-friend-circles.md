# Friend circles - November 17, 2026

Roadmap C1, "Circles and households": group friends once, then reuse the group.

- **Create circles.** Name a circle like "Family" or "Book club" on the Friends
  page and add the friends who belong in it. Only you see your circles.
- **Filter and badge friends.** The friend list shows each friend's circles and
  can be narrowed to one circle.
- **Share a list with a circle.** On a friends-only list, share with a whole
  circle. Members get view access, friends who join later get it automatically,
  and leaving the circle, stopping sharing, deleting the circle, or ending the
  friendship withdraws it. Access you granted to a person by hand is never
  changed.
- **Invite a whole circle to an event.** "Invite a whole circle" selects every
  friend in it who is not already invited.
- **See a circle's lists.** "View their lists" opens Friends' wishlists
  filtered to that circle (`/wishlists?tab=friends&circle=<id>`).

Privacy: members must be your friends, private lists cannot be shared with a
circle, and circle membership is never visible to the people in it. An additive
database migration adds the circle tables and a nullable `CircleId` on
wishlist permissions.
