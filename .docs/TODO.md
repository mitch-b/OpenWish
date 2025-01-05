# Development Plan

> <i>@workspace What should I put as my To Do's for this application given its current state? You'll see I have EF entities but nothing else is yet built. How can I organize my next steps?</i>

## Phase 1: Foundation
1. Project Structure ✓
   - [x] Create Data project for EF Core
   - [x] Create Shared project for models 
   - [x] Set up authentication infrastructure (ASP.NET Identity with Blazor Server)
   - [x] Confirm automatic migrations

2. Core Services Layer ✓
   - [x] User management service
   - [x] Wishlist service
   - [x] Item management service
   - [x] Configure dependency injection

3. Blazor Server UI (In Progress)
   - [x] Authentication state provider
   - [x] Login/Register pages
   - [ ] Wishlist management pages
      - [ ] List view with filters and search
      - [ ] Create/Edit forms
      - [x] Delete confirmation dialog
   - [ ] Item management components
      - [ ] Item card component
      - [ ] Add/Edit item form
      - [ ] Bulk actions (delete, move)
   - [ ] Shared layout and navigation
      - [ ] Responsive navbar
      - [ ] User profile dropdown
      - [ ] Mobile-friendly menu
   - [ ] Error handling
      - [ ] Global error boundary
      - [ ] Form validation messages
      - [ ] Loading states
      - [ ] Empty state components

4. Security & Configuration
   - [x] User secrets for development
   - [ ] CSRF protection
      - [ ] Add antiforgery tokens to forms
      - [ ] Validate tokens on POST/PUT/DELETE
   - [x] Authentication cookie settings
   - [x] Session state configuration
   - [ ] API endpoint security
      - [ ] Authorization policies
      - [ ] Rate limiting
      - [ ] Input validation

5. Testing & Documentation
   - [ ] Unit tests
      - [ ] Service layer tests
      - [ ] Component tests
   - [ ] Integration tests
      - [ ] API endpoint tests
      - [ ] Database tests
   - [ ] Documentation
      - [ ] API documentation
      - [ ] Component usage guide
      - [ ] Deployment guide

## Phase 2: Social Features
1. Friend Management
   - [ ] Friends list component
   - [ ] Friend request service
   - [ ] Wishlist sharing permissions
   - [ ] Privacy settings

2. Interaction Features
   - [ ] Comments system
   - [ ] Item reservations
   - [ ] Activity feed
   - [ ] Notifications

## Phase 3: Events & Special Features
1. Event Organization
   - [ ] Event creation & management
   - [ ] Participant handling
   - [ ] Gift assignments
   - [ ] Event reminders

2. Advanced Features
   - [ ] Smart recommendations
   - [ ] Price tracking
   - [ ] External wishlist import
   - [ ] Shopping integrations

## Cleanup & Optimization
1. Performance
   - [ ] Component lazy loading
   - [ ] Image optimization
   - [ ] Caching strategy
   - [ ] Database indexing

2. Technical Improvements
   - [x] Convert to PostgreSQL from SQL Server
   - [ ] CI/CD pipeline
   - [ ] Monitoring setup
   - [ ] Backup strategy