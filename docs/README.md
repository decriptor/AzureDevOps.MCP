# GitHub Pages Documentation

This directory contains the GitHub Pages website for the Azure DevOps MCP Server project.

## Website Structure

- **index.md** - Homepage with project overview and features
- **getting-started.md** - Complete setup and configuration guide  
- **api-reference.md** - Comprehensive API documentation for all tools
- **examples.md** - Practical examples and common workflows
- **assets/css/style.scss** - Custom styling and responsive design
- **_includes/** - Header and footer components
- **_config.yml** - Jekyll configuration

## Local Development

To run the site locally:

```bash
cd docs
bundle install
bundle exec jekyll serve
```

Visit `http://localhost:4000` to preview the site.

## Deployment

The site is automatically deployed to GitHub Pages via GitHub Actions when changes are pushed to the main branch.

**Live Site**: https://decriptor.github.io/AzureDevOps.MCP

## Features

- **Responsive Design** - Works on desktop, tablet, and mobile
- **Modern Styling** - Azure DevOps themed with professional appearance  
- **Fast Loading** - Optimized assets and caching
- **SEO Optimized** - Meta tags, structured data, and sitemap
- **Accessible** - WCAG compliant with proper contrast and navigation

## Technology

- **Jekyll** - Static site generator
- **GitHub Pages** - Hosting platform
- **Tailwind CSS** - Utility-first CSS framework via CDN
- **GitHub Actions** - Automated deployment