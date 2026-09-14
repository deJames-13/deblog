import { BlogPost, BlogComment, UserAccount, MediaItem, SiteProfile } from '../models/blog.model';

export const INITIAL_PROFILE: SiteProfile = {
  name: 'Derick Espinosa',
  role: 'Power Platform Developer | Full Stack Developer',
  tagline: 'Engineering enterprise automations, clean full-stack backends, and low-latency minimalist interfaces.',
  bio: 'Specializing in Microsoft Power Platform (Power Apps, Power Automate, Dataverse) and modern TypeScript web ecosystems. Passionate about distraction-free systems architecture and BIOS-style computational aesthetics.',
  email: 'drckespinosa.13@gmail.com',
  location: 'Manila, Philippines / Remote',
  avatar_url: '/assets/images/me.png',
  banner_url: 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=1200&q=80',
  copyright_year: '2026',
  social_links: {
    github: 'https://github.com/deJames-13',
    facebook: 'https://facebook.com/the2ndpercyfied',
    linkedin: 'https://linkedin.com/in/derickjamesespinosa',
    instagram: 'https://instagram.com/_doftenfools',
  },
};

export const INITIAL_POSTS: BlogPost[] = [
  {
    id: 'post-001',
    slug: 'architecting-low-latency-power-platform-dataverse-integrations',
    title: 'Architecting Low-Latency Power Platform & Dataverse Integrations',
    subtitle: 'Overcoming API throttling and batch operations in high-throughput enterprise systems',
    excerpt: 'Deep dive into caching strategies, custom API endpoints in Dataverse, and asynchronous message queue patterns to minimize API round-trips.',
    content: `## 01. The Problem of Enterprise Latency

When scaling Microsoft Power Platform applications across multi-tenant environments, standard OData connector queries often encounter threshold bottlenecks. A naive sequential query structure creates cumulative delay that quickly degrades end-user experience.

\`\`\`typescript
// Anti-pattern: Sequential patch operations
for (const record of batch) {
  await dataverseClient.updateRecord("cr_invoices", record.id, record.data);
}

// Recommended: Transactional Batch Changeset
const changeset = buildODataBatchRequest(batch);
await dataverseClient.executeBatch("$batch", changeset);
\`\`\`

### Key Architectural Guidelines

1. **Leverage Dataverse Alternate Keys**: Direct lookup without requiring pre-flight GUID queries cuts network calls by 50%.
2. **ExecuteMultiple vs Custom APIs**: ExecuteMultipleRequest is throttled aggressively. Packaging operations inside server-side Dataverse Custom APIs executes within native database transactions.
3. **Change Tracking & Delta Tokens**: Instead of scanning entire tables, utilize Dataverse Change Tracking sync tokens to pull modified state only.

> "A well-architected automation pipeline is invisible to the user because it finishes before their attention shifts."

### Benchmark Comparison

In our testing across 12,000 nightly reconciliation rows, migrating to Custom API batching reduced execution duration from 42 minutes to under 3 minutes, with zero throttle faults.`,
    cover_image: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=1000&q=80',
    category: 'Power Platform',
    tags: ['Dataverse', 'Architecture', 'TypeScript', 'Enterprise'],
    status: 'published',
    likes_count: 84,
    views_count: 1420,
    reading_time_minutes: 5,
    created_at: '2026-08-14T09:30:00Z',
    updated_at: '2026-08-14T11:00:00Z',
    published_at: '2026-08-14T11:00:00Z',
    is_featured: true,
  },
  {
    id: 'post-002',
    slug: 'the-modern-bios-aesthetic-in-web-systems',
    title: 'The Modern BIOS Aesthetic: Functionality Over Fluff',
    subtitle: 'Why flat corners, monospaced typography, and keyboard-first navigation remain unbeatable',
    excerpt: 'Exploring the UX philosophy behind raw system utilities, zero-radius borders, tactile F-key shortcuts, and distraction-free terminal minimalism.',
    content: `## The Era of Decorative Fatigue

Modern web interfaces frequently drown essential content in redundant gradients, oversized floating shadows, and jarring parallax animations. When you inspect classic system BIOS utilities, terminal editors, and telemetry monitors, every single pixel carries semantic payload.

\`\`\`txt
+-----------------------------------------------------------+
| BIOS SETUP UTILITY - ADVANCED CPU & SYSTEM CONFIGURATION  |
| Date: 09/11/2026  Time: 20:48:42  System Memory: 65536 MB |
+-----------------------------------------------------------+
| [F1] Help   [F2] Search   [F3] Zen Mode   [F8] Admin Mode |
+-----------------------------------------------------------+
\`\`\`

### The Core Tenets of BIOS UX

- **Zero Rounded Corners**: Sharp geometric discipline. Content boundaries are explicit and structured.
- **Unified Ratios**: Strict proportion between navigation sidebars, content zones, and terminal controls.
- **Hardware-Inspired Hotkeys**: Function keys [F1]-[F8] offer instant, friction-free actions without requiring mouse hunting.
- **Tone-Tuned Palettes**: Twilight, Sepia, and Night palettes prevent eye strain over extended reading intervals.

When an engineer or reader arrives at your site, they want uninterrupted signal, not visual noise.`,
    cover_image: 'https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?auto=format&fit=crop&w=1000&q=80',
    category: 'UI/UX Design',
    tags: ['BIOS', 'Minimalism', 'Typography', 'Keyboard-First'],
    status: 'published',
    likes_count: 129,
    views_count: 2840,
    reading_time_minutes: 4,
    created_at: '2026-08-28T14:15:00Z',
    updated_at: '2026-08-29T10:00:00Z',
    published_at: '2026-08-29T10:00:00Z',
    is_featured: true,
  },
  {
    id: 'post-003',
    slug: 'building-resilient-fullstack-services-cloud-run-postgres',
    title: 'Building Resilient Full-Stack Services on Cloud Run & PostgreSQL',
    subtitle: 'Containerized Node runtimes, connection pooling, and zero-downtime schema migrations',
    excerpt: 'A comprehensive engineering guide on configuring PgBouncer, strict TypeScript types, and serverless scale-to-zero microservices.',
    content: `## Cloud Run Meets Relational Integrity

Stateless container platforms like Google Cloud Run offer effortless auto-scaling, but they introduce unique challenges when pairing with stateful relational databases like PostgreSQL.

\`\`\`sql
-- Schema migration pattern with zero lock timeouts
ALTER TABLE blog_posts
ADD COLUMN IF NOT EXISTS reading_time_minutes INTEGER DEFAULT 5 NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_posts_status_created
ON blog_posts (status, created_at DESC);
\`\`\`

### Connection Exhaustion Mitigation

Because Cloud Run can spin up dozens of container instances during traffic spikes, direct database connections will quickly surpass max limits. Integrating a connection pooler like PgBouncer in transaction pooling mode prevents connection rejections while preserving sub-millisecond query latency.`,
    cover_image: 'https://images.unsplash.com/photo-1544197150-b99a580bb7a8?auto=format&fit=crop&w=1000&q=80',
    category: 'Full Stack',
    tags: ['PostgreSQL', 'Cloud Run', 'Node.js', 'DevOps'],
    status: 'published',
    likes_count: 67,
    views_count: 1150,
    reading_time_minutes: 6,
    created_at: '2026-09-02T16:00:00Z',
    updated_at: '2026-09-02T18:20:00Z',
    published_at: '2026-09-02T18:20:00Z',
    is_featured: false,
  },
  {
    id: 'post-004',
    slug: 'mastering-power-automate-error-handling-scopes',
    title: 'Mastering Power Automate Error Handling: Scope Patterns and Run After',
    subtitle: 'Never let silent flow failures disrupt mission-critical business data again',
    excerpt: 'Detailed guide to implementing Try-Catch-Finally architectures within cloud flows, automated failure webhooks, and audit logging.',
    content: `## Why Default Flow Notifications Fail

The built-in weekly email alerts for Power Automate flow failures arrive days too late for mission-critical enterprise operations. Implementing structured scope blocks guarantees resilient execution and immediate alerting.

### The Try-Catch-Finally Flow Template

1. **Try Scope**: Contains core automation actions (API queries, transformations, file generation).
2. **Catch Scope**: Configured with 'Run After' set to: *has failed*, *has timed out*, or *is skipped*.
3. **Finally Scope**: Configured to run whether Try succeeded or Catch executed, ensuring temporary session locks are released.`,
    cover_image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1000&q=80',
    category: 'Power Platform',
    tags: ['Power Automate', 'Error Handling', 'Best Practices'],
    status: 'published',
    likes_count: 95,
    views_count: 1890,
    reading_time_minutes: 4,
    created_at: '2026-09-06T11:20:00Z',
    updated_at: '2026-09-06T13:00:00Z',
    published_at: '2026-09-06T13:00:00Z',
    is_featured: true,
  },
  {
    id: 'post-005',
    slug: 'draft-modern-event-driven-micro-frontends',
    title: 'Draft: Evaluating Custom Connectors vs Graph API Endpoints',
    subtitle: 'Internal notes on security, token management, and maintenance overhead',
    excerpt: 'A comparison review of developing custom Power Platform connectors against direct Microsoft Graph API integrations with managed identities.',
    content: `## Work in Progress

This post explores the architectural trade-offs between creating packaged Power Platform Custom Connectors with OpenAPI Swagger specs versus deploying lightweight Azure Functions that proxy Graph API requests.

Draft notes:
- OAuth client secret rotation challenges
- Application permissions vs delegated permissions
- Tenant boundary constraints in B2B scenarios`,
    category: 'Power Platform',
    tags: ['Custom Connectors', 'Graph API', 'Security'],
    status: 'draft',
    likes_count: 0,
    views_count: 12,
    reading_time_minutes: 3,
    created_at: '2026-09-10T08:00:00Z',
    updated_at: '2026-09-10T14:30:00Z',
    is_featured: false,
  },
  {
    id: 'post-006',
    slug: 'archived-legacy-sharepoint-rest-v1-guide',
    title: 'Archived: Legacy SharePoint REST v1 Workflow Automation',
    subtitle: 'Historic reference for pre-2022 on-premise SharePoint 2013-2016 deployments',
    excerpt: 'Preserved documentation for legacy SOAP and REST v1 API calls in hybrid enterprise environments.',
    content: `## Historical Archive Notice

This document is preserved for backward-compatibility audits in air-gapped intranet infrastructures. Modern projects should use Microsoft Graph and Dataverse exclusively.`,
    category: 'Full Stack',
    tags: ['SharePoint', 'Legacy', 'Archive'],
    status: 'archived',
    likes_count: 14,
    views_count: 420,
    reading_time_minutes: 2,
    created_at: '2026-01-15T10:00:00Z',
    updated_at: '2026-03-20T12:00:00Z',
    published_at: '2026-01-15T12:00:00Z',
    is_featured: false,
  },
];

