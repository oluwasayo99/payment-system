# Student Portal MVP — Engineering Plan (Final Draft)

## 1. BACKEND (Spring Boot) — FULL DETAIL

### 1.1 Tech Stack
| Layer | Technology | Version / Notes |
|-------|-----------|-----------------|
| Language | Java | 17 LTS |
| Framework | Spring Boot | 3.2.x |
| Security | Spring Security 6 | JWT stateless sessions |
| Persistence | Spring Data JPA / Hibernate | 6.x |
| Database | **Microsoft SQL Server** | **2022** (or Azure SQL Edge for Docker dev) |
| Migrations | Flyway | Community edition |
| Build Tool | Maven | Wrapper included |
| Utilities | Lombok | `2.4.x`; MapStruct recommended for DTO mapping |
| Validation | Jakarta Bean Validation | `@Valid` on request DTOs |
| API Docs | SpringDoc OpenAPI | `v2.3.x` |
| Testing | JUnit 5, Mockito, AssertJ, Testcontainers | `mcr.microsoft.com/mssql/server:2022-latest` |

### 1.2 Package Structure
```
src/main/java/com/studentportal/
├── StudentPortalApplication.java
├── config
│   ├── SecurityConfig.java
│   ├── JwtConfig.java
│   ├── OpenApiConfig.java
│   └── AuditConfig.java
├── controller
│   ├── AuthController.java
│   ├── UserController.java
│   └── StudentController.java
├── service
│   ├── AuthService.java
│   ├── UserService.java
│   ├── StudentService.java
│   └── impl
│       ├── AuthServiceImpl.java
│       ├── UserServiceImpl.java
│       └── StudentServiceImpl.java
├── repository
│   ├── UserRepository.java
│   └── StudentRepository.java
├── entity
│   ├── User.java
│   ├── Student.java
│   └── enums
│       └── Role.java
├── dto
│   ├── request
│   │   ├── LoginRequest.java
│   │   ├── CreateUserRequest.java
│   │   ├── UpdateUserRequest.java
│   │   ├── CreateStudentRequest.java
│   │   ├── UpdateStudentRequest.java
│   │   └── StudentSelfUpdateRequest.java
│   └── response
│       ├── AuthResponse.java
│       ├── UserResponse.java
│       ├── StudentResponse.java
│       └── ApiErrorResponse.java
├── mapper
│   ├── UserMapper.java
│   └── StudentMapper.java
├── exception
│   ├── ResourceNotFoundException.java
│   ├── DuplicateResourceException.java
│   ├── ForbiddenOperationException.java
│   └── GlobalExceptionHandler.java
├── security
│   ├── JwtTokenProvider.java
│   ├── JwtAuthenticationFilter.java
│   └── CustomUserDetailsService.java
├── util
│   ├── StudentNumberGenerator.java
│   └── DateUtil.java

src/test/java/com/studentportal/
├── ContextLoadsIT.java
├── controller
│   ├── AuthControllerIT.java
│   ├── UserControllerIT.java
│   └── StudentControllerIT.java
├── service
│   ├── AuthServiceImplTest.java
│   ├── UserServiceImplTest.java
│   └── StudentServiceImplTest.java
├── repository
│   ├── UserRepositoryIT.java
│   └── StudentRepositoryIT.java
└── security
    └── JwtTokenProviderTest.java
```

