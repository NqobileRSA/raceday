# RaceDay - API Endpoint Plan

> This plan was written in Part 1 before any application code, and is the specification the Part 2 API is built to.
> Every endpoint maps to a functional requirement from the project brief.
> **Role key:** `None` = public (no login), `Any` = any logged-in user, `Participant` = Participant role only, `Organiser` = Organiser role only.
> **Authentication:** server-side sessions. A successful register or login stores the user ID and role in the session and returns the `.RaceDay.Session` cookie. Protected endpoints return `401 Unauthorized` without a session and `403 Forbidden` for the wrong role.
> **JSON:** all request and response bodies use snake_case field names.

See [Changes since Part 1](#changes-since-part-1) at the end of this document for the two deliberate updates made while building Part 2.

---

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | `/api/auth/register` | Register a new user account as an Organiser or Participant. Hashes the password, saves the user and starts a session. | None | `{ email, password, first_name, last_name, role, phone?, date_of_birth?, gender?, province? }` | `201 Created` - user object, session cookie set; `400 Bad Request` - validation errors (invalid role, weak password); `409 Conflict` - email already registered |
| POST | `/api/auth/login` | Authenticate with email and password. Stores the user ID and role in the server-side session. | None | `{ email, password }` | `200 OK` - user object, session cookie set; `400 Bad Request` - missing fields; `401 Unauthorized` - invalid credentials |
| POST | `/api/auth/logout` | End the current session by clearing every value stored in it. | None | None | `200 OK` - confirmation message |

---

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/users/me` | Return the profile of the currently logged-in user. | Any | None | `200 OK` - user object (excluding password_hash); `401 Unauthorized` |
| PUT | `/api/users/me` | Update the logged-in user's profile fields (name, phone, province, etc.). | Any | `{ first_name?, last_name?, phone?, date_of_birth?, gender?, province? }` | `200 OK` - updated user object; `400 Bad Request` - validation errors; `401 Unauthorized` |

---

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/events` | List all published events. Supports filtering by province, event_type and date range. | None | None (query params: `province?`, `event_type?`, `from_date?`, `to_date?`, `page?`, `limit?`) | `200 OK` - array of event objects with distances, category and enrolment counts (`X-Total-Count` header); `400 Bad Request` - invalid filter |
| GET | `/api/events/{id}` | Get full details of a single event including its categories, route and announcements. | None | None | `200 OK` - event object with nested categories, route, announcements; `404 Not Found` |
| POST | `/api/events` | Create a new event linked to the logged-in organiser. | Organiser | `{ title, description?, event_date, location, province?, event_type, status?, contact_email?, image_url? }` | `201 Created` - new event object; `400 Bad Request` - validation errors; `401 Unauthorized`; `403 Forbidden` - not an organiser |
| PUT | `/api/events/{id}` | Update an existing event. Only the organiser who owns the event may update it. | Organiser | `{ title?, description?, event_date?, location?, province?, event_type?, status?, contact_email?, image_url? }` | `200 OK` - updated event; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden` - not the owning organiser; `404 Not Found` |
| DELETE | `/api/events/{id}` | Delete an event and all its related categories, enrolments, results, route and announcements. Only the owning organiser. | Organiser | None | `200 OK` - confirmation message; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| GET | `/api/events/my` | List all events created by the logged-in organiser, including drafts. | Organiser | None (query params: `status?`, `page?`, `limit?`) | `200 OK` - array of event objects; `401 Unauthorized`; `403 Forbidden` |

---

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/events/{eventId}/categories` | List all categories for a specific event, including current enrolment counts. | None | None | `200 OK` - array of category objects with enrolment_count and spots_remaining; `404 Not Found` - event does not exist |
| POST | `/api/events/{eventId}/categories` | Add an age or distance category to an event. Only the owning organiser. | Organiser | `{ name, distance_km, entry_fee?, max_participants?, min_age?, max_age?, gender_restriction? }` | `201 Created` - new category; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| PUT | `/api/events/{eventId}/categories/{categoryId}` | Update a category. Only the owning organiser. | Organiser | `{ name?, distance_km?, entry_fee?, max_participants?, min_age?, max_age?, gender_restriction? }` | `200 OK` - updated category; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | Remove a category from an event. Cascades to related enrolments. Only the owning organiser. | Organiser | None | `200 OK` - confirmation; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |

---

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | `/api/events/{eventId}/categories/{categoryId}/enrol` | Enrol the logged-in participant in a category. Validates eligibility (age, gender, capacity, event open). | Participant | `{ emergency_contact_name?, emergency_contact_phone? }` | `201 Created` - enrolment record; `400 Bad Request` - ineligible (age/gender/capacity/closed); `401 Unauthorized`; `403 Forbidden` - not a participant; `404 Not Found`; `409 Conflict` - already enrolled in this event |
| GET | `/api/enrolments/my` | List all enrolments for the logged-in participant, with event and category details. | Participant | None (query params: `status?`, `page?`, `limit?`) | `200 OK` - array of enrolments with nested event and category info; `401 Unauthorized`; `403 Forbidden` |
| DELETE | `/api/enrolments/{enrolmentId}` | Cancel an enrolment (status set to Cancelled). Only the enrolled participant, and only before the event. | Participant | None | `200 OK` - confirmation; `400 Bad Request` - event already past or already cancelled; `401 Unauthorized`; `403 Forbidden` - not the enrolled participant; `404 Not Found` |
| GET | `/api/events/{eventId}/enrolments` | List all enrolments for a specific event. Only the owning organiser. | Organiser | None (query params: `category_id?`, `status?`, `page?`, `limit?`) | `200 OK` - array of enrolments with participant names and category; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| PUT | `/api/enrolments/{enrolmentId}/confirm` | Confirm a pending enrolment and optionally assign a bib number. Only the owning organiser. | Organiser | `{ bib_number? }` | `200 OK` - updated enrolment; `400 Bad Request` - cancelled enrolment or bib already used; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |

---

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | `/api/enrolments/{enrolmentId}/results` | Record a result (finish time and position) for an enrolment. Only the event's organiser. | Organiser | `{ finish_time?, position?, status }` | `201 Created` - result record; `400 Bad Request` - validation errors; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` - result already exists |
| PUT | `/api/results/{resultId}` | Update an existing result (correct a time, change position, update status). Only the event's organiser. | Organiser | `{ finish_time?, position?, status? }` | `200 OK` - updated result; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| GET | `/api/events/{eventId}/results` | Get all results for an event, grouped by category. Public leaderboard. | None | None (query params: `category_id?`, `sort_by?`) | `200 OK` - array of categories with ordered results; `400 Bad Request` - invalid sort_by; `404 Not Found` |
| GET | `/api/results/my` | Get all results for the logged-in participant across all events. Includes total finishers per category. | Participant | None (query params: `page?`, `limit?`) | `200 OK` - array of results with event and category details; `401 Unauthorized`; `403 Forbidden` |

---

## Routes

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/events/{eventId}/route` | Get the route information for an event. | None | None | `200 OK` - route object; `404 Not Found` - event or route does not exist |
| POST | `/api/events/{eventId}/route` | Add route information to an event. One route per event. Only the owning organiser. | Organiser | `{ start_latitude?, start_longitude?, end_latitude?, end_longitude?, description?, elevation_gain_m?, terrain_type? }` | `201 Created` - route object; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` - route already exists |
| PUT | `/api/events/{eventId}/route` | Update the route information. Only the owning organiser. | Organiser | `{ start_latitude?, start_longitude?, end_latitude?, end_longitude?, description?, elevation_gain_m?, terrain_type? }` | `200 OK` - updated route; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |

---

## Event Announcements

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/events/{eventId}/announcements` | List all announcements for an event, newest first. | None | None | `200 OK` - array of announcements; `404 Not Found` |
| POST | `/api/events/{eventId}/announcements` | Post a new announcement for an event. Only the owning organiser. | Organiser | `{ title, message, priority? }` | `201 Created` - announcement object; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |
| DELETE | `/api/events/{eventId}/announcements/{announcementId}` | Delete an announcement. Only the owning organiser. | Organiser | None | `200 OK` - confirmation; `401 Unauthorized`; `403 Forbidden`; `404 Not Found` |

---

## Weather (External API Integration)

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | `/api/events/{eventId}/weather` | Fetch live weather for the event's route start location from the Open-Meteo API. Requires a route with coordinates. | Any | None | `200 OK` - temperature, conditions, wind, humidity; `401 Unauthorized`; `404 Not Found` - event or route coordinates missing; `503 Service Unavailable` - weather API unreachable |

---

## Summary

| Resource | Endpoints | Methods |
|----------|-----------|---------|
| Authentication | 3 | POST |
| User Profile | 2 | GET, PUT |
| Events | 6 | GET, POST, PUT, DELETE |
| Categories | 4 | GET, POST, PUT, DELETE |
| Enrolments | 5 | GET, POST, PUT, DELETE |
| Results | 4 | GET, POST, PUT |
| Routes | 3 | GET, POST, PUT |
| Announcements | 3 | GET, POST, DELETE |
| Weather | 1 | GET |
| **Total** | **31** | |

---

## Changes since Part 1

| Change | Reason |
|--------|--------|
| Authentication uses **server-side sessions** instead of JWT. Register and login now return the user object and set a session cookie rather than returning a token. | The Part 2 brief requires session management that stores the user's ID and role on the server for all subsequent requests. |
| Added **`POST /api/auth/logout`** (endpoint 31). | With sessions the server must be able to end a session; Part 3 also requires users to end their session. |
| `GET /api/events/{eventId}/weather` lists `401 Unauthorized` explicitly. | The endpoint was always role `Any`; the failure code was missing from the Part 1 row. |

No routes, HTTP methods, roles or request bodies from the Part 1 plan were removed or renamed.