export const INITIAL_COMMENTS: BlogComment[] = [
  {
    id: 'comm-101',
    post_id: 'post-001',
    post_title: 'Architecting Low-Latency Power Platform & Dataverse Integrations',
    author_name: 'Marcus Chen',
    author_email: 'marcus.chen@techcorp.io',
    content: 'The insight on Dataverse Custom APIs replacing ExecuteMultiple is spot on. We ran into the 2-minute API limit repeatedly until moving to internal transactional procedures.',
    status: 'approved',
    likes_count: 12,
    created_at: '2026-08-16T10:25:00Z',
  },
  {
    id: 'comm-102',
    post_id: 'post-002',
    post_title: 'The Modern BIOS Aesthetic: Functionality Over Fluff',
    author_name: 'Elena Rostova',
    author_email: 'elena.r@designsys.dev',
    content: 'This zero-radius flat UI aesthetic is so refreshing. Modern websites have become identical cookie-cutter cards. The F-key navigation actually works brilliantly!',
    status: 'approved',
    likes_count: 18,
    created_at: '2026-08-30T16:40:00Z',
  },
  {
    id: 'comm-103',
    post_id: 'post-002',
    post_title: 'The Modern BIOS Aesthetic: Functionality Over Fluff',
    author_name: 'Devon Vance',
    author_email: 'devon.vance@gmail.com',
    content: 'Love the monochrome accents and the sepia reading mode. Perfect contrast for late-night reading.',
    status: 'pending',
    likes_count: 3,
    created_at: '2026-09-08T09:12:00Z',
  },
  {
    id: 'comm-104',
    post_id: 'post-004',
    post_title: 'Mastering Power Automate Error Handling: Scope Patterns and Run After',
    author_name: 'Sarah Jenkins',
    author_email: 's.jenkins@enterprise-solutions.net',
    content: 'Could you share how you handle OAuth token refresh inside the Catch scope when notifying external webhooks?',
    status: 'pending',
    likes_count: 1,
    created_at: '2026-09-09T14:05:00Z',
  },
];