### 1.3 Database Schema (MSSQL)
```sql
-- ============================================
-- Users Table
-- ============================================
CREATE TABLE users (
    id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    email           NVARCHAR(255) NOT NULL UNIQUE,
    password_hash   NVARCHAR(255) NOT NULL,
    first_name      NVARCHAR(100) NOT NULL,
    last_name       NVARCHAR(100) NOT NULL,
    role            NVARCHAR(20)  NOT NULL,
    is_active       BIT           DEFAULT 1,
    created_at      DATETIME2     DEFAULT GETUTCDATE(),
    updated_at      DATETIME2     DEFAULT GETUTCDATE(),
    CONSTRAINT chk_role CHECK (role IN ('ADMIN', 'STAFF', 'STUDENT'))
);

-- ============================================
-- Students Table
-- ============================================
CREATE TABLE students (
    id                BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id           BIGINT NOT NULL UNIQUE,
    student_number    NVARCHAR(50) NOT NULL UNIQUE,
    date_of_birth     DATE,
    phone             NVARCHAR(20),
    address           NVARCHAR(500),
    department_code   NVARCHAR(10) NOT NULL,
    degree_type       NVARCHAR(5)  NOT NULL, -- 'U' or 'P'
    enrollment_year   INT          NOT NULL,
    enrollment_date   DATE DEFAULT GETUTCDATE(),
    created_at        DATETIME2 DEFAULT GETUTCDATE(),
    updated_at        DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT fk_student_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- ============================================
-- Indexes
-- ============================================
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_active ON users(is_active);
CREATE INDEX idx_students_number ON students(student_number);
CREATE INDEX idx_students_user_id ON students(user_id);
CREATE INDEX idx_students_dept_year ON students(department_code, enrollment_year);
```

### 1.4 Flyway Migration File List
| File | Purpose |
|------|---------|
| `db/migration/V1__Create_users_table.sql` | Core identity table |
| `db/migration/V2__Create_students_table.sql` | Student profile table + FK + indexes |
| `db/migration/V3__Seed_default_admin.sql` | Insert system admin |

**V3 Example:**
```sql
INSERT INTO users (email, password_hash, first_name, last_name, role, is_active)
VALUES (
    '${ADMIN_EMAIL}',
    '${ADMIN_BCRYPT_HASH}',
    'System',
    'Admin',
    'ADMIN',
    1
);
```

### 1.5 Complete REST API Endpoint Table

Base Path: `/api/v1`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| `POST` | `/auth/login` | PUBLIC | Authenticate; return `accessToken` |
| `POST` | `/auth/refresh` | PUBLIC | Exchange refresh token |
| `POST` | `/users` | ADMIN | Create new user (STAFF or STUDENT shell) |
| `GET` | `/users` | ADMIN | Paginated list of all users (active + inactive) |
| `GET` | `/users/{id}` | ADMIN | Fetch single user |
| `PUT` | `/users/{id}` | ADMIN | Update user fields (name, **email**, role) |
| `DELETE`| `/users/{id}` | ADMIN | **Soft delete**: set `is_active = 0` |
| `PUT` | `/users/{id}/reactivate` | ADMIN | **Reactivate** soft-deleted user (`is_active = 1`) |
| `POST` | `/students` | ADMIN, STAFF | Enroll student: atomically creates `User` (STUDENT role) + `Student` profile |
| `GET` | `/students` | ADMIN, STAFF | Paginated list of all students |
| `GET` | `/students/{id}` | ADMIN, STAFF, STUDENT* | Get profile. *STUDENT only if `student.user_id == auth.id` |
| `PUT` | `/students/{id}` | ADMIN, STAFF | Full update of student data |
| `PATCH`| `/students/{id}` | ADMIN, STAFF | Partial update of any student field |
| `PATCH`| `/students/me` | STUDENT | Self-service update: **phone, address, date_of_birth only**. Email blocked. |
| `DELETE`| `/students/{id}` | ADMIN | **Soft delete**: sets `users.is_active = 0` |
| `GET` | `/students/me` | STUDENT | Convenience endpoint for current student's profile |

### 1.6 Role-Based Access Matrix
| Action | ADMIN | STAFF | STUDENT |
|--------|-------|-------|---------|
| Login / Refresh | ✅ | ✅ | ✅ |
| Create User | ✅ | ❌ | ❌ |
| List/View All Users | ✅ | ❌ | ❌ |
| Update Any User (inc. email) | ✅ | ❌ | ❌ |
| Soft Delete User | ✅ | ❌ | ❌ |
| **Reactivate User** | ✅ | ❌ | ❌ |
| List All Students | ✅ | ✅ | ❌ |
| Enroll New Student | ✅ | ✅ | ❌ |
| Read Any Student | ✅ | ✅ | ❌ |
| **Read Own Student** | ✅ | ✅ | ✅ |
| Update Any Student (Full) | ✅ | ✅ | ❌ |
| **Update Own Profile** | ❌ | ❌ | ✅ (phone, address, DOB only) |
| Soft Delete Student | ✅ | ❌ | ❌ |

