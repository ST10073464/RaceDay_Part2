# RaceDay API Endpoint Plan

**Module:** Programming 2B (PROG6212/w)  
**Part:** 1 – System Planning and Database  
**Student:** Erwin Mashobane  
**Student Number:** ST10073464

## Purpose

This endpoint plan defines the REST API that will be implemented in Part 2. It covers authentication, user profiles, events, categories, event enrolments and results. The plan is intended to remain the specification for the implementation so that unexplained deviations are avoided.

## Roles

- **Organiser:** Creates, edits and deletes events; manages categories; views enrolments; captures results.
- **Participant:** Registers and logs in; browses events; views categories; enrols in events; views personal enrolments and results.

## Endpoint Specification

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as an Organiser or Participant. | None | { firstName, lastName, email, password, phoneNumber, dateOfBirth, role } | 201 Created - user registered; 400 Bad Request - invalid data; 409 Conflict - email already exists |
| POST | /api/auth/login | Authenticates a user and creates an authenticated session containing the user ID and role. | None | { email, password } | 200 OK - login successful; 401 Unauthorized - invalid credentials |
| POST | /api/auth/logout | Ends the current authenticated session. | Any | None | 204 No Content - session ended; 401 Unauthorized - no authenticated session |
| GET | /api/profile | Returns the authenticated user's own profile. | Any | None | 200 OK - profile; 401 Unauthorized - not logged in |
| PUT | /api/profile | Updates the authenticated user's own profile information. | Any | { firstName, lastName, phoneNumber, dateOfBirth, profilePictureUrl } | 200 OK - updated profile; 400 Bad Request - invalid data; 401 Unauthorized - not logged in |
| GET | /api/events | Returns upcoming events and supports browsing by both roles. | None | None; optional query filters | 200 OK - event list |
| GET | /api/events/{id} | Returns complete information for a specific event. | None | None | 200 OK - event details; 404 Not Found - event does not exist |
| POST | /api/events | Creates a new event owned by the authenticated Organiser. | Organiser | { eventName, description, eventDate, location, distanceKm, eventType, bannerImageUrl } | 201 Created - event; 400 Bad Request - invalid data; 403 Forbidden - wrong role |
| PUT | /api/events/{id} | Updates an event created by the authenticated Organiser. | Organiser | { eventName, description, eventDate, location, distanceKm, eventType, bannerImageUrl } | 200 OK - updated event; 403 Forbidden - wrong role/owner; 404 Not Found - event missing |
| DELETE | /api/events/{id} | Deletes an event created by the authenticated Organiser. | Organiser | None | 204 No Content - deleted; 403 Forbidden - wrong role/owner; 404 Not Found - event missing |
| GET | /api/events/{eventId}/categories | Lists all categories available for an event. | None | None | 200 OK - category list; 404 Not Found - event missing |
| POST | /api/events/{eventId}/categories | Creates an age or distance category for an event. | Organiser | { categoryName, minAge, maxAge, distanceKm } | 201 Created - category; 400 Bad Request - invalid data; 403 Forbidden - wrong role/owner; 404 Not Found - event missing |
| PUT | /api/categories/{id} | Updates an existing event category. | Organiser | { categoryName, minAge, maxAge, distanceKm } | 200 OK - updated category; 403 Forbidden - wrong role/owner; 404 Not Found - category missing |
| DELETE | /api/categories/{id} | Deletes an event category when it is no longer required. | Organiser | None | 204 No Content - deleted; 403 Forbidden - wrong role/owner; 404 Not Found - category missing |
| POST | /api/events/{eventId}/enrolments | Enrols the authenticated Participant in an event using a selected category. | Participant | { categoryId } | 201 Created - enrolment; 400 Bad Request - invalid category; 401 Unauthorized - not logged in; 403 Forbidden - wrong role; 409 Conflict - already enrolled |
| GET | /api/enrolments/my | Returns the authenticated Participant's own event enrolments. | Participant | None | 200 OK - enrolment list; 403 Forbidden - wrong role |
| GET | /api/events/{eventId}/enrolments | Returns all enrolments for an event managed by the authenticated Organiser. | Organiser | None | 200 OK - enrolment list; 403 Forbidden - wrong role/owner; 404 Not Found - event missing |
| PUT | /api/enrolments/{id}/status | Allows the event Organiser to update an enrolment status. | Organiser | { enrolmentStatus } | 200 OK - updated enrolment; 400 Bad Request - invalid status; 403 Forbidden - wrong role/owner; 404 Not Found - enrolment missing |
| POST | /api/events/{eventId}/results | Captures a Participant's result after an event. | Organiser | { enrolmentId, finishTime, finishingPosition } | 201 Created - result; 400 Bad Request - invalid result; 403 Forbidden - wrong role/owner; 404 Not Found - enrolment missing; 409 Conflict - result already exists |
| GET | /api/results/my | Returns the authenticated Participant's personal race history. | Participant | None | 200 OK - result history; 403 Forbidden - wrong role |
| GET | /api/events/{eventId}/results | Returns results for participants in an event managed by the Organiser. | Organiser | None | 200 OK - event results; 403 Forbidden - wrong role/owner; 404 Not Found - event missing |
| PUT | /api/results/{id} | Corrects an existing result entered by the event Organiser. | Organiser | { finishTime, finishingPosition } | 200 OK - updated result; 400 Bad Request - invalid result; 403 Forbidden - wrong role/owner; 404 Not Found - result missing |

## Design Notes

1. Public browsing is limited to registration, login and event/category viewing.
2. Authentication is session-based in Part 2. Protected requests require an authenticated session.
3. Organiser ownership must be checked in addition to the role. An Organiser should not be able to modify another Organiser's event.
4. A Participant may only access their own enrolments and results.
5. Event type is restricted to **Run**, **Walk**, or **Cycle**.
6. Category creation is associated with a specific event.
7. Results are associated with an enrolment so that the result can be traced to a participant, event and selected category.
8. The endpoint design aligns with the Part 1 ERD and SQL schema.
