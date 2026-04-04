# ADR-004: Firebase Auth for Authentication

## Status
Accepted

## Context
We need authentication that handles user registration, login, and token management without building our own auth system.

## Decision
Use Firebase Authentication:
- Mobile app authenticates directly with Firebase → receives JWT
- Backend validates JWTs using Firebase Admin SDK
- Backend stores `firebase_uid` in its user table for linking
- Backend never handles credentials directly

## Consequences
- No need to build auth infrastructure (password hashing, email verification, etc.)
- Social logins (Google, Apple) are easy to add
- Backend remains stateless for auth validation
- Dependency on Firebase for auth availability
- Firebase project must be created and configured separately