### 1.7 Soft Delete & Reactivation Strategy
| Entity | Field | Behavior |
|--------|-------|----------|
| `User` | `is_active` BIT | `0` = soft-deleted. Login blocked. Default queries filter to `is_active = 1`. |
| `Student` | Cascades via `User` | No separate `is_active` flag. Soft-deleting the user deactivates the student profile. |

- **Reactivate Endpoint:** `PUT /users/{id}/reactivate` sets `is_active = 1`, restoring login capability and student profile visibility.

### 1.8 Student Number Generation Logic
**Format:** `{DegreeType}{Year}{DepartmentCode}{ChronologicalID}`  
**Example:** `U16CO1068`

| Segment | Source | Example |
|---------|--------|---------|
| `U` / `P` | `degree_type` from request (`U`ndergraduate / `P`ostgraduate) | `U` |
| `16` | Last two digits of `enrollment_year` | `16` |
| `CO` | `department_code` (validated against allowed set) | `CO` (Computer Engineering) |
| `1068` | Chronological order: `COUNT(*) + 1` of existing students in that `department_code` + `enrollment_year` | `1068` |

> **Implementation:** `StudentNumberGenerator` utility class queries `StudentRepository.countByDepartmentCodeAndEnrollmentYear(...)` to determine the next sequence number, zero-padded to 4 digits (`%04d`). The generation runs inside the `@Transactional` enrollment method.

### 1.9 Unit & Integration Test Strategy

#### Coverage Targets
| Type | Metric | Target |
|------|--------|--------|
| Unit | Line Coverage | ≥ 80% |
| Unit | Branch Coverage | ≥ 70% |
| Integration | Secured API Paths | 100% of endpoints |

#### Unit Tests (Mockito / JUnit 5)
| Class Under Test | Key Assertions |
|------------------|----------------|
| `AuthServiceImpl` | Valid creds → token; invalid → exception; **inactive user login blocked** |
| `UserServiceImpl` | Duplicate email → exception; **email update allowed**; soft delete sets `isActive = false`; **reactivate sets `isActive = true`** |
| `StudentServiceImpl` | Enrollment is `@Transactional`; `studentNumber` format matches regex (`^[UP]\d{2}[A-Z]{2}\d{4}$`); `updateStudentMe` rejects email changes |
| `StudentNumberGenerator` | Correct sequence increment; correct zero-padding (`0001`, `0010`, `1068`) |
| `JwtTokenProvider` | Token generation, role claims, expiry validation |

#### Integration Tests (Testcontainers + MSSQL)
| Test Class | Approach |
|------------|----------|
| `AuthControllerIT` | Verify `200` login, `401` bad password, `403` inactive user login blocked |
| `UserControllerIT` | Verify ADMIN-only access; verify **reactivate endpoint restores `is_active`**; verify soft delete persists record |
| `StudentControllerIT` | Full CRUD flow; assert STUDENT cannot access others' records; assert auto-generated `studentNumber` uniqueness |
| `UserRepositoryIT` | Verify `findByEmailAndIsActiveTrue` filters soft-deleted users |
| `FlywayMigrationIT` | Startup succeeds; schema matches entities |

#### Maven Test Dependencies
```xml
<dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-test</artifactId>
    <scope>test</scope>
</dependency>
<dependency>
    <groupId>org.testcontainers</groupId>
    <artifactId>mssqlserver</artifactId>
    <scope>test</scope>
</dependency>
<dependency>
    <groupId>org.testcontainers</groupId>
    <artifactId>junit-jupiter</artifactId>
    <scope>test</scope>
</dependency>
```

## 2. FRONTEND (React + TypeScript) — Outline
- **Stack:** Next.js 14, TypeScript, Zustand, TanStack Query, Axios, shadcn/ui, Tailwind CSS.
- **Routes:** `/login`, `/dashboard`, `/students`, `/students/[id]`, `/users`, `/users/[id]`, `/profile`.
- **JWT Handling:** Axios request interceptor injects `Authorization` header; 401/403 response interceptor triggers token refresh or logout; middleware handles route guards and role redirects.
- **Component Structure:** `app/`, `components/ui/`, `components/layout/`, `lib/api/`, `stores/`, `hooks/`.

