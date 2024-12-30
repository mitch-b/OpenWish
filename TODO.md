# Development Plan

> <i>@workspace What should I put as my To Do's for this application given its current state? You'll see I have EF entities but nothing else is yet built. How can I organize my next steps?</i>

## Phase 1: Foundation
1. Project Structure
   - [x] Create Data project for EF Core
   - [x] Create Shared project for models
   - [x] Set up authentication infrastructure (ASP.NET Identity with Blazor Server)
   - [x] Confirm automatic migrations

2. Core Services Layer
   - [x] User management service
   - [ ] Wishlist service
   - [ ] Item management service
   - [x] Configure dependency injection

3. Blazor Server UI
   - [x] Authentication state provider
   - [x] Login/Register pages
   - [ ] Wishlist management page
   - [ ] Item management components
   - [ ] Shared layout with navigation
   - [ ] Error boundaries

4. Security & Configuration
   - [ ] User secrets for development
   - [ ] CSRF protection
   - [ ] Authentication cookie settings
   - [ ] Session state configuration

## Phase 2: Social Features
1. Friend Management
   - [ ] Friends list component
   - [ ] Friend request service
   - [ ] Wishlist sharing permissions

## Phase 3: Events
1. Event Organization
   - [ ] Event creation
   - [ ] Participant management
   - [ ] Gift assignment service

## Cleanup
1. Technical changes
   - [ ] Convert to PostgreSQL from SQL Server