export const INITIAL_USERS: UserAccount[] = [
  {
    id: 'user-001',
    name: 'Derick Espinosa',
    email: 'drckespinosa.13@gmail.com',
    role: 'admin',
    status: 'active',
    comments_count: 14,
    created_at: '2026-01-01T00:00:00Z',
    last_active_at: '2026-09-11T20:48:00Z',
  },
  {
    id: 'user-002',
    name: 'Marcus Chen',
    email: 'marcus.chen@techcorp.io',
    role: 'commenter',
    status: 'active',
    comments_count: 5,
    created_at: '2026-05-10T12:00:00Z',
    last_active_at: '2026-08-16T10:25:00Z',
  },
  {
    id: 'user-003',
    name: 'Elena Rostova',
    email: 'elena.r@designsys.dev',
    role: 'commenter',
    status: 'active',
    comments_count: 8,
    created_at: '2026-06-22T08:15:00Z',
    last_active_at: '2026-08-30T16:40:00Z',
  },
  {
    id: 'user-004',
    name: 'Sarah Jenkins',
    email: 's.jenkins@enterprise-solutions.net',
    role: 'subscriber',
    status: 'active',
    comments_count: 2,
    created_at: '2026-07-04T15:30:00Z',
    last_active_at: '2026-09-09T14:05:00Z',
  },
  {
    id: 'user-005',
    name: 'Bot Promo Spammer',
    email: 'crypto-promo-bot@spammer.xyz',
    role: 'commenter',
    status: 'banned',
    comments_count: 0,
    created_at: '2026-09-01T03:12:00Z',
    last_active_at: '2026-09-01T03:12:00Z',
  },
];