## 3. MOBILE (React Native CLI) — Outline
- **Stack:** React Native CLI, Redux Toolkit, Axios, React Native SecureStore (iOS Keychain / Android Keystore), Atomic Design folder structure.
- **Screens:** `LoginScreen`, `DashboardScreen`, `StudentListScreen`, `StudentDetailScreen`, `ProfileScreen`, `UserManagementScreen` (Admin only).
- **Concerns:** Access/Refresh tokens stored via SecureStore; Redux Persist for offline UI state; role-aware tab navigator (Student sees Home/Profile; Admin sees Home/Users/Students/Profile).

## 4. QA — Outline
- **Testing Pyramid:** 70% Unit (Backend/Mobile), 20% Integration (APIs), 10% E2E (Critical user flows).
- **Toolchain:** Postman collections for manual API regression; Newman for CI collection runs; Cypress for Frontend E2E (Auth + Student CRUD flows).
- **Deliverables Checklist:** Test Plan, Traceability Matrix, Automated Test Report, Defect Log, Coverage Report.

## 5. DEVOPS — Outline
- **Docker Compose Services:** **MSSQL 2022** (Dev), **Redis** (token blocklist / refresh store), Backend service (OpenJDK 17 slim), Frontend service (Node 20 + Next.js standalone).
- **BitBucket CI/CD:** `step: test` (Maven verify with Testcontainers) → `step: build` (JAR) → `step: dockerize` (Build & push to registry) → `step: deploy` (SSH / ECS / Render).
- **Environment Variables:** `JWT_SECRET`, `JWT_EXPIRATION_MS`, `DB_URL` (SQL Server connection string), `DB_USER`, `DB_PASSWORD`, `CORS_ALLOWED_ORIGINS`.
- **Hosting Recommendation:** Render / Railway for academy demos; AWS ECS Fargate for production-minded path.

## 6. ADDITIONAL SECTIONS

### 6.1 Mono-Repo Folder Structure
```
student-portal/
├── backend/                  # Spring Boot (Maven)
│   ├── src/
│   ├── pom.xml
│   └── Dockerfile
├── frontend/                 # Next.js
│   ├── app/
│   ├── package.json
│   └── Dockerfile
├── mobile/                   # React Native CLI
│   ├── android/
│   ├── ios/
│   ├── src/
│   └── package.json
├── docker-compose.yml
├── .gitignore
└── README.md
```

### 6.2 4-Sprint Development Milestone Plan
- [ ] **Sprint 1 — Foundation & Auth:** Scaffolding, MSSQL + Flyway, JWT login, `V3` admin seed, `is_active` logic.
- [ ] **Sprint 2 — User Management:** Admin CRUD on `/users`, **soft delete**, **reactivate**, email update allowed for Admin, unit tests.
- [ ] **Sprint 3 — Student Enrollment & CRUD:** `/students` API, 1:1 transaction, **auto-generated `studentNumber` (`U16CO1068`)**, ownership checks, `PATCH /students/me`, integration tests.
- [ ] **Sprint 4 — Hardening & QA:** Edge-cases, global exception handler, OpenAPI docs, ≥80% coverage, QA sign-off.

### 6.3 Open Questions & Assumptions
| # | Question / Assumption | Final Decision |
|---|----------------------|----------------|
| 1 | **Database:** MSSQL standard? | **MSSQL 2022** across all environments. |
| 2 | **Student Self-Service:** Student updates own profile? | **YES**, via `PATCH /students/me`, but **email is immutable for self-service**. |
| 3 | **Admin/Staff Email Update:** Can Admin change emails? | **YES**, Admin/Staff can update user emails via `PUT /users/{id}`. |
| 4 | **Deletion Strategy:** Soft delete preferred? | **`users.is_active = 0`** for soft delete; **Reactivate** endpoint available. |
| 5 | **Student Number:** Auto-generated with specific format? | **YES**, backend generates `U16CO1068` format. |
| 6 | **Email Verification / Password Reset:** Out of scope? | **Out of MVP scope.** Admin resets passwords manually. |
| 7 | **Audit Logging:** Mutation tracking? | **JPA timestamps only**; full auditing deferred post-MVP. |
