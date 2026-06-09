---
uid: 9a915a75-36b0-4107-95ba-2657b449a684
---

# 🤖❓ list-of-open-source-markdown-publishing-alternatives

- I use Obsidian for all my note-taking. 
- I have a large repository of notes in markdown
- I am looking for different ways to create websites that I can publish with GitHub Pages from my markdown notes.
- I am considering tools such as 
	- <https://www.mkdocs.org/>
	- [Welcome to Quartz 4](https://quartz.jzhao.xyz/)
	- [GitBook – The AI-native documentation platform](https://www.gitbook.com/)
	- [The world's fastest framework for building websites](https://gohugo.io/)
- Please make me an extensive list of 
	- tools in this space - especially open source tools
	- Please include 
		- a URL link to each tool
		- A summary of each tool's functionality.
- Please ask any clarifying questions that you may have.

# Add your input below here do not edit above

## 🤖💡 Clarifying Questions

Before I list everything, a few quick questions to help tailor recommendations:

1. **Knowledge base vs blog**: Are you publishing a searchable knowledge base, a blog, or both?
2. **Custom domain**: Will you use `username.github.io` or a custom domain?
3. **Search priority**: Is full-text search important for your use case?
4. **Theme customization**: How much do you want to customize the look and feel?
5. **Wikilinks**: Do you need support for Obsidian-style `[[wikilinks]]` or standard Markdown?

---

## Comprehensive Open-Source Markdown Publishing Tools

### 🏆 **Best for Obsidian Integration**

#### **Quartz** ⭐ (Highly Recommended)
- **URL**: https://quartz.jzhao.xyz/
- **GitHub**: https://github.com/jackyzha0/quartz
- **Summary**: Purpose-built for converting Obsidian vaults to websites. Features full-text search, backlinks, graph view, and automatic sitemap generation. Excellent for knowledge bases with interconnected notes.
- **GitHub Pages**: ✅ Native support
- **Wikilinks**: ✅ Full support
- **Best For**: Second brain/knowledge base publishing

---

### 📚 **Static Site Generators (Broad Compatibility)**

#### **Hugo**
- **URL**: https://gohugo.io/
- **GitHub**: https://github.com/gohugoio/hugo
- **Summary**: The world's fastest static site generator. Highly extensible with hundreds of themes. Requires more configuration than specialized tools but offers maximum flexibility.
- **GitHub Pages**: ✅ Native support
- **Themes for Markdown**: Digital Garden, LoveIt, Book, PaperMod
- **Best For**: Blogs, documentation, general websites

#### **Jekyll**
- **URL**: https://jekyllrb.com/
- **GitHub**: https://github.com/jekyll/jekyll
- **Summary**: GitHub's native static site generator (runs automatically on GitHub Pages). Simpler than Hugo, tight GitHub integration. Built-in support for Markdown, YAML frontmatter.
- **GitHub Pages**: ✅ Built-in (no deployment needed)
- **Best For**: GitHub Pages users who want zero-config publishing

#### **Eleventy (11ty)**
- **URL**: https://www.11ty.dev/
- **GitHub**: https://github.com/11ty/eleventy
- **Summary**: Flexible JavaScript-based SSG. Uses your favorite template language (Markdown, Nunjucks, Liquid, etc.). Growing ecosystem with digital garden plugins.
- **GitHub Pages**: ✅ Full support
- **Best For**: Developers comfortable with Node.js

#### **Zola**
- **URL**: https://www.getzola.org/
- **GitHub**: https://github.com/getzola/zola
- **Summary**: Fast, all-in-one static site generator written in Rust. Built-in Sass support, taxonomies, search functionality. Minimal configuration required.
- **GitHub Pages**: ✅ Full support
- **Best For**: Speed-focused users who want batteries-included features

---

### 🔍 **Specialized for Knowledge Bases**

#### **Docusaurus**
- **URL**: https://docusaurus.io/
- **GitHub**: https://github.com/facebook/docusaurus
- **Summary**: Meta's documentation framework. Excellent for organized documentation sites with versioning. Built-in search, multiple sidebars, i18n support. React-based.
- **GitHub Pages**: ✅ Full support
- **Best For**: Technical documentation with multiple versions

#### **MkDocs** (You mentioned this)
- **URL**: https://www.mkdocs.org/
- **GitHub**: https://github.com/mkdocs/mkdocs
- **Summary**: Python-based documentation generator with beautiful themes (Material for MkDocs is the most popular). Lightweight, quick to setup. Excellent table of contents and navigation.
- **GitHub Pages**: ✅ Full support
- **Best For**: Simple, clean documentation sites

#### **Material for MkDocs**
- **URL**: https://squidfunk.github.io/mkdocs-material/
- **GitHub**: https://github.com/squidfunk/mkdocs-material
- **Summary**: Premium theme for MkDocs with search, dark mode, instant loading, and instant search. Now open-source with both free and paid tiers.
- **GitHub Pages**: ✅ Full support via MkDocs
- **Best For**: Professional-looking docs with minimal effort

#### **VitePress**
- **URL**: https://vitepress.dev/
- **GitHub**: https://github.com/vuejs/vitepress
- **Summary**: Modern docs site generator built on Vite. Vue.js-powered, extremely fast build times. Markdown + Vue components for dynamic content.
- **GitHub Pages**: ✅ Full support
- **Best For**: Developers who want Vue.js integration

#### **Nextra**
- **URL**: https://nextra.site/
- **GitHub**: https://github.com/shuding/nextra
- **Summary**: Framework for building documentation sites. Built on Next.js, supports Markdown and MDX (Markdown + React components). Full-text search built-in.
- **GitHub Pages**: ❌ (Requires Node.js hosting, but works with Vercel free tier)
- **Best For**: Interactive documentation with React components

---

### 🎨 **Digital Garden Focused**

#### **Digital Garden Jekyll Template**
- **URL**: https://github.com/maximevaillancourt/digital-garden
- **GitHub**: https://github.com/maximevaillancourt/digital-garden
- **Summary**: Jekyll template specifically designed for Obsidian-to-web publishing. Features backlinks, graph visualization, and minimal configuration.
- **GitHub Pages**: ✅ Native support
- **Best For**: Obsidian → GitHub Pages with minimal setup

#### **Dendron**
- **URL**: https://www.dendron.so/
- **GitHub**: https://github.com/dendronhq/dendron
- **Summary**: Knowledge management tool with web publishing. VS Code extension, hierarchical note organization, built-in pod system for exporting to HTML/Jekyll.
- **GitHub Pages**: ✅ Via Jekyll export
- **Best For**: Hierarchical note organization

---

### ⚡ **Modern, Fast Alternatives**

#### **Astro**
- **URL**: https://astro.build/
- **GitHub**: https://github.com/withastro/astro
- **Summary**: Modern framework for content-driven sites. Framework-agnostic (use Vue, React, Svelte, or just HTML). Zero JavaScript by default. Excellent for performance.
- **GitHub Pages**: ✅ Full support
- **Best For**: Modern web devs, content-focused sites

#### **Lume**
- **URL**: https://lume.land/
- **GitHub**: https://github.com/lumeland/lume
- **Summary**: Static site generator for Deno (modern JavaScript runtime). Supports multiple formats including Markdown. Fast, simple, and flexible.
- **GitHub Pages**: ✅ Full support
- **Best For**: Deno/modern JavaScript users

#### **Lektor**
- **URL**: https://www.getlektor.com/
- **GitHub**: https://github.com/lektor/lektor
- **Summary**: Flat-file CMS with static site generation. Built-in admin panel for managing content. Python-based, focuses on ease-of-use.
- **GitHub Pages**: ✅ Full support
- **Best For**: Non-technical users who want a visual editor

---

### 📖 **Focused on Writing & Publishing**

#### **Pelican**
- **URL**: https://getpelican.com/
- **GitHub**: https://github.com/getpelican/pelican
- **Summary**: Python-based static blog generator. Author-friendly with Markdown, reStructuredText, and AsciiDoc support. Good for blogging.
- **GitHub Pages**: ✅ Full support
- **Best For**: Python developers, blogging

#### **Hexo**
- **URL**: https://hexo.io/
- **GitHub**: https://github.com/hexojs/hexo
- **Summary**: Fast, simple JavaScript-based blog framework. Minimal configuration, theme ecosystem, plugin support. Very active community.
- **GitHub Pages**: ✅ Full support
- **Best For**: Blogging, minimal configuration needed

#### **Gatsby** (React-based)
- **URL**: https://www.gatsbyjs.com/
- **GitHub**: https://github.com/gatsbyjs/gatsby
- **Summary**: React framework for building fast websites and apps. Plugin ecosystem, GraphQL data layer. More powerful but requires more setup.
- **GitHub Pages**: ✅ Full support
- **Best For**: React developers, complex interactive sites

---

## Quick Comparison Table

| Tool | Learning Curve | Setup Time | GitHub Pages | Wikilinks | Search | Best For |
|------|---|---|---|---|---|---|
| **Quartz** | Low | 5 min | ✅ | ✅ | ✅ | Obsidian → Web |
| **Jekyll** | Low | 2 min | ✅ Built-in | ❌ | Add-on | GitHub Pages users |
| **Hugo** | Medium | 10 min | ✅ | ❌ | Add-on | Flexibility |
| **MkDocs** | Low | 5 min | ✅ | ❌ | ✅ | Documentation |
| **Zola** | Low | 10 min | ✅ | ❌ | ✅ | Speed + features |
| **VitePress** | Medium | 10 min | ✅ | ❌ | ✅ | Modern docs |
| **Eleventy** | Medium | 15 min | ✅ | ❌ | Add-on | JavaScript devs |

---

## 🎯 My Recommendation for Your Use Case

Given that you use **Obsidian** and want GitHub Pages publishing:

1. **First Choice: Quartz** - Purpose-built for your exact use case. One command to deploy, automatic backlinks and graph view, full support for Obsidian formatting.

2. **Second Choice: Digital Garden Jekyll Template** - If you want zero Node.js dependencies. Simple, elegant, and works perfectly with Obsidian.

3. **Alternative: MkDocs + Material Theme** - If you want a cleaner, more professional look and don't need wikilinks. Minimal setup, beautiful output.

Would you like me to create a setup guide for any of these tools?

---

# 🤖💡 Setup Guides for the Top 3 Recommendations

## 1️⃣ QUARTZ (Best for Obsidian Power Users)

### Prerequisites
- Node.js 18.17+ installed ([download](https://nodejs.org/))
- Git installed
- A GitHub account
- Your Obsidian vault already on GitHub (or ready to push)

### Step 1: Clone Quartz
```bash
git clone https://github.com/jackyzha0/quartz.git
cd quartz
npm i
npx quartz create
```

### Step 2: Copy Your Obsidian Vault
```bash
# Copy your markdown notes into the content folder
cp -r /path/to/your/obsidian/vault/* content/
```

### Step 3: Configure Quartz
Edit `quartz.config.ts`:
```typescript
const config: QuartzConfig = {
  configuration: {
    pageTitle: "Your Vault Name",
    enableSPA: true,
    enablePopovers: true,
    analytics: {
      provider: "plausible",
    },
  },
  // ... rest of config
}
```

### Step 4: Test Locally
```bash
npx quartz build --serve
# Visit http://localhost:8080
```

### Step 5: Deploy to GitHub Pages
```bash
npx quartz sync
# This pushes your site to GitHub Pages
```

### Features You Get
- ✅ Automatic backlinks between notes
- ✅ Graph visualization of connections
- ✅ Full-text search
- ✅ Dark/light mode toggle
- ✅ Wikilink support `[[like-this]]`
- ✅ Automatic sitemap for SEO

### Customization
- **Theme colors**: Edit `quartz/styles/base.scss`
- **Backlink text**: Customize in `quartz.config.ts`
- **Add custom CSS**: Create `.scss` files in `quartz/styles`

### Deployment URL
Your site will be available at: `https://your-username.github.io/quartz`

(Or `https://your-username.github.io/` if repo is named `username.github.io`)

### Useful Links
- Docs: https://quartz.jzhao.xyz/
- Customization: https://quartz.jzhao.xyz/configuration
- Troubleshooting: https://quartz.jzhao.xyz/notes/troubleshooting

---

## 2️⃣ DIGITAL GARDEN JEKYLL TEMPLATE (Simplest Setup)

### Prerequisites
- Ruby 2.7+ installed ([how to install](https://www.ruby-lang.org/en/documentation/installation/))
- Git installed
- A GitHub account

### Step 1: Create Repository from Template
1. Go to: https://github.com/maximevaillancourt/digital-garden
2. Click **"Use this template"** → **"Create a new repository"**
3. Name it `your-username.github.io` (this auto-deploys to GitHub Pages)
4. Clone it locally:
```bash
git clone https://github.com/your-username/your-username.github.io.git
cd your-username.github.io
```

### Step 2: Add Your Notes
```bash
# Copy your Obsidian notes
cp -r /path/to/your/obsidian/vault/* _notes/
```

### Step 3: Install Dependencies
```bash
bundle install
```

### Step 4: Test Locally
```bash
bundle exec jekyll serve
# Visit http://localhost:4000
```

### Step 5: Commit and Deploy
```bash
git add .
git commit -m "Add my notes"
git push origin main
```

GitHub Pages automatically builds and deploys! Your site is live in seconds.

### Configuration
Edit `_config.yml`:
```yaml
title: Your Vault Name
description: A place for my thoughts
author: Your Name
url: https://your-username.github.io

# Collections
collections:
  notes:
    output: true
    permalink: /:name
```

### Features You Get
- ✅ Backlinks between notes
- ✅ Graph visualization
- ✅ Clean, minimal design
- ✅ No Node.js required (Ruby only)
- ✅ Built-in Jekyll support (GitHub-native)

### Customization
- **Change colors**: Edit `_sass/_variables.scss`
- **Modify layout**: Edit `_layouts/default.html`
- **Add custom CSS**: Edit `assets/css/style.scss`

### Deployment URL
Your site will be live at: `https://your-username.github.io`

### Useful Links
- Template: https://github.com/maximevaillancourt/digital-garden
- Jekyll Docs: https://jekyllrb.com/docs/
- GitHub Pages Help: https://docs.github.com/en/pages

---

## 3️⃣ MKDOCS + MATERIAL THEME (Professional Look)

### Prerequisites
- Python 3.8+ installed ([download](https://www.python.org/downloads/))
- pip (comes with Python)
- Git installed
- A GitHub account

### Step 1: Create Directory and Virtual Environment
```bash
mkdir my-docs
cd my-docs

# Create virtual environment (best practice)
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
```

### Step 2: Install MkDocs and Material Theme
```bash
pip install mkdocs mkdocs-material
```

### Step 3: Create Project
```bash
mkdocs new .
# This creates mkdocs.yml and docs/ folder
```

### Step 4: Add Your Notes
```bash
# Copy your Obsidian notes to docs/ folder
cp -r /path/to/your/obsidian/vault/* docs/
```

### Step 5: Configure mkdocs.yml
```yaml
site_name: My Knowledge Base
site_author: Your Name
site_description: My personal notes and knowledge base

theme:
  name: material
  palette:
    # Light mode
    - scheme: default
      primary: blue
      accent: indigo
    # Dark mode
    - scheme: slate
      primary: blue
      accent: indigo

plugins:
  - search
  - awesome-pages

nav:
  - Home: index.md
  - Getting Started: getting-started.md
  - Topics: '!include ./nav.yml'  # Optional: organize by folders
```

### Step 6: Test Locally
```bash
mkdocs serve
# Visit http://localhost:8000
```

### Step 7: Initialize Git and Deploy to GitHub Pages
```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/your-username/your-repo.git
git push -u origin main

# Deploy to GitHub Pages
mkdocs gh-deploy
```

This automatically pushes your site to the `gh-pages` branch.

### GitHub Pages Configuration
1. Go to your repo **Settings** → **Pages**
2. Set source to: **Deploy from a branch**
3. Select branch: **gh-pages**, folder: **/root**
4. Save

Your site is now live!

### Features You Get
- ✅ Full-text search
- ✅ Dark/light mode toggle
- ✅ Beautiful, professional theme
- ✅ Mobile-responsive
- ✅ Syntax highlighting for code
- ✅ Table of contents navigation
- ✅ Built-in social sharing

### Customization
- **Logo**: Place logo in `docs/assets/images/` and add to `mkdocs.yml`:
```yaml
theme:
  logo: assets/images/logo.png
```

- **Colors**: Customize in `mkdocs.yml` under `theme.palette`
- **Custom CSS**: Create `docs/stylesheets/custom.css` and reference it

### Useful Plugins
```bash
pip install mkdocs-awesome-pages-plugin  # Better navigation
pip install mkdocs-minify-plugin          # Minify output
```

### Deployment URL
Your site will be at: `https://your-username.github.io/your-repo`

(Or `https://your-username.github.io/` if repo is named `username.github.io`)

### Useful Links
- MkDocs Docs: https://www.mkdocs.org/
- Material Theme: https://squidfunk.github.io/mkdocs-material/
- Plugins: https://squidfunk.github.io/mkdocs-material/plugins/search/

---

## 🎯 Quick Comparison: Getting Started

| Aspect | Quartz | Digital Garden | MkDocs |
|--------|--------|---|---|
| **Install time** | 5 minutes | 2 minutes | 5 minutes |
| **Technical skill needed** | Medium | Low | Low |
| **Dependencies** | Node.js 18+ | Ruby 2.7+ | Python 3.8+ |
| **Backlinks** | ✅ Advanced | ✅ Basic | ❌ No |
| **Graph view** | ✅ Yes | ✅ Yes | ❌ No |
| **Search** | ✅ Built-in | ✅ Built-in | ✅ Built-in |
| **Wikilinks** | ✅ Full support | ⚠️ Converted links | ❌ Standard MD only |
| **Setup complexity** | Medium | Easy | Easy |
| **Customization** | Maximum | Good | Good |

---

## 🚀 Next Steps

1. **Pick one tool** to start with (Quartz for power users, Digital Garden for simplicity, MkDocs for professional look)
2. **Follow the setup guide above**
3. **Test locally** to see how it looks
4. **Push to GitHub**
5. **Enable GitHub Pages** in your repo settings
6. **Share your published site!**

### Need Help?
- Check the tool's official documentation (links provided above)
- Look at example repos to see how others structured theirs
- Quartz has an active Discord community: https://discord.gg/cRFFHYbye7

---

## Pro Tips

### For Quartz Users
- Use `.gitignore` to exclude sensitive notes: Add filenames to `public/.gitignore`
- Keep your source private: Set the repo to private, only publish what you want

### For Digital Garden Users
- Modify `_notes` YAML frontmatter for custom fields:
```yaml
---
title: My Note
date: 2026-03-25
tags: [learning, markdown]
---
```

### For MkDocs Users
- Organize notes in folders and auto-generate nav:
```
docs/
  ├── index.md
  ├── software/
  │   ├── index.md
  │   └── architecture.md
  └── database/
      ├── index.md
      └── sql.md
```

---