export const INITIAL_MEDIA: MediaItem[] = [
  {
    id: 'med-001',
    filename: 'dataverse-architecture-diagram.webp',
    url: 'https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=1000&q=80',
    mime_type: 'image/webp',
    file_size_kb: 48,
    original_size_kb: 340,
    optimized_size_kb: 48,
    dimensions: '1920x1080',
    uploaded_at: '2026-08-14T09:00:00Z',
    alt_text: 'Server racks and networking architecture',
  },
  {
    id: 'med-002',
    filename: 'bios-terminal-preview.webp',
    url: 'https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?auto=format&fit=crop&w=1000&q=80',
    mime_type: 'image/webp',
    file_size_kb: 64,
    original_size_kb: 512,
    optimized_size_kb: 64,
    dimensions: '1600x900',
    uploaded_at: '2026-08-28T14:00:00Z',
    alt_text: 'Monochrome computer screen showing code',
  },
  {
    id: 'med-003',
    filename: 'cloud-run-containers.webp',
    url: 'https://images.unsplash.com/photo-1544197150-b99a580bb7a8?auto=format&fit=crop&w=1000&q=80',
    mime_type: 'image/webp',
    file_size_kb: 52,
    original_size_kb: 420,
    optimized_size_kb: 52,
    dimensions: '1200x800',
    uploaded_at: '2026-09-02T15:30:00Z',
    alt_text: 'Cloud infrastructure components',
  },
  {
    id: 'med-004',
    filename: 'power-automate-flow-diagram.webp',
    url: 'https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1000&q=80',
    mime_type: 'image/webp',
    file_size_kb: 39,
    original_size_kb: 290,
    optimized_size_kb: 39,
    dimensions: '1440x900',
    uploaded_at: '2026-09-06T11:00:00Z',
    alt_text: 'Electronic motherboard macro',
  },
];
